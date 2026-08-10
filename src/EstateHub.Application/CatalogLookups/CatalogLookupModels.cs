using EstateHub.Domain.Enums;

namespace EstateHub.Application.CatalogLookups;

public enum CatalogAmenityTarget
{
    Project,
    Unit
}

public sealed record LocationLookupItem(
    Guid Id,
    Guid? ParentLocationId,
    LocationType Type,
    string NameEn,
    string NameAr,
    string Slug,
    bool HasChildren);

public sealed record UnitTypeLookupItem(
    Guid Id,
    string Code,
    string NameEn,
    string NameAr);

public sealed record CurrencyLookupItem(
    string Code,
    string Name,
    string Symbol,
    int DecimalPlaces);

public sealed record AmenityLookupItem(
    Guid Id,
    string Code,
    string NameEn,
    string NameAr,
    string? IconKey,
    AmenityScope Scope);
