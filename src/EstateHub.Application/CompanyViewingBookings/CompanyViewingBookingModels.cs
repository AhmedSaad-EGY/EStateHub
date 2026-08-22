using EstateHub.Application.Common;
using EstateHub.Domain.Enums;

namespace EstateHub.Application.CompanyViewingBookings;

public sealed record CompanyViewingBookingCustomerSummary(
    Guid Id,
    string FullName,
    CustomerPersona Persona);

public sealed record CompanyViewingBookingSlotSummary(
    Guid Id,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string MeetingPoint,
    ViewingSlotStatus Status);

public sealed record CompanyViewingBookingListingSummary(
    Guid Id,
    string ListingCode,
    string Slug,
    string Title,
    ListingType ListingType);

public sealed record CompanyViewingBookingEmployeeSummary(
    Guid Id,
    string FullName,
    string? JobTitle);

public sealed record CompanyViewingBookingChargeSummary(
    Guid Id,
    decimal AmountSnapshot,
    string CurrencyCode,
    BookingChargeStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? VoidedAt,
    string? VoidReason);

public sealed record CompanyViewingBookingSummary(
    Guid Id,
    string BookingCode,
    ViewingBookingStatus Status,
    int VisitorCount,
    string ContactPhone,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ConfirmedAt,
    DateTimeOffset? CheckedInAt,
    DateTimeOffset? CancelledAt,
    DateTimeOffset? CompletedAt,
    byte[] RowVersion,
    CompanyViewingBookingCustomerSummary Customer,
    CompanyViewingBookingSlotSummary Slot,
    CompanyViewingBookingListingSummary Listing,
    CompanyViewingBookingEmployeeSummary? AssignedEmployee,
    CompanyViewingBookingChargeSummary? Charge);

public sealed record CompanyViewingBookingDetailsHeader(
    CompanyViewingBookingSummary Summary,
    string? SpecialRequests,
    BookingCancellationSource? CancellationSource);

public sealed record CompanyViewingBookingStatusHistoryItem(
    Guid Id,
    ViewingBookingStatus? FromStatus,
    ViewingBookingStatus ToStatus,
    BookingActorType ActorType,
    DateTimeOffset ChangedAt,
    string? Reason);

public sealed record CompanyViewingBookingRescheduleHistoryItem(
    Guid Id,
    Guid FromViewingSlotId,
    DateTimeOffset FromStartsAt,
    DateTimeOffset FromEndsAt,
    Guid ToViewingSlotId,
    DateTimeOffset ToStartsAt,
    DateTimeOffset ToEndsAt,
    DateTimeOffset ChangedAt,
    string? Reason);

public sealed record CompanyViewingBookingDetails(
    CompanyViewingBookingDetailsHeader Header,
    IReadOnlyList<CompanyViewingBookingStatusHistoryItem> StatusHistory,
    IReadOnlyList<CompanyViewingBookingRescheduleHistoryItem> RescheduleHistory);

public sealed record CompanyViewingBookingDirectoryQuery(
    int PageNumber,
    int PageSize,
    string? Search,
    ViewingBookingStatus? Status,
    Guid? ListingId,
    Guid? ViewingSlotId,
    Guid? AssignedEmployeeId,
    DateTimeOffset? From,
    DateTimeOffset? To);

public sealed record CompanyViewingBookingAssignmentCommand(
    Guid? EmployeeId,
    byte[] RowVersion);

public sealed record CompanyViewingBookingLifecycleCommand(
    byte[] RowVersion,
    string? Reason);

public sealed record CompanyViewingBookingRescheduleCommand(
    Guid ToViewingSlotId,
    byte[] RowVersion,
    string? Reason);

public enum CompanyViewingBookingOperationStatus
{
    Succeeded,
    NotFound,
    Conflict,
    ServiceUnavailable
}

public sealed record CompanyViewingBookingMutationResult(
    CompanyViewingBookingOperationStatus Status,
    CompanyViewingBookingDetails? Booking = null);

