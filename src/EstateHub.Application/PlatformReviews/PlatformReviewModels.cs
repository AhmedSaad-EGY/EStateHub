using EstateHub.Domain.Enums;

namespace EstateHub.Application.PlatformReviews;

public sealed record PlatformReviewDirectoryQuery(
    int PageNumber,
    int PageSize,
    CompanyReviewStatus? Status,
    int? Rating,
    Guid? CompanyId,
    DateTimeOffset? CreatedFrom,
    DateTimeOffset? CreatedTo);

public sealed record PlatformReviewCompanySummary(
    Guid Id,
    string Slug,
    string DisplayName);

public sealed record PlatformReviewListingSummary(
    Guid Id,
    string Slug,
    string Title);

public sealed record PlatformReviewReviewerSummary(
    Guid Id,
    string FullName);

public sealed record PlatformReviewSummary(
    Guid Id,
    Guid ViewingBookingId,
    int Rating,
    string? Comment,
    CompanyReviewStatus Status,
    DateTimeOffset CreatedAt,
    Guid? ModeratedByApplicationUserId,
    DateTimeOffset? ModeratedAt,
    string? ModerationReason,
    PlatformReviewCompanySummary Company,
    PlatformReviewListingSummary Listing,
    PlatformReviewReviewerSummary Reviewer);

public sealed record PlatformReviewDetails(
    PlatformReviewSummary Review,
    string BookingCode,
    ViewingBookingStatus BookingStatus,
    DateTimeOffset? CompletedAt,
    DateTimeOffset StartsAt);

public sealed record PlatformReviewModerationCommand(
    CompanyReviewStatus ExpectedStatus,
    CompanyReviewStatus Status,
    string? Reason);

public enum PlatformReviewModerationOperationStatus
{
    Succeeded,
    NotFound,
    Conflict,
    ServiceUnavailable
}

public sealed record PlatformReviewModerationResult<T>(
    PlatformReviewModerationOperationStatus Status,
    T? Value = default);
