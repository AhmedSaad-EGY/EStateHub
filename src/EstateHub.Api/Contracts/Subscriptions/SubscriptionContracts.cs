using EstateHub.Application.Common;
using EstateHub.Application.Subscriptions;
using EstateHub.Domain.Enums;

namespace EstateHub.Api.Contracts.Subscriptions;

public sealed record SubscriptionPlanCurrencyResponse(string Code, string Name, string Symbol, int DecimalPlaces);

public sealed record SubscriptionPlanResponse(Guid Id, string Name, decimal MonthlyPrice, int? MaxPublishedListings, decimal BookingFeeAmount, SubscriptionPlanCurrencyResponse Currency)
{
    public static SubscriptionPlanResponse From(PublicSubscriptionPlan plan) =>
        new(plan.Id, plan.Name, plan.MonthlyPrice, plan.MaxPublishedListings, plan.BookingFeeAmount,
            new SubscriptionPlanCurrencyResponse(plan.Currency.Code, plan.Currency.Name, plan.Currency.Symbol, plan.Currency.DecimalPlaces));
}

public sealed record CompanySubscriptionResponse(
    Guid Id,
    Guid SubscriptionPlanId,
    string PlanName,
    string Status,
    DateTimeOffset CurrentPeriodStart,
    DateTimeOffset CurrentPeriodEnd,
    decimal MonthlyPriceSnapshot,
    string CurrencyCode,
    int? MaxPublishedListingsSnapshot,
    decimal BookingFeeSnapshot,
    DateTimeOffset StartedAt,
    DateTimeOffset? CancellationRequestedAt,
    DateTimeOffset? EndedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    bool IsCurrentlyEffective,
    bool IsCancellationScheduled,
    string RowVersion)
{
    public static CompanySubscriptionResponse From(CompanySubscriptionDetails subscription) => new(
        subscription.Id,
        subscription.SubscriptionPlanId,
        subscription.PlanName,
        SubscriptionEnumText.CompanySubscriptionStatus(subscription.Status),
        subscription.CurrentPeriodStart,
        subscription.CurrentPeriodEnd,
        subscription.MonthlyPriceSnapshot,
        subscription.CurrencyCode,
        subscription.MaxPublishedListingsSnapshot,
        subscription.BookingFeeSnapshot,
        subscription.StartedAt,
        subscription.CancellationRequestedAt,
        subscription.EndedAt,
        subscription.CreatedAt,
        subscription.UpdatedAt,
        subscription.IsCurrentlyEffective,
        subscription.IsCancellationScheduled,
        Convert.ToBase64String(subscription.RowVersion));
}

public sealed record CompanySubscriptionHistoryResponse(IReadOnlyList<CompanySubscriptionResponse> Items, int PageNumber, int PageSize, int TotalCount)
{
    public static CompanySubscriptionHistoryResponse From(PagedResult<CompanySubscriptionDetails> result) =>
        new(result.Items.Select(CompanySubscriptionResponse.From).ToList(), result.PageNumber, result.PageSize, result.TotalCount);
}

public sealed record CheckoutSubscriptionRequest(Guid SubscriptionPlanId);

public sealed record SubscriptionInvoiceResponse(Guid Id, string InvoiceNumber, string Status, string CurrencyCode, decimal Subtotal, decimal Total, DateTimeOffset PeriodStart, DateTimeOffset PeriodEnd, DateTimeOffset IssuedAt, DateTimeOffset PaidAt)
{
    public static SubscriptionInvoiceResponse From(CheckoutInvoice invoice) => new(invoice.Id, invoice.InvoiceNumber, SubscriptionEnumText.BillingInvoiceStatus(invoice.Status), invoice.CurrencyCode, invoice.Subtotal, invoice.Total, invoice.PeriodStart, invoice.PeriodEnd, invoice.IssuedAt, invoice.PaidAt);
}

public sealed record SubscriptionPaymentTransactionResponse(Guid Id, decimal Amount, string CurrencyCode, string Provider, string ProviderReference, string Status, DateTimeOffset CreatedAt, DateTimeOffset? CompletedAt)
{
    public static SubscriptionPaymentTransactionResponse From(CheckoutPayment payment) => new(payment.Id, payment.Amount, payment.CurrencyCode, SubscriptionEnumText.PaymentProvider(payment.Provider), payment.ProviderReference, SubscriptionEnumText.PaymentTransactionStatus(payment.Status), payment.CreatedAt, payment.CompletedAt);
}

public sealed record CheckoutSubscriptionResponse(CompanySubscriptionResponse Subscription, SubscriptionInvoiceResponse Invoice, SubscriptionPaymentTransactionResponse? PaymentTransaction)
{
    public static CheckoutSubscriptionResponse From(CheckoutResult result) => new(CompanySubscriptionResponse.From(result.Subscription), SubscriptionInvoiceResponse.From(result.Invoice), result.PaymentTransaction is null ? null : SubscriptionPaymentTransactionResponse.From(result.PaymentTransaction));
}

public sealed record SubscriptionLifecycleRequest(string? RowVersion);

public sealed record CompanySubscriptionHistoryRequest(int PageNumber = 1, int PageSize = 20, string? Status = null);

internal static class SubscriptionEnumText
{
    public static string CompanySubscriptionStatus(EstateHub.Domain.Enums.CompanySubscriptionStatus value) => value switch
    {
        EstateHub.Domain.Enums.CompanySubscriptionStatus.Active => "Active",
        EstateHub.Domain.Enums.CompanySubscriptionStatus.Expired => "Expired",
        EstateHub.Domain.Enums.CompanySubscriptionStatus.Cancelled => "Cancelled",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown company subscription status.")
    };

    public static string BillingInvoiceStatus(EstateHub.Domain.Enums.BillingInvoiceStatus value) => value switch
    {
        EstateHub.Domain.Enums.BillingInvoiceStatus.Draft => "Draft",
        EstateHub.Domain.Enums.BillingInvoiceStatus.Open => "Open",
        EstateHub.Domain.Enums.BillingInvoiceStatus.Paid => "Paid",
        EstateHub.Domain.Enums.BillingInvoiceStatus.Void => "Void",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown billing invoice status.")
    };

    public static string PaymentProvider(EstateHub.Domain.Enums.PaymentProvider value) => value switch
    {
        EstateHub.Domain.Enums.PaymentProvider.Fake => "Fake",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown payment provider.")
    };

    public static string PaymentTransactionStatus(EstateHub.Domain.Enums.PaymentTransactionStatus value) => value switch
    {
        EstateHub.Domain.Enums.PaymentTransactionStatus.Pending => "Pending",
        EstateHub.Domain.Enums.PaymentTransactionStatus.Succeeded => "Succeeded",
        EstateHub.Domain.Enums.PaymentTransactionStatus.Failed => "Failed",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown payment transaction status.")
    };
}
