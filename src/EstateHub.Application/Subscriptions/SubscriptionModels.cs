using EstateHub.Domain.Enums;

namespace EstateHub.Application.Subscriptions;

public sealed record SubscriptionCurrency(string Code, string Name, string Symbol, int DecimalPlaces);
public sealed record PublicSubscriptionPlan(Guid Id, string Name, decimal MonthlyPrice, int? MaxPublishedListings, decimal BookingFeeAmount, SubscriptionCurrency Currency);
public sealed record CompanySubscriptionDetails(Guid Id, Guid SubscriptionPlanId, string PlanName, CompanySubscriptionStatus Status, DateTimeOffset CurrentPeriodStart, DateTimeOffset CurrentPeriodEnd, decimal MonthlyPriceSnapshot, string CurrencyCode, int? MaxPublishedListingsSnapshot, decimal BookingFeeSnapshot, DateTimeOffset StartedAt, DateTimeOffset? CancellationRequestedAt, DateTimeOffset? EndedAt, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, byte[] RowVersion, bool IsCurrentlyEffective, bool IsCancellationScheduled);
public sealed record CompanySubscriptionHistoryQuery(int PageNumber, int PageSize, CompanySubscriptionStatus? Status);
public sealed record CheckoutSubscriptionCommand(Guid SubscriptionPlanId);
public sealed record CheckoutInvoice(Guid Id, string InvoiceNumber, BillingInvoiceStatus Status, string CurrencyCode, decimal Subtotal, decimal Total, DateTimeOffset PeriodStart, DateTimeOffset PeriodEnd, DateTimeOffset IssuedAt, DateTimeOffset PaidAt);
public sealed record CheckoutPayment(Guid Id, decimal Amount, string CurrencyCode, PaymentProvider Provider, string ProviderReference, PaymentTransactionStatus Status, DateTimeOffset CreatedAt, DateTimeOffset? CompletedAt);
public sealed record CheckoutResult(CompanySubscriptionDetails Subscription, CheckoutInvoice Invoice, CheckoutPayment? PaymentTransaction);
public enum SubscriptionOperationStatus { Succeeded, NotFound, Conflict, ServiceUnavailable }
public sealed record SubscriptionResult<T>(SubscriptionOperationStatus Status, T? Value = default);
