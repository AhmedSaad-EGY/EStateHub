using System.Text.Json;
using EstateHub.Application.Common;
using EstateHub.Application.Notifications;

namespace EstateHub.Api.Contracts.Notifications;

public sealed class NotificationDirectoryRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? State { get; set; }
}

public sealed record NotificationResponse(
    Guid Id,
    string Type,
    string Title,
    string Body,
    JsonElement? Payload,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt,
    DateTimeOffset? ArchivedAt,
    bool IsRead,
    bool IsArchived)
{
    public static NotificationResponse From(NotificationItem notification) => new(
        notification.Id,
        notification.Type,
        notification.Title,
        notification.Body,
        notification.Payload,
        notification.CreatedAt,
        notification.ReadAt,
        notification.ArchivedAt,
        notification.IsRead,
        notification.IsArchived);
}

public sealed record NotificationDirectoryResponse(
    IReadOnlyList<NotificationResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage)
{
    public static NotificationDirectoryResponse From(PagedResult<NotificationItem> result) => new(
        result.Items.Select(NotificationResponse.From).ToList(),
        result.PageNumber,
        result.PageSize,
        result.TotalCount,
        result.TotalPages,
        result.HasPreviousPage,
        result.HasNextPage);
}

public sealed record NotificationUnreadCountResponse(int UnreadCount);
