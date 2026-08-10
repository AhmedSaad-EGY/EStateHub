using EstateHub.Domain.Entities.Catalog;
using EstateHub.Domain.Enums;

namespace EstateHub.Domain.Entities.Billing;

public class PaymentTransaction
{
    public Guid Id { get; set; }
    public Guid BillingInvoiceId { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public PaymentProvider Provider { get; set; }
    public string ProviderReference { get; set; } = string.Empty;
    public PaymentTransactionStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? FailureReason { get; set; }

    public BillingInvoice BillingInvoice { get; set; } = null!;
    public Currency Currency { get; set; } = null!;
}
