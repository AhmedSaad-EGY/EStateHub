using EstateHub.Application.Common;

namespace EstateHub.Application.CompanyViewingBookings;

public interface ICompanyViewingBookingManagementService
{
    Task<PagedResult<CompanyViewingBookingSummary>?> GetBookingsAsync(
        Guid applicationUserId,
        CompanyViewingBookingDirectoryQuery query,
        CancellationToken cancellationToken = default);

    Task<CompanyViewingBookingDetails?> GetBookingAsync(
        Guid applicationUserId,
        Guid bookingId,
        CancellationToken cancellationToken = default);

    Task<CompanyViewingBookingMutationResult> AssignEmployeeAsync(
        Guid applicationUserId,
        Guid bookingId,
        CompanyViewingBookingAssignmentCommand command,
        CancellationToken cancellationToken = default);

    Task<CompanyViewingBookingOperationStatus> ConfirmAsync(
        Guid applicationUserId,
        Guid bookingId,
        CompanyViewingBookingLifecycleCommand command,
        CancellationToken cancellationToken = default);

    Task<CompanyViewingBookingOperationStatus> RejectAsync(
        Guid applicationUserId,
        Guid bookingId,
        CompanyViewingBookingLifecycleCommand command,
        CancellationToken cancellationToken = default);

    Task<CompanyViewingBookingOperationStatus> CheckInAsync(
        Guid applicationUserId,
        Guid bookingId,
        CompanyViewingBookingLifecycleCommand command,
        CancellationToken cancellationToken = default);

    Task<CompanyViewingBookingOperationStatus> CompleteAsync(
        Guid applicationUserId,
        Guid bookingId,
        CompanyViewingBookingLifecycleCommand command,
        CancellationToken cancellationToken = default);

    Task<CompanyViewingBookingOperationStatus> MarkNoShowAsync(
        Guid applicationUserId,
        Guid bookingId,
        CompanyViewingBookingLifecycleCommand command,
        CancellationToken cancellationToken = default);

    Task<CompanyViewingBookingOperationStatus> CancelAsync(
        Guid applicationUserId,
        Guid bookingId,
        CompanyViewingBookingLifecycleCommand command,
        CancellationToken cancellationToken = default);

    Task<CompanyViewingBookingMutationResult> RescheduleAsync(
        Guid applicationUserId,
        Guid bookingId,
        CompanyViewingBookingRescheduleCommand command,
        CancellationToken cancellationToken = default);
}
