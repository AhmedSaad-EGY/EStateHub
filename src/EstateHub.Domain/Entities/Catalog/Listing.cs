using EstateHub.Domain.Entities.Billing;
using EstateHub.Domain.Entities.Bookings;
using EstateHub.Domain.Entities.Companies;
using EstateHub.Domain.Entities.Discovery;
using EstateHub.Domain.Enums;

namespace EstateHub.Domain.Entities.Catalog;

public class Listing
{
    public Guid Id { get; set; }
    public Guid UnitId { get; set; }
    public Guid CompanyId { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string ListingCode { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ListingType ListingType { get; set; }
    public decimal AskingPrice { get; set; }
    public RentPeriod? RentPeriod { get; set; }
    public ListingPublicationStatus PublicationStatus { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset? ArchivedAt { get; set; }
    public Guid CreatedByEmployeeId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public Unit Unit { get; set; } = null!;
    public Company Company { get; set; } = null!;
    public Currency Currency { get; set; } = null!;
    public CompanyEmployee CreatedByEmployee { get; set; } = null!;
    public ICollection<ListingPriceHistory> PriceHistory { get; set; } = [];
    public ICollection<PaymentPlan> PaymentPlans { get; set; } = [];
    public ICollection<ListingMedia> Media { get; set; } = [];
    public ICollection<ListingViewEvent> ViewEvents { get; set; } = [];
    public ICollection<Favorite> Favorites { get; set; } = [];
    public ICollection<ViewingSlot> ViewingSlots { get; set; } = [];
    public ICollection<LeadInterest> LeadInterests { get; set; } = [];
    public ICollection<ListingPromotion> Promotions { get; set; } = [];
}
