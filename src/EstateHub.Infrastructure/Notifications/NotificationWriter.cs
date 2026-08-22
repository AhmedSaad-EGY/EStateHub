using System.Text.Json;
using EstateHub.Application.Notifications;
using EstateHub.Domain.Entities.Users;
using EstateHub.Domain.Enums;
using EstateHub.Infrastructure.Persistence;

namespace EstateHub.Infrastructure.Notifications;

internal sealed class NotificationWriter(
    EstateHubDbContext dbContext,
    TimeProvider timeProvider) : INotificationWriter
{
    private static readonly JsonSerializerOptions PayloadJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public void Add(NotificationWriteCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.RecipientApplicationUserId == Guid.Empty)
        {
            throw new ArgumentException("A notification recipient is required.", nameof(command));
        }

        var title = NormalizeRequired(command.Title, 200, nameof(command.Title));
        var body = NormalizeRequired(command.Body, 4000, nameof(command.Body));
        var (type, payload) = CreatePayload(command.Payload);
        if (type.Length > 100)
        {
            throw new ArgumentException("The notification type is too long.", nameof(command));
        }

        dbContext.Add(new Notification
        {
            Id = Guid.NewGuid(),
            RecipientApplicationUserId = command.RecipientApplicationUserId,
            Type = type,
            Title = title,
            Body = body,
            PayloadJson = JsonSerializer.Serialize(payload, PayloadJsonOptions),
            CreatedAt = timeProvider.GetUtcNow(),
            ReadAt = null,
            ArchivedAt = null
        });
    }

    private static (string Type, object Payload) CreatePayload(NotificationPayload payload) =>
        payload switch
        {
            CompanyApplicationStatusChangedNotificationPayload value
                when value.ApplicationId != Guid.Empty =>
                (
                    NotificationTypeCodes.CompanyApplicationStatusChanged,
                    new
                    {
                        value.ApplicationId,
                        Status = CompanyApplicationStatusText(value.Status)
                    }),
            ViewingBookingStatusChangedNotificationPayload value
                when value.BookingId != Guid.Empty =>
                (
                    NotificationTypeCodes.ViewingBookingStatusChanged,
                    new
                    {
                        value.BookingId,
                        Status = ViewingBookingStatusText(value.Status)
                    }),
            ViewingBookingRescheduledNotificationPayload value
                when value.BookingId != Guid.Empty && value.ViewingSlotId != Guid.Empty =>
                (
                    NotificationTypeCodes.ViewingBookingRescheduled,
                    new
                    {
                        value.BookingId,
                        Status = ViewingBookingStatusText(value.Status),
                        value.ViewingSlotId
                    }),
            CompanyReviewModeratedNotificationPayload value
                when value.ReviewId != Guid.Empty && value.ViewingBookingId != Guid.Empty =>
                (
                    NotificationTypeCodes.CompanyReviewModerated,
                    new
                    {
                        value.ReviewId,
                        value.ViewingBookingId,
                        Status = CompanyReviewStatusText(value.Status)
                    }),
            _ => throw new ArgumentException("The notification payload is invalid.", nameof(payload))
        };

    private static string NormalizeRequired(string value, int maximumLength, string parameterName)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > maximumLength)
        {
            throw new ArgumentException("Notification text is invalid.", parameterName);
        }

        return normalized;
    }

    private static string CompanyApplicationStatusText(CompanyApplicationStatus status) => status switch
    {
        CompanyApplicationStatus.NeedsChanges => "NeedsChanges",
        CompanyApplicationStatus.Rejected => "Rejected",
        CompanyApplicationStatus.Approved => "Approved",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unsupported notification status.")
    };

    private static string ViewingBookingStatusText(ViewingBookingStatus status) => status switch
    {
        ViewingBookingStatus.Pending => "Pending",
        ViewingBookingStatus.Confirmed => "Confirmed",
        ViewingBookingStatus.CheckedIn => "CheckedIn",
        ViewingBookingStatus.Completed => "Completed",
        ViewingBookingStatus.Rejected => "Rejected",
        ViewingBookingStatus.Cancelled => "Cancelled",
        ViewingBookingStatus.NoShow => "NoShow",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unsupported notification status.")
    };

    private static string CompanyReviewStatusText(CompanyReviewStatus status) => status switch
    {
        CompanyReviewStatus.Visible => "Visible",
        CompanyReviewStatus.Hidden => "Hidden",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unsupported notification status.")
    };
}
