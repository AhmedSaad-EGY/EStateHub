using EstateHub.Application.Common;
using EstateHub.Application.ViewingBookings;

namespace EstateHub.Api.Contracts.ViewingBookings;

public sealed class PublicViewingSlotRequest
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
}

public sealed record PublicViewingSlotResponse(Guid Id, DateTimeOffset StartsAt, DateTimeOffset EndsAt, int Capacity, int ReservedVisitorCount, int AvailableVisitorCapacity, string MeetingPoint)
{
    public static PublicViewingSlotResponse From(PublicViewingSlotItem value) => new(value.Id, value.StartsAt, value.EndsAt, value.Capacity, value.ReservedVisitorCount, value.AvailableVisitorCapacity, value.MeetingPoint);
}

public sealed record PublicViewingSlotsResponse(IReadOnlyList<PublicViewingSlotResponse> Items, int PageNumber, int PageSize, int TotalCount, int TotalPages, bool HasPreviousPage, bool HasNextPage)
{
    public static PublicViewingSlotsResponse From(PagedResult<PublicViewingSlotItem> value) => new(value.Items.Select(PublicViewingSlotResponse.From).ToList(), value.PageNumber, value.PageSize, value.TotalCount, value.TotalPages, value.HasPreviousPage, value.HasNextPage);
}
