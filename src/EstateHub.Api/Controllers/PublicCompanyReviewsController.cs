using EstateHub.Api.Contracts.Reviews;
using EstateHub.Application.Reviews;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EstateHub.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/companies/{companySlug}/reviews")]
public sealed class PublicCompanyReviewsController(IReviewService reviewService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PublicCompanyReviewsResponse>> GetReviews(
        string? companySlug,
        [FromQuery] PublicCompanyReviewRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedSlug = companySlug?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedSlug) || normalizedSlug.Length > 200)
        {
            ModelState.AddModelError(
                "companySlug",
                "CompanySlug is required and must not exceed 200 characters.");
        }

        if (request.PageNumber is < 1 or > 10000)
        {
            ModelState.AddModelError("pageNumber", "PageNumber must be between 1 and 10000.");
        }

        if (request.PageSize is < 1 or > 50)
        {
            ModelState.AddModelError("pageSize", "PageSize must be between 1 and 50.");
        }

        if (request.Rating is not null and (< 1 or > 5))
        {
            ModelState.AddModelError("rating", "Rating must be between 1 and 5.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await reviewService.GetPublicCompanyReviewsAsync(
            normalizedSlug!,
            new PublicCompanyReviewQuery(
                request.PageNumber,
                request.PageSize,
                request.Rating),
            cancellationToken);

        return result is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Company not found.")
            : Ok(PublicCompanyReviewsResponse.From(result));
    }
}

