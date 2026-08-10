using EstateHub.Domain.Entities.Catalog;
using EstateHub.Domain.Entities.Companies;
using EstateHub.Domain.Enums;

namespace EstateHub.Domain.Entities.Billing;

public class BillingInvoice
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public BillingInvoiceStatus Status { get; set; }
    public DateTimeOffset? PeriodStart { get; set; }
    public DateTimeOffset? PeriodEnd { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }
    public DateTimeOffset? IssuedAt { get; set; }
    public DateTimeOffset? DueAt { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public Company Company { get; set; } = null!;
    public Currency Currency { get; set; } = null!;
    public ICollection<BillingInvoiceLine> Lines { get; set; } = [];
    public ICollection<PaymentTransaction> PaymentTransactions { get; set; } = [];
}
