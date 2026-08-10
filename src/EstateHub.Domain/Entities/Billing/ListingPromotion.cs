using EstateHub.Domain.Entities.Catalog;
using EstateHub.Domain.Entities.Companies;
using EstateHub.Domain.Enums;

namespace EstateHub.Domain.Entities.Billing;

public class ListingPromotion
{
    public Guid Id { get; set; }
    public Guid ListingId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid PromotionPackageId { get; set; }
    public decimal AmountSnapshot { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string PlacementSnapshot { get; set; } = string.Empty;
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public ListingPromotionStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public Listing Listing { get; set; } = null!;
    public Company Company { get; set; } = null!;
    public PromotionPackage PromotionPackage { get; set; } = null!;
    public Currency Currency { get; set; } = null!;
    public BillingInvoiceLine? InvoiceLine { get; set; }
}
