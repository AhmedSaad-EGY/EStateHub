using EstateHub.Application.CatalogLookups;
using EstateHub.Domain.Entities.Catalog;
using EstateHub.Domain.Enums;
using EstateHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EstateHub.Infrastructure.CatalogLookups;

public sealed class PublicCatalogLookupService(
    EstateHubDbContext dbContext) : IPublicCatalogLookupService
{
    public async Task<IReadOnlyList<LocationLookupItem>> GetLocationsAsync(
        Guid? parentId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<Location>()
            .AsNoTracking()
            .Where(location =>
                location.IsActive
                && (parentId == null
                    ? location.ParentLocationId == null
                    : location.ParentLocationId == parentId
                        && location.ParentLocation!.IsActive))
            .OrderBy(location => location.NameEn)
            .ThenBy(location => location.Id)
            .Select(location => new LocationLookupItem(
                location.Id,
                location.ParentLocationId,
                location.Type,
                location.NameEn,
                location.NameAr,
                location.Slug,
                location.ChildLocations.Any(child => child.IsActive)))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UnitTypeLookupItem>> GetUnitTypesAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<UnitType>()
            .AsNoTracking()
            .Where(unitType => unitType.IsActive)
            .OrderBy(unitType => unitType.NameEn)
            .ThenBy(unitType => unitType.Id)
            .Select(unitType => new UnitTypeLookupItem(
                unitType.Id,
                unitType.Code,
                unitType.NameEn,
                unitType.NameAr))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CurrencyLookupItem>> GetCurrenciesAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<Currency>()
            .AsNoTracking()
            .Where(currency => currency.IsActive)
            .OrderBy(currency => currency.Code)
            .Select(currency => new CurrencyLookupItem(
                currency.Code,
                currency.Name,
                currency.Symbol,
                currency.DecimalPlaces))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AmenityLookupItem>> GetAmenitiesAsync(
        CatalogAmenityTarget? target,
        CancellationToken cancellationToken = default)
    {
        var amenities = dbContext.Set<Amenity>()
            .AsNoTracking()
            .Where(amenity => amenity.IsActive);

        if (target is CatalogAmenityTarget.Project)
        {
            amenities = amenities.Where(amenity =>
                amenity.Scope == AmenityScope.Project
                || amenity.Scope == AmenityScope.Both);
        }
        else if (target is CatalogAmenityTarget.Unit)
        {
            amenities = amenities.Where(amenity =>
                amenity.Scope == AmenityScope.Unit
                || amenity.Scope == AmenityScope.Both);
        }

        return await amenities
            .OrderBy(amenity => amenity.NameEn)
            .ThenBy(amenity => amenity.Id)
            .Select(amenity => new AmenityLookupItem(
                amenity.Id,
                amenity.Code,
                amenity.NameEn,
                amenity.NameAr,
                amenity.IconKey,
                amenity.Scope))
            .ToListAsync(cancellationToken);
    }
}
