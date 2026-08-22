using EstateHub.Domain.Enums;

namespace EstateHub.Application.Reviews;

public sealed record PublicCompanyReviewItem(
    Guid Id,
    int Rating,
    string? Comment,
    DateTimeOffset CreatedAt,
    string ReviewerName);

public sealed record PublicCompanyReviews(
    Guid CompanyId,
    string CompanySlug,
    double? AverageRating,
    int ReviewCount,
    int FilteredCount,
    IReadOnlyList<PublicCompanyReviewItem> Items,
    int PageNumber,
    int PageSize)
{
    public int TotalPages => FilteredCount == 0
        ? 0
        : (int)Math.Ceiling(FilteredCount / (double)PageSize);

    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}

public sealed record PublicCompanyReviewQuery(
    int PageNumber,
    int PageSize,
    int? Rating);

public sealed record CustomerReview(
    Guid Id,
    Guid ViewingBookingId,
    int Rating,
    string? Comment,
    CompanyReviewStatus Status,
    DateTimeOffset CreatedAt);

public sealed record ReviewCommand(
    int Rating,
    string? Comment);

public enum ReviewOperationStatus
{
    Succeeded,
    NotFound,
    Conflict,
    ServiceUnavailable
}

public sealed record ReviewMutationResult(
    ReviewOperationStatus Status,
    CustomerReview? Review = null);

