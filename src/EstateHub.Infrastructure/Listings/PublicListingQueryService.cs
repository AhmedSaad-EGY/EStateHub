using EstateHub.Application.Common;
using EstateHub.Application.Listings;
using EstateHub.Domain.Entities.Catalog;
using EstateHub.Domain.Enums;
using EstateHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EstateHub.Infrastructure.Listings;

public sealed class PublicListingQueryService(
    EstateHubDbContext dbContext,
    TimeProvider timeProvider) : IPublicListingQueryService
{
    public async Task<PagedResult<ListingDirectoryItem>> GetListingsAsync(
        ListingDirectoryQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var utcNow = timeProvider.GetUtcNow();
        var listings = GetPublicListings(utcNow);

        if (query.Search is not null)
        {
            listings = listings.Where(listing =>
                listing.Title.Contains(query.Search)
                || listing.Description.Contains(query.Search)
                || listing.Company.DisplayName.Contains(query.Search)
                || listing.Unit.UnitType.NameEn.Contains(query.Search)
                || listing.Unit.UnitType.NameAr.Contains(query.Search)
                || listing.Unit.Location.NameEn.Contains(query.Search)
                || listing.Unit.Location.NameAr.Contains(query.Search)
                || (listing.Unit.ProjectId != null
                    && listing.Unit.Project!.Name.Contains(query.Search)));
        }

        if (query.ListingType is not null)
        {
            listings = listings.Where(listing =>
                listing.ListingType == query.ListingType.Value);
        }

        if (query.LocationId is not null)
        {
            listings = listings.Where(listing =>
                listing.Unit.LocationId == query.LocationId.Value);
        }

        if (query.UnitTypeId is not null)
        {
            listings = listings.Where(listing =>
                listing.Unit.UnitTypeId == query.UnitTypeId.Value);
        }

        if (query.CompanyId is not null)
        {
            listings = listings.Where(listing =>
                listing.CompanyId == query.CompanyId.Value);
        }

        if (query.ProjectId is not null)
        {
            listings = listings.Where(listing =>
                listing.Unit.ProjectId == query.ProjectId.Value);
        }

        if (query.CurrencyCode is not null)
        {
            listings = listings.Where(listing =>
                listing.CurrencyCode == query.CurrencyCode);
        }

        if (query.MinPrice is not null)
        {
            listings = listings.Where(listing =>
                listing.AskingPrice >= query.MinPrice.Value);
        }

        if (query.MaxPrice is not null)
        {
            listings = listings.Where(listing =>
                listing.AskingPrice <= query.MaxPrice.Value);
        }

        if (query.MinBedrooms is not null)
        {
            listings = listings.Where(listing =>
                listing.Unit.Bedrooms >= query.MinBedrooms.Value);
        }

        if (query.MaxBedrooms is not null)
        {
            listings = listings.Where(listing =>
                listing.Unit.Bedrooms <= query.MaxBedrooms.Value);
        }

        if (query.MinBathrooms is not null)
        {
            listings = listings.Where(listing =>
                listing.Unit.Bathrooms >= query.MinBathrooms.Value);
        }

        if (query.MaxBathrooms is not null)
        {
            listings = listings.Where(listing =>
                listing.Unit.Bathrooms <= query.MaxBathrooms.Value);
        }

        if (query.MinArea is not null)
        {
            listings = listings.Where(listing =>
                listing.Unit.BuiltUpArea >= query.MinArea.Value);
        }

        if (query.MaxArea is not null)
        {
            listings = listings.Where(listing =>
                listing.Unit.BuiltUpArea <= query.MaxArea.Value);
        }

        var totalCount = await listings.CountAsync(cancellationToken);
        var offset = (query.PageNumber - 1) * query.PageSize;

        var items = await ApplyOrdering(listings, query.Sort)
            .Skip(offset)
            .Take(query.PageSize)
            .Select(listing => new ListingDirectoryItem(
                listing.Id,
                listing.Slug,
                listing.Title,
                listing.ListingType,
                listing.AskingPrice,
                listing.RentPeriod,
                listing.PublishedAt,
                listing.Media
                    .Where(media => media.IsCover)
                    .Select(media => (Guid?)media.FileAssetId)
                    .FirstOrDefault(),
                listing.PaymentPlans.Any(plan => plan.IsActive),
                new ListingCurrencySummary(
                    listing.Currency.Code,
                    listing.Currency.Name,
                    listing.Currency.Symbol,
                    listing.Currency.DecimalPlaces),
                new ListingCompanySummary(
                    listing.Company.Id,
                    listing.Company.Slug,
                    listing.Company.DisplayName,
                    listing.Company.LogoFileAssetId),
                new ListingUnitSummary(
                    listing.Unit.Id,
                    listing.Unit.Bedrooms,
                    listing.Unit.Bathrooms,
                    listing.Unit.BuiltUpArea,
                    listing.Unit.LandArea,
                    listing.Unit.FinishingType,
                    listing.Unit.FurnishedStatus,
                    listing.Unit.Status,
                    new ListingUnitTypeSummary(
                        listing.Unit.UnitType.Id,
                        listing.Unit.UnitType.Code,
                        listing.Unit.UnitType.NameEn,
                        listing.Unit.UnitType.NameAr),
                    new ListingLocationSummary(
                        listing.Unit.Location.Id,
                        listing.Unit.Location.Type,
                        listing.Unit.Location.NameEn,
                        listing.Unit.Location.NameAr,
                        listing.Unit.Location.Slug),
                    listing.Unit.Project == null
                        ? null
                        : new ListingProjectSummary(
                            listing.Unit.Project.Id,
                            listing.Unit.Project.Slug,
                            listing.Unit.Project.Name,
                            listing.Unit.Project.DeveloperCompany.Slug))))
            .ToListAsync(cancellationToken);

        return new PagedResult<ListingDirectoryItem>(
            items,
            query.PageNumber,
            query.PageSize,
            totalCount);
    }

    public async Task<ListingDetails?> GetListingBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        var utcNow = timeProvider.GetUtcNow();
        var listing = await GetPublicListings(utcNow)
            .Where(candidate => candidate.Slug == slug)
            .Select(candidate => new ListingDetailsHeader(
                candidate.Id,
                candidate.Slug,
                candidate.Title,
                candidate.Description,
                candidate.ListingType,
                candidate.AskingPrice,
                candidate.RentPeriod,
                candidate.PublishedAt,
                new ListingCurrencySummary(
                    candidate.Currency.Code,
                    candidate.Currency.Name,
                    candidate.Currency.Symbol,
                    candidate.Currency.DecimalPlaces),
                new ListingDetailsCompany(
                    candidate.Company.Id,
                    candidate.Company.Slug,
                    candidate.Company.DisplayName,
                    candidate.Company.LogoFileAssetId,
                    candidate.Company.BusinessEmail,
                    candidate.Company.SupportPhone,
                    candidate.Company.Website),
                new ListingDetailsUnit(
                    candidate.Unit.Id,
                    candidate.Unit.Bedrooms,
                    candidate.Unit.Bathrooms,
                    candidate.Unit.FloorNumber,
                    candidate.Unit.TotalFloors,
                    candidate.Unit.BuiltUpArea,
                    candidate.Unit.LandArea,
                    candidate.Unit.FinishingType,
                    candidate.Unit.FurnishedStatus,
                    candidate.Unit.Status,
                    new ListingUnitTypeSummary(
                        candidate.Unit.UnitType.Id,
                        candidate.Unit.UnitType.Code,
                        candidate.Unit.UnitType.NameEn,
                        candidate.Unit.UnitType.NameAr),
                    new ListingLocationSummary(
                        candidate.Unit.Location.Id,
                        candidate.Unit.Location.Type,
                        candidate.Unit.Location.NameEn,
                        candidate.Unit.Location.NameAr,
                        candidate.Unit.Location.Slug),
                    candidate.Unit.Project == null
                        ? null
                        : new ListingDetailsProject(
                            candidate.Unit.Project.Id,
                            candidate.Unit.Project.Slug,
                            candidate.Unit.Project.Name,
                            candidate.Unit.Project.DeliveryStatus,
                            candidate.Unit.Project.ExpectedDeliveryDate,
                            new ListingDetailsProjectDeveloper(
                                candidate.Unit.Project.DeveloperCompany.Id,
                                candidate.Unit.Project.DeveloperCompany.Slug,
                                candidate.Unit.Project.DeveloperCompany.DisplayName,
                                candidate.Unit.Project.DeveloperCompany.LogoFileAssetId))),
                candidate.Unit.Id,
                candidate.Unit.ProjectId))
            .SingleOrDefaultAsync(cancellationToken);

        if (listing is null)
        {
            return null;
        }

        var media = await dbContext.Set<ListingMedia>()
            .AsNoTracking()
            .Where(item => item.ListingId == listing.Id)
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Id)
            .Select(item => new ListingMediaItem(
                item.FileAssetId,
                item.SortOrder,
                item.IsCover,
                item.Caption))
            .ToListAsync(cancellationToken);

        var paymentPlans = await dbContext.Set<PaymentPlan>()
            .AsNoTracking()
            .Where(plan =>
                plan.ListingId == listing.Id
                && plan.IsActive)
            .OrderBy(plan => plan.TotalPrice)
            .ThenBy(plan => plan.DurationMonths)
            .ThenBy(plan => plan.Id)
            .Select(plan => new ListingPaymentPlanItem(
                plan.Id,
                plan.Name,
                plan.TotalPrice,
                new ListingCurrencySummary(
                    plan.Currency.Code,
                    plan.Currency.Name,
                    plan.Currency.Symbol,
                    plan.Currency.DecimalPlaces),
                plan.DownPaymentPercentage,
                plan.DurationMonths,
                plan.InstallmentFrequency,
                plan.CashDiscountPercentage))
            .ToListAsync(cancellationToken);

        var unitAmenities = await dbContext.Set<UnitAmenity>()
            .AsNoTracking()
            .Where(unitAmenity =>
                unitAmenity.UnitId == listing.UnitId
                && unitAmenity.Amenity.IsActive
                && (unitAmenity.Amenity.Scope == AmenityScope.Unit
                    || unitAmenity.Amenity.Scope == AmenityScope.Both))
            .OrderBy(unitAmenity => unitAmenity.Amenity.NameEn)
            .ThenBy(unitAmenity => unitAmenity.Amenity.Id)
            .Select(unitAmenity => new ListingAmenityItem(
                unitAmenity.Amenity.Id,
                unitAmenity.Amenity.Code,
                unitAmenity.Amenity.NameEn,
                unitAmenity.Amenity.NameAr,
                unitAmenity.Amenity.IconKey))
            .ToListAsync(cancellationToken);

        var unitNearbyPlaces = await dbContext.Set<NearbyPlace>()
            .AsNoTracking()
            .Where(place => place.UnitId == listing.UnitId)
            .OrderBy(place => place.Category)
            .ThenBy(place => place.Name)
            .ThenBy(place => place.Id)
            .Select(place => new ListingNearbyPlaceItem(
                place.Id,
                place.Category,
                place.Name,
                place.DistanceMeters,
                place.TravelMinutes,
                place.Latitude,
                place.Longitude))
            .ToListAsync(cancellationToken);

        IReadOnlyList<ListingAmenityItem> projectAmenities =
            Array.Empty<ListingAmenityItem>();
        IReadOnlyList<ListingNearbyPlaceItem> projectNearbyPlaces =
            Array.Empty<ListingNearbyPlaceItem>();

        if (listing.ProjectId is not null)
        {
            projectAmenities = await dbContext.Set<ProjectAmenity>()
                .AsNoTracking()
                .Where(projectAmenity =>
                    projectAmenity.ProjectId == listing.ProjectId.Value
                    && projectAmenity.Amenity.IsActive
                    && (projectAmenity.Amenity.Scope == AmenityScope.Project
                        || projectAmenity.Amenity.Scope == AmenityScope.Both))
                .OrderBy(projectAmenity => projectAmenity.Amenity.NameEn)
                .ThenBy(projectAmenity => projectAmenity.Amenity.Id)
                .Select(projectAmenity => new ListingAmenityItem(
                    projectAmenity.Amenity.Id,
                    projectAmenity.Amenity.Code,
                    projectAmenity.Amenity.NameEn,
                    projectAmenity.Amenity.NameAr,
                    projectAmenity.Amenity.IconKey))
                .ToListAsync(cancellationToken);

            projectNearbyPlaces = await dbContext.Set<NearbyPlace>()
                .AsNoTracking()
                .Where(place => place.ProjectId == listing.ProjectId.Value)
                .OrderBy(place => place.Category)
                .ThenBy(place => place.Name)
                .ThenBy(place => place.Id)
                .Select(place => new ListingNearbyPlaceItem(
                    place.Id,
                    place.Category,
                    place.Name,
                    place.DistanceMeters,
                    place.TravelMinutes,
                    place.Latitude,
                    place.Longitude))
                .ToListAsync(cancellationToken);
        }

        var coverFileAssetId = media
            .Where(item => item.IsCover)
            .Select(item => (Guid?)item.FileAssetId)
            .SingleOrDefault();

        return new ListingDetails(
            listing.Id,
            listing.Slug,
            listing.Title,
            listing.Description,
            listing.ListingType,
            listing.AskingPrice,
            listing.RentPeriod,
            listing.PublishedAt,
            coverFileAssetId,
            listing.Currency,
            listing.Company,
            listing.Unit,
            media,
            paymentPlans,
            unitAmenities,
            projectAmenities,
            unitNearbyPlaces,
            projectNearbyPlaces);
    }

    private IQueryable<Listing> GetPublicListings(DateTimeOffset utcNow)
    {
        return dbContext.Set<Listing>()
            .AsNoTracking()
            .WherePublic(utcNow);
    }

    private static IOrderedQueryable<Listing> ApplyOrdering(
        IQueryable<Listing> listings,
        ListingDirectorySort sort)
    {
        return sort switch
        {
            ListingDirectorySort.Newest => listings
                .OrderByDescending(listing => listing.PublishedAt)
                .ThenByDescending(listing => listing.Id),
            ListingDirectorySort.PriceLowToHigh => listings
                .OrderBy(listing => listing.AskingPrice)
                .ThenBy(listing => listing.Id),
            ListingDirectorySort.PriceHighToLow => listings
                .OrderByDescending(listing => listing.AskingPrice)
                .ThenBy(listing => listing.Id),
            ListingDirectorySort.AreaLowToHigh => listings
                .OrderBy(listing => listing.Unit.BuiltUpArea)
                .ThenBy(listing => listing.Id),
            ListingDirectorySort.AreaHighToLow => listings
                .OrderByDescending(listing => listing.Unit.BuiltUpArea)
                .ThenBy(listing => listing.Id),
            _ => throw new ArgumentOutOfRangeException(
                nameof(sort),
                sort,
                "Unsupported listing directory sort.")
        };
    }

    private sealed record ListingDetailsHeader(
        Guid Id,
        string Slug,
        string Title,
        string Description,
        ListingType ListingType,
        decimal AskingPrice,
        RentPeriod? RentPeriod,
        DateTimeOffset? PublishedAt,
        ListingCurrencySummary Currency,
        ListingDetailsCompany Company,
        ListingDetailsUnit Unit,
        Guid UnitId,
        Guid? ProjectId);
}
