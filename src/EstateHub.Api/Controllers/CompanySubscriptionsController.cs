using EstateHub.Api.Authorization.CompanyPermissions;
using EstateHub.Api.Contracts.Subscriptions;
using EstateHub.Application.CompanyAccess;
using EstateHub.Application.Subscriptions;
using EstateHub.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EstateHub.Api.Controllers;

[ApiController]
[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/company")]
public sealed class CompanySubscriptionsController(ISubscriptionService service) : ControllerBase
{
    [HttpGet("subscription")]
    [RequireCompanyPermission(CompanyPermissionCodes.BillingRead)]
    public async Task<ActionResult<CompanySubscriptionResponse>> GetCurrent(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await service.GetCurrentAsync(userId, cancellationToken);
        return CurrentResult(result);
    }

    [HttpGet("subscriptions")]
    [RequireCompanyPermission(CompanyPermissionCodes.BillingRead)]
    public async Task<ActionResult<CompanySubscriptionHistoryResponse>> GetHistory([FromQuery] CompanySubscriptionHistoryRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (request.PageNumber is < 1 or > 10000) ModelState.AddModelError("pageNumber", "PageNumber must be between 1 and 10000.");
        if (request.PageSize is < 1 or > 50) ModelState.AddModelError("pageSize", "PageSize must be between 1 and 50.");
        var status = ParseStatus(request.Status);
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = await service.GetHistoryAsync(userId, new CompanySubscriptionHistoryQuery(request.PageNumber, request.PageSize, status), cancellationToken);
        return result.Status switch
        {
            SubscriptionOperationStatus.Succeeded => Ok(CompanySubscriptionHistoryResponse.From(result.Value!)),
            SubscriptionOperationStatus.NotFound => SubscriptionNotFound(),
            _ => SubscriptionUnavailable()
        };
    }

    [HttpPost("subscription/checkout")]
    [RequireCompanyPermission(CompanyPermissionCodes.BillingManage)]
    public async Task<ActionResult<CheckoutSubscriptionResponse>> Checkout(CheckoutSubscriptionRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (request.SubscriptionPlanId == Guid.Empty) ModelState.AddModelError("subscriptionPlanId", "SubscriptionPlanId is required.");
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = await service.CheckoutAsync(userId, new CheckoutSubscriptionCommand(request.SubscriptionPlanId), cancellationToken);
        return result.Status switch
        {
            SubscriptionOperationStatus.Succeeded => CreatedAtAction(nameof(GetCurrent), value: CheckoutSubscriptionResponse.From(result.Value!)),
            SubscriptionOperationStatus.NotFound => SubscriptionNotFound(),
            SubscriptionOperationStatus.Conflict => SubscriptionConflict(),
            _ => SubscriptionUnavailable()
        };
    }

    [HttpPost("subscription/cancel")]
    [RequireCompanyPermission(CompanyPermissionCodes.BillingManage)]
    public Task<IActionResult> Cancel(SubscriptionLifecycleRequest request, CancellationToken cancellationToken) => ChangeCancellation(request, true, cancellationToken);

    [HttpPost("subscription/resume")]
    [RequireCompanyPermission(CompanyPermissionCodes.BillingManage)]
    public Task<IActionResult> Resume(SubscriptionLifecycleRequest request, CancellationToken cancellationToken) => ChangeCancellation(request, false, cancellationToken);

    private async Task<IActionResult> ChangeCancellation(SubscriptionLifecycleRequest request, bool cancel, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var rowVersion = ParseRowVersion(request.RowVersion);
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var status = cancel
            ? await service.CancelAsync(userId, rowVersion!, cancellationToken)
            : await service.ResumeAsync(userId, rowVersion!, cancellationToken);
        return status switch
        {
            SubscriptionOperationStatus.Succeeded => NoContent(),
            SubscriptionOperationStatus.NotFound => SubscriptionNotFound(),
            SubscriptionOperationStatus.Conflict => SubscriptionConflict(),
            _ => SubscriptionUnavailable()
        };
    }

    private ActionResult<CompanySubscriptionResponse> CurrentResult(SubscriptionResult<CompanySubscriptionDetails> result) => result.Status switch
    {
        SubscriptionOperationStatus.Succeeded => Ok(CompanySubscriptionResponse.From(result.Value!)),
        SubscriptionOperationStatus.NotFound => SubscriptionNotFound(),
        _ => SubscriptionUnavailable()
    };

    private CompanySubscriptionStatus? ParseStatus(string? value)
    {
        if (value is null) return null;
        var normalized = value.Trim();
        if (normalized.Length != 0 && Enum.TryParse<CompanySubscriptionStatus>(normalized, true, out var status) && Enum.IsDefined(status) && string.Equals(normalized, status.ToString(), StringComparison.OrdinalIgnoreCase)) return status;
        ModelState.AddModelError("status", "Status must be Active, Expired, or Cancelled.");
        return null;
    }

    private byte[]? ParseRowVersion(string? value)
    {
        try
        {
            var bytes = string.IsNullOrWhiteSpace(value) ? null : Convert.FromBase64String(value);
            if (bytes is { Length: 8 }) return bytes;
        }
        catch (FormatException) { }
        ModelState.AddModelError("rowVersion", "RowVersion must be Base64 encoded eight bytes.");
        return null;
    }

    private bool TryGetUserId(out Guid userId) => Guid.TryParse(User.FindFirst("sub")?.Value, out userId) && userId != Guid.Empty;
    private ObjectResult SubscriptionNotFound() => Problem(statusCode: StatusCodes.Status404NotFound, title: "Subscription not found.");
    private ObjectResult SubscriptionConflict() => Problem(statusCode: StatusCodes.Status409Conflict, title: "Subscription operation conflict.");
    private ObjectResult SubscriptionUnavailable() => Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Subscription operation is temporarily unavailable.");
}
