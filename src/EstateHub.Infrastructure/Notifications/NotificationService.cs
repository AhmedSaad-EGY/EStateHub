using System.Data.Common;
using System.Text.Json;
using EstateHub.Application.Common;
using EstateHub.Application.Notifications;
using EstateHub.Domain.Entities.Users;
using EstateHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EstateHub.Infrastructure.Notifications;

public sealed class NotificationService(
    EstateHubDbContext dbContext,
    TimeProvider timeProvider) : INotificationService
{
    public async Task<NotificationResult<PagedResult<NotificationItem>>> GetNotificationsAsync(
        Guid applicationUserId,
        NotificationDirectoryQuery query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var notifications = OwnedNotifications(applicationUserId);
            notifications = query.State switch
            {
                null => notifications.Where(notification => notification.ArchivedAt == null),
                NotificationInboxState.Unread => notifications.Where(notification =>
                    notification.ReadAt == null && notification.ArchivedAt == null),
                NotificationInboxState.Read => notifications.Where(notification =>
                    notification.ReadAt != null && notification.ArchivedAt == null),
                NotificationInboxState.Archived => notifications.Where(notification =>
                    notification.ArchivedAt != null),
                _ => throw new ArgumentOutOfRangeException(nameof(query), query.State, "Unknown inbox state.")
            };

            var totalCount = await notifications.CountAsync(cancellationToken);
            var rows = await notifications
                .OrderByDescending(notification => notification.CreatedAt)
                .ThenByDescending(notification => notification.Id)
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(notification => new NotificationRow(
                    notification.Id,
                    notification.Type,
                    notification.Title,
                    notification.Body,
                    notification.PayloadJson,
                    notification.CreatedAt,
                    notification.ReadAt,
                    notification.ArchivedAt))
                .ToListAsync(cancellationToken);

            var items = new List<NotificationItem>(rows.Count);
            foreach (var row in rows)
            {
                if (!TryParsePayload(row.PayloadJson, out var payload))
                {
                    return new(NotificationOperationStatus.ServiceUnavailable);
                }

                items.Add(new NotificationItem(
                    row.Id,
                    row.Type,
                    row.Title,
                    row.Body,
                    payload,
                    row.CreatedAt,
                    row.ReadAt,
                    row.ArchivedAt));
            }

            return new(
                NotificationOperationStatus.Succeeded,
                new PagedResult<NotificationItem>(
                    items,
                    query.PageNumber,
                    query.PageSize,
                    totalCount));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (JsonException)
        {
            return new(NotificationOperationStatus.ServiceUnavailable);
        }
        catch (DbException)
        {
            return new(NotificationOperationStatus.ServiceUnavailable);
        }
    }

    public async Task<NotificationResult<int>> GetUnreadCountAsync(
        Guid applicationUserId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var count = await OwnedNotifications(applicationUserId)
                .CountAsync(notification =>
                    notification.ReadAt == null && notification.ArchivedAt == null,
                    cancellationToken);
            return new(NotificationOperationStatus.Succeeded, count);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DbException)
        {
            return new(NotificationOperationStatus.ServiceUnavailable);
        }
    }

    public Task<NotificationOperationStatus> MarkReadAsync(
        Guid applicationUserId,
        Guid notificationId,
        CancellationToken cancellationToken = default) =>
        SetTimestampOnceAsync(
            applicationUserId,
            notificationId,
            TimestampOperation.Read,
            cancellationToken);

    public async Task<NotificationOperationStatus> MarkAllReadAsync(
        Guid applicationUserId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var readAt = timeProvider.GetUtcNow();
            await dbContext.Set<Notification>()
                .Where(notification =>
                    notification.RecipientApplicationUserId == applicationUserId
                    && notification.ReadAt == null
                    && notification.ArchivedAt == null)
                .ExecuteUpdateAsync(
                    updates => updates.SetProperty(notification => notification.ReadAt, readAt),
                    cancellationToken);
            return NotificationOperationStatus.Succeeded;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DbUpdateException)
        {
            return NotificationOperationStatus.ServiceUnavailable;
        }
        catch (DbException)
        {
            return NotificationOperationStatus.ServiceUnavailable;
        }
    }

    public Task<NotificationOperationStatus> ArchiveAsync(
        Guid applicationUserId,
        Guid notificationId,
        CancellationToken cancellationToken = default) =>
        SetTimestampOnceAsync(
            applicationUserId,
            notificationId,
            TimestampOperation.Archive,
            cancellationToken);

    private async Task<NotificationOperationStatus> SetTimestampOnceAsync(
        Guid applicationUserId,
        Guid notificationId,
        TimestampOperation operation,
        CancellationToken cancellationToken)
    {
        try
        {
            var utcNow = timeProvider.GetUtcNow();
            var notifications = dbContext.Set<Notification>().Where(notification =>
                notification.Id == notificationId
                && notification.RecipientApplicationUserId == applicationUserId);
            var affected = operation switch
            {
                TimestampOperation.Read => await notifications
                    .Where(notification => notification.ReadAt == null)
                    .ExecuteUpdateAsync(
                        updates => updates.SetProperty(notification => notification.ReadAt, utcNow),
                        cancellationToken),
                TimestampOperation.Archive => await notifications
                    .Where(notification => notification.ArchivedAt == null)
                    .ExecuteUpdateAsync(
                        updates => updates.SetProperty(notification => notification.ArchivedAt, utcNow),
                        cancellationToken),
                _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
            };
            if (affected == 1)
            {
                return NotificationOperationStatus.Succeeded;
            }

            var exists = await notifications
                .AsNoTracking()
                .AnyAsync(cancellationToken);
            return exists
                ? NotificationOperationStatus.Succeeded
                : NotificationOperationStatus.NotFound;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DbUpdateException)
        {
            return NotificationOperationStatus.ServiceUnavailable;
        }
        catch (DbException)
        {
            return NotificationOperationStatus.ServiceUnavailable;
        }
    }

    private IQueryable<Notification> OwnedNotifications(Guid applicationUserId) =>
        dbContext.Set<Notification>()
            .AsNoTracking()
            .Where(notification =>
                notification.RecipientApplicationUserId == applicationUserId);

    private static bool TryParsePayload(
        string? payloadJson,
        out JsonElement? payload)
    {
        if (payloadJson is null)
        {
            payload = null;
            return true;
        }

        using var document = JsonDocument.Parse(payloadJson);
        if (document.RootElement.ValueKind == JsonValueKind.Null)
        {
            payload = null;
            return true;
        }

        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            payload = null;
            return false;
        }

        payload = document.RootElement.Clone();
        return true;
    }

    private sealed record NotificationRow(
        Guid Id,
        string Type,
        string Title,
        string Body,
        string? PayloadJson,
        DateTimeOffset CreatedAt,
        DateTimeOffset? ReadAt,
        DateTimeOffset? ArchivedAt);

    private enum TimestampOperation
    {
        Read,
        Archive
    }
}
