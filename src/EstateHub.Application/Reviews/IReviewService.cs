namespace EstateHub.Application.Reviews;

public interface IReviewService
{
    Task<PublicCompanyReviews?> GetPublicCompanyReviewsAsync(
        string companySlug,
        PublicCompanyReviewQuery query,
        CancellationToken cancellationToken = default);

    Task<CustomerReview?> GetCustomerReviewAsync(
        Guid applicationUserId,
        Guid bookingId,
        CancellationToken cancellationToken = default);

    Task<ReviewMutationResult> CreateReviewAsync(
        Guid applicationUserId,
        Guid bookingId,
        ReviewCommand command,
        CancellationToken cancellationToken = default);

    Task<ReviewMutationResult> UpdateReviewAsync(
        Guid applicationUserId,
        Guid reviewId,
        ReviewCommand command,
        CancellationToken cancellationToken = default);

    Task<ReviewOperationStatus> DeleteReviewAsync(
        Guid applicationUserId,
        Guid reviewId,
        CancellationToken cancellationToken = default);
}
