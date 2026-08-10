namespace EstateHub.Domain.Entities.Catalog;

public class ListingViewEvent
{
    public Guid Id { get; set; }
    public Guid ListingId { get; set; }
    public Guid? ViewerApplicationUserId { get; set; }
    public string? AnonymousSessionHash { get; set; }
    public string? Source { get; set; }
    public DateTimeOffset ViewedAt { get; set; }

    public Listing Listing { get; set; } = null!;
}
