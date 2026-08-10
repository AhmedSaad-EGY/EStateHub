using EstateHub.Application.CatalogLookups;
using EstateHub.Domain.Enums;

namespace EstateHub.Api.Contracts.CatalogLookups;

public sealed record LocationLookupResponse(
    Guid Id,
    Guid? ParentLocationId,
    string Type,
    string NameEn,
    string NameAr,
    string Slug,
    bool HasChildren)
{
    public static LocationLookupResponse From(LocationLookupItem location)
    {
        return new LocationLookupResponse(
            location.Id,
            location.ParentLocationId,
            CatalogLookupEnumText.From(location.Type),
            location.NameEn,
            location.NameAr,
            location.Slug,
            location.HasChildren);
    }
}

public sealed record UnitTypeLookupResponse(
    Guid Id,
    string Code,
    string NameEn,
    string NameAr)
{
    public static UnitTypeLookupResponse From(UnitTypeLookupItem unitType)
    {
        return new UnitTypeLookupResponse(
            unitType.Id,
            unitType.Code,
            unitType.NameEn,
            unitType.NameAr);
    }
}

public sealed record CurrencyLookupResponse(
    string Code,
    string Name,
    string Symbol,
    int DecimalPlaces)
{
    public static CurrencyLookupResponse From(CurrencyLookupItem currency)
    {
        return new CurrencyLookupResponse(
            currency.Code,
            currency.Name,
            currency.Symbol,
            currency.DecimalPlaces);
    }
}

public sealed record AmenityLookupResponse(
    Guid Id,
    string Code,
    string NameEn,
    string NameAr,
    string? IconKey,
    string Scope)
{
    public static AmenityLookupResponse From(AmenityLookupItem amenity)
    {
        return new AmenityLookupResponse(
            amenity.Id,
            amenity.Code,
            amenity.NameEn,
            amenity.NameAr,
            amenity.IconKey,
            CatalogLookupEnumText.From(amenity.Scope));
    }
}

internal static class CatalogLookupEnumText
{
    public static string From(LocationType value)
    {
        return value switch
        {
            LocationType.Country => "Country",
            LocationType.Governorate => "Governorate",
            LocationType.City => "City",
            LocationType.District => "District",
            _ => Unsupported(value)
        };
    }

    public static string From(AmenityScope value)
    {
        return value switch
        {
            AmenityScope.Project => "Project",
            AmenityScope.Unit => "Unit",
            AmenityScope.Both => "Both",
            _ => Unsupported(value)
        };
    }

    private static string Unsupported<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        throw new ArgumentOutOfRangeException(
            nameof(value),
            value,
            $"Unsupported {typeof(TEnum).Name} value.");
    }
}
