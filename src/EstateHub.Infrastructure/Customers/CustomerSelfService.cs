using EstateHub.Application.Common;
using EstateHub.Application.Customers;
using EstateHub.Application.Listings;
using EstateHub.Domain.Entities.Catalog;
using EstateHub.Domain.Entities.Discovery;
using EstateHub.Domain.Entities.Users;
using EstateHub.Infrastructure.Identity;
using EstateHub.Infrastructure.Listings;
using EstateHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EstateHub.Infrastructure.Customers;

internal sealed class CustomerSelfService(
    EstateHubDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    TimeProvider timeProvider) : ICustomerSelfService
{
    private const int FilterSchemaVersion = 1;

    private readonly EstateHubDbContext _dbContext = dbContext;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly TimeProvider _timeProvider = timeProvider;

    public Task<CustomerProfileDetails?> GetProfileAsync(
        Guid applicationUserId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Set<CustomerProfile>()
            .AsNoTracking()
            .Where(profile => profile.ApplicationUserId == applicationUserId)
            .Join(
                _dbContext.Users.AsNoTracking(),
                profile => profile.ApplicationUserId,
                user => user.Id,
                (profile, user) => new CustomerProfileDetails(
                    profile.Id,
                    profile.FullName,
                    profile.Persona,
                    user.Email ?? string.Empty,
                    user.PhoneNumber,
                    profile.CreatedAt,
                    profile.UpdatedAt))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<UpdateCustomerProfileResult> UpdateProfileAsync(
        Guid applicationUserId,
        UpdateCustomerProfileCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(cancellationToken);

        try
        {
            var profile = await _dbContext.Set<CustomerProfile>()
                .SingleOrDefaultAsync(
                    item => item.ApplicationUserId == applicationUserId,
                    cancellationToken);

            if (profile is null)
            {
                return new UpdateCustomerProfileResult(
                    UpdateCustomerProfileStatus.NotFound,
                    null);
            }

            var user = await _dbContext.Users
                .SingleOrDefaultAsync(
                    item => item.Id == applicationUserId,
                    cancellationToken);

            if (user is null)
            {
                return new UpdateCustomerProfileResult(
                    UpdateCustomerProfileStatus.NotFound,
                    null);
            }

            profile.FullName = command.FullName;
            profile.Persona = command.Persona;
            profile.UpdatedAt = _timeProvider.GetUtcNow();

            if (!string.Equals(
                user.PhoneNumber,
                command.PhoneNumber,
                StringComparison.Ordinal))
            {
                var identityResult = await _userManager.SetPhoneNumberAsync(
                    user,
                    command.PhoneNumber);

                if (!identityResult.Succeeded)
                {
                    await transaction.RollbackAsync(CancellationToken.None);

                    return new UpdateCustomerProfileResult(
                        UpdateCustomerProfileStatus.ServiceUnavailable,
                        null);
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new UpdateCustomerProfileResult(
                UpdateCustomerProfileStatus.Succeeded,
                new CustomerProfileDetails(
                    profile.Id,
                    profile.FullName,
                    profile.Persona,
                    user.Email ?? string.Empty,
                    user.PhoneNumber,
                    profile.CreatedAt,
                    profile.UpdatedAt));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(CancellationToken.None);

            return new UpdateCustomerProfileResult(
                UpdateCustomerProfileStatus.ServiceUnavailable,
                null);
        }
    }

    public async Task<PagedResult<CustomerFavoriteItem>?> GetFavoritesAsync(
        Guid applicationUserId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var utcNow = _timeProvider.GetUtcNow();
        var customerProfileId = await GetCustomerProfileIdAsync(
            applicationUserId,
            cancellationToken);

        if (customerProfileId is null)
        {
            return null;
        }

        var publicListings = _dbContext.Set<Listing>()
            .AsNoTracking()
            .WherePublic(utcNow);

        var favorites = _dbContext.Set<Favorite>()
            .AsNoTracking()
            .Where(favorite => favorite.CustomerProfileId == customerProfileId.Value)
            .Where(favorite => publicListings.Any(
                listing => listing.Id == favorite.ListingId));

        var totalCount = await favorites.CountAsync(cancellationToken);
        var offset = (pageNumber - 1) * pageSize;

        var items = await favorites
            .OrderByDescending(favorite => favorite.CreatedAt)
            .ThenByDescending(favorite => favorite.Id)
            .Skip(offset)
            .Take(pageSize)
            .Select(favorite => new CustomerFavoriteItem(
                favorite.Id,
                favorite.CreatedAt,
                new ListingDirectoryItem(
                    favorite.Listing.Id,
                    favorite.Listing.Slug,
                    favorite.Listing.Title,
                    favorite.Listing.ListingType,
                    favorite.Listing.AskingPrice,
                    favorite.Listing.RentPeriod,
                    favorite.Listing.PublishedAt,
                    favorite.Listing.Media
                        .Where(media => media.IsCover)
                        .Select(media => (Guid?)media.FileAssetId)
                        .FirstOrDefault(),
                    favorite.Listing.PaymentPlans.Any(plan => plan.IsActive),
                    new ListingCurrencySummary(
                        favorite.Listing.Currency.Code,
                        favorite.Listing.Currency.Name,
                        favorite.Listing.Currency.Symbol,
                        favorite.Listing.Currency.DecimalPlaces),
                    new ListingCompanySummary(
                        favorite.Listing.Company.Id,
                        favorite.Listing.Company.Slug,
                        favorite.Listing.Company.DisplayName,
                        favorite.Listing.Company.LogoFileAssetId),
                    new ListingUnitSummary(
                        favorite.Listing.Unit.Id,
                        favorite.Listing.Unit.Bedrooms,
                        favorite.Listing.Unit.Bathrooms,
                        favorite.Listing.Unit.BuiltUpArea,
                        favorite.Listing.Unit.LandArea,
                        favorite.Listing.Unit.FinishingType,
                        favorite.Listing.Unit.FurnishedStatus,
                        favorite.Listing.Unit.Status,
                        new ListingUnitTypeSummary(
                            favorite.Listing.Unit.UnitType.Id,
                            favorite.Listing.Unit.UnitType.Code,
                            favorite.Listing.Unit.UnitType.NameEn,
                            favorite.Listing.Unit.UnitType.NameAr),
                        new ListingLocationSummary(
                            favorite.Listing.Unit.Location.Id,
                            favorite.Listing.Unit.Location.Type,
                            favorite.Listing.Unit.Location.NameEn,
                            favorite.Listing.Unit.Location.NameAr,
                            favorite.Listing.Unit.Location.Slug),
                        favorite.Listing.Unit.Project == null
                            ? null
                            : new ListingProjectSummary(
                                favorite.Listing.Unit.Project.Id,
                                favorite.Listing.Unit.Project.Slug,
                                favorite.Listing.Unit.Project.Name,
                                favorite.Listing.Unit.Project.DeveloperCompany.Slug)))))
            .ToListAsync(cancellationToken);

        return new PagedResult<CustomerFavoriteItem>(
            items,
            pageNumber,
            pageSize,
            totalCount);
    }

    public async Task<AddFavoriteResult> AddFavoriteAsync(
        Guid applicationUserId,
        Guid listingId,
        CancellationToken cancellationToken = default)
    {
        var utcNow = _timeProvider.GetUtcNow();
        var customerProfileId = await GetCustomerProfileIdAsync(
            applicationUserId,
            cancellationToken);

        if (customerProfileId is null)
        {
            return new AddFavoriteResult(AddFavoriteStatus.CustomerNotFound);
        }

        var listingIsPublic = await _dbContext.Set<Listing>()
            .AsNoTracking()
            .WherePublic(utcNow)
            .AnyAsync(listing => listing.Id == listingId, cancellationToken);

        if (!listingIsPublic)
        {
            return new AddFavoriteResult(AddFavoriteStatus.ListingNotFound);
        }

        var alreadyExists = await _dbContext.Set<Favorite>()
            .AsNoTracking()
            .AnyAsync(
                favorite => favorite.CustomerProfileId == customerProfileId.Value
                    && favorite.ListingId == listingId,
                cancellationToken);

        if (alreadyExists)
        {
            return new AddFavoriteResult(AddFavoriteStatus.Succeeded);
        }

        var favorite = new Favorite
        {
            Id = Guid.NewGuid(),
            CustomerProfileId = customerProfileId.Value,
            ListingId = listingId,
            CreatedAt = utcNow
        };

        _dbContext.Set<Favorite>().Add(favorite);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            _dbContext.Entry(favorite).State = EntityState.Detached;

            var nowExists = await _dbContext.Set<Favorite>()
                .AsNoTracking()
                .AnyAsync(
                    item => item.CustomerProfileId == customerProfileId.Value
                        && item.ListingId == listingId,
                    cancellationToken);

            if (!nowExists)
            {
                throw;
            }
        }

        return new AddFavoriteResult(AddFavoriteStatus.Succeeded);
    }

    public async Task<bool> RemoveFavoriteAsync(
        Guid applicationUserId,
        Guid listingId,
        CancellationToken cancellationToken = default)
    {
        var customerProfileId = await GetCustomerProfileIdAsync(
            applicationUserId,
            cancellationToken);

        if (customerProfileId is null)
        {
            return false;
        }

        await _dbContext.Set<Favorite>()
            .Where(favorite =>
                favorite.CustomerProfileId == customerProfileId.Value
                && favorite.ListingId == listingId)
            .ExecuteDeleteAsync(cancellationToken);

        return true;
    }

    public async Task<PagedResult<SavedSearchDetails>?> GetSavedSearchesAsync(
        Guid applicationUserId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var customerProfileId = await GetCustomerProfileIdAsync(
            applicationUserId,
            cancellationToken);

        if (customerProfileId is null)
        {
            return null;
        }

        var searches = _dbContext.Set<SavedSearch>()
            .AsNoTracking()
            .Where(search => search.CustomerProfileId == customerProfileId.Value);

        var totalCount = await searches.CountAsync(cancellationToken);
        var offset = (pageNumber - 1) * pageSize;

        var items = await searches
            .OrderByDescending(search => search.UpdatedAt)
            .ThenByDescending(search => search.Id)
            .Skip(offset)
            .Take(pageSize)
            .Select(search => new SavedSearchDetails(
                search.Id,
                search.Name,
                search.FilterJson,
                search.FilterSchemaVersion,
                search.AlertsEnabled,
                search.CreatedAt,
                search.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<SavedSearchDetails>(
            items,
            pageNumber,
            pageSize,
            totalCount);
    }

    public async Task<SavedSearchDetails?> CreateSavedSearchAsync(
        Guid applicationUserId,
        SaveSearchCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var customerProfileId = await GetCustomerProfileIdAsync(
            applicationUserId,
            cancellationToken);

        if (customerProfileId is null)
        {
            return null;
        }

        var now = _timeProvider.GetUtcNow();
        var savedSearch = new SavedSearch
        {
            Id = Guid.NewGuid(),
            CustomerProfileId = customerProfileId.Value,
            Name = command.Name,
            FilterJson = command.FilterJson,
            FilterSchemaVersion = FilterSchemaVersion,
            AlertsEnabled = command.AlertsEnabled,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Set<SavedSearch>().Add(savedSearch);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToSavedSearchDetails(savedSearch);
    }

    public async Task<UpdateSavedSearchResult> UpdateSavedSearchAsync(
        Guid applicationUserId,
        Guid savedSearchId,
        SaveSearchCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var customerProfileId = await GetCustomerProfileIdAsync(
            applicationUserId,
            cancellationToken);

        if (customerProfileId is null)
        {
            return new UpdateSavedSearchResult(UpdateSavedSearchStatus.NotFound, null);
        }

        var savedSearch = await _dbContext.Set<SavedSearch>()
            .SingleOrDefaultAsync(
                search => search.Id == savedSearchId
                    && search.CustomerProfileId == customerProfileId.Value,
                cancellationToken);

        if (savedSearch is null)
        {
            return new UpdateSavedSearchResult(UpdateSavedSearchStatus.NotFound, null);
        }

        savedSearch.Name = command.Name;
        savedSearch.FilterJson = command.FilterJson;
        savedSearch.FilterSchemaVersion = FilterSchemaVersion;
        savedSearch.AlertsEnabled = command.AlertsEnabled;
        savedSearch.UpdatedAt = _timeProvider.GetUtcNow();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new UpdateSavedSearchResult(
            UpdateSavedSearchStatus.Succeeded,
            ToSavedSearchDetails(savedSearch));
    }

    public async Task<bool> RemoveSavedSearchAsync(
        Guid applicationUserId,
        Guid savedSearchId,
        CancellationToken cancellationToken = default)
    {
        var customerProfileId = await GetCustomerProfileIdAsync(
            applicationUserId,
            cancellationToken);

        if (customerProfileId is null)
        {
            return false;
        }

        await _dbContext.Set<SavedSearch>()
            .Where(search =>
                search.CustomerProfileId == customerProfileId.Value
                && search.Id == savedSearchId)
            .ExecuteDeleteAsync(cancellationToken);

        return true;
    }

    private Task<Guid?> GetCustomerProfileIdAsync(
        Guid applicationUserId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Set<CustomerProfile>()
            .AsNoTracking()
            .Where(profile => profile.ApplicationUserId == applicationUserId)
            .Select(profile => (Guid?)profile.Id)
            .SingleOrDefaultAsync(cancellationToken);
    }

    private static SavedSearchDetails ToSavedSearchDetails(SavedSearch savedSearch)
    {
        return new SavedSearchDetails(
            savedSearch.Id,
            savedSearch.Name,
            savedSearch.FilterJson,
            savedSearch.FilterSchemaVersion,
            savedSearch.AlertsEnabled,
            savedSearch.CreatedAt,
            savedSearch.UpdatedAt);
    }
}
