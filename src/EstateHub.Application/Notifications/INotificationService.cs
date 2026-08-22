using EstateHub.Application.Common;

namespace EstateHub.Application.Notifications;

public interface INotificationService
{
    Task<NotificationResult<PagedResult<NotificationItem>>> GetNotificationsAsync(
        Guid applicationUserId,
        NotificationDirectoryQuery query,
        CancellationToken cancellationToken = default);

    Task<NotificationResult<int>> GetUnreadCountAsync(
        Guid applicationUserId,
        CancellationToken cancellationToken = default);

    Task<NotificationOperationStatus> MarkReadAsync(
        Guid applicationUserId,
        Guid notificationId,
        CancellationToken cancellationToken = default);

    Task<NotificationOperationStatus> MarkAllReadAsync(
        Guid applicationUserId,
        CancellationToken cancellationToken = default);

    Task<NotificationOperationStatus> ArchiveAsync(
        Guid applicationUserId,
        Guid notificationId,
        CancellationToken cancellationToken = default);
}
