using EstateHub.Application.Common;
using EstateHub.Application.ViewingBookings;
using EstateHub.Domain.Enums;

namespace EstateHub.Api.Contracts.ViewingBookings;

public sealed class CustomerViewingBookingDirectoryRequest
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Status { get; init; }
}

public sealed record CreateCustomerViewingBookingRequest(Guid ViewingSlotId, int VisitorCount, string? ContactPhone, string? SpecialRequests);
public sealed record CancelCustomerViewingBookingRequest(string? RowVersion, string? Reason);
public sealed record CustomerBookingSlotResponse(Guid Id, DateTimeOffset StartsAt, DateTimeOffset EndsAt, string MeetingPoint, string Status);
public sealed record CustomerBookingListingResponse(Guid Id, string Slug, string Title, string ListingType);
public sealed record CustomerBookingCompanyResponse(Guid Id, string Slug, string DisplayName, Guid? LogoFileAssetId);

public sealed record CustomerViewingBookingResponse(Guid Id, string BookingCode, string Status, int VisitorCount, string ContactPhone, DateTimeOffset CreatedAt, DateTimeOffset? ConfirmedAt, DateTimeOffset? CheckedInAt, DateTimeOffset? CancelledAt, DateTimeOffset? CompletedAt, string RowVersion, CustomerBookingSlotResponse Slot, CustomerBookingListingResponse Listing, CustomerBookingCompanyResponse Company)
{
    public static CustomerViewingBookingResponse From(CustomerViewingBookingSummary value) => new(
        value.Id, value.BookingCode, BookingEnumText.Status(value.Status), value.VisitorCount, value.ContactPhone,
        value.CreatedAt, value.ConfirmedAt, value.CheckedInAt, value.CancelledAt, value.CompletedAt, Convert.ToBase64String(value.RowVersion),
        new CustomerBookingSlotResponse(value.Slot.Id, value.Slot.StartsAt, value.Slot.EndsAt, value.Slot.MeetingPoint, BookingEnumText.SlotStatus(value.Slot.Status)),
        new CustomerBookingListingResponse(value.Listing.Id, value.Listing.Slug, value.Listing.Title, BookingEnumText.ListingType(value.Listing.ListingType)),
        new CustomerBookingCompanyResponse(value.Company.Id, value.Company.Slug, value.Company.DisplayName, value.Company.LogoFileAssetId));
}

public sealed record CustomerViewingBookingsResponse(IReadOnlyList<CustomerViewingBookingResponse> Items, int PageNumber, int PageSize, int TotalCount, int TotalPages, bool HasPreviousPage, bool HasNextPage)
{
    public static CustomerViewingBookingsResponse From(PagedResult<CustomerViewingBookingSummary> value) => new(value.Items.Select(CustomerViewingBookingResponse.From).ToList(), value.PageNumber, value.PageSize, value.TotalCount, value.TotalPages, value.HasPreviousPage, value.HasNextPage);
}

public sealed record CustomerBookingStatusHistoryResponse(Guid Id, string? FromStatus, string ToStatus, string ActorType, DateTimeOffset ChangedAt, string? Reason)
{
    public static CustomerBookingStatusHistoryResponse From(CustomerBookingStatusHistoryItem value) => new(value.Id, value.FromStatus is null ? null : BookingEnumText.Status(value.FromStatus.Value), BookingEnumText.Status(value.ToStatus), BookingEnumText.ActorType(value.ActorType), value.ChangedAt, value.Reason);
}

public sealed record CustomerBookingRescheduleHistoryResponse(Guid Id, Guid FromViewingSlotId, DateTimeOffset FromStartsAt, DateTimeOffset FromEndsAt, Guid ToViewingSlotId, DateTimeOffset ToStartsAt, DateTimeOffset ToEndsAt, DateTimeOffset ChangedAt, string? Reason)
{
    public static CustomerBookingRescheduleHistoryResponse From(CustomerBookingRescheduleHistoryItem value) => new(value.Id, value.FromViewingSlotId, value.FromStartsAt, value.FromEndsAt, value.ToViewingSlotId, value.ToStartsAt, value.ToEndsAt, value.ChangedAt, value.Reason);
}

public sealed record CustomerViewingBookingDetailsResponse(Guid Id, string BookingCode, string Status, int VisitorCount, string ContactPhone, string? SpecialRequests, string? CancellationSource, DateTimeOffset CreatedAt, DateTimeOffset? ConfirmedAt, DateTimeOffset? CheckedInAt, DateTimeOffset? CancelledAt, DateTimeOffset? CompletedAt, string RowVersion, CustomerBookingSlotResponse Slot, CustomerBookingListingResponse Listing, CustomerBookingCompanyResponse Company, IReadOnlyList<CustomerBookingStatusHistoryResponse> StatusHistory, IReadOnlyList<CustomerBookingRescheduleHistoryResponse> RescheduleHistory)
{
    public static CustomerViewingBookingDetailsResponse From(CustomerViewingBookingDetails value)
    {
        var header = CustomerViewingBookingResponse.From(value.Header);
        return new(header.Id, header.BookingCode, header.Status, header.VisitorCount, header.ContactPhone, value.SpecialRequests, value.CancellationSource is null ? null : BookingEnumText.CancellationSource(value.CancellationSource.Value), header.CreatedAt, header.ConfirmedAt, header.CheckedInAt, header.CancelledAt, header.CompletedAt, header.RowVersion, header.Slot, header.Listing, header.Company, value.StatusHistory.Select(CustomerBookingStatusHistoryResponse.From).ToList(), value.RescheduleHistory.Select(CustomerBookingRescheduleHistoryResponse.From).ToList());
    }
}

internal static class BookingEnumText
{
    public static string Status(ViewingBookingStatus value) => value switch
    {
        ViewingBookingStatus.Pending => "Pending", ViewingBookingStatus.Confirmed => "Confirmed", ViewingBookingStatus.CheckedIn => "CheckedIn", ViewingBookingStatus.Completed => "Completed", ViewingBookingStatus.Rejected => "Rejected", ViewingBookingStatus.Cancelled => "Cancelled", ViewingBookingStatus.NoShow => "NoShow",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };
    public static string SlotStatus(ViewingSlotStatus value) => value switch
    {
        ViewingSlotStatus.Open => "Open", ViewingSlotStatus.Closed => "Closed", ViewingSlotStatus.Cancelled => "Cancelled",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };
    public static string CancellationSource(BookingCancellationSource value) => value switch
    {
        BookingCancellationSource.Customer => "Customer", BookingCancellationSource.Company => "Company", BookingCancellationSource.System => "System",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };
    public static string ActorType(BookingActorType value) => value switch
    {
        BookingActorType.Customer => "Customer", BookingActorType.Employee => "Employee", BookingActorType.Platform => "Platform", BookingActorType.System => "System",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };
    public static string ListingType(global::EstateHub.Domain.Enums.ListingType value) => value switch
    {
        global::EstateHub.Domain.Enums.ListingType.Sale => "Sale", global::EstateHub.Domain.Enums.ListingType.Rent => "Rent",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };
}
