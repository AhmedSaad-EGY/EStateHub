using System.Data;
using System.Data.Common;
using EstateHub.Application.Common;
using EstateHub.Application.CompanyAccess;
using EstateHub.Application.Listings;
using EstateHub.Application.Promotions;
using EstateHub.Domain.Entities.Billing;
using EstateHub.Domain.Entities.Catalog;
using EstateHub.Domain.Enums;
using EstateHub.Infrastructure.Listings;
using EstateHub.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EstateHub.Infrastructure.Promotions;

public sealed class PromotionService(EstateHubDbContext dbContext, ICompanyAccessService companyAccessService, TimeProvider timeProvider) : IPromotionService
{
    public async Task<IReadOnlyList<PublicPromotionPackage>> GetPublicPackagesAsync(CancellationToken cancellationToken = default) =>
        await PackageQuery().OrderBy(item => item.Price).ThenBy(item => item.Name).ThenBy(item => item.Id).ToListAsync(cancellationToken);

    public Task<PublicPromotionPackage?> GetPublicPackageAsync(Guid packageId, CancellationToken cancellationToken = default) =>
        PackageQuery().SingleOrDefaultAsync(item => item.Id == packageId, cancellationToken);

    public async Task<PagedResult<PromotedListingItem>> GetPromotedListingsAsync(PromotedListingQuery query, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var publicListings = dbContext.Set<Listing>().AsNoTracking().WherePublic(now);
        var promotions = dbContext.Set<ListingPromotion>().AsNoTracking()
            .Where(item => item.PlacementSnapshot == query.Placement
                && (item.Status == ListingPromotionStatus.Scheduled || item.Status == ListingPromotionStatus.Active)
                && item.StartsAt <= now && item.EndsAt > now
                && publicListings.Any(listing => listing.Id == item.ListingId));
        var total = await promotions.CountAsync(cancellationToken);
        var items = await promotions.OrderByDescending(item => item.StartsAt).ThenByDescending(item => item.Id)
            .Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize)
            .Select(item => new PromotedListingItem(item.PlacementSnapshot, item.StartsAt, item.EndsAt,
                new ListingDirectoryItem(item.Listing.Id, item.Listing.Slug, item.Listing.Title, item.Listing.ListingType, item.Listing.AskingPrice, item.Listing.RentPeriod, item.Listing.PublishedAt,
                    item.Listing.Media.Where(media => media.IsCover).Select(media => (Guid?)media.FileAssetId).FirstOrDefault(), item.Listing.PaymentPlans.Any(plan => plan.IsActive),
                    new ListingCurrencySummary(item.Listing.Currency.Code, item.Listing.Currency.Name, item.Listing.Currency.Symbol, item.Listing.Currency.DecimalPlaces),
                    new ListingCompanySummary(item.Listing.Company.Id, item.Listing.Company.Slug, item.Listing.Company.DisplayName, item.Listing.Company.LogoFileAssetId),
                    new ListingUnitSummary(item.Listing.Unit.Id, item.Listing.Unit.Bedrooms, item.Listing.Unit.Bathrooms, item.Listing.Unit.BuiltUpArea, item.Listing.Unit.LandArea, item.Listing.Unit.FinishingType, item.Listing.Unit.FurnishedStatus, item.Listing.Unit.Status,
                        new ListingUnitTypeSummary(item.Listing.Unit.UnitType.Id, item.Listing.Unit.UnitType.Code, item.Listing.Unit.UnitType.NameEn, item.Listing.Unit.UnitType.NameAr),
                        new ListingLocationSummary(item.Listing.Unit.Location.Id, item.Listing.Unit.Location.Type, item.Listing.Unit.Location.NameEn, item.Listing.Unit.Location.NameAr, item.Listing.Unit.Location.Slug),
                        item.Listing.Unit.Project == null ? null : new ListingProjectSummary(item.Listing.Unit.Project.Id, item.Listing.Unit.Project.Slug, item.Listing.Unit.Project.Name, item.Listing.Unit.Project.DeveloperCompany.Slug)))))
            .ToListAsync(cancellationToken);
        return new PagedResult<PromotedListingItem>(items, query.PageNumber, query.PageSize, total);
    }

    public async Task<PromotionResult<PagedResult<CompanyListingPromotionDetails>>> GetCompanyPromotionsAsync(Guid applicationUserId, CompanyListingPromotionDirectoryQuery query, CancellationToken cancellationToken = default)
    {
        var scope = await Scope(applicationUserId, CompanyPermissionCodes.BillingRead, cancellationToken); if (scope is null) return new(PromotionOperationStatus.NotFound);
        try
        {
            var now = timeProvider.GetUtcNow();
            var promotions = CompanyPromotionQuery(scope.CompanyId);
            if (query.ListingId is not null) promotions = promotions.Where(item => item.Listing.Id == query.ListingId.Value);
            if (query.Placement is not null) promotions = promotions.Where(item => item.PlacementSnapshot == query.Placement);
            if (query.From is not null) promotions = promotions.Where(item => item.StartsAt >= query.From.Value);
            if (query.To is not null) promotions = promotions.Where(item => item.StartsAt < query.To.Value);
            if (query.Status is not null) promotions = promotions.Where(item => (item.Status == ListingPromotionStatus.Cancelled ? ListingPromotionStatus.Cancelled : item.Status == ListingPromotionStatus.Completed ? ListingPromotionStatus.Completed : item.EndsAt <= now ? ListingPromotionStatus.Completed : item.StartsAt > now ? ListingPromotionStatus.Scheduled : ListingPromotionStatus.Active) == query.Status.Value);
            var total = await promotions.CountAsync(cancellationToken);
            var items = await promotions.OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.Id)
                .Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize)
                .Select(item => new CompanyListingPromotionDetails(item.Id, new CompanyPromotionListingSummary(item.Listing.Id, item.Listing.ListingCode, item.Listing.Slug, item.Listing.Title, item.Listing.ListingType), item.PromotionPackageId, item.PromotionPackage.Name, item.AmountSnapshot, item.CurrencyCode, item.PlacementSnapshot, item.StartsAt, item.EndsAt,
                    item.Status == ListingPromotionStatus.Cancelled ? ListingPromotionStatus.Cancelled : item.Status == ListingPromotionStatus.Completed ? ListingPromotionStatus.Completed : item.EndsAt <= now ? ListingPromotionStatus.Completed : item.StartsAt > now ? ListingPromotionStatus.Scheduled : ListingPromotionStatus.Active,
                    item.Status != ListingPromotionStatus.Cancelled && item.Status != ListingPromotionStatus.Completed && item.EndsAt > now && item.StartsAt <= now, item.CreatedAt, item.RowVersion,
                    item.InvoiceLine == null ? null : new CompanyPromotionInvoiceSummary(item.InvoiceLine.BillingInvoice.Id, item.InvoiceLine.BillingInvoice.InvoiceNumber, item.InvoiceLine.BillingInvoice.Status, item.InvoiceLine.BillingInvoice.Total, item.InvoiceLine.BillingInvoice.PaidAt)))
                .ToListAsync(cancellationToken);
            return items.Any(item => item.Invoice is null)
                ? new(PromotionOperationStatus.ServiceUnavailable)
                : new(PromotionOperationStatus.Succeeded, new PagedResult<CompanyListingPromotionDetails>(items, query.PageNumber, query.PageSize, total));
        }
        catch (OperationCanceledException) { throw; }
        catch (DbException) { return new(PromotionOperationStatus.ServiceUnavailable); }
    }

    public async Task<PromotionResult<CompanyListingPromotionDetails>> GetCompanyPromotionAsync(Guid applicationUserId, Guid promotionId, CancellationToken cancellationToken = default)
    {
        var scope = await Scope(applicationUserId, CompanyPermissionCodes.BillingRead, cancellationToken); if (scope is null) return new(PromotionOperationStatus.NotFound);
        try
        {
            var now = timeProvider.GetUtcNow();
            var item = await CompanyPromotionQuery(scope.CompanyId).Where(item => item.Id == promotionId).Select(item => new CompanyListingPromotionDetails(item.Id, new CompanyPromotionListingSummary(item.Listing.Id, item.Listing.ListingCode, item.Listing.Slug, item.Listing.Title, item.Listing.ListingType), item.PromotionPackageId, item.PromotionPackage.Name, item.AmountSnapshot, item.CurrencyCode, item.PlacementSnapshot, item.StartsAt, item.EndsAt,
                item.Status == ListingPromotionStatus.Cancelled ? ListingPromotionStatus.Cancelled : item.Status == ListingPromotionStatus.Completed ? ListingPromotionStatus.Completed : item.EndsAt <= now ? ListingPromotionStatus.Completed : item.StartsAt > now ? ListingPromotionStatus.Scheduled : ListingPromotionStatus.Active,
                item.Status != ListingPromotionStatus.Cancelled && item.Status != ListingPromotionStatus.Completed && item.EndsAt > now && item.StartsAt <= now, item.CreatedAt, item.RowVersion,
                item.InvoiceLine == null ? null : new CompanyPromotionInvoiceSummary(item.InvoiceLine.BillingInvoice.Id, item.InvoiceLine.BillingInvoice.InvoiceNumber, item.InvoiceLine.BillingInvoice.Status, item.InvoiceLine.BillingInvoice.Total, item.InvoiceLine.BillingInvoice.PaidAt))).SingleOrDefaultAsync(cancellationToken);
            return item switch { null => new(PromotionOperationStatus.NotFound), { Invoice: null } => new(PromotionOperationStatus.ServiceUnavailable), _ => new(PromotionOperationStatus.Succeeded, item) };
        }
        catch (OperationCanceledException) { throw; }
        catch (DbException) { return new(PromotionOperationStatus.ServiceUnavailable); }
    }

    public async Task<PromotionResult<PromotionCheckoutResult>> CheckoutAsync(Guid applicationUserId, CheckoutPromotionCommand command, CancellationToken cancellationToken = default)
    {
        var scope = await Scope(applicationUserId, CompanyPermissionCodes.BillingManage, cancellationToken); if (scope is null) return new(PromotionOperationStatus.NotFound);
        var now = timeProvider.GetUtcNow();
        if (command.StartsAt is not null && command.StartsAt.Value < now) return new(PromotionOperationStatus.InvalidRequest);
        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var locked = await dbContext.Set<Listing>().Where(item => item.Id == command.ListingId && item.CompanyId == scope.CompanyId).ExecuteUpdateAsync(setters => setters.SetProperty(item => item.UpdatedAt, now), cancellationToken);
            if (locked != 1) return new(PromotionOperationStatus.NotFound);
            var listing = await dbContext.Set<Listing>().AsNoTracking().Where(item => item.Id == command.ListingId && item.CompanyId == scope.CompanyId).WherePublic(now).Select(item => new CompanyPromotionListingSummary(item.Id, item.ListingCode, item.Slug, item.Title, item.ListingType)).SingleOrDefaultAsync(cancellationToken);
            if (listing is null) return new(PromotionOperationStatus.Conflict);
            var package = await dbContext.Set<PromotionPackage>().AsNoTracking().Where(item => item.Id == command.PromotionPackageId && item.IsActive)
                .Select(item => new PackageSnapshot(item.Id, item.Name, item.Placement, item.DurationDays, item.Price, item.CurrencyCode)).SingleOrDefaultAsync(cancellationToken);
            if (package is null) return new(PromotionOperationStatus.NotFound);
            var currencyExists = await dbContext.Set<Currency>().AsNoTracking().AnyAsync(item => item.Code == package.CurrencyCode, cancellationToken);
            if (package.DurationDays <= 0 || package.Price < 0 || !currencyExists || package.Name.Length + package.Placement.Length > 486) return new(PromotionOperationStatus.ServiceUnavailable);
            var startsAt = command.StartsAt ?? now;
            DateTimeOffset endsAt;
            try { endsAt = startsAt.AddDays(package.DurationDays); }
            catch (ArgumentOutOfRangeException) { return new(PromotionOperationStatus.ServiceUnavailable); }
            var overlap = await dbContext.Set<ListingPromotion>().AsNoTracking().AnyAsync(item => item.ListingId == command.ListingId && item.PlacementSnapshot == package.Placement && (item.Status == ListingPromotionStatus.Scheduled || item.Status == ListingPromotionStatus.Active) && item.StartsAt < endsAt && startsAt < item.EndsAt, cancellationToken);
            if (overlap) return new(PromotionOperationStatus.Conflict);
            var promotion = new ListingPromotion { Id = Guid.NewGuid(), ListingId = command.ListingId, CompanyId = scope.CompanyId, PromotionPackageId = package.Id, AmountSnapshot = package.Price, CurrencyCode = package.CurrencyCode, PlacementSnapshot = package.Placement, StartsAt = startsAt, EndsAt = endsAt, Status = startsAt > now ? ListingPromotionStatus.Scheduled : ListingPromotionStatus.Active, CreatedAt = now };
            var invoice = new BillingInvoice { Id = Guid.NewGuid(), CompanyId = scope.CompanyId, InvoiceNumber = $"PRO-{now:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}", CurrencyCode = package.CurrencyCode, Status = BillingInvoiceStatus.Paid, PeriodStart = startsAt, PeriodEnd = endsAt, Subtotal = package.Price, Total = package.Price, IssuedAt = now, DueAt = now, PaidAt = now, CreatedAt = now };
            dbContext.Add(promotion); dbContext.Add(invoice); dbContext.Add(new BillingInvoiceLine { Id = Guid.NewGuid(), BillingInvoiceId = invoice.Id, LineType = BillingLineType.Promotion, CompanySubscriptionId = null, BookingChargeId = null, ListingPromotionId = promotion.Id, DescriptionSnapshot = $"Promotion: {package.Name} ({package.Placement})", Quantity = 1m, UnitAmount = package.Price, LineTotal = package.Price });
            PaymentTransaction? payment = null;
            if (package.Price > 0) { payment = new PaymentTransaction { Id = Guid.NewGuid(), BillingInvoiceId = invoice.Id, Amount = package.Price, CurrencyCode = package.CurrencyCode, Provider = PaymentProvider.Fake, ProviderReference = $"FAKE-PRO-{Guid.NewGuid():N}", Status = PaymentTransactionStatus.Succeeded, CreatedAt = now, CompletedAt = now, FailureReason = null }; dbContext.Add(payment); }
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            var details = new CompanyListingPromotionDetails(promotion.Id, listing, promotion.PromotionPackageId, package.Name, promotion.AmountSnapshot, promotion.CurrencyCode, promotion.PlacementSnapshot, promotion.StartsAt, promotion.EndsAt, EffectiveStatus(promotion, now), promotion.Status == ListingPromotionStatus.Active && promotion.StartsAt <= now && promotion.EndsAt > now, promotion.CreatedAt, promotion.RowVersion, new CompanyPromotionInvoiceSummary(invoice.Id, invoice.InvoiceNumber, invoice.Status, invoice.Total, invoice.PaidAt));
            return new(PromotionOperationStatus.Succeeded, new PromotionCheckoutResult(details, details.Invoice!, payment is null ? null : new PromotionCheckoutPayment(payment.Id, payment.Amount, payment.CurrencyCode, payment.Provider, payment.ProviderReference, payment.Status, payment.CreatedAt, payment.CompletedAt)));
        }
        catch (OperationCanceledException) { throw; }
        catch (DbUpdateConcurrencyException) { return new(PromotionOperationStatus.Conflict); }
        catch (DbUpdateException exception) when (Unique(exception)) { return new(PromotionOperationStatus.Conflict); }
        catch (DbUpdateException) { return new(PromotionOperationStatus.ServiceUnavailable); }
        catch (DbException exception) when (exception is SqlException { Number: 2601 or 2627 }) { return new(PromotionOperationStatus.Conflict); }
        catch (DbException) { return new(PromotionOperationStatus.ServiceUnavailable); }
    }

    public async Task<PromotionOperationStatus> CancelAsync(Guid applicationUserId, Guid promotionId, byte[] rowVersion, CancellationToken cancellationToken = default)
    {
        var scope = await Scope(applicationUserId, CompanyPermissionCodes.BillingManage, cancellationToken); if (scope is null) return PromotionOperationStatus.NotFound;
        var now = timeProvider.GetUtcNow();
        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var promotion = await dbContext.Set<ListingPromotion>().Where(item => item.Id == promotionId && item.CompanyId == scope.CompanyId).SingleOrDefaultAsync(cancellationToken);
            if (promotion is null) return PromotionOperationStatus.NotFound;
            if (promotion.Status == ListingPromotionStatus.Cancelled) return PromotionOperationStatus.Succeeded;
            if (EffectiveStatus(promotion, now) == ListingPromotionStatus.Completed) return PromotionOperationStatus.Conflict;
            if (!promotion.RowVersion.AsSpan().SequenceEqual(rowVersion)) return PromotionOperationStatus.Conflict;
            dbContext.Entry(promotion).Property(item => item.RowVersion).OriginalValue = rowVersion;
            promotion.Status = ListingPromotionStatus.Cancelled;
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return PromotionOperationStatus.Succeeded;
        }
        catch (OperationCanceledException) { throw; }
        catch (DbUpdateConcurrencyException) { return PromotionOperationStatus.Conflict; }
        catch (DbUpdateException) { return PromotionOperationStatus.ServiceUnavailable; }
        catch (DbException) { return PromotionOperationStatus.ServiceUnavailable; }
    }

    private IQueryable<PublicPromotionPackage> PackageQuery() => dbContext.Set<PromotionPackage>().AsNoTracking().Where(item => item.IsActive).Select(item => new PublicPromotionPackage(item.Id, item.Name, item.Placement, item.DurationDays, item.Price, new PromotionCurrency(item.Currency.Code, item.Currency.Name, item.Currency.Symbol, item.Currency.DecimalPlaces)));
    private IQueryable<ListingPromotion> CompanyPromotionQuery(Guid companyId) => dbContext.Set<ListingPromotion>().AsNoTracking().Where(item => item.CompanyId == companyId);
    private Task<CompanyAccessScope?> Scope(Guid userId, string permission, CancellationToken cancellationToken) => companyAccessService.GetAuthorizedScopeAsync(userId, permission, cancellationToken);
    private static ListingPromotionStatus EffectiveStatus(ListingPromotion promotion, DateTimeOffset now) => promotion.Status switch { ListingPromotionStatus.Cancelled => ListingPromotionStatus.Cancelled, ListingPromotionStatus.Completed => ListingPromotionStatus.Completed, _ when promotion.EndsAt <= now => ListingPromotionStatus.Completed, _ when promotion.StartsAt > now => ListingPromotionStatus.Scheduled, _ => ListingPromotionStatus.Active };
    private static bool Unique(DbUpdateException exception) => exception.GetBaseException() is SqlException { Number: 2601 or 2627 };
    private sealed record PackageSnapshot(Guid Id, string Name, string Placement, int DurationDays, decimal Price, string CurrencyCode);
}
