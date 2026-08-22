using EstateHub.Application.Reviews;
using EstateHub.Domain.Enums;

namespace EstateHub.Api.Contracts.Reviews;

public sealed class PublicCompanyReviewRequest
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public int? Rating { get; init; }
}

public sealed record ReviewRequest(int Rating, string? Comment);

public sealed record PublicCompanyReviewResponse(
    Guid Id,
    int Rating,
    string? Comment,
    DateTimeOffset CreatedAt,
    string ReviewerName)
{
    public static PublicCompanyReviewResponse From(PublicCompanyReviewItem value) => new(
        value.Id,
        value.Rating,
        value.Comment,
        value.CreatedAt,
        value.ReviewerName);
}

public sealed record PublicCompanyReviewsResponse(
    Guid CompanyId,
    string CompanySlug,
    double? AverageRating,
    int ReviewCount,
    int FilteredCount,
    IReadOnlyList<PublicCompanyReviewResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage)
{
    public static PublicCompanyReviewsResponse From(PublicCompanyReviews value) => new(
        value.CompanyId,
        value.CompanySlug,
        value.AverageRating,
        value.ReviewCount,
        value.FilteredCount,
        value.Items.Select(PublicCompanyReviewResponse.From).ToList(),
        value.PageNumber,
        value.PageSize,
        value.TotalPages,
        value.HasPreviousPage,
        value.HasNextPage);
}

public sealed record CustomerReviewResponse(
    Guid Id,
    Guid ViewingBookingId,
    int Rating,
    string? Comment,
    string Status,
    DateTimeOffset CreatedAt)
{
    public static CustomerReviewResponse From(CustomerReview value) => new(
        value.Id,
        value.ViewingBookingId,
        value.Rating,
        value.Comment,
        ReviewEnumText.Status(value.Status),
        value.CreatedAt);
}

internal static class ReviewEnumText
{
    public static string Status(CompanyReviewStatus value) => value switch
    {
        CompanyReviewStatus.Visible => "Visible",
        CompanyReviewStatus.Hidden => "Hidden",
        CompanyReviewStatus.PendingModeration => "PendingModeration",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };
}

