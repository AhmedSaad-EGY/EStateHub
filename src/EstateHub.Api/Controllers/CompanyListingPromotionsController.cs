using EstateHub.Api.Authorization.CompanyPermissions;
using EstateHub.Api.Contracts.Promotions;
using EstateHub.Application.CompanyAccess;
using EstateHub.Application.Promotions;
using EstateHub.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EstateHub.Api.Controllers;

[ApiController]
[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/company/listing-promotions")]
public sealed class CompanyListingPromotionsController(IPromotionService service) : ControllerBase
{
    [HttpGet]
    [RequireCompanyPermission(CompanyPermissionCodes.BillingRead)]
    public async Task<ActionResult<CompanyListingPromotionsResponse>> GetPromotions([FromQuery] CompanyListingPromotionDirectoryRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var query = BuildDirectoryQuery(request);
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = await service.GetCompanyPromotionsAsync(userId, query, cancellationToken);
        return result.Status switch { PromotionOperationStatus.Succeeded => Ok(CompanyListingPromotionsResponse.From(result.Value!)), PromotionOperationStatus.NotFound => PromotionNotFound(), _ => PromotionUnavailable() };
    }

    [HttpGet("{promotionId}")]
    [RequireCompanyPermission(CompanyPermissionCodes.BillingRead)]
    public async Task<ActionResult<CompanyListingPromotionResponse>> GetPromotion(Guid promotionId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (promotionId == Guid.Empty) { ModelState.AddModelError("promotionId", "PromotionId is required."); return ValidationProblem(ModelState); }
        var result = await service.GetCompanyPromotionAsync(userId, promotionId, cancellationToken);
        return result.Status switch { PromotionOperationStatus.Succeeded => Ok(CompanyListingPromotionResponse.From(result.Value!)), PromotionOperationStatus.NotFound => PromotionNotFound(), _ => PromotionUnavailable() };
    }

    [HttpPost("checkout")]
    [RequireCompanyPermission(CompanyPermissionCodes.BillingManage)]
    [RequireCompanyPermission(CompanyPermissionCodes.ListingsManage)]
    public async Task<ActionResult<PromotionCheckoutResponse>> Checkout(CheckoutPromotionRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (request.ListingId == Guid.Empty) ModelState.AddModelError("listingId", "ListingId is required.");
        if (request.PromotionPackageId == Guid.Empty) ModelState.AddModelError("promotionPackageId", "PromotionPackageId is required.");
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = await service.CheckoutAsync(userId, new CheckoutPromotionCommand(request.ListingId, request.PromotionPackageId, request.StartsAt), cancellationToken);
        return result.Status switch
        {
            PromotionOperationStatus.Succeeded => CreatedAtAction(nameof(GetPromotion), new { promotionId = result.Value!.Promotion.Id }, PromotionCheckoutResponse.From(result.Value)),
            PromotionOperationStatus.NotFound => PromotionNotFound(),
            PromotionOperationStatus.InvalidRequest => BadRequest(new ProblemDetails { Status = StatusCodes.Status400BadRequest, Title = "Listing promotion request is invalid." }),
            PromotionOperationStatus.Conflict => PromotionConflict(),
            _ => PromotionUnavailable()
        };
    }

    [HttpPost("{promotionId}/cancel")]
    [RequireCompanyPermission(CompanyPermissionCodes.BillingManage)]
    [RequireCompanyPermission(CompanyPermissionCodes.ListingsManage)]
    public async Task<IActionResult> Cancel(Guid promotionId, PromotionCancellationRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (promotionId == Guid.Empty) ModelState.AddModelError("promotionId", "PromotionId is required.");
        var rowVersion = ParseRowVersion(request.RowVersion);
        var reason = request.Reason?.Trim();
        if (reason?.Length > 1000) ModelState.AddModelError("reason", "Reason must not exceed 1000 characters.");
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var status = await service.CancelAsync(userId, promotionId, rowVersion!, cancellationToken);
        return status switch { PromotionOperationStatus.Succeeded => NoContent(), PromotionOperationStatus.NotFound => PromotionNotFound(), PromotionOperationStatus.Conflict => PromotionConflict(), _ => PromotionUnavailable() };
    }

    private CompanyListingPromotionDirectoryQuery BuildDirectoryQuery(CompanyListingPromotionDirectoryRequest request)
    {
        if (request.PageNumber is < 1 or > 10000) ModelState.AddModelError("pageNumber", "PageNumber must be between 1 and 10000.");
        if (request.PageSize is < 1 or > 50) ModelState.AddModelError("pageSize", "PageSize must be between 1 and 50.");
        if (request.ListingId == Guid.Empty) ModelState.AddModelError("listingId", "ListingId cannot be empty when supplied.");
        if (request.From is not null && request.To is not null && request.From >= request.To) ModelState.AddModelError("to", "To must be later than From.");
        var placement = request.Placement?.Trim();
        if (placement is { Length: 0 }) placement = null;
        if (placement?.Length > 100) ModelState.AddModelError("placement", "Placement must not exceed 100 characters.");
        return new CompanyListingPromotionDirectoryQuery(request.PageNumber, request.PageSize, request.ListingId, ParseStatus(request.Status), placement, request.From, request.To);
    }

    private ListingPromotionStatus? ParseStatus(string? value)
    {
        if (value is null) return null;
        var normalized = value.Trim();
        if (normalized.Length != 0 && Enum.TryParse<ListingPromotionStatus>(normalized, true, out var status) && Enum.IsDefined(status) && string.Equals(normalized, status.ToString(), StringComparison.OrdinalIgnoreCase)) return status;
        ModelState.AddModelError("status", "Status must be Scheduled, Active, Completed, or Cancelled."); return null;
    }

    private byte[]? ParseRowVersion(string? value)
    {
        try { var bytes = string.IsNullOrWhiteSpace(value) ? null : Convert.FromBase64String(value); if (bytes is { Length: 8 }) return bytes; } catch (FormatException) { }
        ModelState.AddModelError("rowVersion", "RowVersion must be Base64 encoded eight bytes."); return null;
    }

    private bool TryGetUserId(out Guid userId) => Guid.TryParse(User.FindFirst("sub")?.Value, out userId) && userId != Guid.Empty;
    private ObjectResult PromotionNotFound() => Problem(statusCode: StatusCodes.Status404NotFound, title: "Listing promotion not found.");
    private ObjectResult PromotionConflict() => Problem(statusCode: StatusCodes.Status409Conflict, title: "Listing promotion operation conflict.");
    private ObjectResult PromotionUnavailable() => Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Listing promotion operation is temporarily unavailable.");
}
