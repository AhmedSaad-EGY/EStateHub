using EstateHub.Application.Common;

namespace EstateHub.Application.Subscriptions;

public interface ISubscriptionService
{
    Task<IReadOnlyList<PublicSubscriptionPlan>> GetPublicPlansAsync(CancellationToken cancellationToken = default);
    Task<PublicSubscriptionPlan?> GetPublicPlanAsync(Guid planId, CancellationToken cancellationToken = default);
    Task<SubscriptionResult<CompanySubscriptionDetails>> GetCurrentAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<SubscriptionResult<PagedResult<CompanySubscriptionDetails>>> GetHistoryAsync(Guid userId, CompanySubscriptionHistoryQuery query, CancellationToken cancellationToken = default);
    Task<SubscriptionResult<CheckoutResult>> CheckoutAsync(Guid userId, CheckoutSubscriptionCommand command, CancellationToken cancellationToken = default);
    Task<SubscriptionOperationStatus> CancelAsync(Guid userId, byte[] rowVersion, CancellationToken cancellationToken = default);
    Task<SubscriptionOperationStatus> ResumeAsync(Guid userId, byte[] rowVersion, CancellationToken cancellationToken = default);
}
