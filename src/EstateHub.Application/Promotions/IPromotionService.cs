using EstateHub.Application.Common;

namespace EstateHub.Application.Promotions;

public interface IPromotionService
{
    Task<IReadOnlyList<PublicPromotionPackage>> GetPublicPackagesAsync(CancellationToken cancellationToken = default);
    Task<PublicPromotionPackage?> GetPublicPackageAsync(Guid packageId, CancellationToken cancellationToken = default);
    Task<PagedResult<PromotedListingItem>> GetPromotedListingsAsync(PromotedListingQuery query, CancellationToken cancellationToken = default);
    Task<PromotionResult<PagedResult<CompanyListingPromotionDetails>>> GetCompanyPromotionsAsync(Guid applicationUserId, CompanyListingPromotionDirectoryQuery query, CancellationToken cancellationToken = default);
    Task<PromotionResult<CompanyListingPromotionDetails>> GetCompanyPromotionAsync(Guid applicationUserId, Guid promotionId, CancellationToken cancellationToken = default);
    Task<PromotionResult<PromotionCheckoutResult>> CheckoutAsync(Guid applicationUserId, CheckoutPromotionCommand command, CancellationToken cancellationToken = default);
    Task<PromotionOperationStatus> CancelAsync(Guid applicationUserId, Guid promotionId, byte[] rowVersion, CancellationToken cancellationToken = default);
}
