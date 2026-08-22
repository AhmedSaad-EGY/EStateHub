using EstateHub.Application.Common;
using EstateHub.Application.CompanyViewingSlots;
using EstateHub.Domain.Enums;

namespace EstateHub.Api.Contracts.CompanyViewingSlots;

public sealed class CompanyViewingSlotDirectoryRequest
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public Guid? ListingId { get; init; }
    public string? Status { get; init; }
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
}

public sealed record CreateCompanyViewingSlotRequest(Guid ListingId, DateTimeOffset StartsAt, DateTimeOffset EndsAt, int Capacity, string? MeetingPoint);
public sealed record UpdateCompanyViewingSlotRequest(DateTimeOffset StartsAt, DateTimeOffset EndsAt, int Capacity, string? MeetingPoint, string? RowVersion);
public sealed record CompanyViewingSlotLifecycleRequest(string? RowVersion);

public sealed record CompanyViewingSlotResponse(Guid Id, Guid ListingId, string ListingCode, string ListingSlug, string ListingTitle, DateTimeOffset StartsAt, DateTimeOffset EndsAt, int Capacity, string MeetingPoint, string Status, int ActiveBookingCount, int ReservedVisitorCount, string RowVersion)
{
    public static CompanyViewingSlotResponse From(CompanyViewingSlotDetails value) => new(value.Id, value.ListingId, value.ListingCode, value.ListingSlug, value.ListingTitle, value.StartsAt, value.EndsAt, value.Capacity, value.MeetingPoint, Text(value.Status), value.ActiveBookingCount, value.ReservedVisitorCount, Convert.ToBase64String(value.RowVersion));
    private static string Text(ViewingSlotStatus value) => value switch
    {
        ViewingSlotStatus.Open => "Open", ViewingSlotStatus.Closed => "Closed", ViewingSlotStatus.Cancelled => "Cancelled",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };
}

public sealed record CompanyViewingSlotsResponse(IReadOnlyList<CompanyViewingSlotResponse> Items, int PageNumber, int PageSize, int TotalCount, int TotalPages, bool HasPreviousPage, bool HasNextPage)
{
    public static CompanyViewingSlotsResponse From(PagedResult<CompanyViewingSlotDetails> value) => new(value.Items.Select(CompanyViewingSlotResponse.From).ToList(), value.PageNumber, value.PageSize, value.TotalCount, value.TotalPages, value.HasPreviousPage, value.HasNextPage);
}
