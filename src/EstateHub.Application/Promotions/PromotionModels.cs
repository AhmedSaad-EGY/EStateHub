using EstateHub.Application.Listings;
using EstateHub.Domain.Enums;

namespace EstateHub.Application.Promotions;

public sealed record PromotionCurrency(string Code, string Name, string Symbol, int DecimalPlaces);
public sealed record PublicPromotionPackage(Guid Id, string Name, string Placement, int DurationDays, decimal Price, PromotionCurrency Currency);
public sealed record PromotedListingItem(string Placement, DateTimeOffset PromotionStartsAt, DateTimeOffset PromotionEndsAt, ListingDirectoryItem Listing);
public sealed record PromotedListingQuery(string Placement, int PageNumber, int PageSize);
public sealed record CompanyPromotionListingSummary(Guid Id, string ListingCode, string Slug, string Title, ListingType ListingType);
public sealed record CompanyPromotionInvoiceSummary(Guid Id, string InvoiceNumber, BillingInvoiceStatus Status, decimal Total, DateTimeOffset? PaidAt);
public sealed record CompanyListingPromotionDetails(Guid Id, CompanyPromotionListingSummary Listing, Guid PromotionPackageId, string PackageName, decimal AmountSnapshot, string CurrencyCode, string PlacementSnapshot, DateTimeOffset StartsAt, DateTimeOffset EndsAt, ListingPromotionStatus EffectiveStatus, bool IsCurrentlyActive, DateTimeOffset CreatedAt, byte[] RowVersion, CompanyPromotionInvoiceSummary? Invoice);
public sealed record CompanyListingPromotionDirectoryQuery(int PageNumber, int PageSize, Guid? ListingId, ListingPromotionStatus? Status, string? Placement, DateTimeOffset? From, DateTimeOffset? To);
public sealed record CheckoutPromotionCommand(Guid ListingId, Guid PromotionPackageId, DateTimeOffset? StartsAt);
public sealed record PromotionCheckoutPayment(Guid Id, decimal Amount, string CurrencyCode, PaymentProvider Provider, string ProviderReference, PaymentTransactionStatus Status, DateTimeOffset CreatedAt, DateTimeOffset? CompletedAt);
public sealed record PromotionCheckoutResult(CompanyListingPromotionDetails Promotion, CompanyPromotionInvoiceSummary Invoice, PromotionCheckoutPayment? PaymentTransaction);
public enum PromotionOperationStatus { Succeeded, NotFound, InvalidRequest, Conflict, ServiceUnavailable }
public sealed record PromotionResult<T>(PromotionOperationStatus Status, T? Value = default);
