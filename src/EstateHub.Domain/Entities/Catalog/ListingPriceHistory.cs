namespace EstateHub.Domain.Entities.Catalog;

public class ListingPriceHistory
{
    public Guid Id { get; set; }
    public Guid ListingId { get; set; }
    public decimal Price { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public DateTimeOffset EffectiveFrom { get; set; }
    public DateTimeOffset? EffectiveTo { get; set; }
    public string? ChangeReason { get; set; }

    public Listing Listing { get; set; } = null!;
    public Currency Currency { get; set; } = null!;
}
