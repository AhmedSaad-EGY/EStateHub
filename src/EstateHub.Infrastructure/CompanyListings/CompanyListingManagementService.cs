using System.Data;
using System.Data.Common;
using EstateHub.Application.Common;
using EstateHub.Application.CompanyAccess;
using EstateHub.Application.CompanyListings;
using EstateHub.Domain.Entities.Billing;
using EstateHub.Domain.Entities.Catalog;
using EstateHub.Domain.Entities.Companies;
using EstateHub.Domain.Enums;
using EstateHub.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EstateHub.Infrastructure.CompanyListings;

public sealed class CompanyListingManagementService(
    EstateHubDbContext dbContext,
    ICompanyAccessService companyAccessService,
    TimeProvider timeProvider) : ICompanyListingManagementService
{
    public async Task<PagedResult<CompanyListingSummary>?> GetListingsAsync(Guid applicationUserId, CompanyListingDirectoryQuery query, CancellationToken cancellationToken = default)
    {
        var scope = await GetScopeAsync(applicationUserId, CompanyPermissionCodes.ListingsRead, cancellationToken);
        if (scope is null) return null;

        var listings = GetSummaryQuery(scope.CompanyId);
        if (query.Search is not null)
            listings = listings.Where(x => x.ListingCode.Contains(query.Search) || x.Slug.Contains(query.Search) || x.Title.Contains(query.Search));
        if (query.PublicationStatus is not null) listings = listings.Where(x => x.PublicationStatus == query.PublicationStatus);
        if (query.ListingType is not null) listings = listings.Where(x => x.ListingType == query.ListingType);
        if (query.UnitId is not null) listings = listings.Where(x => x.UnitId == query.UnitId);
        if (query.ProjectId is not null) listings = listings.Where(x => x.ProjectId == query.ProjectId);
        if (query.CurrencyCode is not null) listings = listings.Where(x => x.Currency.Code == query.CurrencyCode);

        var totalCount = await listings.CountAsync(cancellationToken);
        var items = await listings.OrderByDescending(x => x.UpdatedAt).ThenByDescending(x => x.Id)
            .Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);
        return new PagedResult<CompanyListingSummary>(items, query.PageNumber, query.PageSize, totalCount);
    }

    public async Task<CompanyListingDetails?> GetListingAsync(Guid applicationUserId, Guid listingId, CancellationToken cancellationToken = default)
    {
        var scope = await GetScopeAsync(applicationUserId, CompanyPermissionCodes.ListingsRead, cancellationToken);
        return scope is null ? null : await GetDetailsAsync(scope, listingId, cancellationToken);
    }

    public Task<CompanyListingMutationResult> CreateListingAsync(Guid applicationUserId, CompanyListingCommand command, CancellationToken cancellationToken = default) =>
        SaveAsync(applicationUserId, Guid.Empty, command, true, cancellationToken);

    public Task<CompanyListingMutationResult> UpdateListingAsync(Guid applicationUserId, Guid listingId, CompanyListingCommand command, CancellationToken cancellationToken = default) =>
        SaveAsync(applicationUserId, listingId, command, false, cancellationToken);

    public async Task<CompanyListingMediaResult> ReplaceMediaAsync(Guid applicationUserId, Guid listingId, IReadOnlyList<CompanyListingMediaItem> items, byte[] rowVersion, CancellationToken cancellationToken = default)
    {
        var scope = await GetScopeAsync(applicationUserId, CompanyPermissionCodes.ListingsManage, cancellationToken);
        if (scope is null) return new(CompanyListingOperationStatus.NotFound);
        if (items.Count > 50 || items.Any(x => x.FileAssetId == Guid.Empty || x.SortOrder < 0 || x.Caption?.Length > 500)
            || items.Select(x => x.FileAssetId).Distinct().Count() != items.Count || items.Count(x => x.IsCover) > 1)
            return new(CompanyListingOperationStatus.InvalidRequest);

        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var listing = await dbContext.Set<Listing>().SingleOrDefaultAsync(x => x.Id == listingId && x.CompanyId == scope.CompanyId, cancellationToken);
            if (listing is null) return new(CompanyListingOperationStatus.NotFound);
            if (listing.PublicationStatus is ListingPublicationStatus.Pending or ListingPublicationStatus.Archived) return new(CompanyListingOperationStatus.Conflict);

            var assetIds = items.Select(x => x.FileAssetId).ToList();
            var eligibleAssetCount = await dbContext.Set<FileAsset>().AsNoTracking()
                .Where(x => assetIds.Contains(x.Id)
                    && x.FileType == FileType.Image
                    && dbContext.Set<CompanyEmployee>().Any(employee =>
                        employee.CompanyId == scope.CompanyId
                        && employee.ApplicationUserId == x.UploadedByApplicationUserId
                        && employee.Status == CompanyEmployeeStatus.Active
                        && employee.EndedAt == null))
                .CountAsync(cancellationToken);
            if (eligibleAssetCount != items.Count) return new(CompanyListingOperationStatus.InvalidReference);

            dbContext.Entry(listing).Property(x => x.RowVersion).OriginalValue = rowVersion;
            var existingMedia = await dbContext.Set<ListingMedia>().Where(x => x.ListingId == listingId).ToListAsync(cancellationToken);
            dbContext.RemoveRange(existingMedia);
            await dbContext.SaveChangesAsync(cancellationToken);
            dbContext.AddRange(items.Select(x => new ListingMedia
            {
                Id = Guid.NewGuid(), ListingId = listingId, FileAssetId = x.FileAssetId,
                SortOrder = x.SortOrder, IsCover = x.IsCover, Caption = x.Caption
            }));
            listing.UpdatedAt = timeProvider.GetUtcNow();
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new(CompanyListingOperationStatus.Succeeded, listing.RowVersion,
                items.OrderBy(x => x.SortOrder).ThenBy(x => x.FileAssetId).ToList());
        }
        catch (OperationCanceledException) { throw; }
        catch (DbUpdateConcurrencyException) { return new(CompanyListingOperationStatus.Conflict); }
        catch (DbUpdateException) { return new(CompanyListingOperationStatus.ServiceUnavailable); }
        catch (DbException) { return new(CompanyListingOperationStatus.ServiceUnavailable); }
    }

    public Task<CompanyListingPaymentPlanResult> CreatePaymentPlanAsync(Guid applicationUserId, Guid listingId, CompanyListingPaymentPlanCommand command, byte[] rowVersion, CancellationToken cancellationToken = default) =>
        SavePaymentPlanAsync(applicationUserId, listingId, Guid.Empty, command, rowVersion, true, cancellationToken);

    public Task<CompanyListingPaymentPlanResult> UpdatePaymentPlanAsync(Guid applicationUserId, Guid listingId, Guid paymentPlanId, CompanyListingPaymentPlanCommand command, byte[] rowVersion, CancellationToken cancellationToken = default) =>
        SavePaymentPlanAsync(applicationUserId, listingId, paymentPlanId, command, rowVersion, false, cancellationToken);

    public async Task<CompanyListingOperationStatus> DeletePaymentPlanAsync(Guid applicationUserId, Guid listingId, Guid paymentPlanId, byte[] rowVersion, CancellationToken cancellationToken = default)
    {
        var scope = await GetScopeAsync(applicationUserId, CompanyPermissionCodes.ListingsManage, cancellationToken);
        if (scope is null) return CompanyListingOperationStatus.NotFound;
        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var listing = await dbContext.Set<Listing>().SingleOrDefaultAsync(x => x.Id == listingId && x.CompanyId == scope.CompanyId, cancellationToken);
            if (listing is null) return CompanyListingOperationStatus.NotFound;
            if (listing.PublicationStatus is ListingPublicationStatus.Pending or ListingPublicationStatus.Archived) return CompanyListingOperationStatus.Conflict;
            var paymentPlan = await dbContext.Set<PaymentPlan>().SingleOrDefaultAsync(x => x.Id == paymentPlanId && x.ListingId == listingId, cancellationToken);
            if (paymentPlan is null) return CompanyListingOperationStatus.NotFound;
            dbContext.Entry(listing).Property(x => x.RowVersion).OriginalValue = rowVersion;
            dbContext.Remove(paymentPlan);
            listing.UpdatedAt = timeProvider.GetUtcNow();
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return CompanyListingOperationStatus.Succeeded;
        }
        catch (OperationCanceledException) { throw; }
        catch (DbUpdateConcurrencyException) { return CompanyListingOperationStatus.Conflict; }
        catch (DbUpdateException) { return CompanyListingOperationStatus.ServiceUnavailable; }
        catch (DbException) { return CompanyListingOperationStatus.ServiceUnavailable; }
    }

    public async Task<CompanyListingOperationStatus> PublishListingAsync(Guid applicationUserId, Guid listingId, byte[] rowVersion, CancellationToken cancellationToken = default)
    {
        var scope = await GetScopeAsync(applicationUserId, CompanyPermissionCodes.ListingsManage, cancellationToken);
        if (scope is null) return CompanyListingOperationStatus.NotFound;

        var initialStatus = await dbContext.Set<Listing>().AsNoTracking()
            .Where(x => x.Id == listingId && x.CompanyId == scope.CompanyId)
            .Select(x => (ListingPublicationStatus?)x.PublicationStatus)
            .SingleOrDefaultAsync(cancellationToken);
        if (initialStatus is null) return CompanyListingOperationStatus.NotFound;
        if (initialStatus == ListingPublicationStatus.Published) return CompanyListingOperationStatus.Succeeded;
        if (initialStatus != ListingPublicationStatus.Draft) return CompanyListingOperationStatus.Conflict;

        var operationTime = timeProvider.GetUtcNow();
        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

            var lockedSubscriptionCount = await dbContext.Set<CompanySubscription>()
                .Where(x => x.CompanyId == scope.CompanyId
                    && x.Status == CompanySubscriptionStatus.Active
                    && x.EndedAt == null
                    && x.CurrentPeriodStart <= operationTime
                    && x.CurrentPeriodEnd > operationTime)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.UpdatedAt, operationTime), cancellationToken);
            if (lockedSubscriptionCount != 1)
            {
                await transaction.RollbackAsync(cancellationToken);
                return CompanyListingOperationStatus.Conflict;
            }

            var subscription = await dbContext.Set<CompanySubscription>().AsNoTracking()
                .Where(x => x.CompanyId == scope.CompanyId
                    && x.Status == CompanySubscriptionStatus.Active
                    && x.EndedAt == null
                    && x.CurrentPeriodStart <= operationTime
                    && x.CurrentPeriodEnd > operationTime)
                .Select(x => new { x.Id, x.MaxPublishedListingsSnapshot })
                .SingleOrDefaultAsync(cancellationToken);
            if (subscription is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return CompanyListingOperationStatus.Conflict;
            }

            var listing = await dbContext.Set<Listing>()
                .SingleOrDefaultAsync(x => x.Id == listingId && x.CompanyId == scope.CompanyId, cancellationToken);
            if (listing is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return CompanyListingOperationStatus.NotFound;
            }
            if (listing.PublicationStatus == ListingPublicationStatus.Published)
            {
                await transaction.RollbackAsync(cancellationToken);
                return CompanyListingOperationStatus.Succeeded;
            }
            if (listing.PublicationStatus != ListingPublicationStatus.Draft)
            {
                await transaction.RollbackAsync(cancellationToken);
                return CompanyListingOperationStatus.Conflict;
            }

            var listingIsEligible = await dbContext.Set<Listing>().AsNoTracking().AnyAsync(x =>
                x.Id == listingId
                && x.CompanyId == scope.CompanyId
                && x.Unit.ManagingCompanyId == scope.CompanyId
                && x.Currency.IsActive
                && (x.Unit.ProjectId == null
                    || (x.Unit.Project!.ProjectStatus == ProjectStatus.Published
                        && x.Unit.Project.DeveloperCompany.CompanyType == CompanyType.Developer
                        && x.Unit.Project.DeveloperCompany.Status == CompanyStatus.Active
                        && x.Unit.Project.DeveloperCompany.VerifiedAt != null)), cancellationToken);
            if (!listingIsEligible)
            {
                await transaction.RollbackAsync(cancellationToken);
                return CompanyListingOperationStatus.Conflict;
            }

            var publishedCount = await dbContext.Set<Listing>().AsNoTracking()
                .CountAsync(x => x.CompanyId == scope.CompanyId && x.PublicationStatus == ListingPublicationStatus.Published, cancellationToken);
            if (subscription.MaxPublishedListingsSnapshot is int limit && publishedCount >= limit)
            {
                await transaction.RollbackAsync(cancellationToken);
                return CompanyListingOperationStatus.Conflict;
            }

            var unitAlreadyPublished = await dbContext.Set<Listing>().AsNoTracking().AnyAsync(x =>
                x.UnitId == listing.UnitId
                && x.Id != listing.Id
                && x.PublicationStatus == ListingPublicationStatus.Published, cancellationToken);
            if (unitAlreadyPublished)
            {
                await transaction.RollbackAsync(cancellationToken);
                return CompanyListingOperationStatus.Conflict;
            }

            dbContext.Entry(listing).Property(x => x.RowVersion).OriginalValue = rowVersion;
            listing.PublicationStatus = ListingPublicationStatus.Published;
            listing.PublishedAt = operationTime;
            listing.ArchivedAt = null;
            listing.UpdatedAt = operationTime;
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return CompanyListingOperationStatus.Succeeded;
        }
        catch (OperationCanceledException) { throw; }
        catch (DbUpdateConcurrencyException) { return CompanyListingOperationStatus.Conflict; }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception)) { return CompanyListingOperationStatus.Conflict; }
        catch (DbUpdateException) { return CompanyListingOperationStatus.ServiceUnavailable; }
        catch (DbException) { return CompanyListingOperationStatus.ServiceUnavailable; }
    }

    public async Task<CompanyListingOperationStatus> ArchiveListingAsync(Guid applicationUserId, Guid listingId, byte[] rowVersion, CancellationToken cancellationToken = default)
    {
        var scope = await GetScopeAsync(applicationUserId, CompanyPermissionCodes.ListingsManage, cancellationToken);
        if (scope is null) return CompanyListingOperationStatus.NotFound;
        var operationTime = timeProvider.GetUtcNow();
        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var listing = await dbContext.Set<Listing>()
                .SingleOrDefaultAsync(x => x.Id == listingId && x.CompanyId == scope.CompanyId, cancellationToken);
            if (listing is null) return CompanyListingOperationStatus.NotFound;
            if (listing.PublicationStatus == ListingPublicationStatus.Archived) return CompanyListingOperationStatus.Succeeded;
            if (listing.PublicationStatus is not (ListingPublicationStatus.Draft or ListingPublicationStatus.Published)) return CompanyListingOperationStatus.Conflict;

            dbContext.Entry(listing).Property(x => x.RowVersion).OriginalValue = rowVersion;
            listing.PublicationStatus = ListingPublicationStatus.Archived;
            listing.ArchivedAt = operationTime;
            listing.UpdatedAt = operationTime;
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return CompanyListingOperationStatus.Succeeded;
        }
        catch (OperationCanceledException) { throw; }
        catch (DbUpdateConcurrencyException) { return CompanyListingOperationStatus.Conflict; }
        catch (DbUpdateException) { return CompanyListingOperationStatus.ServiceUnavailable; }
        catch (DbException) { return CompanyListingOperationStatus.ServiceUnavailable; }
    }

    private async Task<CompanyListingMutationResult> SaveAsync(Guid applicationUserId, Guid listingId, CompanyListingCommand command, bool create, CancellationToken cancellationToken)
    {
        var scope = await GetScopeAsync(applicationUserId, CompanyPermissionCodes.ListingsManage, cancellationToken);
        if (scope is null) return new(CompanyListingOperationStatus.NotFound);

        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            Listing listing;
            var now = timeProvider.GetUtcNow();

            if (create)
            {
                if (!await ReferencesAreAvailableAsync(scope.CompanyId, command, cancellationToken)) return new(CompanyListingOperationStatus.InvalidReference);
                if (await SlugExistsAsync(command.Slug, null, cancellationToken)
                    || await ListingCodeExistsAsync(scope.CompanyId, command.ListingCode, null, cancellationToken))
                    return new(CompanyListingOperationStatus.Conflict);

                listing = new Listing
                {
                    Id = Guid.NewGuid(), UnitId = command.UnitId, CompanyId = scope.CompanyId,
                    CreatedByEmployeeId = scope.CompanyEmployeeId, CurrencyCode = command.CurrencyCode,
                    ListingCode = command.ListingCode, Slug = command.Slug, Title = command.Title,
                    Description = command.Description, ListingType = command.ListingType,
                    AskingPrice = command.AskingPrice, RentPeriod = command.RentPeriod,
                    PublicationStatus = ListingPublicationStatus.Draft, PublishedAt = null, ArchivedAt = null,
                    CreatedAt = now, UpdatedAt = now
                };
                dbContext.Add(listing);
                dbContext.Add(new ListingPriceHistory
                {
                    Id = Guid.NewGuid(), ListingId = listing.Id, Price = listing.AskingPrice,
                    CurrencyCode = listing.CurrencyCode, EffectiveFrom = now, EffectiveTo = null, ChangeReason = null
                });
            }
            else
            {
                var existing = await dbContext.Set<Listing>().SingleOrDefaultAsync(x => x.Id == listingId && x.CompanyId == scope.CompanyId, cancellationToken);
                if (existing is null) return new(CompanyListingOperationStatus.NotFound);
                if (existing.PublicationStatus is ListingPublicationStatus.Pending or ListingPublicationStatus.Archived) return new(CompanyListingOperationStatus.Conflict);
                if (existing.PublicationStatus == ListingPublicationStatus.Published
                    && (existing.UnitId != command.UnitId || existing.ListingCode != command.ListingCode || existing.Slug != command.Slug || existing.ListingType != command.ListingType))
                    return new(CompanyListingOperationStatus.Conflict);
                if (!await ReferencesAreAvailableAsync(scope.CompanyId, command, cancellationToken)) return new(CompanyListingOperationStatus.InvalidReference);
                if (await SlugExistsAsync(command.Slug, listingId, cancellationToken)
                    || await ListingCodeExistsAsync(scope.CompanyId, command.ListingCode, listingId, cancellationToken))
                    return new(CompanyListingOperationStatus.Conflict);

                var priceChanged = existing.AskingPrice != command.AskingPrice || existing.CurrencyCode != command.CurrencyCode;
                if (!priceChanged && command.PriceChangeReason is not null) return new(CompanyListingOperationStatus.InvalidRequest);

                dbContext.Entry(existing).Property(x => x.RowVersion).OriginalValue = command.RowVersion!;
                if (priceChanged)
                {
                    var openRows = await dbContext.Set<ListingPriceHistory>()
                        .Where(x => x.ListingId == existing.Id && x.EffectiveTo == null)
                        .Take(2).ToListAsync(cancellationToken);
                    if (openRows.Count != 1) return new(CompanyListingOperationStatus.ServiceUnavailable);
                    if (now <= openRows[0].EffectiveFrom && openRows[0].EffectiveFrom == DateTimeOffset.MaxValue)
                        return new(CompanyListingOperationStatus.ServiceUnavailable);
                    var effectiveFrom = now > openRows[0].EffectiveFrom ? now : openRows[0].EffectiveFrom.AddTicks(1);
                    openRows[0].EffectiveTo = effectiveFrom;
                    dbContext.Add(new ListingPriceHistory
                    {
                        Id = Guid.NewGuid(), ListingId = existing.Id, Price = command.AskingPrice,
                        CurrencyCode = command.CurrencyCode, EffectiveFrom = effectiveFrom,
                        EffectiveTo = null, ChangeReason = command.PriceChangeReason
                    });
                }

                existing.UnitId = command.UnitId;
                existing.CurrencyCode = command.CurrencyCode;
                existing.ListingCode = command.ListingCode;
                existing.Slug = command.Slug;
                existing.Title = command.Title;
                existing.Description = command.Description;
                existing.ListingType = command.ListingType;
                existing.AskingPrice = command.AskingPrice;
                existing.RentPeriod = command.RentPeriod;
                existing.UpdatedAt = now;
                listing = existing;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            var details = await GetDetailsAsync(scope, listing.Id, cancellationToken);
            return new(CompanyListingOperationStatus.Succeeded, details);
        }
        catch (OperationCanceledException) { throw; }
        catch (DbUpdateConcurrencyException) { return new(CompanyListingOperationStatus.Conflict); }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception)) { return new(CompanyListingOperationStatus.Conflict); }
        catch (DbUpdateException) { return new(CompanyListingOperationStatus.ServiceUnavailable); }
        catch (DbException) { return new(CompanyListingOperationStatus.ServiceUnavailable); }
    }

    private async Task<CompanyListingDetails?> GetDetailsAsync(CompanyAccessScope scope, Guid listingId, CancellationToken cancellationToken)
    {
        var header = await GetHeaderQuery(scope.CompanyId).SingleOrDefaultAsync(x => x.Header.Id == listingId, cancellationToken);
        if (header is null) return null;
        var history = await dbContext.Set<ListingPriceHistory>().AsNoTracking()
            .Where(x => x.ListingId == listingId && x.Listing.CompanyId == scope.CompanyId)
            .OrderByDescending(x => x.EffectiveFrom).ThenByDescending(x => x.Id)
            .Select(x => new CompanyListingPriceHistoryItem(x.Id, x.Price,
                new CompanyListingCurrencySummary(x.Currency.Code, x.Currency.Name, x.Currency.Symbol, x.Currency.DecimalPlaces),
                x.EffectiveFrom, x.EffectiveTo, x.ChangeReason))
            .ToListAsync(cancellationToken);
        var media = await dbContext.Set<ListingMedia>().AsNoTracking()
            .Where(x => x.ListingId == listingId)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.FileAssetId)
            .Select(x => new CompanyListingMediaItem(x.FileAssetId, x.SortOrder, x.IsCover, x.Caption))
            .ToListAsync(cancellationToken);
        var paymentPlans = await dbContext.Set<PaymentPlan>().AsNoTracking()
            .Where(x => x.ListingId == listingId && x.Listing.CompanyId == scope.CompanyId)
            .OrderByDescending(x => x.IsActive).ThenBy(x => x.TotalPrice).ThenBy(x => x.DurationMonths).ThenBy(x => x.Id)
            .Select(x => new CompanyListingPaymentPlanItem(x.Id, x.Name, x.TotalPrice,
                new CompanyListingCurrencySummary(x.Currency.Code, x.Currency.Name, x.Currency.Symbol, x.Currency.DecimalPlaces),
                x.DownPaymentPercentage, x.DurationMonths, x.InstallmentFrequency, x.CashDiscountPercentage, x.IsActive))
            .ToListAsync(cancellationToken);
        return new CompanyListingDetails(header.Header, header.Description, header.CreatedByEmployee, history, media, paymentPlans);
    }

    private IQueryable<CompanyListingSummary> GetSummaryQuery(Guid companyId) => dbContext.Set<Listing>().AsNoTracking()
        .Where(x => x.CompanyId == companyId)
        .Select(x => new CompanyListingSummary(
            x.Id, x.UnitId, x.Unit.ProjectId, x.ListingCode, x.Slug, x.Title, x.ListingType, x.AskingPrice,
            new CompanyListingCurrencySummary(x.Currency.Code, x.Currency.Name, x.Currency.Symbol, x.Currency.DecimalPlaces),
            x.RentPeriod, x.PublicationStatus, x.PublishedAt, x.ArchivedAt,
            x.Media.Where(media => media.IsCover).Select(media => (Guid?)media.FileAssetId).FirstOrDefault(),
            new CompanyListingUnitSummary(x.Unit.Id, x.Unit.UnitCode, x.Unit.Status,
                new CompanyListingUnitTypeSummary(x.Unit.UnitType.Id, x.Unit.UnitType.Code, x.Unit.UnitType.NameEn, x.Unit.UnitType.NameAr),
                new CompanyListingLocationSummary(x.Unit.Location.Id, x.Unit.Location.Type, x.Unit.Location.NameEn, x.Unit.Location.NameAr, x.Unit.Location.Slug),
                x.Unit.ProjectId == null ? null : new CompanyListingProjectSummary(x.Unit.Project!.Id, x.Unit.Project.Slug, x.Unit.Project.Name, x.Unit.Project.ProjectStatus)),
            x.CreatedAt, x.UpdatedAt, x.RowVersion));

    private IQueryable<CompanyListingHeader> GetHeaderQuery(Guid companyId) => dbContext.Set<Listing>().AsNoTracking()
        .Where(x => x.CompanyId == companyId)
        .Select(x => new CompanyListingHeader(
            new CompanyListingSummary(
                x.Id, x.UnitId, x.Unit.ProjectId, x.ListingCode, x.Slug, x.Title, x.ListingType, x.AskingPrice,
                new CompanyListingCurrencySummary(x.Currency.Code, x.Currency.Name, x.Currency.Symbol, x.Currency.DecimalPlaces),
                x.RentPeriod, x.PublicationStatus, x.PublishedAt, x.ArchivedAt,
                x.Media.Where(media => media.IsCover).Select(media => (Guid?)media.FileAssetId).FirstOrDefault(),
                new CompanyListingUnitSummary(x.Unit.Id, x.Unit.UnitCode, x.Unit.Status,
                    new CompanyListingUnitTypeSummary(x.Unit.UnitType.Id, x.Unit.UnitType.Code, x.Unit.UnitType.NameEn, x.Unit.UnitType.NameAr),
                    new CompanyListingLocationSummary(x.Unit.Location.Id, x.Unit.Location.Type, x.Unit.Location.NameEn, x.Unit.Location.NameAr, x.Unit.Location.Slug),
                    x.Unit.ProjectId == null ? null : new CompanyListingProjectSummary(x.Unit.Project!.Id, x.Unit.Project.Slug, x.Unit.Project.Name, x.Unit.Project.ProjectStatus)),
                x.CreatedAt, x.UpdatedAt, x.RowVersion),
            x.Description, new CompanyListingCreatedByEmployeeSummary(x.CreatedByEmployee.Id, x.CreatedByEmployee.FullName)));

    private async Task<bool> ReferencesAreAvailableAsync(Guid companyId, CompanyListingCommand command, CancellationToken cancellationToken) =>
        await dbContext.Set<Unit>().AsNoTracking().AnyAsync(x => x.Id == command.UnitId && x.ManagingCompanyId == companyId, cancellationToken)
        && await dbContext.Set<Currency>().AsNoTracking().AnyAsync(x => x.Code == command.CurrencyCode && x.IsActive, cancellationToken);

    private async Task<CompanyListingPaymentPlanResult> SavePaymentPlanAsync(Guid applicationUserId, Guid listingId, Guid paymentPlanId, CompanyListingPaymentPlanCommand command, byte[] rowVersion, bool create, CancellationToken cancellationToken)
    {
        var scope = await GetScopeAsync(applicationUserId, CompanyPermissionCodes.ListingsManage, cancellationToken);
        if (scope is null) return new(CompanyListingOperationStatus.NotFound);
        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var listing = await dbContext.Set<Listing>().SingleOrDefaultAsync(x => x.Id == listingId && x.CompanyId == scope.CompanyId, cancellationToken);
            if (listing is null) return new(CompanyListingOperationStatus.NotFound);
            if (listing.PublicationStatus is ListingPublicationStatus.Pending or ListingPublicationStatus.Archived) return new(CompanyListingOperationStatus.Conflict);
            if (!await dbContext.Set<Currency>().AsNoTracking().AnyAsync(x => x.Code == command.CurrencyCode && x.IsActive, cancellationToken)) return new(CompanyListingOperationStatus.InvalidReference);

            PaymentPlan paymentPlan;
            if (create)
            {
                paymentPlan = new PaymentPlan { Id = Guid.NewGuid(), ListingId = listingId };
                dbContext.Add(paymentPlan);
            }
            else
            {
                var existing = await dbContext.Set<PaymentPlan>().SingleOrDefaultAsync(x => x.Id == paymentPlanId && x.ListingId == listingId, cancellationToken);
                if (existing is null) return new(CompanyListingOperationStatus.NotFound);
                paymentPlan = existing;
            }

            dbContext.Entry(listing).Property(x => x.RowVersion).OriginalValue = rowVersion;
            paymentPlan.Name = command.Name;
            paymentPlan.TotalPrice = command.TotalPrice;
            paymentPlan.CurrencyCode = command.CurrencyCode;
            paymentPlan.DownPaymentPercentage = command.DownPaymentPercentage;
            paymentPlan.DurationMonths = command.DurationMonths;
            paymentPlan.InstallmentFrequency = command.InstallmentFrequency;
            paymentPlan.CashDiscountPercentage = command.CashDiscountPercentage;
            paymentPlan.IsActive = command.IsActive;
            listing.UpdatedAt = timeProvider.GetUtcNow();
            await dbContext.SaveChangesAsync(cancellationToken);
            var projection = await GetPaymentPlanAsync(scope.CompanyId, paymentPlan.Id, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new(CompanyListingOperationStatus.Succeeded, listing.RowVersion, projection);
        }
        catch (OperationCanceledException) { throw; }
        catch (DbUpdateConcurrencyException) { return new(CompanyListingOperationStatus.Conflict); }
        catch (DbUpdateException) { return new(CompanyListingOperationStatus.ServiceUnavailable); }
        catch (DbException) { return new(CompanyListingOperationStatus.ServiceUnavailable); }
    }

    private Task<CompanyListingPaymentPlanItem> GetPaymentPlanAsync(Guid companyId, Guid paymentPlanId, CancellationToken cancellationToken) =>
        dbContext.Set<PaymentPlan>().AsNoTracking().Where(x => x.Id == paymentPlanId && x.Listing.CompanyId == companyId)
            .Select(x => new CompanyListingPaymentPlanItem(x.Id, x.Name, x.TotalPrice,
                new CompanyListingCurrencySummary(x.Currency.Code, x.Currency.Name, x.Currency.Symbol, x.Currency.DecimalPlaces),
                x.DownPaymentPercentage, x.DurationMonths, x.InstallmentFrequency, x.CashDiscountPercentage, x.IsActive))
            .SingleAsync(cancellationToken);

    private Task<bool> SlugExistsAsync(string slug, Guid? exceptId, CancellationToken cancellationToken) =>
        dbContext.Set<Listing>().AsNoTracking().AnyAsync(x => x.Slug == slug && (exceptId == null || x.Id != exceptId), cancellationToken);

    private Task<bool> ListingCodeExistsAsync(Guid companyId, string listingCode, Guid? exceptId, CancellationToken cancellationToken) =>
        dbContext.Set<Listing>().AsNoTracking().AnyAsync(x => x.CompanyId == companyId && x.ListingCode == listingCode && (exceptId == null || x.Id != exceptId), cancellationToken);

    private async Task<CompanyAccessScope?> GetScopeAsync(Guid applicationUserId, string permissionCode, CancellationToken cancellationToken)
    {
        var scope = await companyAccessService.GetAuthorizedScopeAsync(applicationUserId, permissionCode, cancellationToken);
        if (scope is null) return null;
        return await dbContext.Set<Company>().AsNoTracking().AnyAsync(x => x.Id == scope.CompanyId && x.Status == CompanyStatus.Active && x.VerifiedAt != null && (x.CompanyType == CompanyType.Developer || x.CompanyType == CompanyType.BrokerAgency), cancellationToken)
            ? scope : null;
    }

    private static bool IsUniqueViolation(DbUpdateException exception) => exception.GetBaseException() is SqlException { Number: 2601 or 2627 };
    private sealed record CompanyListingHeader(CompanyListingSummary Header, string Description, CompanyListingCreatedByEmployeeSummary CreatedByEmployee);
}
