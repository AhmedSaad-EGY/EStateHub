using EstateHub.Api.Contracts.Promotions;
using EstateHub.Application.Promotions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EstateHub.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/promotion-packages")]
public sealed class PromotionPackagesController(IPromotionService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PromotionPackageResponse>>> GetPackages(CancellationToken cancellationToken) => Ok((await service.GetPublicPackagesAsync(cancellationToken)).Select(PromotionPackageResponse.From).ToList());

    [HttpGet("{packageId}")]
    public async Task<ActionResult<PromotionPackageResponse>> GetPackage(Guid packageId, CancellationToken cancellationToken)
    {
        if (packageId == Guid.Empty) { ModelState.AddModelError("packageId", "PackageId is required."); return ValidationProblem(ModelState); }
        var package = await service.GetPublicPackageAsync(packageId, cancellationToken);
        return package is null ? PackageNotFound() : Ok(PromotionPackageResponse.From(package));
    }

    private ObjectResult PackageNotFound() => Problem(statusCode: StatusCodes.Status404NotFound, title: "Promotion package not found.");
}
