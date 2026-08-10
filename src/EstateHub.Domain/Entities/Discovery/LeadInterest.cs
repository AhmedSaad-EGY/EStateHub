using EstateHub.Domain.Entities.Catalog;

namespace EstateHub.Domain.Entities.Discovery;

public class LeadInterest
{
    public Guid Id { get; set; }
    public Guid LeadId { get; set; }
    public Guid ListingId { get; set; }
    public string InterestType { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    public Lead Lead { get; set; } = null!;
    public Listing Listing { get; set; } = null!;
}
