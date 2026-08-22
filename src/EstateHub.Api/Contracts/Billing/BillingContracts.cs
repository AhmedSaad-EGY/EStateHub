using EstateHub.Application.Billing;
using EstateHub.Application.Common;
using EstateHub.Domain.Enums;

namespace EstateHub.Api.Contracts.Billing;

public sealed record InvoiceDirectoryRequest(int PageNumber = 1, int PageSize = 20, string? Status = null, string? LineType = null, string? CurrencyCode = null, DateTimeOffset? CreatedFrom = null, DateTimeOffset? CreatedTo = null, string? Search = null);
public sealed record BookingChargeDirectoryRequest(int PageNumber = 1, int PageSize = 20, string? Status = null, string? CurrencyCode = null, DateTimeOffset? CreatedFrom = null, DateTimeOffset? CreatedTo = null, string? Search = null);
public sealed record PaymentTransactionDirectoryRequest(int PageNumber = 1, int PageSize = 20, string? Status = null, string? Provider = null, string? CurrencyCode = null, DateTimeOffset? CreatedFrom = null, DateTimeOffset? CreatedTo = null);
public sealed record BookingChargeSelectionRequest(IReadOnlyList<Guid>? BookingChargeIds);

public sealed record BillingInvoiceSummaryResponse(Guid Id, string InvoiceNumber, string Status, string CurrencyCode, DateTimeOffset? PeriodStart, DateTimeOffset? PeriodEnd, decimal Subtotal, decimal Total, DateTimeOffset? IssuedAt, DateTimeOffset? DueAt, DateTimeOffset? PaidAt, DateTimeOffset CreatedAt, int LineCount, int PaymentCount, string RowVersion)
{
    public static BillingInvoiceSummaryResponse From(BillingInvoiceSummary item) => new(item.Id, item.InvoiceNumber, BillingEnumText.From(item.Status), item.CurrencyCode, item.PeriodStart, item.PeriodEnd, item.Subtotal, item.Total, item.IssuedAt, item.DueAt, item.PaidAt, item.CreatedAt, item.LineCount, item.PaymentCount, Convert.ToBase64String(item.RowVersion));
}
public sealed record BillingInvoiceDirectoryResponse(IReadOnlyList<BillingInvoiceSummaryResponse> Items, int PageNumber, int PageSize, int TotalCount, int TotalPages, bool HasPreviousPage, bool HasNextPage)
{
    public static BillingInvoiceDirectoryResponse From(PagedResult<BillingInvoiceSummary> result) => new(result.Items.Select(BillingInvoiceSummaryResponse.From).ToList(), result.PageNumber, result.PageSize, result.TotalCount, result.TotalPages, result.HasPreviousPage, result.HasNextPage);
}

public sealed record BillingSubscriptionSourceResponse(Guid CompanySubscriptionId, Guid SubscriptionPlanId, string PlanName);
public sealed record BillingBookingChargeSourceResponse(Guid BookingChargeId, Guid ViewingBookingId, string BookingCode);
public sealed record BillingPromotionListingResponse(Guid Id, string Slug, string Title);
public sealed record BillingPromotionSourceResponse(Guid ListingPromotionId, BillingPromotionListingResponse Listing);
public sealed record BillingInvoiceLineResponse(Guid Id, string LineType, string DescriptionSnapshot, decimal Quantity, decimal UnitAmount, decimal LineTotal, Guid? CompanySubscriptionId, Guid? BookingChargeId, Guid? ListingPromotionId, BillingSubscriptionSourceResponse? Subscription, BillingBookingChargeSourceResponse? BookingCharge, BillingPromotionSourceResponse? Promotion)
{
    public static BillingInvoiceLineResponse From(BillingInvoiceLineItem item) => new(item.Id, BillingEnumText.From(item.LineType), item.DescriptionSnapshot, item.Quantity, item.UnitAmount, item.LineTotal, item.CompanySubscriptionId, item.BookingChargeId, item.ListingPromotionId,
        item.Subscription is null ? null : new BillingSubscriptionSourceResponse(item.Subscription.CompanySubscriptionId, item.Subscription.SubscriptionPlanId, item.Subscription.PlanName),
        item.BookingCharge is null ? null : new BillingBookingChargeSourceResponse(item.BookingCharge.BookingChargeId, item.BookingCharge.ViewingBookingId, item.BookingCharge.BookingCode),
        item.Promotion is null ? null : new BillingPromotionSourceResponse(item.Promotion.ListingPromotionId, new BillingPromotionListingResponse(item.Promotion.Listing.Id, item.Promotion.Listing.Slug, item.Promotion.Listing.Title)));
}
public sealed record BillingPaymentResponse(Guid Id, Guid BillingInvoiceId, string InvoiceNumber, string Provider, string ProviderReference, string Status, decimal Amount, string CurrencyCode, DateTimeOffset CreatedAt, DateTimeOffset? CompletedAt, string? FailureReason)
{
    public static BillingPaymentResponse From(BillingPaymentItem item) => new(item.Id, item.BillingInvoiceId, item.InvoiceNumber, BillingEnumText.From(item.Provider), item.ProviderReference, BillingEnumText.From(item.Status), item.Amount, item.CurrencyCode, item.CreatedAt, item.CompletedAt, item.FailureReason);
}
public sealed record BillingInvoiceDetailsResponse(BillingInvoiceSummaryResponse Invoice, IReadOnlyList<BillingInvoiceLineResponse> Lines, IReadOnlyList<BillingPaymentResponse> PaymentTransactions)
{
    public static BillingInvoiceDetailsResponse From(BillingInvoiceDetails item) => new(BillingInvoiceSummaryResponse.From(item.Invoice), item.Lines.Select(BillingInvoiceLineResponse.From).ToList(), item.PaymentTransactions.Select(BillingPaymentResponse.From).ToList());
}

public sealed record BookingChargeResponse(Guid Id, Guid ViewingBookingId, string BookingCode, Guid ListingId, string ListingSlug, string ListingTitle, string CustomerDisplayName, decimal AmountSnapshot, string CurrencyCode, string Status, DateTimeOffset CreatedAt, DateTimeOffset? VoidedAt, string? VoidReason, Guid? InvoiceId, string? InvoiceNumber)
{
    public static BookingChargeResponse From(BookingChargeDirectoryItem item) => new(item.Id, item.ViewingBookingId, item.BookingCode, item.ListingId, item.ListingSlug, item.ListingTitle, item.CustomerDisplayName, item.AmountSnapshot, item.CurrencyCode, BillingEnumText.From(item.Status), item.CreatedAt, item.VoidedAt, item.VoidReason, item.InvoiceId, item.InvoiceNumber);
}
public sealed record BookingChargeDirectoryResponse(IReadOnlyList<BookingChargeResponse> Items, int PageNumber, int PageSize, int TotalCount, int TotalPages, bool HasPreviousPage, bool HasNextPage)
{
    public static BookingChargeDirectoryResponse From(PagedResult<BookingChargeDirectoryItem> result) => new(result.Items.Select(BookingChargeResponse.From).ToList(), result.PageNumber, result.PageSize, result.TotalCount, result.TotalPages, result.HasPreviousPage, result.HasNextPage);
}
public sealed record PaymentTransactionDirectoryResponse(IReadOnlyList<BillingPaymentResponse> Items, int PageNumber, int PageSize, int TotalCount, int TotalPages, bool HasPreviousPage, bool HasNextPage)
{
    public static PaymentTransactionDirectoryResponse From(PagedResult<BillingPaymentItem> result) => new(result.Items.Select(BillingPaymentResponse.From).ToList(), result.PageNumber, result.PageSize, result.TotalCount, result.TotalPages, result.HasPreviousPage, result.HasNextPage);
}
public sealed record BookingChargePreviewItemResponse(Guid Id, Guid ViewingBookingId, string BookingCode, decimal AmountSnapshot, string CurrencyCode);
public sealed record BookingChargePreviewResponse(string CurrencyCode, int ChargeCount, decimal Subtotal, decimal Total, bool RequiresCheckoutRevalidation, IReadOnlyList<BookingChargePreviewItemResponse> Items)
{
    public static BookingChargePreviewResponse From(BookingChargePreview preview) => new(preview.CurrencyCode, preview.ChargeCount, preview.Subtotal, preview.Total, true, preview.Items.Select(item => new BookingChargePreviewItemResponse(item.Id, item.ViewingBookingId, item.BookingCode, item.AmountSnapshot, item.CurrencyCode)).ToList());
}

internal static class BillingEnumText
{
    public static string From(BillingInvoiceStatus value) => value switch { BillingInvoiceStatus.Draft => "Draft", BillingInvoiceStatus.Open => "Open", BillingInvoiceStatus.Paid => "Paid", BillingInvoiceStatus.Void => "Void", _ => Unsupported(value) };
    public static string From(BillingLineType value) => value switch { BillingLineType.Subscription => "Subscription", BillingLineType.BookingCharge => "BookingCharge", BillingLineType.Promotion => "Promotion", _ => Unsupported(value) };
    public static string From(BookingChargeStatus value) => value switch { BookingChargeStatus.Billable => "Billable", BookingChargeStatus.Invoiced => "Invoiced", BookingChargeStatus.Void => "Void", _ => Unsupported(value) };
    public static string From(PaymentProvider value) => value switch { PaymentProvider.Fake => "Fake", _ => Unsupported(value) };
    public static string From(PaymentTransactionStatus value) => value switch { PaymentTransactionStatus.Pending => "Pending", PaymentTransactionStatus.Succeeded => "Succeeded", PaymentTransactionStatus.Failed => "Failed", _ => Unsupported(value) };
    private static string Unsupported<T>(T value) where T : struct, Enum => throw new ArgumentOutOfRangeException(nameof(value), value, $"Unknown {typeof(T).Name} value.");
}
