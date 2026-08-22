using EstateHub.Api.Contracts.Promotions;
using EstateHub.Application.Promotions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EstateHub.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/promoted-listings")]
public sealed class PromotedListingsController(IPromotionService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PromotedListingsResponse>> GetPromotedListings([FromQuery] string? placement, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var normalizedPlacement = placement?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedPlacement) || normalizedPlacement.Length > 100) ModelState.AddModelError("placement", "Placement is required and must not exceed 100 characters.");
        if (pageNumber is < 1 or > 10000) ModelState.AddModelError("pageNumber", "PageNumber must be between 1 and 10000.");
        if (pageSize is < 1 or > 50) ModelState.AddModelError("pageSize", "PageSize must be between 1 and 50.");
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = await service.GetPromotedListingsAsync(new PromotedListingQuery(normalizedPlacement!, pageNumber, pageSize), cancellationToken);
        return Ok(PromotedListingsResponse.From(result));
    }
}
