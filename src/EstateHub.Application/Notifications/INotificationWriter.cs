using EstateHub.Domain.Enums;

namespace EstateHub.Application.Notifications;

public interface INotificationWriter
{
    void Add(NotificationWriteCommand command);
}

public sealed record NotificationWriteCommand(
    Guid RecipientApplicationUserId,
    string Title,
    string Body,
    NotificationPayload Payload);

public abstract record NotificationPayload;

public sealed record CompanyApplicationStatusChangedNotificationPayload(
    Guid ApplicationId,
    CompanyApplicationStatus Status) : NotificationPayload;

public sealed record ViewingBookingStatusChangedNotificationPayload(
    Guid BookingId,
    ViewingBookingStatus Status) : NotificationPayload;

public sealed record ViewingBookingRescheduledNotificationPayload(
    Guid BookingId,
    ViewingBookingStatus Status,
    Guid ViewingSlotId) : NotificationPayload;

public sealed record CompanyReviewModeratedNotificationPayload(
    Guid ReviewId,
    Guid ViewingBookingId,
    CompanyReviewStatus Status) : NotificationPayload;
