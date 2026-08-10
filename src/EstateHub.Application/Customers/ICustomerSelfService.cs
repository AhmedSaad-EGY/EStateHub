using EstateHub.Application.Common;

namespace EstateHub.Application.Customers;

public interface ICustomerSelfService
{
    Task<CustomerProfileDetails?> GetProfileAsync(
        Guid applicationUserId,
        CancellationToken cancellationToken = default);

    Task<UpdateCustomerProfileResult> UpdateProfileAsync(
        Guid applicationUserId,
        UpdateCustomerProfileCommand command,
        CancellationToken cancellationToken = default);

    Task<PagedResult<CustomerFavoriteItem>?> GetFavoritesAsync(
        Guid applicationUserId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<AddFavoriteResult> AddFavoriteAsync(
        Guid applicationUserId,
        Guid listingId,
        CancellationToken cancellationToken = default);

    Task<bool> RemoveFavoriteAsync(
        Guid applicationUserId,
        Guid listingId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<SavedSearchDetails>?> GetSavedSearchesAsync(
        Guid applicationUserId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<SavedSearchDetails?> CreateSavedSearchAsync(
        Guid applicationUserId,
        SaveSearchCommand command,
        CancellationToken cancellationToken = default);

    Task<UpdateSavedSearchResult> UpdateSavedSearchAsync(
        Guid applicationUserId,
        Guid savedSearchId,
        SaveSearchCommand command,
        CancellationToken cancellationToken = default);

    Task<bool> RemoveSavedSearchAsync(
        Guid applicationUserId,
        Guid savedSearchId,
        CancellationToken cancellationToken = default);
}
