using System.Data;
using System.Data.Common;
using EstateHub.Application.Common;
using EstateHub.Application.CompanyAccess;
using EstateHub.Application.Subscriptions;
using EstateHub.Domain.Entities.Billing;
using EstateHub.Domain.Entities.Companies;
using EstateHub.Domain.Enums;
using EstateHub.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EstateHub.Infrastructure.Subscriptions;

public sealed class SubscriptionService(EstateHubDbContext dbContext, ICompanyAccessService access, TimeProvider timeProvider) : ISubscriptionService
{
    public async Task<IReadOnlyList<PublicSubscriptionPlan>> GetPublicPlansAsync(CancellationToken cancellationToken = default) =>
        await PlanQuery().OrderBy(x => x.MonthlyPrice).ThenBy(x => x.Name).ThenBy(x => x.Id).ToListAsync(cancellationToken);

    public Task<PublicSubscriptionPlan?> GetPublicPlanAsync(Guid planId, CancellationToken cancellationToken = default) =>
        PlanQuery().SingleOrDefaultAsync(x => x.Id == planId, cancellationToken);

    public async Task<SubscriptionResult<CompanySubscriptionDetails>> GetCurrentAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var scope = await Scope(userId, CompanyPermissionCodes.BillingRead, cancellationToken); if (scope is null) return new(SubscriptionOperationStatus.NotFound);
        try
        {
            var now = timeProvider.GetUtcNow();
            var rows = await SubscriptionQuery(scope.CompanyId, now).Where(x => x.EndedAt == null).Take(2).ToListAsync(cancellationToken);
            return rows.Count switch { 0 => new(SubscriptionOperationStatus.NotFound), 1 => new(SubscriptionOperationStatus.Succeeded, rows[0]), _ => new(SubscriptionOperationStatus.ServiceUnavailable) };
        }
        catch (OperationCanceledException) { throw; } catch (DbException) { return new(SubscriptionOperationStatus.ServiceUnavailable); }
    }

    public async Task<SubscriptionResult<PagedResult<CompanySubscriptionDetails>>> GetHistoryAsync(Guid userId, CompanySubscriptionHistoryQuery query, CancellationToken cancellationToken = default)
    {
        var scope = await Scope(userId, CompanyPermissionCodes.BillingRead, cancellationToken); if (scope is null) return new(SubscriptionOperationStatus.NotFound);
        try
        {
            var now = timeProvider.GetUtcNow();
            var subscriptions = SubscriptionQuery(scope.CompanyId, now); if (query.Status is not null) subscriptions = subscriptions.Where(x => x.Status == query.Status);
            var count = await subscriptions.CountAsync(cancellationToken); var items = await subscriptions.OrderByDescending(x => x.StartedAt).ThenByDescending(x => x.Id).Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);
            return new(SubscriptionOperationStatus.Succeeded, new PagedResult<CompanySubscriptionDetails>(items, query.PageNumber, query.PageSize, count));
        }
        catch (OperationCanceledException) { throw; } catch (DbException) { return new(SubscriptionOperationStatus.ServiceUnavailable); }
    }

    public async Task<SubscriptionResult<CheckoutResult>> CheckoutAsync(Guid userId, CheckoutSubscriptionCommand command, CancellationToken cancellationToken = default)
    {
        var scope = await Scope(userId, CompanyPermissionCodes.BillingManage, cancellationToken); if (scope is null) return new(SubscriptionOperationStatus.NotFound);
        var now = timeProvider.GetUtcNow();
        try
        {
            await using var tx = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var locked = await dbContext.Set<Company>().Where(x => x.Id == scope.CompanyId).ExecuteUpdateAsync(s => s.SetProperty(x => x.UpdatedAt, now), cancellationToken);
            if (locked != 1) return new(SubscriptionOperationStatus.NotFound);
            var plan = await dbContext.Set<SubscriptionPlan>().AsNoTracking().Where(x => x.Id == command.SubscriptionPlanId && x.IsActive).Select(x => new PlanSnapshot(x.Id, x.Name, x.MonthlyPrice, x.CurrencyCode, x.MaxPublishedListings, x.BookingFeeAmount)).SingleOrDefaultAsync(cancellationToken);
            if (plan is null) return new(SubscriptionOperationStatus.NotFound);
            if (plan.MonthlyPrice < 0 || plan.BookingFeeAmount < 0 || plan.MaxPublishedListings < 0 || plan.Name.Length > 486) return new(SubscriptionOperationStatus.ServiceUnavailable);
            var current = await dbContext.Set<CompanySubscription>().Where(x => x.CompanyId == scope.CompanyId && x.EndedAt == null).Take(2).ToListAsync(cancellationToken);
            if (current.Count > 1) return new(SubscriptionOperationStatus.ServiceUnavailable);
            if (current.Count == 1)
            {
                var old = current[0];
                var blocks = old.Status == CompanySubscriptionStatus.Active && (old.CurrentPeriodStart > now || old.CurrentPeriodEnd > now);
                if (blocks) return new(SubscriptionOperationStatus.Conflict);
                if (old.CurrentPeriodEnd > now && old.Status is not (CompanySubscriptionStatus.Expired or CompanySubscriptionStatus.Cancelled)) return new(SubscriptionOperationStatus.Conflict);
                old.Status = old.CancellationRequestedAt is null ? CompanySubscriptionStatus.Expired : CompanySubscriptionStatus.Cancelled; old.EndedAt = now; old.UpdatedAt = now;
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            var subscription = new CompanySubscription { Id = Guid.NewGuid(), CompanyId = scope.CompanyId, SubscriptionPlanId = plan.Id, Status = CompanySubscriptionStatus.Active, CurrentPeriodStart = now, CurrentPeriodEnd = now.AddMonths(1), MonthlyPriceSnapshot = plan.MonthlyPrice, CurrencyCode = plan.CurrencyCode, MaxPublishedListingsSnapshot = plan.MaxPublishedListings, BookingFeeSnapshot = plan.BookingFeeAmount, StartedAt = now, CancellationRequestedAt = null, EndedAt = null, CreatedAt = now, UpdatedAt = now };
            var invoice = new BillingInvoice { Id = Guid.NewGuid(), CompanyId = scope.CompanyId, InvoiceNumber = $"SUB-{now:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}", CurrencyCode = plan.CurrencyCode, Status = BillingInvoiceStatus.Paid, PeriodStart = subscription.CurrentPeriodStart, PeriodEnd = subscription.CurrentPeriodEnd, Subtotal = plan.MonthlyPrice, Total = plan.MonthlyPrice, IssuedAt = now, DueAt = now, PaidAt = now, CreatedAt = now };
            dbContext.Add(subscription); dbContext.Add(invoice); dbContext.Add(new BillingInvoiceLine { Id = Guid.NewGuid(), BillingInvoiceId = invoice.Id, LineType = BillingLineType.Subscription, CompanySubscriptionId = subscription.Id, BookingChargeId = null, ListingPromotionId = null, DescriptionSnapshot = "Subscription: " + plan.Name, Quantity = 1m, UnitAmount = plan.MonthlyPrice, LineTotal = plan.MonthlyPrice });
            PaymentTransaction? payment = null;
            if (plan.MonthlyPrice > 0) { payment = new PaymentTransaction { Id = Guid.NewGuid(), BillingInvoiceId = invoice.Id, Amount = plan.MonthlyPrice, CurrencyCode = plan.CurrencyCode, Provider = PaymentProvider.Fake, ProviderReference = $"FAKE-SUB-{Guid.NewGuid():N}", Status = PaymentTransactionStatus.Succeeded, CreatedAt = now, CompletedAt = now, FailureReason = null }; dbContext.Add(payment); }
            await dbContext.SaveChangesAsync(cancellationToken);
            var result = new CheckoutResult(Map(subscription, plan.Name, now), new CheckoutInvoice(invoice.Id, invoice.InvoiceNumber, invoice.Status, invoice.CurrencyCode, invoice.Subtotal, invoice.Total, invoice.PeriodStart!.Value, invoice.PeriodEnd!.Value, invoice.IssuedAt!.Value, invoice.PaidAt!.Value), payment is null ? null : new CheckoutPayment(payment.Id, payment.Amount, payment.CurrencyCode, payment.Provider, payment.ProviderReference, payment.Status, payment.CreatedAt, payment.CompletedAt));
            await tx.CommitAsync(cancellationToken); return new(SubscriptionOperationStatus.Succeeded, result);
        }
        catch (OperationCanceledException) { throw; }
        catch (DbUpdateConcurrencyException) { return new(SubscriptionOperationStatus.Conflict); }
        catch (DbUpdateException e) when (Unique(e)) { return new(SubscriptionOperationStatus.Conflict); }
        catch (DbUpdateException) { return new(SubscriptionOperationStatus.ServiceUnavailable); }
        catch (DbException e) when (e is SqlException { Number: 2601 or 2627 }) { return new(SubscriptionOperationStatus.Conflict); }
        catch (DbException) { return new(SubscriptionOperationStatus.ServiceUnavailable); }
    }

    public Task<SubscriptionOperationStatus> CancelAsync(Guid userId, byte[] rowVersion, CancellationToken token = default) => ChangeCancellationAsync(userId, rowVersion, true, token);
    public Task<SubscriptionOperationStatus> ResumeAsync(Guid userId, byte[] rowVersion, CancellationToken token = default) => ChangeCancellationAsync(userId, rowVersion, false, token);

    private async Task<SubscriptionOperationStatus> ChangeCancellationAsync(Guid userId, byte[] rowVersion, bool cancel, CancellationToken token)
    {
        var scope = await Scope(userId, CompanyPermissionCodes.BillingManage, token); if (scope is null) return SubscriptionOperationStatus.NotFound; var now = timeProvider.GetUtcNow();
        try
        {
            await using var tx = await dbContext.Database.BeginTransactionAsync(token); var rows = await dbContext.Set<CompanySubscription>().Where(x => x.CompanyId == scope.CompanyId && x.EndedAt == null).Take(2).ToListAsync(token); if (rows.Count == 0) return SubscriptionOperationStatus.NotFound; if (rows.Count > 1) return SubscriptionOperationStatus.ServiceUnavailable; var s = rows[0];
            if (s.Status != CompanySubscriptionStatus.Active || s.CurrentPeriodStart > now || s.CurrentPeriodEnd <= now) return SubscriptionOperationStatus.Conflict;
            if ((cancel && s.CancellationRequestedAt is not null) || (!cancel && s.CancellationRequestedAt is null)) return SubscriptionOperationStatus.Succeeded;
            if (!s.RowVersion.AsSpan().SequenceEqual(rowVersion)) return SubscriptionOperationStatus.Conflict;
            dbContext.Entry(s).Property(x => x.RowVersion).OriginalValue = rowVersion; s.CancellationRequestedAt = cancel ? now : null; s.UpdatedAt = now; await dbContext.SaveChangesAsync(token); await tx.CommitAsync(token); return SubscriptionOperationStatus.Succeeded;
        }
        catch (OperationCanceledException) { throw; } catch (DbUpdateConcurrencyException) { return SubscriptionOperationStatus.Conflict; } catch (DbUpdateException) { return SubscriptionOperationStatus.ServiceUnavailable; } catch (DbException) { return SubscriptionOperationStatus.ServiceUnavailable; }
    }

    private IQueryable<PublicSubscriptionPlan> PlanQuery() => dbContext.Set<SubscriptionPlan>().AsNoTracking().Where(x => x.IsActive).Select(x => new PublicSubscriptionPlan(x.Id, x.Name, x.MonthlyPrice, x.MaxPublishedListings, x.BookingFeeAmount, new SubscriptionCurrency(x.Currency.Code, x.Currency.Name, x.Currency.Symbol, x.Currency.DecimalPlaces)));
    private IQueryable<CompanySubscriptionDetails> SubscriptionQuery(Guid companyId, DateTimeOffset now) => dbContext.Set<CompanySubscription>().AsNoTracking().Where(x => x.CompanyId == companyId).Select(x => new CompanySubscriptionDetails(x.Id, x.SubscriptionPlanId, x.SubscriptionPlan.Name, x.Status, x.CurrentPeriodStart, x.CurrentPeriodEnd, x.MonthlyPriceSnapshot, x.CurrencyCode, x.MaxPublishedListingsSnapshot, x.BookingFeeSnapshot, x.StartedAt, x.CancellationRequestedAt, x.EndedAt, x.CreatedAt, x.UpdatedAt, x.RowVersion, x.Status == CompanySubscriptionStatus.Active && x.EndedAt == null && x.CurrentPeriodStart <= now && x.CurrentPeriodEnd > now, x.CancellationRequestedAt != null && x.EndedAt == null));
    private Task<CompanyAccessScope?> Scope(Guid userId, string permission, CancellationToken token) => access.GetAuthorizedScopeAsync(userId, permission, token);
    private static CompanySubscriptionDetails Map(CompanySubscription x, string planName, DateTimeOffset now) => new(x.Id, x.SubscriptionPlanId, planName, x.Status, x.CurrentPeriodStart, x.CurrentPeriodEnd, x.MonthlyPriceSnapshot, x.CurrencyCode, x.MaxPublishedListingsSnapshot, x.BookingFeeSnapshot, x.StartedAt, x.CancellationRequestedAt, x.EndedAt, x.CreatedAt, x.UpdatedAt, x.RowVersion, x.Status == CompanySubscriptionStatus.Active && x.EndedAt == null && x.CurrentPeriodStart <= now && x.CurrentPeriodEnd > now, x.CancellationRequestedAt != null && x.EndedAt == null);
    private static bool Unique(DbUpdateException x) => x.GetBaseException() is SqlException { Number: 2601 or 2627 };
    private sealed record PlanSnapshot(Guid Id, string Name, decimal MonthlyPrice, string CurrencyCode, int? MaxPublishedListings, decimal BookingFeeAmount);
}
