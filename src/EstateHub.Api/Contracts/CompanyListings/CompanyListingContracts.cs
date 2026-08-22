using EstateHub.Application.Common;
using EstateHub.Application.CompanyListings;
using EstateHub.Domain.Enums;

namespace EstateHub.Api.Contracts.CompanyListings;

public sealed class CompanyListingDirectoryRequest
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string? PublicationStatus { get; init; }
    public string? ListingType { get; init; }
    public Guid? UnitId { get; init; }
    public Guid? ProjectId { get; init; }
    public string? CurrencyCode { get; init; }
}

public sealed record CreateCompanyListingRequest(Guid UnitId, string? CurrencyCode, string? ListingCode, string? Slug, string? Title, string? Description, string? ListingType, decimal AskingPrice, string? RentPeriod);
public sealed record UpdateCompanyListingRequest(Guid UnitId, string? CurrencyCode, string? ListingCode, string? Slug, string? Title, string? Description, string? ListingType, decimal AskingPrice, string? RentPeriod, string? RowVersion, string? PriceChangeReason);
public sealed record CompanyListingMediaRequest(Guid FileAssetId, int SortOrder, bool IsCover, string? Caption);
public sealed record ReplaceCompanyListingMediaRequest(string? RowVersion, IReadOnlyList<CompanyListingMediaRequest>? Items);
public sealed record CreateCompanyListingPaymentPlanRequest(string? Name, decimal TotalPrice, string? CurrencyCode, decimal DownPaymentPercentage, int DurationMonths, string? InstallmentFrequency, decimal? CashDiscountPercentage, bool? IsActive, string? RowVersion);
public sealed record UpdateCompanyListingPaymentPlanRequest(string? Name, decimal TotalPrice, string? CurrencyCode, decimal DownPaymentPercentage, int DurationMonths, string? InstallmentFrequency, decimal? CashDiscountPercentage, bool? IsActive, string? RowVersion);
public sealed record CompanyListingLifecycleRequest(string? RowVersion);

public sealed record CompanyListingCurrencyResponse(string Code, string Name, string Symbol, int DecimalPlaces)
{
    public static CompanyListingCurrencyResponse From(CompanyListingCurrencySummary value) => new(value.Code, value.Name, value.Symbol, value.DecimalPlaces);
}

public sealed record CompanyListingLocationResponse(Guid Id, string Type, string NameEn, string NameAr, string Slug)
{
    public static CompanyListingLocationResponse From(CompanyListingLocationSummary value) => new(value.Id, Text(value.Type), value.NameEn, value.NameAr, value.Slug);
    private static string Text(LocationType value) => value switch
    {
        LocationType.Country => "Country", LocationType.Governorate => "Governorate", LocationType.City => "City", LocationType.District => "District",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };
}

public sealed record CompanyListingUnitTypeResponse(Guid Id, string Code, string NameEn, string NameAr)
{
    public static CompanyListingUnitTypeResponse From(CompanyListingUnitTypeSummary value) => new(value.Id, value.Code, value.NameEn, value.NameAr);
}

public sealed record CompanyListingProjectResponse(Guid Id, string Slug, string Name, string Status)
{
    public static CompanyListingProjectResponse From(CompanyListingProjectSummary value) => new(value.Id, value.Slug, value.Name, Text(value.Status));
    private static string Text(ProjectStatus value) => value switch
    {
        ProjectStatus.Draft => "Draft", ProjectStatus.Published => "Published", ProjectStatus.Archived => "Archived",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };
}

public sealed record CompanyListingUnitResponse(Guid Id, string UnitCode, string Status, CompanyListingUnitTypeResponse UnitType, CompanyListingLocationResponse Location, CompanyListingProjectResponse? Project)
{
    public static CompanyListingUnitResponse From(CompanyListingUnitSummary value) => new(value.Id, value.UnitCode, Text(value.Status), CompanyListingUnitTypeResponse.From(value.UnitType), CompanyListingLocationResponse.From(value.Location), value.Project is null ? null : CompanyListingProjectResponse.From(value.Project));
    private static string Text(UnitStatus value) => value switch
    {
        UnitStatus.Available => "Available", UnitStatus.Reserved => "Reserved", UnitStatus.Sold => "Sold", UnitStatus.Rented => "Rented", UnitStatus.Withdrawn => "Withdrawn",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };
}

public sealed record CompanyListingListItemResponse(Guid Id, Guid UnitId, string ListingCode, string Slug, string Title, string ListingType, decimal AskingPrice, CompanyListingCurrencyResponse Currency, string? RentPeriod, string PublicationStatus, DateTimeOffset? PublishedAt, DateTimeOffset? ArchivedAt, Guid? CoverFileAssetId, CompanyListingUnitResponse Unit, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, string RowVersion)
{
    public static CompanyListingListItemResponse From(CompanyListingSummary value) => new(value.Id, value.UnitId, value.ListingCode, value.Slug, value.Title, Text(value.ListingType), value.AskingPrice, CompanyListingCurrencyResponse.From(value.Currency), value.RentPeriod is null ? null : Text(value.RentPeriod.Value), Text(value.PublicationStatus), value.PublishedAt, value.ArchivedAt, value.CoverFileAssetId, CompanyListingUnitResponse.From(value.Unit), value.CreatedAt, value.UpdatedAt, Convert.ToBase64String(value.RowVersion));
    internal static string Text(global::EstateHub.Domain.Enums.ListingType value) => value switch { global::EstateHub.Domain.Enums.ListingType.Sale => "Sale", global::EstateHub.Domain.Enums.ListingType.Rent => "Rent", _ => throw new ArgumentOutOfRangeException(nameof(value), value, null) };
    internal static string Text(global::EstateHub.Domain.Enums.RentPeriod value) => value switch { global::EstateHub.Domain.Enums.RentPeriod.Monthly => "Monthly", global::EstateHub.Domain.Enums.RentPeriod.Yearly => "Yearly", _ => throw new ArgumentOutOfRangeException(nameof(value), value, null) };
    internal static string Text(ListingPublicationStatus value) => value switch { ListingPublicationStatus.Draft => "Draft", ListingPublicationStatus.Pending => "Pending", ListingPublicationStatus.Published => "Published", ListingPublicationStatus.Archived => "Archived", _ => throw new ArgumentOutOfRangeException(nameof(value), value, null) };
}

public sealed record CompanyListingsResponse(IReadOnlyList<CompanyListingListItemResponse> Items, int PageNumber, int PageSize, int TotalCount, int TotalPages, bool HasPreviousPage, bool HasNextPage)
{
    public static CompanyListingsResponse From(PagedResult<CompanyListingSummary> value) => new(value.Items.Select(CompanyListingListItemResponse.From).ToList(), value.PageNumber, value.PageSize, value.TotalCount, value.TotalPages, value.HasPreviousPage, value.HasNextPage);
}

public sealed record CompanyListingCreatedByEmployeeResponse(Guid Id, string FullName)
{
    public static CompanyListingCreatedByEmployeeResponse From(CompanyListingCreatedByEmployeeSummary value) => new(value.Id, value.FullName);
}

public sealed record CompanyListingPriceHistoryResponse(Guid Id, decimal Price, CompanyListingCurrencyResponse Currency, DateTimeOffset EffectiveFrom, DateTimeOffset? EffectiveTo, string? ChangeReason)
{
    public static CompanyListingPriceHistoryResponse From(CompanyListingPriceHistoryItem value) => new(value.Id, value.Price, CompanyListingCurrencyResponse.From(value.Currency), value.EffectiveFrom, value.EffectiveTo, value.ChangeReason);
}

public sealed record CompanyListingMediaResponse(Guid FileAssetId, int SortOrder, bool IsCover, string? Caption)
{
    public static CompanyListingMediaResponse From(CompanyListingMediaItem value) => new(value.FileAssetId, value.SortOrder, value.IsCover, value.Caption);
}

public sealed record ReplaceCompanyListingMediaResponse(string RowVersion, IReadOnlyList<CompanyListingMediaResponse> Items);

public sealed record CompanyListingPaymentPlanResponse(Guid Id, string Name, decimal TotalPrice, CompanyListingCurrencyResponse Currency, decimal DownPaymentPercentage, int DurationMonths, string InstallmentFrequency, decimal? CashDiscountPercentage, bool IsActive)
{
    public static CompanyListingPaymentPlanResponse From(CompanyListingPaymentPlanItem value) => new(value.Id, value.Name, value.TotalPrice, CompanyListingCurrencyResponse.From(value.Currency), value.DownPaymentPercentage, value.DurationMonths, Text(value.InstallmentFrequency), value.CashDiscountPercentage, value.IsActive);
    private static string Text(global::EstateHub.Domain.Enums.InstallmentFrequency value) => value switch
    {
        global::EstateHub.Domain.Enums.InstallmentFrequency.Monthly => "Monthly", global::EstateHub.Domain.Enums.InstallmentFrequency.Quarterly => "Quarterly", global::EstateHub.Domain.Enums.InstallmentFrequency.SemiAnnual => "SemiAnnual", global::EstateHub.Domain.Enums.InstallmentFrequency.Annual => "Annual",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };
}

public sealed record CompanyListingPaymentPlanMutationResponse(string RowVersion, CompanyListingPaymentPlanResponse PaymentPlan);

public sealed record CompanyListingDetailsResponse(Guid Id, Guid UnitId, string ListingCode, string Slug, string Title, string Description, string ListingType, decimal AskingPrice, CompanyListingCurrencyResponse Currency, string? RentPeriod, string PublicationStatus, DateTimeOffset? PublishedAt, DateTimeOffset? ArchivedAt, Guid? CoverFileAssetId, CompanyListingUnitResponse Unit, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, string RowVersion, CompanyListingCreatedByEmployeeResponse CreatedByEmployee, IReadOnlyList<CompanyListingPriceHistoryResponse> PriceHistory, IReadOnlyList<CompanyListingMediaResponse> Media, IReadOnlyList<CompanyListingPaymentPlanResponse> PaymentPlans)
{
    public static CompanyListingDetailsResponse From(CompanyListingDetails value)
    {
        var header = CompanyListingListItemResponse.From(value.Header);
        return new(header.Id, header.UnitId, header.ListingCode, header.Slug, header.Title, value.Description, header.ListingType, header.AskingPrice, header.Currency, header.RentPeriod, header.PublicationStatus, header.PublishedAt, header.ArchivedAt, header.CoverFileAssetId, header.Unit, header.CreatedAt, header.UpdatedAt, header.RowVersion, CompanyListingCreatedByEmployeeResponse.From(value.CreatedByEmployee), value.PriceHistory.Select(CompanyListingPriceHistoryResponse.From).ToList(), value.Media.Select(CompanyListingMediaResponse.From).ToList(), value.PaymentPlans.Select(CompanyListingPaymentPlanResponse.From).ToList());
    }
}
