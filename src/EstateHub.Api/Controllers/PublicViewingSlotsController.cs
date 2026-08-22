using EstateHub.Api.Contracts.ViewingBookings;
using EstateHub.Application.ViewingBookings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EstateHub.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/listings/{listingSlug}/viewing-slots")]
public sealed class PublicViewingSlotsController(IPublicViewingSlotQueryService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PublicViewingSlotsResponse>> GetSlots(string? listingSlug, [FromQuery] PublicViewingSlotRequest request, CancellationToken cancellationToken)
    {
        var normalizedSlug = listingSlug?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedSlug) || normalizedSlug.Length > 200) ModelState.AddModelError("listingSlug", "ListingSlug is required and must not exceed 200 characters.");
        if (request.PageNumber is < 1 or > 10000) ModelState.AddModelError("pageNumber", "PageNumber must be between 1 and 10000.");
        if (request.PageSize is < 1 or > 50) ModelState.AddModelError("pageSize", "PageSize must be between 1 and 50.");
        if (request.From is not null && request.To is not null && request.From >= request.To) ModelState.AddModelError("to", "To must be later than From.");
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = await service.GetSlotsAsync(normalizedSlug!, new PublicViewingSlotQuery(request.PageNumber, request.PageSize, request.From, request.To), cancellationToken);
        return result is null ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Listing not found.") : Ok(PublicViewingSlotsResponse.From(result));
    }
}
