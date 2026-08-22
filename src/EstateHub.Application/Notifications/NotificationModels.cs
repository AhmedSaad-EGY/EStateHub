using System.Text.Json;

namespace EstateHub.Application.Notifications;

public static class NotificationTypeCodes
{
    public const string CompanyApplicationStatusChanged = "CompanyApplicationStatusChanged";
    public const string ViewingBookingStatusChanged = "ViewingBookingStatusChanged";
    public const string ViewingBookingRescheduled = "ViewingBookingRescheduled";
    public const string CompanyReviewModerated = "CompanyReviewModerated";
}

public enum NotificationInboxState
{
    Unread,
    Read,
    Archived
}

public sealed record NotificationDirectoryQuery(
    int PageNumber,
    int PageSize,
    NotificationInboxState? State);

public sealed record NotificationItem(
    Guid Id,
    string Type,
    string Title,
    string Body,
    JsonElement? Payload,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt,
    DateTimeOffset? ArchivedAt)
{
    public bool IsRead => ReadAt is not null;
    public bool IsArchived => ArchivedAt is not null;
}

public enum NotificationOperationStatus
{
    Succeeded,
    NotFound,
    ServiceUnavailable
}

public sealed record NotificationResult<T>(
    NotificationOperationStatus Status,
    T? Value = default);
