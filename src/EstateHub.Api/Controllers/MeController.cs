using System.Text.Json;
using EstateHub.Api.Contracts.Customers;
using EstateHub.Application.Customers;
using EstateHub.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EstateHub.Api.Controllers;

[ApiController]
[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/me")]
public sealed class MeController(
    ICustomerSelfService customerSelfService) : ControllerBase
{
    private const int MaximumPageNumber = 10000;
    private const int MaximumPageSize = 50;
    private const int MaximumProfileTextLength = 200;
    private const int MaximumPhoneNumberLength = 32;
    private const int MaximumSavedSearchFilterLength = 16000;

    [HttpGet("profile")]
    public async Task<ActionResult<CustomerProfileResponse>> GetProfile(
        CancellationToken cancellationToken)
    {
        if (!TryGetApplicationUserId(out var applicationUserId))
        {
            return Unauthorized();
        }

        var profile = await customerSelfService.GetProfileAsync(
            applicationUserId,
            cancellationToken);

        return profile is null
            ? CustomerProfileNotFound()
            : Ok(CustomerProfileResponse.From(profile));
    }

    [HttpPut("profile")]
    public async Task<ActionResult<CustomerProfileResponse>> UpdateProfile(
        UpdateCustomerProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetApplicationUserId(out var applicationUserId))
        {
            return Unauthorized();
        }

        var fullName = request.FullName?.Trim();
        var phoneNumber = request.PhoneNumber?.Trim();
        var personaIsValid = TryParsePersona(request.Persona?.Trim(), out var persona);

        AddRequiredAndMaximumLengthError(
            nameof(request.FullName),
            fullName,
            MaximumProfileTextLength);
        AddRequiredAndMaximumLengthError(
            nameof(request.PhoneNumber),
            phoneNumber,
            MaximumPhoneNumberLength);

        if (!personaIsValid)
        {
            ModelState.AddModelError(
                nameof(request.Persona),
                "Persona must be Buyer, Renter, or Agent.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await customerSelfService.UpdateProfileAsync(
            applicationUserId,
            new UpdateCustomerProfileCommand(fullName!, persona, phoneNumber!),
            cancellationToken);

        return result.Status switch
        {
            UpdateCustomerProfileStatus.Succeeded => Ok(
                CustomerProfileResponse.From(result.Profile!)),
            UpdateCustomerProfileStatus.ServiceUnavailable => Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Profile update is temporarily unavailable."),
            _ => CustomerProfileNotFound()
        };
    }

    [HttpGet("favorites")]
    public async Task<ActionResult<CustomerFavoritesResponse>> GetFavorites(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetApplicationUserId(out var applicationUserId))
        {
            return Unauthorized();
        }

        ValidatePaging(pageNumber, pageSize);

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var favorites = await customerSelfService.GetFavoritesAsync(
            applicationUserId,
            pageNumber,
            pageSize,
            cancellationToken);

        return favorites is null
            ? CustomerProfileNotFound()
            : Ok(CustomerFavoritesResponse.From(favorites));
    }

    [HttpPut("favorites/{listingId}")]
    public async Task<IActionResult> AddFavorite(
        Guid listingId,
        CancellationToken cancellationToken)
    {
        if (!TryGetApplicationUserId(out var applicationUserId))
        {
            return Unauthorized();
        }

        if (listingId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(listingId), "ListingId is required.");
            return ValidationProblem(ModelState);
        }

        var result = await customerSelfService.AddFavoriteAsync(
            applicationUserId,
            listingId,
            cancellationToken);

        return result.Status switch
        {
            AddFavoriteStatus.Succeeded => NoContent(),
            AddFavoriteStatus.ListingNotFound => ListingNotFound(),
            _ => CustomerProfileNotFound()
        };
    }

    [HttpDelete("favorites/{listingId}")]
    public async Task<IActionResult> RemoveFavorite(
        Guid listingId,
        CancellationToken cancellationToken)
    {
        if (!TryGetApplicationUserId(out var applicationUserId))
        {
            return Unauthorized();
        }

        if (listingId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(listingId), "ListingId is required.");
            return ValidationProblem(ModelState);
        }

        var customerExists = await customerSelfService.RemoveFavoriteAsync(
            applicationUserId,
            listingId,
            cancellationToken);

        return customerExists ? NoContent() : CustomerProfileNotFound();
    }

    [HttpGet("saved-searches")]
    public async Task<ActionResult<SavedSearchesResponse>> GetSavedSearches(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetApplicationUserId(out var applicationUserId))
        {
            return Unauthorized();
        }

        ValidatePaging(pageNumber, pageSize);

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var searches = await customerSelfService.GetSavedSearchesAsync(
            applicationUserId,
            pageNumber,
            pageSize,
            cancellationToken);

        return searches is null
            ? CustomerProfileNotFound()
            : Ok(SavedSearchesResponse.From(searches));
    }

    [HttpPost("saved-searches")]
    public async Task<ActionResult<SavedSearchResponse>> CreateSavedSearch(
        SaveSearchRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetApplicationUserId(out var applicationUserId))
        {
            return Unauthorized();
        }

        if (!TryCreateSaveSearchCommand(request, out var command))
        {
            return ValidationProblem(ModelState);
        }

        var savedSearch = await customerSelfService.CreateSavedSearchAsync(
            applicationUserId,
            command,
            cancellationToken);

        return savedSearch is null
            ? CustomerProfileNotFound()
            : StatusCode(
                StatusCodes.Status201Created,
                SavedSearchResponse.From(savedSearch));
    }

    [HttpPut("saved-searches/{savedSearchId}")]
    public async Task<ActionResult<SavedSearchResponse>> UpdateSavedSearch(
        Guid savedSearchId,
        SaveSearchRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetApplicationUserId(out var applicationUserId))
        {
            return Unauthorized();
        }

        if (savedSearchId == Guid.Empty)
        {
            ModelState.AddModelError(
                nameof(savedSearchId),
                "SavedSearchId is required.");
        }

        var isValidCommand = TryCreateSaveSearchCommand(request, out var command);

        if (!isValidCommand || !ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await customerSelfService.UpdateSavedSearchAsync(
            applicationUserId,
            savedSearchId,
            command,
            cancellationToken);

        return result.Status == UpdateSavedSearchStatus.Succeeded
            ? Ok(SavedSearchResponse.From(result.SavedSearch!))
            : SavedSearchNotFound();
    }

    [HttpDelete("saved-searches/{savedSearchId}")]
    public async Task<IActionResult> RemoveSavedSearch(
        Guid savedSearchId,
        CancellationToken cancellationToken)
    {
        if (!TryGetApplicationUserId(out var applicationUserId))
        {
            return Unauthorized();
        }

        if (savedSearchId == Guid.Empty)
        {
            ModelState.AddModelError(
                nameof(savedSearchId),
                "SavedSearchId is required.");
            return ValidationProblem(ModelState);
        }

        var customerExists = await customerSelfService.RemoveSavedSearchAsync(
            applicationUserId,
            savedSearchId,
            cancellationToken);

        return customerExists ? NoContent() : CustomerProfileNotFound();
    }

    private bool TryCreateSaveSearchCommand(
        SaveSearchRequest request,
        out SaveSearchCommand command)
    {
        var name = request.Name?.Trim();
        AddRequiredAndMaximumLengthError(
            nameof(request.Name),
            name,
            MaximumProfileTextLength);

        string? filterJson = null;

        if (request.Filters is null
            || request.Filters.Value.ValueKind != JsonValueKind.Object)
        {
            ModelState.AddModelError(
                nameof(request.Filters),
                "Filters must be a JSON object.");
        }
        else
        {
            filterJson = JsonSerializer.Serialize(request.Filters.Value);

            if (filterJson.Length > MaximumSavedSearchFilterLength)
            {
                ModelState.AddModelError(
                    nameof(request.Filters),
                    $"Filters must not exceed {MaximumSavedSearchFilterLength} characters.");
            }
        }

        if (!ModelState.IsValid)
        {
            command = null!;
            return false;
        }

        command = new SaveSearchCommand(
            name!,
            filterJson!,
            request.AlertsEnabled);

        return true;
    }

    private bool TryGetApplicationUserId(out Guid applicationUserId)
    {
        var subject = User.FindFirst("sub")?.Value;

        return Guid.TryParse(subject, out applicationUserId)
            && applicationUserId != Guid.Empty;
    }

    private void ValidatePaging(int pageNumber, int pageSize)
    {
        if (pageNumber is < 1 or > MaximumPageNumber)
        {
            ModelState.AddModelError(
                nameof(pageNumber),
                $"PageNumber must be between 1 and {MaximumPageNumber}.");
        }

        if (pageSize is < 1 or > MaximumPageSize)
        {
            ModelState.AddModelError(
                nameof(pageSize),
                $"PageSize must be between 1 and {MaximumPageSize}.");
        }
    }

    private void AddRequiredAndMaximumLengthError(
        string key,
        string? value,
        int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > maximumLength)
        {
            ModelState.AddModelError(
                key,
                $"A value of at most {maximumLength} characters is required.");
        }
    }

    private static bool TryParsePersona(
        string? value,
        out CustomerPersona persona)
    {
        if (string.Equals(value, "Buyer", StringComparison.OrdinalIgnoreCase))
        {
            persona = CustomerPersona.Buyer;
            return true;
        }

        if (string.Equals(value, "Renter", StringComparison.OrdinalIgnoreCase))
        {
            persona = CustomerPersona.Renter;
            return true;
        }

        if (string.Equals(value, "Agent", StringComparison.OrdinalIgnoreCase))
        {
            persona = CustomerPersona.Agent;
            return true;
        }

        persona = default;
        return false;
    }

    private ObjectResult CustomerProfileNotFound()
    {
        return Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Customer profile not found.");
    }

    private ObjectResult ListingNotFound()
    {
        return Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Listing not found.");
    }

    private ObjectResult SavedSearchNotFound()
    {
        return Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Saved search not found.");
    }
}
