using EstateHub.Domain.Entities.Bookings;
using EstateHub.Domain.Entities.Catalog;
using EstateHub.Domain.Entities.Companies;
using EstateHub.Domain.Enums;

namespace EstateHub.Domain.Entities.Billing;

public class CompanySubscription
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid SubscriptionPlanId { get; set; }
    public CompanySubscriptionStatus Status { get; set; }
    public DateTimeOffset CurrentPeriodStart { get; set; }
    public DateTimeOffset CurrentPeriodEnd { get; set; }
    public decimal MonthlyPriceSnapshot { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public int? MaxPublishedListingsSnapshot { get; set; }
    public decimal BookingFeeSnapshot { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CancellationRequestedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public Company Company { get; set; } = null!;
    public SubscriptionPlan SubscriptionPlan { get; set; } = null!;
    public Currency Currency { get; set; } = null!;
    public ICollection<BookingCharge> BookingCharges { get; set; } = [];
    public ICollection<BillingInvoiceLine> InvoiceLines { get; set; } = [];
}
