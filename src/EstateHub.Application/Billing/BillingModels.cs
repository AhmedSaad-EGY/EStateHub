using EstateHub.Application.Common;
using EstateHub.Domain.Enums;

namespace EstateHub.Application.Billing;

public sealed record InvoiceDirectoryQuery(int PageNumber, int PageSize, BillingInvoiceStatus? Status, BillingLineType? LineType, string? CurrencyCode, DateTimeOffset? CreatedFrom, DateTimeOffset? CreatedTo, string? Search);
public sealed record BillingInvoiceSummary(Guid Id, string InvoiceNumber, BillingInvoiceStatus Status, string CurrencyCode, DateTimeOffset? PeriodStart, DateTimeOffset? PeriodEnd, decimal Subtotal, decimal Total, DateTimeOffset? IssuedAt, DateTimeOffset? DueAt, DateTimeOffset? PaidAt, DateTimeOffset CreatedAt, int LineCount, int PaymentCount, byte[] RowVersion);
public sealed record BillingSubscriptionSource(Guid CompanySubscriptionId, Guid SubscriptionPlanId, string PlanName);
public sealed record BillingBookingChargeSource(Guid BookingChargeId, Guid ViewingBookingId, string BookingCode);
public sealed record BillingPromotionListingSummary(Guid Id, string Slug, string Title);
public sealed record BillingPromotionSource(Guid ListingPromotionId, BillingPromotionListingSummary Listing);
public sealed record BillingInvoiceLineItem(Guid Id, BillingLineType LineType, string DescriptionSnapshot, decimal Quantity, decimal UnitAmount, decimal LineTotal, Guid? CompanySubscriptionId, Guid? BookingChargeId, Guid? ListingPromotionId, BillingSubscriptionSource? Subscription, BillingBookingChargeSource? BookingCharge, BillingPromotionSource? Promotion);
public sealed record BillingPaymentItem(Guid Id, Guid BillingInvoiceId, string InvoiceNumber, PaymentProvider Provider, string ProviderReference, PaymentTransactionStatus Status, decimal Amount, string CurrencyCode, DateTimeOffset CreatedAt, DateTimeOffset? CompletedAt, string? FailureReason);
public sealed record BillingInvoiceDetails(BillingInvoiceSummary Invoice, IReadOnlyList<BillingInvoiceLineItem> Lines, IReadOnlyList<BillingPaymentItem> PaymentTransactions);
public sealed record BookingChargeDirectoryQuery(int PageNumber, int PageSize, BookingChargeStatus? Status, string? CurrencyCode, DateTimeOffset? CreatedFrom, DateTimeOffset? CreatedTo, string? Search);
public sealed record BookingChargeDirectoryItem(Guid Id, Guid ViewingBookingId, string BookingCode, Guid ListingId, string ListingSlug, string ListingTitle, string CustomerDisplayName, decimal AmountSnapshot, string CurrencyCode, BookingChargeStatus Status, DateTimeOffset CreatedAt, DateTimeOffset? VoidedAt, string? VoidReason, Guid? InvoiceId, string? InvoiceNumber);
public sealed record PaymentDirectoryQuery(int PageNumber, int PageSize, PaymentTransactionStatus? Status, PaymentProvider? Provider, string? CurrencyCode, DateTimeOffset? CreatedFrom, DateTimeOffset? CreatedTo);
public sealed record BookingChargeSelection(Guid Id, Guid ViewingBookingId, string BookingCode, decimal AmountSnapshot, string CurrencyCode);
public sealed record BookingChargePreview(string CurrencyCode, int ChargeCount, decimal Subtotal, decimal Total, IReadOnlyList<BookingChargeSelection> Items);
public enum BillingOperationStatus { Succeeded, NotFound, Conflict, ServiceUnavailable }
public sealed record BillingResult<T>(BillingOperationStatus Status, T? Value = default);
