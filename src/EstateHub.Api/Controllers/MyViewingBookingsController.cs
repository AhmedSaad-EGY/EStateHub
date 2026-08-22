using EstateHub.Api.Contracts.ViewingBookings;
using EstateHub.Application.ViewingBookings;
using EstateHub.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EstateHub.Api.Controllers;

[ApiController]
[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/me/viewing-bookings")]
public sealed class MyViewingBookingsController(ICustomerViewingBookingService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CustomerViewingBookingsResponse>> GetBookings([FromQuery] CustomerViewingBookingDirectoryRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (request.PageNumber is < 1 or > 10000) ModelState.AddModelError("pageNumber", "PageNumber must be between 1 and 10000.");
        if (request.PageSize is < 1 or > 50) ModelState.AddModelError("pageSize", "PageSize must be between 1 and 50.");
        var status = ParseStatus(request.Status);
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = await service.GetBookingsAsync(userId, new CustomerViewingBookingDirectoryQuery(request.PageNumber, request.PageSize, status), cancellationToken);
        return result is null ? BookingNotFound() : Ok(CustomerViewingBookingsResponse.From(result));
    }

    [HttpGet("{bookingId}")]
    public async Task<ActionResult<CustomerViewingBookingDetailsResponse>> GetBooking(Guid bookingId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (bookingId == Guid.Empty) return InvalidRoute("bookingId", "BookingId is required.");
        var result = await service.GetBookingAsync(userId, bookingId, cancellationToken);
        return result is null ? BookingNotFound() : Ok(CustomerViewingBookingDetailsResponse.From(result));
    }

    [HttpPost]
    public async Task<ActionResult<CustomerViewingBookingDetailsResponse>> CreateBooking(CreateCustomerViewingBookingRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (request.ViewingSlotId == Guid.Empty) ModelState.AddModelError("viewingSlotId", "ViewingSlotId is required.");
        if (request.VisitorCount <= 0) ModelState.AddModelError("visitorCount", "VisitorCount must be positive.");
        var contactPhone = request.ContactPhone?.Trim();
        if (string.IsNullOrWhiteSpace(contactPhone) || contactPhone.Length > 32) ModelState.AddModelError("contactPhone", "ContactPhone is required and must not exceed 32 characters.");
        var specialRequests = string.IsNullOrWhiteSpace(request.SpecialRequests) ? null : request.SpecialRequests.Trim();
        if (specialRequests?.Length > 4000) ModelState.AddModelError("specialRequests", "SpecialRequests must not exceed 4000 characters.");
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = await service.CreateBookingAsync(userId, new CreateCustomerViewingBookingCommand(request.ViewingSlotId, request.VisitorCount, contactPhone!, specialRequests), cancellationToken);
        return result.Status switch
        {
            CustomerViewingBookingOperationStatus.Succeeded => CreatedAtAction(nameof(GetBooking), new { bookingId = result.Booking!.Header.Id }, CustomerViewingBookingDetailsResponse.From(result.Booking)),
            CustomerViewingBookingOperationStatus.NotFound => BookingNotFound(),
            CustomerViewingBookingOperationStatus.Conflict => BookingConflict(),
            _ => BookingUnavailable()
        };
    }

    [HttpPost("{bookingId}/cancel")]
    public async Task<IActionResult> CancelBooking(Guid bookingId, CancelCustomerViewingBookingRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (bookingId == Guid.Empty) ModelState.AddModelError("bookingId", "BookingId is required.");
        var rowVersion = ParseRowVersion(request.RowVersion);
        var reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();
        if (reason?.Length > 1000) ModelState.AddModelError("reason", "Reason must not exceed 1000 characters.");
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = await service.CancelBookingAsync(userId, bookingId, rowVersion!, reason, cancellationToken);
        return result switch
        {
            CustomerViewingBookingOperationStatus.Succeeded => NoContent(),
            CustomerViewingBookingOperationStatus.NotFound => BookingNotFound(),
            CustomerViewingBookingOperationStatus.Conflict => BookingConflict(),
            _ => BookingUnavailable()
        };
    }

    private ViewingBookingStatus? ParseStatus(string? value)
    {
        if (value is null) return null;
        var normalized = value.Trim();
        if (!string.IsNullOrEmpty(normalized) && Enum.TryParse<ViewingBookingStatus>(normalized, true, out var candidate) && Enum.IsDefined(candidate) && string.Equals(normalized, candidate.ToString(), StringComparison.OrdinalIgnoreCase)) return candidate;
        ModelState.AddModelError("status", "Status is invalid."); return null;
    }

    private byte[]? ParseRowVersion(string? value)
    {
        try { var bytes = string.IsNullOrWhiteSpace(value) ? null : Convert.FromBase64String(value); if (bytes is { Length: 8 }) return bytes; } catch (FormatException) { }
        ModelState.AddModelError("rowVersion", "RowVersion must be Base64 encoded eight bytes."); return null;
    }

    private bool TryGetUserId(out Guid userId) => Guid.TryParse(User.FindFirst("sub")?.Value, out userId) && userId != Guid.Empty;
    private ActionResult InvalidRoute(string key, string message) { ModelState.AddModelError(key, message); return ValidationProblem(ModelState); }
    private ObjectResult BookingNotFound() => Problem(statusCode: StatusCodes.Status404NotFound, title: "Viewing booking not found.");
    private ObjectResult BookingConflict() => Problem(statusCode: StatusCodes.Status409Conflict, title: "Viewing booking operation conflict.");
    private ObjectResult BookingUnavailable() => Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Viewing booking operation is temporarily unavailable.");
}
