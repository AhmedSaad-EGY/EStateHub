using EstateHub.Api.Contracts.Reviews;
using EstateHub.Application.Reviews;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EstateHub.Api.Controllers;

[ApiController]
[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/me")]
public sealed class MyReviewsController(IReviewService reviewService) : ControllerBase
{
    [HttpGet("viewing-bookings/{bookingId}/review")]
    public async Task<ActionResult<CustomerReviewResponse>> GetBookingReview(
        Guid bookingId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        if (bookingId == Guid.Empty)
        {
            return InvalidRoute("bookingId", "BookingId is required.");
        }

        var result = await reviewService.GetCustomerReviewAsync(
            userId,
            bookingId,
            cancellationToken);
        return result is null ? ReviewNotFound() : Ok(CustomerReviewResponse.From(result));
    }

    [HttpPost("viewing-bookings/{bookingId}/review")]
    public async Task<ActionResult<CustomerReviewResponse>> CreateReview(
        Guid bookingId,
        ReviewRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        if (bookingId == Guid.Empty)
        {
            ModelState.AddModelError("bookingId", "BookingId is required.");
        }

        var command = ValidateRequest(request);
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await reviewService.CreateReviewAsync(
            userId,
            bookingId,
            command,
            cancellationToken);
        return result.Status switch
        {
            ReviewOperationStatus.Succeeded => CreatedAtAction(
                nameof(GetBookingReview),
                new { bookingId },
                CustomerReviewResponse.From(result.Review!)),
            ReviewOperationStatus.NotFound => ReviewNotFound(),
            ReviewOperationStatus.Conflict => ReviewConflict(),
            _ => ReviewUnavailable()
        };
    }

    [HttpPut("reviews/{reviewId}")]
    public async Task<ActionResult<CustomerReviewResponse>> UpdateReview(
        Guid reviewId,
        ReviewRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        if (reviewId == Guid.Empty)
        {
            ModelState.AddModelError("reviewId", "ReviewId is required.");
        }

        var command = ValidateRequest(request);
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await reviewService.UpdateReviewAsync(
            userId,
            reviewId,
            command,
            cancellationToken);
        return result.Status switch
        {
            ReviewOperationStatus.Succeeded => Ok(CustomerReviewResponse.From(result.Review!)),
            ReviewOperationStatus.NotFound => ReviewNotFound(),
            ReviewOperationStatus.Conflict => ReviewConflict(),
            _ => ReviewUnavailable()
        };
    }

    [HttpDelete("reviews/{reviewId}")]
    public async Task<IActionResult> DeleteReview(
        Guid reviewId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        if (reviewId == Guid.Empty)
        {
            return InvalidRoute("reviewId", "ReviewId is required.");
        }

        var result = await reviewService.DeleteReviewAsync(
            userId,
            reviewId,
            cancellationToken);
        return result switch
        {
            ReviewOperationStatus.Succeeded => NoContent(),
            ReviewOperationStatus.NotFound => ReviewNotFound(),
            _ => ReviewUnavailable()
        };
    }

    private ReviewCommand ValidateRequest(ReviewRequest request)
    {
        if (request.Rating is < 1 or > 5)
        {
            ModelState.AddModelError("rating", "Rating must be between 1 and 5.");
        }

        var comment = string.IsNullOrWhiteSpace(request.Comment)
            ? null
            : request.Comment.Trim();
        if (comment?.Length > 4000)
        {
            ModelState.AddModelError("comment", "Comment must not exceed 4000 characters.");
        }

        return new ReviewCommand(request.Rating, comment);
    }

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(User.FindFirst("sub")?.Value, out userId)
        && userId != Guid.Empty;

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
        title: "Review operation conflict.");

    private ObjectResult ReviewUnavailable() => Problem(
        statusCode: StatusCodes.Status503ServiceUnavailable,
        title: "Review operation is temporarily unavailable.");
}
