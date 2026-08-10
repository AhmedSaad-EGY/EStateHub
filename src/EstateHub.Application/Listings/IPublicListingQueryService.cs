using EstateHub.Application.Common;

namespace EstateHub.Application.Listings;

public interface IPublicListingQueryService
{
    Task<PagedResult<ListingDirectoryItem>> GetListingsAsync(
        ListingDirectoryQuery query,
        CancellationToken cancellationToken = default);

    Task<ListingDetails?> GetListingBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default);
}
