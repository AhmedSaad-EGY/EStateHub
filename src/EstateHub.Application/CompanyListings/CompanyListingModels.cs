using EstateHub.Application.Common;
using EstateHub.Domain.Enums;

namespace EstateHub.Application.CompanyListings;

public sealed record CompanyListingCurrencySummary(string Code, string Name, string Symbol, int DecimalPlaces);
public sealed record CompanyListingLocationSummary(Guid Id, LocationType Type, string NameEn, string NameAr, string Slug);
public sealed record CompanyListingUnitTypeSummary(Guid Id, string Code, string NameEn, string NameAr);
public sealed record CompanyListingProjectSummary(Guid Id, string Slug, string Name, ProjectStatus Status);
public sealed record CompanyListingUnitSummary(Guid Id, string UnitCode, UnitStatus Status, CompanyListingUnitTypeSummary UnitType, CompanyListingLocationSummary Location, CompanyListingProjectSummary? Project);
public sealed record CompanyListingCreatedByEmployeeSummary(Guid Id, string FullName);
public sealed record CompanyListingPriceHistoryItem(Guid Id, decimal Price, CompanyListingCurrencySummary Currency, DateTimeOffset EffectiveFrom, DateTimeOffset? EffectiveTo, string? ChangeReason);
public sealed record CompanyListingMediaItem(Guid FileAssetId, int SortOrder, bool IsCover, string? Caption);
public sealed record CompanyListingPaymentPlanItem(Guid Id, string Name, decimal TotalPrice, CompanyListingCurrencySummary Currency, decimal DownPaymentPercentage, int DurationMonths, InstallmentFrequency InstallmentFrequency, decimal? CashDiscountPercentage, bool IsActive);
public sealed record CompanyListingSummary(Guid Id, Guid UnitId, Guid? ProjectId, string ListingCode, string Slug, string Title, ListingType ListingType, decimal AskingPrice, CompanyListingCurrencySummary Currency, RentPeriod? RentPeriod, ListingPublicationStatus PublicationStatus, DateTimeOffset? PublishedAt, DateTimeOffset? ArchivedAt, Guid? CoverFileAssetId, CompanyListingUnitSummary Unit, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, byte[] RowVersion);
public sealed record CompanyListingDetails(CompanyListingSummary Header, string Description, CompanyListingCreatedByEmployeeSummary CreatedByEmployee, IReadOnlyList<CompanyListingPriceHistoryItem> PriceHistory, IReadOnlyList<CompanyListingMediaItem> Media, IReadOnlyList<CompanyListingPaymentPlanItem> PaymentPlans);
public sealed record CompanyListingDirectoryQuery(int PageNumber, int PageSize, string? Search, ListingPublicationStatus? PublicationStatus, ListingType? ListingType, Guid? UnitId, Guid? ProjectId, string? CurrencyCode);
public sealed record CompanyListingCommand(Guid UnitId, string CurrencyCode, string ListingCode, string Slug, string Title, string Description, ListingType ListingType, decimal AskingPrice, RentPeriod? RentPeriod, byte[]? RowVersion, string? PriceChangeReason);
public enum CompanyListingOperationStatus { Succeeded, NotFound, Conflict, InvalidReference, InvalidRequest, ServiceUnavailable }
public sealed record CompanyListingMutationResult(CompanyListingOperationStatus Status, CompanyListingDetails? Listing = null);
public sealed record CompanyListingMediaResult(CompanyListingOperationStatus Status, byte[]? RowVersion = null, IReadOnlyList<CompanyListingMediaItem>? Items = null);
public sealed record CompanyListingPaymentPlanCommand(string Name, decimal TotalPrice, string CurrencyCode, decimal DownPaymentPercentage, int DurationMonths, InstallmentFrequency InstallmentFrequency, decimal? CashDiscountPercentage, bool IsActive);
public sealed record CompanyListingPaymentPlanResult(CompanyListingOperationStatus Status, byte[]? RowVersion = null, CompanyListingPaymentPlanItem? PaymentPlan = null);
