using EstateHub.Domain.Entities.Catalog;

namespace EstateHub.Domain.Entities.Billing;

public class PromotionPackage
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Placement { get; set; } = string.Empty;
    public int DurationDays { get; set; }
    public decimal Price { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Currency Currency { get; set; } = null!;
    public ICollection<ListingPromotion> ListingPromotions { get; set; } = [];
}
