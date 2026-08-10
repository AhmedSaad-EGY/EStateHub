using System.Text.Json;
using EstateHub.Api.Contracts.Listings;
using EstateHub.Application.Common;
using EstateHub.Application.Customers;
using EstateHub.Domain.Enums;

namespace EstateHub.Api.Contracts.Customers;

public sealed record CustomerProfileResponse(
    Guid Id,
    string FullName,
    string Persona,
    string Email,
    string? PhoneNumber,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static CustomerProfileResponse From(CustomerProfileDetails profile)
    {
        return new CustomerProfileResponse(
            profile.Id,
            profile.FullName,
            CustomerPersonaText.From(profile.Persona),
            profile.Email,
            profile.PhoneNumber,
            profile.CreatedAt,
            profile.UpdatedAt);
    }
}

public sealed record CustomerFavoriteResponse(
    Guid FavoriteId,
    DateTimeOffset CreatedAt,
    ListingDirectoryItemResponse Listing)
{
    public static CustomerFavoriteResponse From(CustomerFavoriteItem favorite)
    {
        return new CustomerFavoriteResponse(
            favorite.FavoriteId,
            favorite.CreatedAt,
            ListingDirectoryItemResponse.From(favorite.Listing));
    }
}

public sealed record CustomerFavoritesResponse(
    IReadOnlyList<CustomerFavoriteResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage)
{
    public static CustomerFavoritesResponse From(
        PagedResult<CustomerFavoriteItem> result)
    {
        return new CustomerFavoritesResponse(
            result.Items.Select(CustomerFavoriteResponse.From).ToArray(),
            result.PageNumber,
            result.PageSize,
            result.TotalCount,
            result.TotalPages,
            result.HasPreviousPage,
            result.HasNextPage);
    }
}

public sealed record SavedSearchResponse(
    Guid Id,
    string Name,
    JsonElement Filters,
    int FilterSchemaVersion,
    bool AlertsEnabled,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static SavedSearchResponse From(SavedSearchDetails savedSearch)
    {
        using var document = JsonDocument.Parse(savedSearch.FilterJson);

        return new SavedSearchResponse(
            savedSearch.Id,
            savedSearch.Name,
            document.RootElement.Clone(),
            savedSearch.FilterSchemaVersion,
            savedSearch.AlertsEnabled,
            savedSearch.CreatedAt,
            savedSearch.UpdatedAt);
    }
}

public sealed record SavedSearchesResponse(
    IReadOnlyList<SavedSearchResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage)
{
    public static SavedSearchesResponse From(PagedResult<SavedSearchDetails> result)
    {
        return new SavedSearchesResponse(
            result.Items.Select(SavedSearchResponse.From).ToArray(),
            result.PageNumber,
            result.PageSize,
            result.TotalCount,
            result.TotalPages,
            result.HasPreviousPage,
            result.HasNextPage);
    }
}

internal static class CustomerPersonaText
{
    public static string From(CustomerPersona value)
    {
        return value switch
        {
            CustomerPersona.Buyer => "Buyer",
            CustomerPersona.Renter => "Renter",
            CustomerPersona.Agent => "Agent",
            _ => throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "Unsupported CustomerPersona value.")
        };
    }
}
