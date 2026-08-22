using EstateHub.Api.Authorization.CompanyPermissions;
using EstateHub.Api.Contracts.CompanyViewingSlots;
using EstateHub.Application.CompanyAccess;
using EstateHub.Application.CompanyViewingSlots;
using EstateHub.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EstateHub.Api.Controllers;

[ApiController]
[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/company/viewing-slots")]
public sealed class CompanyViewingSlotsController(ICompanyViewingSlotManagementService service) : ControllerBase
{
    [HttpGet]
    [RequireCompanyPermission(CompanyPermissionCodes.BookingsRead)]
    public async Task<ActionResult<CompanyViewingSlotsResponse>> GetSlots([FromQuery] CompanyViewingSlotDirectoryRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var query = BuildDirectoryQuery(request);
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = await service.GetSlotsAsync(userId, query, cancellationToken);
        return result is null ? SlotNotFound() : Ok(CompanyViewingSlotsResponse.From(result));
    }

    [HttpGet("{slotId}")]
    [RequireCompanyPermission(CompanyPermissionCodes.BookingsRead)]
    public async Task<ActionResult<CompanyViewingSlotResponse>> GetSlot(Guid slotId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (slotId == Guid.Empty) return InvalidRoute("slotId", "SlotId is required.");
        var result = await service.GetSlotAsync(userId, slotId, cancellationToken);
        return result is null ? SlotNotFound() : Ok(CompanyViewingSlotResponse.From(result));
    }

    [HttpPost]
    [RequireCompanyPermission(CompanyPermissionCodes.BookingsManage)]
    public async Task<ActionResult<CompanyViewingSlotResponse>> CreateSlot(CreateCompanyViewingSlotRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var command = BuildCommand(request.ListingId, request.StartsAt, request.EndsAt, request.Capacity, request.MeetingPoint, null, false);
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = await service.CreateSlotAsync(userId, command, cancellationToken);
        return Mutation(result, true);
    }

    [HttpPut("{slotId}")]
    [RequireCompanyPermission(CompanyPermissionCodes.BookingsManage)]
    public async Task<ActionResult<CompanyViewingSlotResponse>> UpdateSlot(Guid slotId, UpdateCompanyViewingSlotRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (slotId == Guid.Empty) ModelState.AddModelError("slotId", "SlotId is required.");
        var command = BuildCommand(Guid.Empty, request.StartsAt, request.EndsAt, request.Capacity, request.MeetingPoint, request.RowVersion, true);
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = await service.UpdateSlotAsync(userId, slotId, command, cancellationToken);
        return Mutation(result, false);
    }

    [HttpPost("{slotId}/close")]
    [RequireCompanyPermission(CompanyPermissionCodes.BookingsManage)]
    public Task<IActionResult> CloseSlot(Guid slotId, CompanyViewingSlotLifecycleRequest request, CancellationToken cancellationToken) => Lifecycle(slotId, request, LifecycleOperation.Close, cancellationToken);

    [HttpPost("{slotId}/reopen")]
    [RequireCompanyPermission(CompanyPermissionCodes.BookingsManage)]
    public Task<IActionResult> ReopenSlot(Guid slotId, CompanyViewingSlotLifecycleRequest request, CancellationToken cancellationToken) => Lifecycle(slotId, request, LifecycleOperation.Reopen, cancellationToken);

    [HttpPost("{slotId}/cancel")]
    [RequireCompanyPermission(CompanyPermissionCodes.BookingsManage)]
    public Task<IActionResult> CancelSlot(Guid slotId, CompanyViewingSlotLifecycleRequest request, CancellationToken cancellationToken) => Lifecycle(slotId, request, LifecycleOperation.Cancel, cancellationToken);

    private async Task<IActionResult> Lifecycle(Guid slotId, CompanyViewingSlotLifecycleRequest request, LifecycleOperation operation, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (slotId == Guid.Empty) ModelState.AddModelError("slotId", "SlotId is required.");
        var rowVersion = ParseRowVersion(request.RowVersion);
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = operation switch
        {
            LifecycleOperation.Close => await service.CloseSlotAsync(userId, slotId, rowVersion!, cancellationToken),
            LifecycleOperation.Reopen => await service.ReopenSlotAsync(userId, slotId, rowVersion!, cancellationToken),
            _ => await service.CancelSlotAsync(userId, slotId, rowVersion!, cancellationToken)
        };
        return result switch
        {
            CompanyViewingSlotOperationStatus.Succeeded => NoContent(),
            CompanyViewingSlotOperationStatus.NotFound => SlotNotFound(),
            CompanyViewingSlotOperationStatus.Conflict => SlotConflict(),
            _ => SlotUnavailable()
        };
    }

    private ActionResult<CompanyViewingSlotResponse> Mutation(CompanyViewingSlotMutationResult result, bool created) => result.Status switch
    {
        CompanyViewingSlotOperationStatus.Succeeded when created => CreatedAtAction(nameof(GetSlot), new { slotId = result.Slot!.Id }, CompanyViewingSlotResponse.From(result.Slot)),
        CompanyViewingSlotOperationStatus.Succeeded => Ok(CompanyViewingSlotResponse.From(result.Slot!)),
        CompanyViewingSlotOperationStatus.NotFound => SlotNotFound(),
        CompanyViewingSlotOperationStatus.InvalidRequest => BadRequest(new ProblemDetails { Status = StatusCodes.Status400BadRequest, Title = "Viewing slot request is invalid." }),
        CompanyViewingSlotOperationStatus.Conflict => SlotConflict(),
        _ => SlotUnavailable()
    };

    private CompanyViewingSlotDirectoryQuery BuildDirectoryQuery(CompanyViewingSlotDirectoryRequest request)
    {
        if (request.PageNumber is < 1 or > 10000) ModelState.AddModelError("pageNumber", "PageNumber must be between 1 and 10000.");
        if (request.PageSize is < 1 or > 50) ModelState.AddModelError("pageSize", "PageSize must be between 1 and 50.");
        if (request.ListingId == Guid.Empty) ModelState.AddModelError("listingId", "ListingId cannot be empty when supplied.");
        if (request.From is not null && request.To is not null && request.From >= request.To) ModelState.AddModelError("to", "To must be later than From.");
        var status = ParseEnum<ViewingSlotStatus>(request.Status, "status", false);
        return new CompanyViewingSlotDirectoryQuery(request.PageNumber, request.PageSize, request.ListingId, status, request.From, request.To);
    }

    private CompanyViewingSlotCommand BuildCommand(Guid listingId, DateTimeOffset startsAt, DateTimeOffset endsAt, int capacity, string? meetingPointValue, string? rowVersionValue, bool requireRowVersion)
    {
        if (!requireRowVersion && listingId == Guid.Empty) ModelState.AddModelError("listingId", "ListingId is required.");
        if (endsAt <= startsAt) ModelState.AddModelError("endsAt", "EndsAt must be later than StartsAt.");
        if (capacity <= 0) ModelState.AddModelError("capacity", "Capacity must be positive.");
        var meetingPoint = meetingPointValue?.Trim();
        if (string.IsNullOrWhiteSpace(meetingPoint) || meetingPoint.Length > 500) ModelState.AddModelError("meetingPoint", "MeetingPoint is required and must not exceed 500 characters.");
        var rowVersion = requireRowVersion ? ParseRowVersion(rowVersionValue) : null;
        return new CompanyViewingSlotCommand(listingId, startsAt, endsAt, capacity, meetingPoint ?? string.Empty, rowVersion);
    }

    private T? ParseEnum<T>(string? value, string key, bool required) where T : struct, Enum
    {
        if (value is null) { if (required) ModelState.AddModelError(key, $"{key} is required."); return null; }
        var normalized = value.Trim();
        if (!string.IsNullOrEmpty(normalized) && Enum.TryParse<T>(normalized, true, out var candidate) && Enum.IsDefined(candidate) && string.Equals(normalized, candidate.ToString(), StringComparison.OrdinalIgnoreCase)) return candidate;
        ModelState.AddModelError(key, $"{key} is invalid."); return null;
    }

    private byte[]? ParseRowVersion(string? value)
    {
        try { var bytes = string.IsNullOrWhiteSpace(value) ? null : Convert.FromBase64String(value); if (bytes is { Length: 8 }) return bytes; } catch (FormatException) { }
        ModelState.AddModelError("rowVersion", "RowVersion must be Base64 encoded eight bytes."); return null;
    }

    private bool TryGetUserId(out Guid userId) => Guid.TryParse(User.FindFirst("sub")?.Value, out userId) && userId != Guid.Empty;
    private ActionResult InvalidRoute(string key, string message) { ModelState.AddModelError(key, message); return ValidationProblem(ModelState); }
    private ObjectResult SlotNotFound() => Problem(statusCode: StatusCodes.Status404NotFound, title: "Viewing slot not found.");
    private ObjectResult SlotConflict() => Problem(statusCode: StatusCodes.Status409Conflict, title: "Viewing slot operation conflict.");
    private ObjectResult SlotUnavailable() => Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Viewing slot operation is temporarily unavailable.");
    private enum LifecycleOperation { Close, Reopen, Cancel }
}
