using EstateHub.Application.Common;

namespace EstateHub.Application.PlatformReviews;

public interface IPlatformReviewModerationService
{
    Task<PlatformReviewModerationResult<PagedResult<PlatformReviewSummary>>> GetReviewsAsync(
        PlatformReviewDirectoryQuery query,
        CancellationToken cancellationToken = default);

    Task<PlatformReviewModerationResult<PlatformReviewDetails>> GetReviewAsync(
        Guid reviewId,
        CancellationToken cancellationToken = default);

    Task<PlatformReviewModerationOperationStatus> ModerateAsync(
        Guid actingPlatformAdminId,
        Guid reviewId,
        PlatformReviewModerationCommand command,
        CancellationToken cancellationToken = default);
}
