using EstateHub.Api.Authorization.CompanyPermissions;
using EstateHub.Api.Contracts.CompanyViewingBookings;
using EstateHub.Application.CompanyAccess;
using EstateHub.Application.CompanyViewingBookings;
using EstateHub.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EstateHub.Api.Controllers;

[ApiController]
[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/company/viewing-bookings")]
public sealed class CompanyViewingBookingsController(
    ICompanyViewingBookingManagementService service) : ControllerBase
{
    [HttpGet]
    [RequireCompanyPermission(CompanyPermissionCodes.BookingsRead)]
    public async Task<ActionResult<CompanyViewingBookingsResponse>> GetBookings(
        [FromQuery] CompanyViewingBookingDirectoryRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var query = BuildDirectoryQuery(request);
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await service.GetBookingsAsync(userId, query, cancellationToken);
        return result is null
            ? BookingNotFound()
            : Ok(CompanyViewingBookingsResponse.From(result));
    }

    [HttpGet("{bookingId}")]
    [RequireCompanyPermission(CompanyPermissionCodes.BookingsRead)]
    public async Task<ActionResult<CompanyViewingBookingDetailsResponse>> GetBooking(
        Guid bookingId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        if (bookingId == Guid.Empty)
        {
            return InvalidRoute("bookingId", "BookingId is required.");
        }

        var result = await service.GetBookingAsync(userId, bookingId, cancellationToken);
        return result is null
            ? BookingNotFound()
            : Ok(CompanyViewingBookingDetailsResponse.From(result));
    }

    [HttpPut("{bookingId}/assignment")]
    [RequireCompanyPermission(CompanyPermissionCodes.BookingsManage)]
    public async Task<ActionResult<CompanyViewingBookingDetailsResponse>> AssignEmployee(
        Guid bookingId,
        CompanyViewingBookingAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        if (bookingId == Guid.Empty)
        {
            ModelState.AddModelError("bookingId", "BookingId is required.");
        }

        if (request.EmployeeId == Guid.Empty)
        {
            ModelState.AddModelError("employeeId", "EmployeeId cannot be empty when supplied.");
        }

        var rowVersion = ParseRowVersion(request.RowVersion);
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await service.AssignEmployeeAsync(
            userId,
            bookingId,
            new CompanyViewingBookingAssignmentCommand(request.EmployeeId, rowVersion!),
            cancellationToken);
        return Mutation(result);
    }

    [HttpPost("{bookingId}/confirm")]
    [RequireCompanyPermission(CompanyPermissionCodes.BookingsManage)]
    public Task<IActionResult> Confirm(
        Guid bookingId,
        CompanyViewingBookingLifecycleRequest request,
        CancellationToken cancellationToken) =>
        Lifecycle(bookingId, request, LifecycleOperation.Confirm, cancellationToken);

    [HttpPost("{bookingId}/reject")]
    [RequireCompanyPermission(CompanyPermissionCodes.BookingsManage)]
    public Task<IActionResult> Reject(
        Guid bookingId,
        CompanyViewingBookingLifecycleRequest request,
        CancellationToken cancellationToken) =>
        Lifecycle(bookingId, request, LifecycleOperation.Reject, cancellationToken);

    [HttpPost("{bookingId}/check-in")]
    [RequireCompanyPermission(CompanyPermissionCodes.BookingsManage)]
    public Task<IActionResult> CheckIn(
        Guid bookingId,
        CompanyViewingBookingLifecycleRequest request,
        CancellationToken cancellationToken) =>
        Lifecycle(bookingId, request, LifecycleOperation.CheckIn, cancellationToken);

    [HttpPost("{bookingId}/complete")]
    [RequireCompanyPermission(CompanyPermissionCodes.BookingsManage)]
    public Task<IActionResult> Complete(
        Guid bookingId,
        CompanyViewingBookingLifecycleRequest request,
        CancellationToken cancellationToken) =>
        Lifecycle(bookingId, request, LifecycleOperation.Complete, cancellationToken);

    [HttpPost("{bookingId}/no-show")]
    [RequireCompanyPermission(CompanyPermissionCodes.BookingsManage)]
    public Task<IActionResult> MarkNoShow(
        Guid bookingId,
        CompanyViewingBookingLifecycleRequest request,
        CancellationToken cancellationToken) =>
        Lifecycle(bookingId, request, LifecycleOperation.NoShow, cancellationToken);

    [HttpPost("{bookingId}/cancel")]
    [RequireCompanyPermission(CompanyPermissionCodes.BookingsManage)]
    public Task<IActionResult> Cancel(
        Guid bookingId,
        CompanyViewingBookingLifecycleRequest request,
        CancellationToken cancellationToken) =>
        Lifecycle(bookingId, request, LifecycleOperation.Cancel, cancellationToken);

    [HttpPost("{bookingId}/reschedule")]
    [RequireCompanyPermission(CompanyPermissionCodes.BookingsManage)]
    public async Task<ActionResult<CompanyViewingBookingDetailsResponse>> Reschedule(
        Guid bookingId,
        CompanyViewingBookingRescheduleRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        if (bookingId == Guid.Empty)
        {
            ModelState.AddModelError("bookingId", "BookingId is required.");
        }

        if (request.ToViewingSlotId == Guid.Empty)
        {
            ModelState.AddModelError("toViewingSlotId", "ToViewingSlotId is required.");
        }

        var rowVersion = ParseRowVersion(request.RowVersion);
        var reason = NormalizeReason(request.Reason);
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await service.RescheduleAsync(
            userId,
            bookingId,
            new CompanyViewingBookingRescheduleCommand(
                request.ToViewingSlotId,
                rowVersion!,
                reason),
            cancellationToken);
        return Mutation(result);
    }

    private async Task<IActionResult> Lifecycle(
        Guid bookingId,
        CompanyViewingBookingLifecycleRequest request,
        LifecycleOperation operation,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        if (bookingId == Guid.Empty)
        {
            ModelState.AddModelError("bookingId", "BookingId is required.");
        }

        var rowVersion = ParseRowVersion(request.RowVersion);
        var reason = NormalizeReason(request.Reason);
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var command = new CompanyViewingBookingLifecycleCommand(rowVersion!, reason);
        var result = operation switch
        {
            LifecycleOperation.Confirm => await service.ConfirmAsync(
                userId,
                bookingId,
                command,
                cancellationToken),
            LifecycleOperation.Reject => await service.RejectAsync(
                userId,
                bookingId,
                command,
                cancellationToken),
            LifecycleOperation.CheckIn => await service.CheckInAsync(
                userId,
                bookingId,
                command,
                cancellationToken),
            LifecycleOperation.Complete => await service.CompleteAsync(
                userId,
                bookingId,
                command,
                cancellationToken),
            LifecycleOperation.NoShow => await service.MarkNoShowAsync(
                userId,
                bookingId,
                command,
                cancellationToken),
            LifecycleOperation.Cancel => await service.CancelAsync(
                userId,
                bookingId,
                command,
                cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
        };

        return result switch
        {
            CompanyViewingBookingOperationStatus.Succeeded => NoContent(),
            CompanyViewingBookingOperationStatus.NotFound => BookingNotFound(),
            CompanyViewingBookingOperationStatus.Conflict => BookingConflict(),
            _ => BookingUnavailable()
        };
    }

    private CompanyViewingBookingDirectoryQuery BuildDirectoryQuery(
        CompanyViewingBookingDirectoryRequest request)
    {
        if (request.PageNumber is < 1 or > 10000)
        {
            ModelState.AddModelError("pageNumber", "PageNumber must be between 1 and 10000.");
        }

        if (request.PageSize is < 1 or > 50)
        {
            ModelState.AddModelError("pageSize", "PageSize must be between 1 and 50.");
        }

        var search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();
        if (search?.Length > 100)
        {
            ModelState.AddModelError("search", "Search must not exceed 100 characters.");
        }

        if (request.ListingId == Guid.Empty)
        {
            ModelState.AddModelError("listingId", "ListingId cannot be empty when supplied.");
        }

        if (request.ViewingSlotId == Guid.Empty)
        {
            ModelState.AddModelError("viewingSlotId", "ViewingSlotId cannot be empty when supplied.");
        }

        if (request.AssignedEmployeeId == Guid.Empty)
        {
            ModelState.AddModelError(
                "assignedEmployeeId",
                "AssignedEmployeeId cannot be empty when supplied.");
        }

        if (request.From is not null && request.To is not null && request.From >= request.To)
        {
            ModelState.AddModelError("to", "To must be later than From.");
        }

        var status = ParseStatus(request.Status);
        return new CompanyViewingBookingDirectoryQuery(
            request.PageNumber,
            request.PageSize,
            search,
            status,
            request.ListingId,
            request.ViewingSlotId,
            request.AssignedEmployeeId,
            request.From,
            request.To);
    }

    private ViewingBookingStatus? ParseStatus(string? value)
    {
        if (value is null)
        {
            return null;
        }

        var normalized = value.Trim();
        if (!string.IsNullOrEmpty(normalized)
            && Enum.TryParse<ViewingBookingStatus>(normalized, true, out var candidate)
            && Enum.IsDefined(candidate)
            && string.Equals(normalized, candidate.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return candidate;
        }

        ModelState.AddModelError("status", "Status is invalid.");
        return null;
    }

    private byte[]? ParseRowVersion(string? value)
    {
        if (string.IsNullOrEmpty(value) || value.Any(char.IsWhiteSpace))
        {
            ModelState.AddModelError(
                "rowVersion",
                "RowVersion must be Base64 encoded eight bytes.");
            return null;
        }

        try
        {
            var bytes = Convert.FromBase64String(value);
            if (bytes.Length == 8)
            {
                return bytes;
            }
        }
        catch (FormatException)
        {
        }

        ModelState.AddModelError(
            "rowVersion",
            "RowVersion must be Base64 encoded eight bytes.");
        return null;
    }

    private string? NormalizeReason(string? value)
    {
        var reason = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (reason?.Length > 1000)
        {
            ModelState.AddModelError("reason", "Reason must not exceed 1000 characters.");
        }

        return reason;
    }

    private ActionResult<CompanyViewingBookingDetailsResponse> Mutation(
        CompanyViewingBookingMutationResult result) => result.Status switch
    {
        CompanyViewingBookingOperationStatus.Succeeded => Ok(
            CompanyViewingBookingDetailsResponse.From(result.Booking!)),
        CompanyViewingBookingOperationStatus.NotFound => BookingNotFound(),
        CompanyViewingBookingOperationStatus.Conflict => BookingConflict(),
        _ => BookingUnavailable()
    };

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(User.FindFirst("sub")?.Value, out userId)
        && userId != Guid.Empty;

    private ActionResult InvalidRoute(string key, string message)
    {
        ModelState.AddModelError(key, message);
        return ValidationProblem(ModelState);
    }

    private ObjectResult BookingNotFound() => Problem(
        statusCode: StatusCodes.Status404NotFound,
        title: "Viewing booking not found.");

    private ObjectResult BookingConflict() => Problem(
        statusCode: StatusCodes.Status409Conflict,
        title: "Viewing booking operation conflict.");

    private ObjectResult BookingUnavailable() => Problem(
        statusCode: StatusCodes.Status503ServiceUnavailable,
        title: "Viewing booking operation is temporarily unavailable.");

    private enum LifecycleOperation
    {
        Confirm,
        Reject,
        CheckIn,
        Complete,
        NoShow,
        Cancel
    }
}

