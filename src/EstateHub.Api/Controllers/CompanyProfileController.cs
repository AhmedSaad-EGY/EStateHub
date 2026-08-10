using System.ComponentModel.DataAnnotations;
using EstateHub.Api.Authorization.CompanyPermissions;
using EstateHub.Api.Contracts.CompanyManagement;
using EstateHub.Application.CompanyAccess;
using EstateHub.Application.CompanyManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EstateHub.Api.Controllers;

[ApiController]
[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/company/profile")]
public sealed class CompanyProfileController(
    ICompanyManagementService companyManagementService) : ControllerBase
{
    private static readonly EmailAddressAttribute EmailValidator = new();

    [HttpGet]
    [RequireCompanyPermission(CompanyPermissionCodes.CompanyProfileRead)]
    public async Task<ActionResult<CompanyProfileResponse>> GetProfile(
        CancellationToken cancellationToken)
    {
        if (!TryGetApplicationUserId(out var applicationUserId))
        {
            return Unauthorized();
        }

        var profile = await companyManagementService.GetProfileAsync(
            applicationUserId,
            cancellationToken);

        return profile is null
            ? CompanyProfileNotFound()
            : Ok(CompanyProfileResponse.From(profile));
    }

    [HttpPut]
    [RequireCompanyPermission(CompanyPermissionCodes.CompanyProfileManage)]
    public async Task<ActionResult<CompanyProfileResponse>> UpdateProfile(
        UpdateCompanyProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetApplicationUserId(out var applicationUserId))
        {
            return Unauthorized();
        }

        if (!TryCreateCommand(request, out var command))
        {
            return ValidationProblem(ModelState);
        }

        var result = await companyManagementService.UpdateProfileAsync(
            applicationUserId,
            command,
            cancellationToken);

        return result.Status switch
        {
            CompanyManagementStatus.Succeeded => Ok(
                CompanyProfileResponse.From(result.Profile!)),
            CompanyManagementStatus.NotFound => CompanyProfileNotFound(),
            CompanyManagementStatus.Conflict => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Company profile update conflict."),
            CompanyManagementStatus.InvalidReference => ValidationProblem(
                new ValidationProblemDetails(
                    new Dictionary<string, string[]>
                    {
                        ["profile"] = ["The supplied currency or address location is unavailable."]
                    })),
            _ => Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Company profile update is temporarily unavailable.")
        };
    }

    private bool TryCreateCommand(
        UpdateCompanyProfileRequest request,
        out UpdateCompanyProfileCommand command)
    {
        var displayName = TrimAndValidateRequired(
            request.DisplayName,
            "displayName",
            200);
        var businessEmail = request.BusinessEmail?.Trim();
        var supportPhone = TrimAndValidateRequired(
            request.SupportPhone,
            "supportPhone",
            32);
        var website = request.Website?.Trim();
        var timeZoneId = TrimAndValidateRequired(
            request.TimeZoneId,
            "timeZoneId",
            100);
        var currencyCode = request.BaseCurrencyCode?.Trim().ToUpperInvariant();
        var addressLine1 = TrimAndValidateRequired(
            request.AddressLine1,
            "addressLine1",
            300);
        var addressLine2 = TrimAndValidateOptional(request.AddressLine2, "addressLine2", 300);
        var postalCode = TrimAndValidateOptional(request.PostalCode, "postalCode", 20);
        var rowVersion = ParseRowVersion(request.RowVersion, "rowVersion");

        if (string.IsNullOrWhiteSpace(businessEmail)
            || businessEmail.Length > 256
            || !EmailValidator.IsValid(businessEmail))
        {
            ModelState.AddModelError(
                "businessEmail",
                "BusinessEmail must be a valid email address of at most 256 characters.");
        }

        if (website is not null
            && (website.Length > 2048
                || !Uri.TryCreate(website, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp
                    && uri.Scheme != Uri.UriSchemeHttps)))
        {
            ModelState.AddModelError(
                "website",
                "Website must be an absolute HTTP or HTTPS URL of at most 2048 characters.");
        }

        if (!IsThreeAsciiLetters(currencyCode))
        {
            ModelState.AddModelError(
                "baseCurrencyCode",
                "BaseCurrencyCode must contain exactly three ASCII letters.");
        }

        if (request.AddressLocationId == Guid.Empty)
        {
            ModelState.AddModelError("addressLocationId", "AddressLocationId is required.");
        }

        if (!AreCoordinatesValid(request.Latitude, request.Longitude))
        {
            ModelState.AddModelError(
                "coordinates",
                "Latitude and longitude must both be null or within their valid ranges.");
        }

        if (!ModelState.IsValid)
        {
            command = null!;
            return false;
        }

        command = new UpdateCompanyProfileCommand(
            displayName!,
            businessEmail!,
            supportPhone!,
            website,
            timeZoneId!,
            currencyCode!,
            request.AddressLocationId,
            addressLine1!,
            addressLine2,
            postalCode,
            request.Latitude,
            request.Longitude,
            rowVersion!);
        return true;
    }

    private string? TrimAndValidateRequired(string? value, string key, int maximumLength)
    {
        var normalized = value?.Trim();

        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > maximumLength)
        {
            ModelState.AddModelError(
                key,
                $"{key} is required and must not exceed {maximumLength} characters.");
        }

        return normalized;
    }

    private string? TrimAndValidateOptional(string? value, string key, int maximumLength)
    {
        var normalized = value?.Trim();

        if (normalized is { Length: > 0 } && normalized.Length > maximumLength)
        {
            ModelState.AddModelError(key, $"{key} must not exceed {maximumLength} characters.");
        }

        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }

    private byte[]? ParseRowVersion(string? value, string key)
    {
        try
        {
            var bytes = string.IsNullOrWhiteSpace(value)
                ? null
                : Convert.FromBase64String(value);

            if (bytes is { Length: 8 })
            {
                return bytes;
            }
        }
        catch (FormatException)
        {
            // The validation error below is intentionally generic.
        }

        ModelState.AddModelError(key, "RowVersion must be Base64 encoded eight bytes.");
        return null;
    }

    private static bool IsThreeAsciiLetters(string? value)
    {
        return value is { Length: 3 }
            && value.All(character =>
                character is >= 'A' and <= 'Z'
                || character is >= 'a' and <= 'z');
    }

    private static bool AreCoordinatesValid(decimal? latitude, decimal? longitude)
    {
        return latitude.HasValue == longitude.HasValue
            && (!latitude.HasValue
                || (latitude.Value >= -90m && latitude.Value <= 90m
                    && longitude!.Value >= -180m && longitude.Value <= 180m));
    }

    private bool TryGetApplicationUserId(out Guid applicationUserId)
    {
        var subject = User.FindFirst("sub")?.Value;

        return Guid.TryParse(subject, out applicationUserId)
            && applicationUserId != Guid.Empty;
    }

    private ObjectResult CompanyProfileNotFound() => Problem(
        statusCode: StatusCodes.Status404NotFound,
        title: "Company profile not found.");
}
