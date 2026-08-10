using EstateHub.Domain.Enums;

namespace EstateHub.Domain.Entities.Catalog;

public class PaymentPlan
{
    public Guid Id { get; set; }
    public Guid ListingId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal DownPaymentPercentage { get; set; }
    public int DurationMonths { get; set; }
    public InstallmentFrequency InstallmentFrequency { get; set; }
    public decimal? CashDiscountPercentage { get; set; }
    public bool IsActive { get; set; }

    public Listing Listing { get; set; } = null!;
    public Currency Currency { get; set; } = null!;
}
