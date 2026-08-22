using EstateHub.Api.Contracts.Subscriptions;
using EstateHub.Application.Subscriptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EstateHub.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/subscription-plans")]
public sealed class SubscriptionPlansController(ISubscriptionService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SubscriptionPlanResponse>>> GetPlans(CancellationToken cancellationToken) =>
        Ok((await service.GetPublicPlansAsync(cancellationToken)).Select(SubscriptionPlanResponse.From).ToList());

    [HttpGet("{planId}")]
    public async Task<ActionResult<SubscriptionPlanResponse>> GetPlan(Guid planId, CancellationToken cancellationToken)
    {
        if (planId == Guid.Empty)
        {
            ModelState.AddModelError("planId", "PlanId is required.");
            return ValidationProblem(ModelState);
        }

        var plan = await service.GetPublicPlanAsync(planId, cancellationToken);
        return plan is null ? PlanNotFound() : Ok(SubscriptionPlanResponse.From(plan));
    }

    private ObjectResult PlanNotFound() => Problem(statusCode: StatusCodes.Status404NotFound, title: "Subscription plan not found.");
}
