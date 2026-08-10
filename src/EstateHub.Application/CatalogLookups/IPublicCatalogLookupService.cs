namespace EstateHub.Application.CatalogLookups;

public interface IPublicCatalogLookupService
{
    Task<IReadOnlyList<LocationLookupItem>> GetLocationsAsync(
        Guid? parentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UnitTypeLookupItem>> GetUnitTypesAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CurrencyLookupItem>> GetCurrenciesAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AmenityLookupItem>> GetAmenitiesAsync(
        CatalogAmenityTarget? target,
        CancellationToken cancellationToken = default);
}
