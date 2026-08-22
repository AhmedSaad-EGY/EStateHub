using EstateHub.Api.Contracts.PlatformReviews;
using EstateHub.Application.PlatformAccess;
using EstateHub.Application.PlatformReviews;
using EstateHub.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EstateHub.Api.Controllers;

[ApiController]
[Authorize(Policy = PlatformRoleNames.PlatformAdmin)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/platform/reviews")]
public sealed class PlatformReviewsController(
    IPlatformReviewModerationService reviewService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PlatformReviewDirectoryResponse>> GetReviews(
        [FromQuery] PlatformReviewDirectoryRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetPlatformAdminId(out _))
        {
            return Unauthorized();
        }

        if (request.PageNumber is < 1 or > 10000)
        {
            ModelState.AddModelError("pageNumber", "PageNumber must be between 1 and 10000.");
        }

        if (request.PageSize is < 1 or > 50)
        {
            ModelState.AddModelError("pageSize", "PageSize must be between 1 and 50.");
        }

        CompanyReviewStatus? status = null;
        if (request.Status is not null)
        {
            if (TryParseReviewStatus(request.Status, out var parsedStatus))
            {
                status = parsedStatus;
            }
            else
            {
                ModelState.AddModelError(
                    "status",
                    "Status must be Visible, Hidden, or PendingModeration.");
            }
        }

        if (request.Rating is not null and (< 1 or > 5))
        {
            ModelState.AddModelError("rating", "Rating must be between 1 and 5.");
        }

        if (request.CompanyId == Guid.Empty)
        {
            ModelState.AddModelError("companyId", "CompanyId must be a non-empty GUID.");
        }

        ValidateUtcRange(request.CreatedFrom, request.CreatedTo);
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await reviewService.GetReviewsAsync(
            new PlatformReviewDirectoryQuery(
                request.PageNumber,
                request.PageSize,
                status,
                request.Rating,
                request.CompanyId,
                request.CreatedFrom,
                request.CreatedTo),
            cancellationToken);
        return result.Status == PlatformReviewModerationOperationStatus.Succeeded
            ? Ok(PlatformReviewDirectoryResponse.From(result.Value!))
            : ReviewUnavailable();
    }

    [HttpGet("{reviewId}")]
    public async Task<ActionResult<PlatformReviewDetailsResponse>> GetReview(
        Guid reviewId,
        CancellationToken cancellationToken)
    {
        if (!TryGetPlatformAdminId(out _))
        {
            return Unauthorized();
        }

        if (reviewId == Guid.Empty)
        {
            return InvalidRoute("reviewId", "ReviewId is required.");
        }

        var result = await reviewService.GetReviewAsync(reviewId, cancellationToken);
        return result.Status switch
        {
            PlatformReviewModerationOperationStatus.Succeeded =>
                Ok(PlatformReviewDetailsResponse.From(result.Value!)),
            PlatformReviewModerationOperationStatus.NotFound => ReviewNotFound(),
            _ => ReviewUnavailable()
        };
    }

    [HttpPut("{reviewId}/moderation")]
    public async Task<IActionResult> ModerateReview(
        Guid reviewId,
        PlatformReviewModerationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetPlatformAdminId(out var platformAdminId))
        {
            return Unauthorized();
        }

        if (reviewId == Guid.Empty)
        {
            ModelState.AddModelError("reviewId", "ReviewId is required.");
        }

        var expectedStatus = ParseRequiredReviewStatus(
            request.ExpectedStatus,
            "expectedStatus",
            allowPendingModeration: true);
        var status = ParseRequiredReviewStatus(
            request.Status,
            "status",
            allowPendingModeration: false);
        var reason = NormalizeReason(request.Reason, status);
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await reviewService.ModerateAsync(
            platformAdminId,
            reviewId,
            new PlatformReviewModerationCommand(
                expectedStatus!.Value,
                status!.Value,
                reason),
            cancellationToken);
        return result switch
        {
            PlatformReviewModerationOperationStatus.Succeeded => NoContent(),
            PlatformReviewModerationOperationStatus.NotFound => ReviewNotFound(),
            PlatformReviewModerationOperationStatus.Conflict => ReviewConflict(),
            _ => ReviewUnavailable()
        };
    }

    private CompanyReviewStatus? ParseRequiredReviewStatus(
        string? value,
        string key,
        bool allowPendingModeration)
    {
        if (!TryParseReviewStatus(value, out var status)
            || (!allowPendingModeration && status == CompanyReviewStatus.PendingModeration))
        {
            ModelState.AddModelError(
                key,
                allowPendingModeration
                    ? "ExpectedStatus must be Visible, Hidden, or PendingModeration."
                    : "Status must be Visible or Hidden.");
            return null;
        }

        return status;
    }

    private string? NormalizeReason(
        string? value,
        CompanyReviewStatus? status)
    {
        var reason = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (reason?.Length > 1000)
        {
            ModelState.AddModelError("reason", "Reason must not exceed 1000 characters.");
        }

        if (status == CompanyReviewStatus.Hidden && reason is null)
        {
            ModelState.AddModelError("reason", "Reason is required when hiding a review.");
        }
        else if (status == CompanyReviewStatus.Visible && reason is not null)
        {
            ModelState.AddModelError("reason", "Reason must be omitted when making a review visible.");
        }

        return status == CompanyReviewStatus.Visible ? null : reason;
    }

    private void ValidateUtcRange(
        DateTimeOffset? createdFrom,
        DateTimeOffset? createdTo)
    {
        if (createdFrom is not null && createdFrom.Value.Offset != TimeSpan.Zero)
        {
            ModelState.AddModelError("createdFrom", "CreatedFrom must use UTC.");
        }

        if (createdTo is not null && createdTo.Value.Offset != TimeSpan.Zero)
        {
            ModelState.AddModelError("createdTo", "CreatedTo must use UTC.");
        }

        if (createdFrom > createdTo)
        {
            ModelState.AddModelError("createdFrom", "CreatedFrom must not be later than CreatedTo.");
        }
    }

    private static bool TryParseReviewStatus(
        string? value,
        out CompanyReviewStatus status)
    {
        var normalized = value?.Trim();
        if (string.Equals(normalized, "Visible", StringComparison.OrdinalIgnoreCase))
        {
            status = CompanyReviewStatus.Visible;
            return true;
        }

        if (string.Equals(normalized, "Hidden", StringComparison.OrdinalIgnoreCase))
        {
            status = CompanyReviewStatus.Hidden;
            return true;
        }

        if (string.Equals(normalized, "PendingModeration", StringComparison.OrdinalIgnoreCase))
        {
            status = CompanyReviewStatus.PendingModeration;
            return true;
        }

        status = default;
        return false;
    }

    private bool TryGetPlatformAdminId(out Guid applicationUserId) =>
        Guid.TryParse(User.FindFirst("sub")?.Value, out applicationUserId)
        && applicationUserId != Guid.Empty;

    private ActionResult InvalidRoute(string key, string message)
    {
        ModelState.AddModelError(key, message);
        return ValidationProblem(ModelState);
    }

    private ObjectResult ReviewNotFound() => Problem(
        statusCode: StatusCodes.Status404NotFound,
        title: "Review not found.");

    private ObjectResult ReviewConflict() => Problem(
        statusCode: StatusCodes.Status409Conflict,
        title: "Review moderation conflict.");

    private ObjectResult ReviewUnavailable() => Problem(
        statusCode: StatusCodes.Status503ServiceUnavailable,
        title: "Review moderation service is temporarily unavailable.");
}
