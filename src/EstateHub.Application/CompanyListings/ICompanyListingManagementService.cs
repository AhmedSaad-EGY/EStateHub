using EstateHub.Application.Common;

namespace EstateHub.Application.CompanyListings;

public interface ICompanyListingManagementService
{
    Task<PagedResult<CompanyListingSummary>?> GetListingsAsync(Guid applicationUserId, CompanyListingDirectoryQuery query, CancellationToken cancellationToken = default);
    Task<CompanyListingDetails?> GetListingAsync(Guid applicationUserId, Guid listingId, CancellationToken cancellationToken = default);
    Task<CompanyListingMutationResult> CreateListingAsync(Guid applicationUserId, CompanyListingCommand command, CancellationToken cancellationToken = default);
    Task<CompanyListingMutationResult> UpdateListingAsync(Guid applicationUserId, Guid listingId, CompanyListingCommand command, CancellationToken cancellationToken = default);
    Task<CompanyListingMediaResult> ReplaceMediaAsync(Guid applicationUserId, Guid listingId, IReadOnlyList<CompanyListingMediaItem> items, byte[] rowVersion, CancellationToken cancellationToken = default);
    Task<CompanyListingPaymentPlanResult> CreatePaymentPlanAsync(Guid applicationUserId, Guid listingId, CompanyListingPaymentPlanCommand command, byte[] rowVersion, CancellationToken cancellationToken = default);
    Task<CompanyListingPaymentPlanResult> UpdatePaymentPlanAsync(Guid applicationUserId, Guid listingId, Guid paymentPlanId, CompanyListingPaymentPlanCommand command, byte[] rowVersion, CancellationToken cancellationToken = default);
    Task<CompanyListingOperationStatus> DeletePaymentPlanAsync(Guid applicationUserId, Guid listingId, Guid paymentPlanId, byte[] rowVersion, CancellationToken cancellationToken = default);
    Task<CompanyListingOperationStatus> PublishListingAsync(Guid applicationUserId, Guid listingId, byte[] rowVersion, CancellationToken cancellationToken = default);
    Task<CompanyListingOperationStatus> ArchiveListingAsync(Guid applicationUserId, Guid listingId, byte[] rowVersion, CancellationToken cancellationToken = default);
}
