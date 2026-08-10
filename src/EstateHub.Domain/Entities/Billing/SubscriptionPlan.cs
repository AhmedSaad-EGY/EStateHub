using EstateHub.Domain.Entities.Catalog;

namespace EstateHub.Domain.Entities.Billing;

public class SubscriptionPlan
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal MonthlyPrice { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public int? MaxPublishedListings { get; set; }
    public decimal BookingFeeAmount { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Currency Currency { get; set; } = null!;
    public ICollection<CompanySubscription> Subscriptions { get; set; } = [];
}
