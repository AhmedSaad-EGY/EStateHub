using EstateHub.Api.Contracts.Notifications;
using EstateHub.Application.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EstateHub.Api.Controllers;

[ApiController]
[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/me/notifications")]
public sealed class MyNotificationsController(
    INotificationService notificationService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<NotificationDirectoryResponse>> GetNotifications(
        [FromQuery] NotificationDirectoryRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var applicationUserId))
        {
            return Unauthorized();
        }

        if (request.PageNumber is < 1 or > 10000)
        {
            ModelState.AddModelError("pageNumber", "PageNumber must be between 1 and 10000.");
        }

        if (request.PageSize is < 1 or > 50)
        {
            ModelState.AddModelError("pageSize", "PageSize must be between 1 and 50.");
        }

        NotificationInboxState? state = null;
        if (request.State is not null && !TryParseState(request.State, out state))
        {
            ModelState.AddModelError("state", "State must be Unread, Read, or Archived.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await notificationService.GetNotificationsAsync(
            applicationUserId,
            new NotificationDirectoryQuery(
                request.PageNumber,
                request.PageSize,
                state),
            cancellationToken);
        return result.Status == NotificationOperationStatus.Succeeded
            ? Ok(NotificationDirectoryResponse.From(result.Value!))
            : NotificationUnavailable();
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<NotificationUnreadCountResponse>> GetUnreadCount(
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var applicationUserId))
        {
            return Unauthorized();
        }

        var result = await notificationService.GetUnreadCountAsync(
            applicationUserId,
            cancellationToken);
        return result.Status == NotificationOperationStatus.Succeeded
            ? Ok(new NotificationUnreadCountResponse(result.Value))
            : NotificationUnavailable();
    }

    [HttpPut("{notificationId}/read")]
    public Task<IActionResult> MarkRead(
        Guid notificationId,
        CancellationToken cancellationToken) =>
        ExecuteIndividualMutation(
            notificationId,
            notificationService.MarkReadAsync,
            cancellationToken);

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var applicationUserId))
        {
            return Unauthorized();
        }

        var result = await notificationService.MarkAllReadAsync(
            applicationUserId,
            cancellationToken);
        return result == NotificationOperationStatus.Succeeded
            ? NoContent()
            : NotificationUnavailable();
    }

    [HttpPut("{notificationId}/archive")]
    public Task<IActionResult> Archive(
        Guid notificationId,
        CancellationToken cancellationToken) =>
        ExecuteIndividualMutation(
            notificationId,
            notificationService.ArchiveAsync,
            cancellationToken);

    private async Task<IActionResult> ExecuteIndividualMutation(
        Guid notificationId,
        Func<Guid, Guid, CancellationToken, Task<NotificationOperationStatus>> operation,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var applicationUserId))
        {
            return Unauthorized();
        }

        if (notificationId == Guid.Empty)
        {
            ModelState.AddModelError("notificationId", "NotificationId is required.");
            return ValidationProblem(ModelState);
        }

        var result = await operation(
            applicationUserId,
            notificationId,
            cancellationToken);
        return result switch
        {
            NotificationOperationStatus.Succeeded => NoContent(),
            NotificationOperationStatus.NotFound => NotificationNotFound(),
            _ => NotificationUnavailable()
        };
    }

    private static bool TryParseState(
        string value,
        out NotificationInboxState? state)
    {
        var normalized = value.Trim();
        if (string.Equals(normalized, "Unread", StringComparison.OrdinalIgnoreCase))
        {
            state = NotificationInboxState.Unread;
            return true;
        }

        if (string.Equals(normalized, "Read", StringComparison.OrdinalIgnoreCase))
        {
            state = NotificationInboxState.Read;
            return true;
        }

        if (string.Equals(normalized, "Archived", StringComparison.OrdinalIgnoreCase))
        {
            state = NotificationInboxState.Archived;
            return true;
        }

        state = null;
        return false;
    }

    private bool TryGetUserId(out Guid applicationUserId) =>
        Guid.TryParse(User.FindFirst("sub")?.Value, out applicationUserId)
        && applicationUserId != Guid.Empty;

    private ObjectResult NotificationNotFound() => Problem(
        statusCode: StatusCodes.Status404NotFound,
        title: "Notification not found.");

    private ObjectResult NotificationUnavailable() => Problem(
        statusCode: StatusCodes.Status503ServiceUnavailable,
        title: "Notification service is temporarily unavailable.");
}
