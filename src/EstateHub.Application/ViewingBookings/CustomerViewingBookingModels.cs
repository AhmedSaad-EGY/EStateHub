using EstateHub.Application.Common;
using EstateHub.Domain.Enums;

namespace EstateHub.Application.ViewingBookings;

public sealed record CustomerBookingSlotSummary(Guid Id, DateTimeOffset StartsAt, DateTimeOffset EndsAt, string MeetingPoint, ViewingSlotStatus Status);
public sealed record CustomerBookingListingSummary(Guid Id, string Slug, string Title, ListingType ListingType);
public sealed record CustomerBookingCompanySummary(Guid Id, string Slug, string DisplayName, Guid? LogoFileAssetId);
public sealed record CustomerViewingBookingSummary(Guid Id, string BookingCode, ViewingBookingStatus Status, int VisitorCount, string ContactPhone, string? SpecialRequests, BookingCancellationSource? CancellationSource, DateTimeOffset CreatedAt, DateTimeOffset? ConfirmedAt, DateTimeOffset? CheckedInAt, DateTimeOffset? CancelledAt, DateTimeOffset? CompletedAt, byte[] RowVersion, CustomerBookingSlotSummary Slot, CustomerBookingListingSummary Listing, CustomerBookingCompanySummary Company);
public sealed record CustomerBookingStatusHistoryItem(Guid Id, ViewingBookingStatus? FromStatus, ViewingBookingStatus ToStatus, BookingActorType ActorType, DateTimeOffset ChangedAt, string? Reason);
public sealed record CustomerBookingRescheduleHistoryItem(Guid Id, Guid FromViewingSlotId, DateTimeOffset FromStartsAt, DateTimeOffset FromEndsAt, Guid ToViewingSlotId, DateTimeOffset ToStartsAt, DateTimeOffset ToEndsAt, DateTimeOffset ChangedAt, string? Reason);
public sealed record CustomerViewingBookingDetails(CustomerViewingBookingSummary Header, string? SpecialRequests, BookingCancellationSource? CancellationSource, IReadOnlyList<CustomerBookingStatusHistoryItem> StatusHistory, IReadOnlyList<CustomerBookingRescheduleHistoryItem> RescheduleHistory);
public sealed record CustomerViewingBookingDirectoryQuery(int PageNumber, int PageSize, ViewingBookingStatus? Status);
public sealed record CreateCustomerViewingBookingCommand(Guid ViewingSlotId, int VisitorCount, string ContactPhone, string? SpecialRequests);
public enum CustomerViewingBookingOperationStatus { Succeeded, NotFound, Conflict, ServiceUnavailable }
public sealed record CustomerViewingBookingMutationResult(CustomerViewingBookingOperationStatus Status, CustomerViewingBookingDetails? Booking = null);

public interface ICustomerViewingBookingService
{
    Task<PagedResult<CustomerViewingBookingSummary>?> GetBookingsAsync(Guid applicationUserId, CustomerViewingBookingDirectoryQuery query, CancellationToken cancellationToken = default);
    Task<CustomerViewingBookingDetails?> GetBookingAsync(Guid applicationUserId, Guid bookingId, CancellationToken cancellationToken = default);
    Task<CustomerViewingBookingMutationResult> CreateBookingAsync(Guid applicationUserId, CreateCustomerViewingBookingCommand command, CancellationToken cancellationToken = default);
    Task<CustomerViewingBookingOperationStatus> CancelBookingAsync(Guid applicationUserId, Guid bookingId, byte[] rowVersion, string? reason, CancellationToken cancellationToken = default);
}
