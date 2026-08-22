using EstateHub.Application.Common;

namespace EstateHub.Application.ViewingBookings;

public sealed record PublicViewingSlotItem(Guid Id, DateTimeOffset StartsAt, DateTimeOffset EndsAt, int Capacity, int ReservedVisitorCount, int AvailableVisitorCapacity, string MeetingPoint);
public sealed record PublicViewingSlotQuery(int PageNumber, int PageSize, DateTimeOffset? From, DateTimeOffset? To);

public interface IPublicViewingSlotQueryService
{
    Task<PagedResult<PublicViewingSlotItem>?> GetSlotsAsync(string listingSlug, PublicViewingSlotQuery query, CancellationToken cancellationToken = default);
}
