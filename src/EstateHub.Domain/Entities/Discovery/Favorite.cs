using EstateHub.Domain.Entities.Catalog;
using EstateHub.Domain.Entities.Users;

namespace EstateHub.Domain.Entities.Discovery;

public class Favorite
{
    public Guid Id { get; set; }
    public Guid CustomerProfileId { get; set; }
    public Guid ListingId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public CustomerProfile CustomerProfile { get; set; } = null!;
    public Listing Listing { get; set; } = null!;
}
