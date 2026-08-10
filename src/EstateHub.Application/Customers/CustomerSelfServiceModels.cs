using EstateHub.Application.Common;
using EstateHub.Application.Listings;
using EstateHub.Domain.Enums;

namespace EstateHub.Application.Customers;

public sealed record CustomerProfileDetails(
    Guid Id,
    string FullName,
    CustomerPersona Persona,
    string Email,
    string? PhoneNumber,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record UpdateCustomerProfileCommand(
    string FullName,
    CustomerPersona Persona,
    string PhoneNumber);

public enum UpdateCustomerProfileStatus
{
    Succeeded,
    NotFound,
    ServiceUnavailable
}

public sealed record UpdateCustomerProfileResult(
    UpdateCustomerProfileStatus Status,
    CustomerProfileDetails? Profile);

public sealed record CustomerFavoriteItem(
    Guid FavoriteId,
    DateTimeOffset CreatedAt,
    ListingDirectoryItem Listing);

public enum AddFavoriteStatus
{
    Succeeded,
    CustomerNotFound,
    ListingNotFound
}

public sealed record AddFavoriteResult(AddFavoriteStatus Status);

public sealed record SavedSearchDetails(
    Guid Id,
    string Name,
    string FilterJson,
    int FilterSchemaVersion,
    bool AlertsEnabled,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record SaveSearchCommand(
    string Name,
    string FilterJson,
    bool AlertsEnabled);

public enum UpdateSavedSearchStatus
{
    Succeeded,
    NotFound
}

public sealed record UpdateSavedSearchResult(
    UpdateSavedSearchStatus Status,
    SavedSearchDetails? SavedSearch);
