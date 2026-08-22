using EstateHub.Application.Common;
using EstateHub.Application.Listings;
using EstateHub.Application.Promotions;
using EstateHub.Domain.Enums;
using EstateHub.Api.Contracts.Listings;

namespace EstateHub.Api.Contracts.Promotions;

public sealed record PromotionPackageCurrencyResponse(string Code, string Name, string Symbol, int DecimalPlaces);
public sealed record PromotionPackageResponse(Guid Id, string Name, string Placement, int DurationDays, decimal Price, PromotionPackageCurrencyResponse Currency)
{
    public static PromotionPackageResponse From(PublicPromotionPackage package) => new(package.Id, package.Name, package.Placement, package.DurationDays, package.Price, new PromotionPackageCurrencyResponse(package.Currency.Code, package.Currency.Name, package.Currency.Symbol, package.Currency.DecimalPlaces));
}

public sealed record PromotedListingResponse(string Placement, DateTimeOffset PromotionStartsAt, DateTimeOffset PromotionEndsAt, ListingDirectoryItemResponse Listing)
{
    public static PromotedListingResponse From(PromotedListingItem item) => new(item.Placement, item.PromotionStartsAt, item.PromotionEndsAt, ListingDirectoryItemResponse.From(item.Listing));
}

public sealed record PromotedListingsResponse(IReadOnlyList<PromotedListingResponse> Items, int PageNumber, int PageSize, int TotalCount, int TotalPages, bool HasPreviousPage, bool HasNextPage)
{
    public static PromotedListingsResponse From(PagedResult<PromotedListingItem> result) => new(result.Items.Select(PromotedListingResponse.From).ToList(), result.PageNumber, result.PageSize, result.TotalCount, result.TotalPages, result.HasPreviousPage, result.HasNextPage);
}

public sealed record CompanyPromotionListingResponse(Guid Id, string ListingCode, string Slug, string Title, string ListingType)
{
    public static CompanyPromotionListingResponse From(CompanyPromotionListingSummary listing) => new(listing.Id, listing.ListingCode, listing.Slug, listing.Title, PromotionEnumText.ListingType(listing.ListingType));
}

public sealed record CompanyPromotionInvoiceResponse(Guid Id, string InvoiceNumber, string Status, decimal Total, DateTimeOffset? PaidAt)
{
    public static CompanyPromotionInvoiceResponse From(CompanyPromotionInvoiceSummary invoice) => new(invoice.Id, invoice.InvoiceNumber, PromotionEnumText.BillingInvoiceStatus(invoice.Status), invoice.Total, invoice.PaidAt);
}

public sealed record CompanyListingPromotionResponse(Guid Id, CompanyPromotionListingResponse Listing, Guid PromotionPackageId, string PackageName, decimal AmountSnapshot, string CurrencyCode, string PlacementSnapshot, DateTimeOffset StartsAt, DateTimeOffset EndsAt, string EffectiveStatus, bool IsCurrentlyActive, DateTimeOffset CreatedAt, string RowVersion, CompanyPromotionInvoiceResponse Invoice)
{
    public static CompanyListingPromotionResponse From(CompanyListingPromotionDetails promotion) => new(promotion.Id, CompanyPromotionListingResponse.From(promotion.Listing), promotion.PromotionPackageId, promotion.PackageName, promotion.AmountSnapshot, promotion.CurrencyCode, promotion.PlacementSnapshot, promotion.StartsAt, promotion.EndsAt, PromotionEnumText.ListingPromotionStatus(promotion.EffectiveStatus), promotion.IsCurrentlyActive, promotion.CreatedAt, Convert.ToBase64String(promotion.RowVersion), CompanyPromotionInvoiceResponse.From(promotion.Invoice!));
}

public sealed record CompanyListingPromotionsResponse(IReadOnlyList<CompanyListingPromotionResponse> Items, int PageNumber, int PageSize, int TotalCount, int TotalPages, bool HasPreviousPage, bool HasNextPage)
{
    public static CompanyListingPromotionsResponse From(PagedResult<CompanyListingPromotionDetails> result) => new(result.Items.Select(CompanyListingPromotionResponse.From).ToList(), result.PageNumber, result.PageSize, result.TotalCount, result.TotalPages, result.HasPreviousPage, result.HasNextPage);
}

public sealed record CheckoutPromotionRequest(Guid ListingId, Guid PromotionPackageId, DateTimeOffset? StartsAt);
public sealed record PromotionCancellationRequest(string? RowVersion, string? Reason);
public sealed record PromotionPaymentTransactionResponse(Guid Id, decimal Amount, string CurrencyCode, string Provider, string ProviderReference, string Status, DateTimeOffset CreatedAt, DateTimeOffset? CompletedAt)
{
    public static PromotionPaymentTransactionResponse From(PromotionCheckoutPayment payment) => new(payment.Id, payment.Amount, payment.CurrencyCode, PromotionEnumText.PaymentProvider(payment.Provider), payment.ProviderReference, PromotionEnumText.PaymentTransactionStatus(payment.Status), payment.CreatedAt, payment.CompletedAt);
}
public sealed record PromotionCheckoutResponse(CompanyListingPromotionResponse Promotion, CompanyPromotionInvoiceResponse Invoice, PromotionPaymentTransactionResponse? PaymentTransaction)
{
    public static PromotionCheckoutResponse From(PromotionCheckoutResult result) => new(CompanyListingPromotionResponse.From(result.Promotion), CompanyPromotionInvoiceResponse.From(result.Invoice), result.PaymentTransaction is null ? null : PromotionPaymentTransactionResponse.From(result.PaymentTransaction));
}
public sealed record CompanyListingPromotionDirectoryRequest(int PageNumber = 1, int PageSize = 20, Guid? ListingId = null, string? Status = null, string? Placement = null, DateTimeOffset? From = null, DateTimeOffset? To = null);

internal static class PromotionEnumText
{
    public static string ListingPromotionStatus(EstateHub.Domain.Enums.ListingPromotionStatus value) => value switch { EstateHub.Domain.Enums.ListingPromotionStatus.Scheduled => "Scheduled", EstateHub.Domain.Enums.ListingPromotionStatus.Active => "Active", EstateHub.Domain.Enums.ListingPromotionStatus.Completed => "Completed", EstateHub.Domain.Enums.ListingPromotionStatus.Cancelled => "Cancelled", _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown listing promotion status.") };
    public static string BillingInvoiceStatus(EstateHub.Domain.Enums.BillingInvoiceStatus value) => value switch { EstateHub.Domain.Enums.BillingInvoiceStatus.Draft => "Draft", EstateHub.Domain.Enums.BillingInvoiceStatus.Open => "Open", EstateHub.Domain.Enums.BillingInvoiceStatus.Paid => "Paid", EstateHub.Domain.Enums.BillingInvoiceStatus.Void => "Void", _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown invoice status.") };
    public static string PaymentProvider(EstateHub.Domain.Enums.PaymentProvider value) => value switch { EstateHub.Domain.Enums.PaymentProvider.Fake => "Fake", _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown payment provider.") };
    public static string PaymentTransactionStatus(EstateHub.Domain.Enums.PaymentTransactionStatus value) => value switch { EstateHub.Domain.Enums.PaymentTransactionStatus.Pending => "Pending", EstateHub.Domain.Enums.PaymentTransactionStatus.Succeeded => "Succeeded", EstateHub.Domain.Enums.PaymentTransactionStatus.Failed => "Failed", _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown payment status.") };
    public static string ListingType(EstateHub.Domain.Enums.ListingType value) => value switch { EstateHub.Domain.Enums.ListingType.Sale => "Sale", EstateHub.Domain.Enums.ListingType.Rent => "Rent", _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown listing type.") };
}
