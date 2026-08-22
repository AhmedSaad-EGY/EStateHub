using EstateHub.Application.Common;
using EstateHub.Application.PlatformReviews;
using EstateHub.Domain.Enums;

namespace EstateHub.Api.Contracts.PlatformReviews;

public sealed class PlatformReviewDirectoryRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Status { get; set; }
    public int? Rating { get; set; }
    public Guid? CompanyId { get; set; }
    public DateTimeOffset? CreatedFrom { get; set; }
    public DateTimeOffset? CreatedTo { get; set; }
}

public sealed class PlatformReviewModerationRequest
{
    public string? ExpectedStatus { get; set; }
    public string? Status { get; set; }
    public string? Reason { get; set; }
}

public sealed record PlatformReviewCompanyResponse(
    Guid Id,
    string Slug,
    string DisplayName)
{
    public static PlatformReviewCompanyResponse From(PlatformReviewCompanySummary company) =>
        new(company.Id, company.Slug, company.DisplayName);
}

public sealed record PlatformReviewListingResponse(
    Guid Id,
    string Slug,
    string Title)
{
    public static PlatformReviewListingResponse From(PlatformReviewListingSummary listing) =>
        new(listing.Id, listing.Slug, listing.Title);
}

public sealed record PlatformReviewReviewerResponse(
    Guid Id,
    string FullName)
{
    public static PlatformReviewReviewerResponse From(PlatformReviewReviewerSummary reviewer) =>
        new(reviewer.Id, reviewer.FullName);
}

public sealed record PlatformReviewSummaryResponse(
    Guid Id,
    Guid ViewingBookingId,
    int Rating,
    string? Comment,
    string Status,
    DateTimeOffset CreatedAt,
    Guid? ModeratedByApplicationUserId,
    DateTimeOffset? ModeratedAt,
    string? ModerationReason,
    PlatformReviewCompanyResponse Company,
    PlatformReviewListingResponse Listing,
    PlatformReviewReviewerResponse Reviewer)
{
    public static PlatformReviewSummaryResponse From(PlatformReviewSummary review) => new(
        review.Id,
        review.ViewingBookingId,
        review.Rating,
        review.Comment,
        PlatformReviewEnumText.ReviewStatus(review.Status),
        review.CreatedAt,
        review.ModeratedByApplicationUserId,
        review.ModeratedAt,
        review.ModerationReason,
        PlatformReviewCompanyResponse.From(review.Company),
        PlatformReviewListingResponse.From(review.Listing),
        PlatformReviewReviewerResponse.From(review.Reviewer));
}

public sealed record PlatformReviewDirectoryResponse(
    IReadOnlyList<PlatformReviewSummaryResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage)
{
    public static PlatformReviewDirectoryResponse From(
        PagedResult<PlatformReviewSummary> result) => new(
            result.Items.Select(PlatformReviewSummaryResponse.From).ToList(),
            result.PageNumber,
            result.PageSize,
            result.TotalCount,
            result.TotalPages,
            result.HasPreviousPage,
            result.HasNextPage);
}

public sealed record PlatformReviewDetailsResponse(
    Guid Id,
    Guid ViewingBookingId,
    int Rating,
    string? Comment,
    string Status,
    DateTimeOffset CreatedAt,
    Guid? ModeratedByApplicationUserId,
    DateTimeOffset? ModeratedAt,
    string? ModerationReason,
    PlatformReviewCompanyResponse Company,
    PlatformReviewListingResponse Listing,
    PlatformReviewReviewerResponse Reviewer,
    string BookingCode,
    string BookingStatus,
    DateTimeOffset? CompletedAt,
    DateTimeOffset StartsAt)
{
    public static PlatformReviewDetailsResponse From(PlatformReviewDetails details) => new(
        details.Review.Id,
        details.Review.ViewingBookingId,
        details.Review.Rating,
        details.Review.Comment,
        PlatformReviewEnumText.ReviewStatus(details.Review.Status),
        details.Review.CreatedAt,
        details.Review.ModeratedByApplicationUserId,
        details.Review.ModeratedAt,
        details.Review.ModerationReason,
        PlatformReviewCompanyResponse.From(details.Review.Company),
        PlatformReviewListingResponse.From(details.Review.Listing),
        PlatformReviewReviewerResponse.From(details.Review.Reviewer),
        details.BookingCode,
        PlatformReviewEnumText.BookingStatus(details.BookingStatus),
        details.CompletedAt,
        details.StartsAt);
}

internal static class PlatformReviewEnumText
{
    public static string ReviewStatus(CompanyReviewStatus value) => value switch
    {
        CompanyReviewStatus.Visible => "Visible",
        CompanyReviewStatus.Hidden => "Hidden",
        CompanyReviewStatus.PendingModeration => "PendingModeration",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown review status.")
    };

    public static string BookingStatus(ViewingBookingStatus value) => value switch
    {
        ViewingBookingStatus.Pending => "Pending",
        ViewingBookingStatus.Confirmed => "Confirmed",
        ViewingBookingStatus.CheckedIn => "CheckedIn",
        ViewingBookingStatus.Completed => "Completed",
        ViewingBookingStatus.Rejected => "Rejected",
        ViewingBookingStatus.Cancelled => "Cancelled",
        ViewingBookingStatus.NoShow => "NoShow",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown booking status.")
    };
}
