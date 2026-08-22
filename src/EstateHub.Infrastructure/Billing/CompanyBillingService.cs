using System.Data;
using System.Data.Common;
using EstateHub.Application.Billing;
using EstateHub.Application.Common;
using EstateHub.Application.CompanyAccess;
using EstateHub.Domain.Entities.Billing;
using EstateHub.Domain.Entities.Bookings;
using EstateHub.Domain.Entities.Companies;
using EstateHub.Domain.Enums;
using EstateHub.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EstateHub.Infrastructure.Billing;

public sealed class CompanyBillingService(EstateHubDbContext dbContext, ICompanyAccessService companyAccessService, TimeProvider timeProvider) : ICompanyBillingService
{
    private const decimal MaximumDecimal18_2 = 9999999999999999.99m;

    public async Task<BillingResult<PagedResult<BillingInvoiceSummary>>> GetInvoicesAsync(Guid applicationUserId, InvoiceDirectoryQuery query, CancellationToken cancellationToken = default)
    {
        var scope = await Scope(applicationUserId, CompanyPermissionCodes.BillingRead, cancellationToken); if (scope is null) return new(BillingOperationStatus.NotFound);
        try
        {
            var invoices = dbContext.Set<BillingInvoice>().AsNoTracking().Where(item => item.CompanyId == scope.CompanyId);
            if (query.Status is not null) invoices = invoices.Where(item => item.Status == query.Status.Value);
            if (query.LineType is not null) invoices = invoices.Where(item => item.Lines.Any(line => line.LineType == query.LineType.Value));
            if (query.CurrencyCode is not null) invoices = invoices.Where(item => item.CurrencyCode == query.CurrencyCode);
            if (query.CreatedFrom is not null) invoices = invoices.Where(item => item.CreatedAt >= query.CreatedFrom.Value);
            if (query.CreatedTo is not null) invoices = invoices.Where(item => item.CreatedAt < query.CreatedTo.Value);
            if (query.Search is not null) invoices = invoices.Where(item => item.InvoiceNumber.Contains(query.Search));
            var total = await invoices.CountAsync(cancellationToken);
            var items = await invoices.OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.Id).Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize)
                .Select(item => new BillingInvoiceSummary(item.Id, item.InvoiceNumber, item.Status, item.CurrencyCode, item.PeriodStart, item.PeriodEnd, item.Subtotal, item.Total, item.IssuedAt, item.DueAt, item.PaidAt, item.CreatedAt, item.Lines.Count, item.PaymentTransactions.Count, item.RowVersion)).ToListAsync(cancellationToken);
            return new(BillingOperationStatus.Succeeded, new PagedResult<BillingInvoiceSummary>(items, query.PageNumber, query.PageSize, total));
        }
        catch (OperationCanceledException) { throw; }
        catch (DbException) { return new(BillingOperationStatus.ServiceUnavailable); }
    }

    public async Task<BillingResult<BillingInvoiceDetails>> GetInvoiceAsync(Guid applicationUserId, Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var scope = await Scope(applicationUserId, CompanyPermissionCodes.BillingRead, cancellationToken); if (scope is null) return new(BillingOperationStatus.NotFound);
        try
        {
            var header = await dbContext.Set<BillingInvoice>().AsNoTracking().Where(item => item.Id == invoiceId && item.CompanyId == scope.CompanyId)
                .Select(item => new BillingInvoiceSummary(item.Id, item.InvoiceNumber, item.Status, item.CurrencyCode, item.PeriodStart, item.PeriodEnd, item.Subtotal, item.Total, item.IssuedAt, item.DueAt, item.PaidAt, item.CreatedAt, item.Lines.Count, item.PaymentTransactions.Count, item.RowVersion)).SingleOrDefaultAsync(cancellationToken);
            if (header is null) return new(BillingOperationStatus.NotFound);
            var lines = await dbContext.Set<BillingInvoiceLine>().AsNoTracking().Where(item => item.BillingInvoiceId == invoiceId).OrderBy(item => item.Id)
                .Select(item => new BillingInvoiceLineItem(item.Id, item.LineType, item.DescriptionSnapshot, item.Quantity, item.UnitAmount, item.LineTotal, item.CompanySubscriptionId, item.BookingChargeId, item.ListingPromotionId,
                    item.CompanySubscription == null ? null : new BillingSubscriptionSource(item.CompanySubscription.Id, item.CompanySubscription.SubscriptionPlanId, item.CompanySubscription.SubscriptionPlan.Name),
                    item.BookingCharge == null ? null : new BillingBookingChargeSource(item.BookingCharge.Id, item.BookingCharge.ViewingBookingId, item.BookingCharge.ViewingBooking.BookingCode),
                    item.ListingPromotion == null ? null : new BillingPromotionSource(item.ListingPromotion.Id, new BillingPromotionListingSummary(item.ListingPromotion.Listing.Id, item.ListingPromotion.Listing.Slug, item.ListingPromotion.Listing.Title))))
                .ToListAsync(cancellationToken);
            if (lines.Any(item => !ValidLine(item))) return new(BillingOperationStatus.ServiceUnavailable);
            var payments = await dbContext.Set<PaymentTransaction>().AsNoTracking().Where(item => item.BillingInvoiceId == invoiceId).OrderBy(item => item.CreatedAt).ThenBy(item => item.Id)
                .Select(item => new BillingPaymentItem(item.Id, item.BillingInvoiceId, header.InvoiceNumber, item.Provider, item.ProviderReference, item.Status, item.Amount, item.CurrencyCode, item.CreatedAt, item.CompletedAt, item.FailureReason)).ToListAsync(cancellationToken);
            if (header.Status == BillingInvoiceStatus.Paid && header.Total > 0 && !payments.Any(item => item.Provider == PaymentProvider.Fake && item.Status == PaymentTransactionStatus.Succeeded && item.Amount == header.Total && item.CurrencyCode == header.CurrencyCode)) return new(BillingOperationStatus.ServiceUnavailable);
            return new(BillingOperationStatus.Succeeded, new BillingInvoiceDetails(header, lines, payments));
        }
        catch (OperationCanceledException) { throw; }
        catch (DbException) { return new(BillingOperationStatus.ServiceUnavailable); }
    }

    public async Task<BillingResult<PagedResult<BookingChargeDirectoryItem>>> GetBookingChargesAsync(Guid applicationUserId, BookingChargeDirectoryQuery query, CancellationToken cancellationToken = default)
    {
        var scope = await Scope(applicationUserId, CompanyPermissionCodes.BillingRead, cancellationToken); if (scope is null) return new(BillingOperationStatus.NotFound);
        try
        {
            var charges = dbContext.Set<BookingCharge>().AsNoTracking().Where(item => item.CompanyId == scope.CompanyId);
            if (query.Status is not null) charges = charges.Where(item => item.Status == query.Status.Value);
            if (query.CurrencyCode is not null) charges = charges.Where(item => item.CurrencyCode == query.CurrencyCode);
            if (query.CreatedFrom is not null) charges = charges.Where(item => item.CreatedAt >= query.CreatedFrom.Value);
            if (query.CreatedTo is not null) charges = charges.Where(item => item.CreatedAt < query.CreatedTo.Value);
            if (query.Search is not null) charges = charges.Where(item => item.ViewingBooking.BookingCode.Contains(query.Search));
            var total = await charges.CountAsync(cancellationToken);
            var items = await charges.OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.Id).Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize)
                .Select(item => new BookingChargeDirectoryItem(item.Id, item.ViewingBookingId, item.ViewingBooking.BookingCode, item.ViewingBooking.ViewingSlot.Listing.Id, item.ViewingBooking.ViewingSlot.Listing.Slug, item.ViewingBooking.ViewingSlot.Listing.Title, item.ViewingBooking.CustomerProfile.FullName, item.AmountSnapshot, item.CurrencyCode, item.Status, item.CreatedAt, item.VoidedAt, item.VoidReason, item.InvoiceLine == null ? null : item.InvoiceLine.BillingInvoiceId, item.InvoiceLine == null ? null : item.InvoiceLine.BillingInvoice.InvoiceNumber)).ToListAsync(cancellationToken);
            return new(BillingOperationStatus.Succeeded, new PagedResult<BookingChargeDirectoryItem>(items, query.PageNumber, query.PageSize, total));
        }
        catch (OperationCanceledException) { throw; }
        catch (DbException) { return new(BillingOperationStatus.ServiceUnavailable); }
    }

    public async Task<BillingResult<PagedResult<BillingPaymentItem>>> GetPaymentTransactionsAsync(Guid applicationUserId, PaymentDirectoryQuery query, CancellationToken cancellationToken = default)
    {
        var scope = await Scope(applicationUserId, CompanyPermissionCodes.BillingRead, cancellationToken); if (scope is null) return new(BillingOperationStatus.NotFound);
        try
        {
            var payments = dbContext.Set<PaymentTransaction>().AsNoTracking().Where(item => item.BillingInvoice.CompanyId == scope.CompanyId);
            if (query.Status is not null) payments = payments.Where(item => item.Status == query.Status.Value);
            if (query.Provider is not null) payments = payments.Where(item => item.Provider == query.Provider.Value);
            if (query.CurrencyCode is not null) payments = payments.Where(item => item.CurrencyCode == query.CurrencyCode);
            if (query.CreatedFrom is not null) payments = payments.Where(item => item.CreatedAt >= query.CreatedFrom.Value);
            if (query.CreatedTo is not null) payments = payments.Where(item => item.CreatedAt < query.CreatedTo.Value);
            var total = await payments.CountAsync(cancellationToken);
            var items = await payments.OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.Id).Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize)
                .Select(item => new BillingPaymentItem(item.Id, item.BillingInvoiceId, item.BillingInvoice.InvoiceNumber, item.Provider, item.ProviderReference, item.Status, item.Amount, item.CurrencyCode, item.CreatedAt, item.CompletedAt, item.FailureReason)).ToListAsync(cancellationToken);
            return new(BillingOperationStatus.Succeeded, new PagedResult<BillingPaymentItem>(items, query.PageNumber, query.PageSize, total));
        }
        catch (OperationCanceledException) { throw; }
        catch (DbException) { return new(BillingOperationStatus.ServiceUnavailable); }
    }

    public async Task<BillingResult<BookingChargePreview>> PreviewBookingChargesAsync(Guid applicationUserId, IReadOnlyCollection<Guid> bookingChargeIds, CancellationToken cancellationToken = default)
    {
        var scope = await Scope(applicationUserId, CompanyPermissionCodes.BillingRead, cancellationToken); if (scope is null) return new(BillingOperationStatus.NotFound);
        try
        {
            var items = await dbContext.Set<BookingCharge>().AsNoTracking().Where(item => item.CompanyId == scope.CompanyId && bookingChargeIds.Contains(item.Id)).OrderBy(item => item.ViewingBooking.BookingCode).ThenBy(item => item.Id)
                .Select(item => new PreviewCandidate(item.Id, item.ViewingBookingId, item.ViewingBooking.BookingCode, item.AmountSnapshot, item.CurrencyCode, item.Status, item.InvoiceLine != null)).ToListAsync(cancellationToken);
            var validation = ValidatePreview(items, bookingChargeIds.Count, null);
            if (validation.Status != BillingOperationStatus.Succeeded) return validation;
            var subtotal = await dbContext.Set<BookingCharge>().AsNoTracking().Where(item => item.CompanyId == scope.CompanyId && bookingChargeIds.Contains(item.Id)).SumAsync(item => item.AmountSnapshot, cancellationToken);
            return ValidatePreview(items, bookingChargeIds.Count, subtotal);
        }
        catch (OperationCanceledException) { throw; }
        catch (DbException) { return new(BillingOperationStatus.ServiceUnavailable); }
    }

    public async Task<BillingResult<BillingInvoiceDetails>> CheckoutBookingChargesAsync(Guid applicationUserId, IReadOnlyCollection<Guid> bookingChargeIds, CancellationToken cancellationToken = default)
    {
        var scope = await Scope(applicationUserId, CompanyPermissionCodes.BillingManage, cancellationToken); if (scope is null) return new(BillingOperationStatus.NotFound);
        var now = timeProvider.GetUtcNow();
        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var locked = await dbContext.Set<Company>().Where(item => item.Id == scope.CompanyId).ExecuteUpdateAsync(setters => setters.SetProperty(item => item.UpdatedAt, now), cancellationToken);
            if (locked != 1) return new(BillingOperationStatus.NotFound);
            var charges = await dbContext.Set<BookingCharge>().Where(item => item.CompanyId == scope.CompanyId && bookingChargeIds.Contains(item.Id)).ToListAsync(cancellationToken);
            if (charges.Count != bookingChargeIds.Count) return new(BillingOperationStatus.NotFound);
            if (charges.Any(item => item.Status != BookingChargeStatus.Billable)) return new(BillingOperationStatus.Conflict);
            if (charges.Any(item => item.AmountSnapshot < 0)) return new(BillingOperationStatus.ServiceUnavailable);
            var currencyCodes = charges.Select(item => item.CurrencyCode).Distinct(StringComparer.Ordinal).ToList();
            if (currencyCodes.Count != 1) return new(BillingOperationStatus.Conflict);
            var alreadyInvoiced = await dbContext.Set<BillingInvoiceLine>().AsNoTracking().AnyAsync(item => item.BookingChargeId != null && bookingChargeIds.Contains(item.BookingChargeId.Value), cancellationToken);
            if (alreadyInvoiced) return new(BillingOperationStatus.Conflict);
            var selections = await dbContext.Set<BookingCharge>().AsNoTracking().Where(item => item.CompanyId == scope.CompanyId && bookingChargeIds.Contains(item.Id)).OrderBy(item => item.ViewingBooking.BookingCode).ThenBy(item => item.Id)
                .Select(item => new BookingChargeSelection(item.Id, item.ViewingBookingId, item.ViewingBooking.BookingCode, item.AmountSnapshot, item.CurrencyCode)).ToListAsync(cancellationToken);
            if (selections.Count != bookingChargeIds.Count) return new(BillingOperationStatus.ServiceUnavailable);
            var total = await dbContext.Set<BookingCharge>().AsNoTracking().Where(item => item.CompanyId == scope.CompanyId && bookingChargeIds.Contains(item.Id)).SumAsync(item => item.AmountSnapshot, cancellationToken);
            if (total < 0 || total > MaximumDecimal18_2) return new(BillingOperationStatus.ServiceUnavailable);
            var currencyCode = currencyCodes[0];
            var invoice = new BillingInvoice { Id = Guid.NewGuid(), CompanyId = scope.CompanyId, InvoiceNumber = $"BKG-{now:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}", CurrencyCode = currencyCode, Status = BillingInvoiceStatus.Paid, PeriodStart = null, PeriodEnd = null, Subtotal = total, Total = total, IssuedAt = now, DueAt = now, PaidAt = now, CreatedAt = now };
            var codeByCharge = selections.ToDictionary(item => item.Id, item => item.BookingCode);
            var lines = charges.Select(charge => new BillingInvoiceLine { Id = Guid.NewGuid(), BillingInvoiceId = invoice.Id, LineType = BillingLineType.BookingCharge, CompanySubscriptionId = null, BookingChargeId = charge.Id, ListingPromotionId = null, DescriptionSnapshot = $"Booking charge: {codeByCharge[charge.Id]}", Quantity = 1m, UnitAmount = charge.AmountSnapshot, LineTotal = charge.AmountSnapshot }).ToList();
            foreach (var charge in charges) charge.Status = BookingChargeStatus.Invoiced;
            dbContext.Add(invoice); dbContext.AddRange(lines);
            PaymentTransaction? payment = null;
            if (total > 0) { payment = new PaymentTransaction { Id = Guid.NewGuid(), BillingInvoiceId = invoice.Id, Amount = total, CurrencyCode = currencyCode, Provider = PaymentProvider.Fake, ProviderReference = $"FAKE-BKG-{Guid.NewGuid():N}", Status = PaymentTransactionStatus.Succeeded, CreatedAt = now, CompletedAt = now, FailureReason = null }; dbContext.Add(payment); }
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            var summary = new BillingInvoiceSummary(invoice.Id, invoice.InvoiceNumber, invoice.Status, invoice.CurrencyCode, null, null, invoice.Subtotal, invoice.Total, invoice.IssuedAt, invoice.DueAt, invoice.PaidAt, invoice.CreatedAt, lines.Count, payment is null ? 0 : 1, invoice.RowVersion);
            var lineItems = lines.OrderBy(item => item.Id).Select(line => { var selection = selections.Single(item => item.Id == line.BookingChargeId); return new BillingInvoiceLineItem(line.Id, line.LineType, line.DescriptionSnapshot, line.Quantity, line.UnitAmount, line.LineTotal, null, line.BookingChargeId, null, null, new BillingBookingChargeSource(selection.Id, selection.ViewingBookingId, selection.BookingCode), null); }).ToList();
            var paymentItems = payment is null ? Array.Empty<BillingPaymentItem>() : new[] { new BillingPaymentItem(payment.Id, invoice.Id, invoice.InvoiceNumber, payment.Provider, payment.ProviderReference, payment.Status, payment.Amount, payment.CurrencyCode, payment.CreatedAt, payment.CompletedAt, payment.FailureReason) };
            return new(BillingOperationStatus.Succeeded, new BillingInvoiceDetails(summary, lineItems, paymentItems));
        }
        catch (OperationCanceledException) { throw; }
        catch (DbUpdateConcurrencyException) { return new(BillingOperationStatus.Conflict); }
        catch (DbUpdateException exception) when (Unique(exception)) { return new(BillingOperationStatus.Conflict); }
        catch (DbUpdateException) { return new(BillingOperationStatus.ServiceUnavailable); }
        catch (DbException exception) when (exception is SqlException { Number: 2601 or 2627 }) { return new(BillingOperationStatus.Conflict); }
        catch (DbException) { return new(BillingOperationStatus.ServiceUnavailable); }
    }

    private Task<CompanyAccessScope?> Scope(Guid userId, string permission, CancellationToken cancellationToken) => companyAccessService.GetAuthorizedScopeAsync(userId, permission, cancellationToken);
    private static bool ValidLine(BillingInvoiceLineItem item) => item.LineType switch
    {
        BillingLineType.Subscription => item.CompanySubscriptionId is not null && item.BookingChargeId is null && item.ListingPromotionId is null && item.Subscription is not null && item.BookingCharge is null && item.Promotion is null,
        BillingLineType.BookingCharge => item.CompanySubscriptionId is null && item.BookingChargeId is not null && item.ListingPromotionId is null && item.Subscription is null && item.BookingCharge is not null && item.Promotion is null,
        BillingLineType.Promotion => item.CompanySubscriptionId is null && item.BookingChargeId is null && item.ListingPromotionId is not null && item.Subscription is null && item.BookingCharge is null && item.Promotion is not null,
        _ => false
    };
    private static BillingResult<BookingChargePreview> ValidatePreview(IReadOnlyList<PreviewCandidate> items, int requestedCount, decimal? sqlSubtotal)
    {
        if (items.Count != requestedCount) return new(BillingOperationStatus.NotFound);
        if (items.Any(item => item.Status != BookingChargeStatus.Billable || item.HasInvoiceLine)) return new(BillingOperationStatus.Conflict);
        if (items.Any(item => item.AmountSnapshot < 0)) return new(BillingOperationStatus.ServiceUnavailable);
        var currencies = items.Select(item => item.CurrencyCode).Distinct(StringComparer.Ordinal).ToList();
        if (currencies.Count != 1) return new(BillingOperationStatus.Conflict);
        var selections = items.Select(item => new BookingChargeSelection(item.Id, item.ViewingBookingId, item.BookingCode, item.AmountSnapshot, item.CurrencyCode)).ToList();
        var total = sqlSubtotal ?? selections.Sum(item => item.AmountSnapshot);
        return new(BillingOperationStatus.Succeeded, new BookingChargePreview(currencies[0], selections.Count, total, total, selections));
    }
    private static bool Unique(DbUpdateException exception) => exception.GetBaseException() is SqlException { Number: 2601 or 2627 };
    private sealed record PreviewCandidate(Guid Id, Guid ViewingBookingId, string BookingCode, decimal AmountSnapshot, string CurrencyCode, BookingChargeStatus Status, bool HasInvoiceLine);
}
