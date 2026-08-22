using System.Text.RegularExpressions;
using EstateHub.Api.Authorization.CompanyPermissions;
using EstateHub.Api.Contracts.CompanyListings;
using EstateHub.Application.CompanyAccess;
using EstateHub.Application.CompanyListings;
using EstateHub.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EstateHub.Api.Controllers;

[ApiController]
[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/company/listings")]
public sealed class CompanyListingsController(ICompanyListingManagementService service) : ControllerBase
{
    private const decimal MaximumDecimal18_2 = 9999999999999999.99m;
    private static readonly Regex SlugPattern = new("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.Compiled);

    [HttpGet]
    [RequireCompanyPermission(CompanyPermissionCodes.ListingsRead)]
    public async Task<ActionResult<CompanyListingsResponse>> GetListings([FromQuery] CompanyListingDirectoryRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var query = BuildDirectoryQuery(request);
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = await service.GetListingsAsync(userId, query, cancellationToken);
        return result is null ? ListingNotFound() : Ok(CompanyListingsResponse.From(result));
    }

    [HttpGet("{listingId}")]
    [RequireCompanyPermission(CompanyPermissionCodes.ListingsRead)]
    public async Task<ActionResult<CompanyListingDetailsResponse>> GetListing(Guid listingId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (listingId == Guid.Empty) return InvalidRoute("listingId", "ListingId is required.");
        var result = await service.GetListingAsync(userId, listingId, cancellationToken);
        return result is null ? ListingNotFound() : Ok(CompanyListingDetailsResponse.From(result));
    }

    [HttpPost]
    [RequireCompanyPermission(CompanyPermissionCodes.ListingsManage)]
    public async Task<ActionResult<CompanyListingDetailsResponse>> CreateListing(CreateCompanyListingRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var command = BuildCommand(request.UnitId, request.CurrencyCode, request.ListingCode, request.Slug, request.Title, request.Description, request.ListingType, request.AskingPrice, request.RentPeriod, null, null, false);
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = await service.CreateListingAsync(userId, command, cancellationToken);
        return MutationResult(result, true);
    }

    [HttpPut("{listingId}")]
    [RequireCompanyPermission(CompanyPermissionCodes.ListingsManage)]
    public async Task<ActionResult<CompanyListingDetailsResponse>> UpdateListing(Guid listingId, UpdateCompanyListingRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (listingId == Guid.Empty) ModelState.AddModelError("listingId", "ListingId is required.");
        var command = BuildCommand(request.UnitId, request.CurrencyCode, request.ListingCode, request.Slug, request.Title, request.Description, request.ListingType, request.AskingPrice, request.RentPeriod, request.RowVersion, request.PriceChangeReason, true);
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = await service.UpdateListingAsync(userId, listingId, command, cancellationToken);
        return MutationResult(result, false);
    }

    [HttpPut("{listingId}/media")]
    [RequireCompanyPermission(CompanyPermissionCodes.ListingsManage)]
    public async Task<ActionResult<ReplaceCompanyListingMediaResponse>> ReplaceMedia(Guid listingId, ReplaceCompanyListingMediaRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (listingId == Guid.Empty) ModelState.AddModelError("listingId", "ListingId is required.");
        var rowVersion = ParseRowVersion(request.RowVersion);
        var input = request.Items;
        if (input is null || input.Count > 50 || input.Any(x => x.FileAssetId == Guid.Empty || x.SortOrder < 0 || x.Caption?.Trim().Length > 500)
            || input.Select(x => x.FileAssetId).Distinct().Count() != input.Count || input.Count(x => x.IsCover) > 1)
            ModelState.AddModelError("items", "Items are invalid.");
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var items = input!.Select(x => new CompanyListingMediaItem(x.FileAssetId, x.SortOrder, x.IsCover, string.IsNullOrWhiteSpace(x.Caption) ? null : x.Caption.Trim())).ToList();
        var result = await service.ReplaceMediaAsync(userId, listingId, items, rowVersion!, cancellationToken);
        return result.Status switch
        {
            CompanyListingOperationStatus.Succeeded => Ok(new ReplaceCompanyListingMediaResponse(Convert.ToBase64String(result.RowVersion!), result.Items!.Select(CompanyListingMediaResponse.From).ToList())),
            CompanyListingOperationStatus.NotFound or CompanyListingOperationStatus.InvalidReference => ListingNotFound(),
            CompanyListingOperationStatus.InvalidRequest => BadRequest(new ProblemDetails { Status = StatusCodes.Status400BadRequest, Title = "Listing media request is invalid." }),
            CompanyListingOperationStatus.Conflict => ListingConflict(),
            _ => ListingUnavailable()
        };
    }

    [HttpPost("{listingId}/payment-plans")]
    [RequireCompanyPermission(CompanyPermissionCodes.ListingsManage)]
    public async Task<ActionResult<CompanyListingPaymentPlanMutationResponse>> CreatePaymentPlan(Guid listingId, CreateCompanyListingPaymentPlanRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (listingId == Guid.Empty) ModelState.AddModelError("listingId", "ListingId is required.");
        var command = BuildPaymentPlanCommand(request.Name, request.TotalPrice, request.CurrencyCode, request.DownPaymentPercentage, request.DurationMonths, request.InstallmentFrequency, request.CashDiscountPercentage, request.IsActive);
        var rowVersion = ParseRowVersion(request.RowVersion);
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = await service.CreatePaymentPlanAsync(userId, listingId, command, rowVersion!, cancellationToken);
        return PaymentPlanMutationResult(result, true);
    }

    [HttpPut("{listingId}/payment-plans/{paymentPlanId}")]
    [RequireCompanyPermission(CompanyPermissionCodes.ListingsManage)]
    public async Task<ActionResult<CompanyListingPaymentPlanMutationResponse>> UpdatePaymentPlan(Guid listingId, Guid paymentPlanId, UpdateCompanyListingPaymentPlanRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (listingId == Guid.Empty) ModelState.AddModelError("listingId", "ListingId is required.");
        if (paymentPlanId == Guid.Empty) ModelState.AddModelError("paymentPlanId", "PaymentPlanId is required.");
        var command = BuildPaymentPlanCommand(request.Name, request.TotalPrice, request.CurrencyCode, request.DownPaymentPercentage, request.DurationMonths, request.InstallmentFrequency, request.CashDiscountPercentage, request.IsActive);
        var rowVersion = ParseRowVersion(request.RowVersion);
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = await service.UpdatePaymentPlanAsync(userId, listingId, paymentPlanId, command, rowVersion!, cancellationToken);
        return PaymentPlanMutationResult(result, false);
    }

    [HttpDelete("{listingId}/payment-plans/{paymentPlanId}")]
    [RequireCompanyPermission(CompanyPermissionCodes.ListingsManage)]
    public async Task<IActionResult> DeletePaymentPlan(Guid listingId, Guid paymentPlanId, [FromQuery] string? rowVersion, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (listingId == Guid.Empty) ModelState.AddModelError("listingId", "ListingId is required.");
        if (paymentPlanId == Guid.Empty) ModelState.AddModelError("paymentPlanId", "PaymentPlanId is required.");
        var rowVersionBytes = ParseRowVersion(rowVersion);
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = await service.DeletePaymentPlanAsync(userId, listingId, paymentPlanId, rowVersionBytes!, cancellationToken);
        return result switch
        {
            CompanyListingOperationStatus.Succeeded => NoContent(),
            CompanyListingOperationStatus.NotFound => ListingNotFound(),
            CompanyListingOperationStatus.Conflict => ListingConflict(),
            _ => ListingUnavailable()
        };
    }

    [HttpPost("{listingId}/publish")]
    [RequireCompanyPermission(CompanyPermissionCodes.ListingsManage)]
    public Task<IActionResult> PublishListing(Guid listingId, CompanyListingLifecycleRequest request, CancellationToken cancellationToken) =>
        ChangeLifecycle(listingId, request, true, cancellationToken);

    [HttpPost("{listingId}/archive")]
    [RequireCompanyPermission(CompanyPermissionCodes.ListingsManage)]
    public Task<IActionResult> ArchiveListing(Guid listingId, CompanyListingLifecycleRequest request, CancellationToken cancellationToken) =>
        ChangeLifecycle(listingId, request, false, cancellationToken);

    private ActionResult<CompanyListingDetailsResponse> MutationResult(CompanyListingMutationResult result, bool create) => result.Status switch
    {
        CompanyListingOperationStatus.Succeeded when create => CreatedAtAction(nameof(GetListing), new { listingId = result.Listing!.Header.Id }, CompanyListingDetailsResponse.From(result.Listing)),
        CompanyListingOperationStatus.Succeeded => Ok(CompanyListingDetailsResponse.From(result.Listing!)),
        CompanyListingOperationStatus.NotFound or CompanyListingOperationStatus.InvalidReference => ListingNotFound(),
        CompanyListingOperationStatus.InvalidRequest => BadRequest(new ProblemDetails { Status = StatusCodes.Status400BadRequest, Title = "Listing request is invalid." }),
        CompanyListingOperationStatus.Conflict => ListingConflict(),
        _ => ListingUnavailable()
    };

    private ActionResult<CompanyListingPaymentPlanMutationResponse> PaymentPlanMutationResult(CompanyListingPaymentPlanResult result, bool created) => result.Status switch
    {
        CompanyListingOperationStatus.Succeeded when created => StatusCode(StatusCodes.Status201Created, new CompanyListingPaymentPlanMutationResponse(Convert.ToBase64String(result.RowVersion!), CompanyListingPaymentPlanResponse.From(result.PaymentPlan!))),
        CompanyListingOperationStatus.Succeeded => Ok(new CompanyListingPaymentPlanMutationResponse(Convert.ToBase64String(result.RowVersion!), CompanyListingPaymentPlanResponse.From(result.PaymentPlan!))),
        CompanyListingOperationStatus.NotFound or CompanyListingOperationStatus.InvalidReference => ListingNotFound(),
        CompanyListingOperationStatus.InvalidRequest => BadRequest(new ProblemDetails { Status = StatusCodes.Status400BadRequest, Title = "Payment plan request is invalid." }),
        CompanyListingOperationStatus.Conflict => ListingConflict(),
        _ => ListingUnavailable()
    };

    private async Task<IActionResult> ChangeLifecycle(Guid listingId, CompanyListingLifecycleRequest request, bool publish, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (listingId == Guid.Empty) ModelState.AddModelError("listingId", "ListingId is required.");
        var rowVersion = ParseRowVersion(request.RowVersion);
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = publish
            ? await service.PublishListingAsync(userId, listingId, rowVersion!, cancellationToken)
            : await service.ArchiveListingAsync(userId, listingId, rowVersion!, cancellationToken);
        return result switch
        {
            CompanyListingOperationStatus.Succeeded => NoContent(),
            CompanyListingOperationStatus.NotFound => ListingNotFound(),
            CompanyListingOperationStatus.Conflict => ListingConflict(),
            _ => ListingUnavailable()
        };
    }

    private CompanyListingDirectoryQuery BuildDirectoryQuery(CompanyListingDirectoryRequest request)
    {
        if (request.PageNumber is < 1 or > 10000) ModelState.AddModelError("pageNumber", "PageNumber must be between 1 and 10000.");
        if (request.PageSize is < 1 or > 50) ModelState.AddModelError("pageSize", "PageSize must be between 1 and 50.");
        var search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();
        if (search?.Length > 100) ModelState.AddModelError("search", "Search must not exceed 100 characters.");
        var publicationStatus = ParseEnum<ListingPublicationStatus>(request.PublicationStatus, "publicationStatus", false);
        var listingType = ParseEnum<ListingType>(request.ListingType, "listingType", false);
        ValidateOptionalGuid(request.UnitId, "unitId");
        ValidateOptionalGuid(request.ProjectId, "projectId");
        var currencyCode = NormalizeCurrencyCode(request.CurrencyCode, "currencyCode", false);
        return new CompanyListingDirectoryQuery(request.PageNumber, request.PageSize, search, publicationStatus, listingType, request.UnitId, request.ProjectId, currencyCode);
    }

    private CompanyListingCommand BuildCommand(Guid unitId, string? currencyCodeValue, string? listingCodeValue, string? slugValue, string? titleValue, string? descriptionValue, string? listingTypeValue, decimal askingPrice, string? rentPeriodValue, string? rowVersionValue, string? priceChangeReasonValue, bool requireRowVersion)
    {
        if (unitId == Guid.Empty) ModelState.AddModelError("unitId", "UnitId is required.");
        var currencyCode = NormalizeCurrencyCode(currencyCodeValue, "currencyCode", true);
        var listingCode = listingCodeValue?.Trim();
        if (string.IsNullOrWhiteSpace(listingCode) || listingCode.Length > 100) ModelState.AddModelError("listingCode", "ListingCode is required and must not exceed 100 characters.");
        var slug = slugValue?.Trim();
        if (string.IsNullOrWhiteSpace(slug) || slug.Length > 200 || slug != slug.ToLowerInvariant() || !SlugPattern.IsMatch(slug)) ModelState.AddModelError("slug", "Slug must be lowercase kebab-case and must not exceed 200 characters.");
        var title = titleValue?.Trim();
        if (string.IsNullOrWhiteSpace(title) || title.Length > 300) ModelState.AddModelError("title", "Title is required and must not exceed 300 characters.");
        var description = descriptionValue?.Trim();
        if (string.IsNullOrWhiteSpace(description) || description.Length > 4000) ModelState.AddModelError("description", "Description is required and must not exceed 4000 characters.");
        var listingType = ParseEnum<ListingType>(listingTypeValue, "listingType", true);
        var rentPeriod = listingType == ListingType.Rent ? ParseEnum<RentPeriod>(rentPeriodValue, "rentPeriod", true) : null;
        if (listingType == ListingType.Sale && rentPeriodValue is not null) ModelState.AddModelError("rentPeriod", "RentPeriod must not be supplied for a sale listing.");
        if (askingPrice <= 0m || askingPrice > MaximumDecimal18_2 || DecimalScale(askingPrice) > 2) ModelState.AddModelError("askingPrice", "AskingPrice must be a positive decimal(18,2) value.");
        var rowVersion = requireRowVersion ? ParseRowVersion(rowVersionValue) : null;
        var priceChangeReason = string.IsNullOrWhiteSpace(priceChangeReasonValue) ? null : priceChangeReasonValue.Trim();
        if (priceChangeReason?.Length > 500) ModelState.AddModelError("priceChangeReason", "PriceChangeReason must not exceed 500 characters.");
        return new CompanyListingCommand(unitId, currencyCode ?? string.Empty, listingCode ?? string.Empty, slug ?? string.Empty, title ?? string.Empty, description ?? string.Empty, listingType ?? default, askingPrice, rentPeriod, rowVersion, priceChangeReason);
    }

    private CompanyListingPaymentPlanCommand BuildPaymentPlanCommand(string? nameValue, decimal totalPrice, string? currencyCodeValue, decimal downPaymentPercentage, int durationMonths, string? installmentFrequencyValue, decimal? cashDiscountPercentage, bool? isActive)
    {
        var name = nameValue?.Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length > 200) ModelState.AddModelError("name", "Name is required and must not exceed 200 characters.");
        if (totalPrice <= 0m || totalPrice > MaximumDecimal18_2 || DecimalScale(totalPrice) > 2) ModelState.AddModelError("totalPrice", "TotalPrice must be a positive decimal(18,2) value.");
        var currencyCode = NormalizeCurrencyCode(currencyCodeValue, "currencyCode", true);
        if (downPaymentPercentage is < 0m or > 100m || DecimalScale(downPaymentPercentage) > 2) ModelState.AddModelError("downPaymentPercentage", "DownPaymentPercentage must be a decimal(5,2) percentage between 0 and 100.");
        if (durationMonths <= 0) ModelState.AddModelError("durationMonths", "DurationMonths must be greater than zero.");
        var installmentFrequency = ParseEnum<InstallmentFrequency>(installmentFrequencyValue, "installmentFrequency", true);
        if (cashDiscountPercentage is < 0m or > 100m || (cashDiscountPercentage is not null && DecimalScale(cashDiscountPercentage.Value) > 2)) ModelState.AddModelError("cashDiscountPercentage", "CashDiscountPercentage must be a decimal(5,2) percentage between 0 and 100.");
        if (isActive is null) ModelState.AddModelError("isActive", "IsActive is required.");
        return new CompanyListingPaymentPlanCommand(name ?? string.Empty, totalPrice, currencyCode ?? string.Empty, downPaymentPercentage, durationMonths, installmentFrequency ?? default, cashDiscountPercentage, isActive ?? false);
    }

    private T? ParseEnum<T>(string? value, string key, bool required) where T : struct, Enum
    {
        if (value is null)
        {
            if (required) ModelState.AddModelError(key, $"{key} is required.");
            return null;
        }
        var normalized = value.Trim();
        if (!string.IsNullOrEmpty(normalized) && Enum.TryParse<T>(normalized, true, out var candidate) && Enum.IsDefined(candidate) && string.Equals(normalized, candidate.ToString(), StringComparison.OrdinalIgnoreCase)) return candidate;
        ModelState.AddModelError(key, $"{key} is invalid.");
        return null;
    }

    private string? NormalizeCurrencyCode(string? value, string key, bool required)
    {
        if (value is null)
        {
            if (required) ModelState.AddModelError(key, "CurrencyCode is required.");
            return null;
        }
        var normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length == 3 && normalized.All(character => character is >= 'A' and <= 'Z')) return normalized;
        ModelState.AddModelError(key, "CurrencyCode must contain exactly three ASCII letters.");
        return null;
    }

    private byte[]? ParseRowVersion(string? value)
    {
        try
        {
            var bytes = string.IsNullOrWhiteSpace(value) ? null : Convert.FromBase64String(value);
            if (bytes is { Length: 8 }) return bytes;
        }
        catch (FormatException) { }
        ModelState.AddModelError("rowVersion", "RowVersion must be Base64 encoded eight bytes.");
        return null;
    }

    private static int DecimalScale(decimal value) => (decimal.GetBits(value)[3] >> 16) & 0xff;
    private void ValidateOptionalGuid(Guid? value, string key) { if (value == Guid.Empty) ModelState.AddModelError(key, $"{key} cannot be empty when supplied."); }
    private bool TryGetUserId(out Guid userId) => Guid.TryParse(User.FindFirst("sub")?.Value, out userId) && userId != Guid.Empty;
    private ActionResult InvalidRoute(string key, string message) { ModelState.AddModelError(key, message); return ValidationProblem(ModelState); }
    private ObjectResult ListingNotFound() => Problem(statusCode: StatusCodes.Status404NotFound, title: "Listing not found.");
    private ObjectResult ListingConflict() => Problem(statusCode: StatusCodes.Status409Conflict, title: "Listing operation conflict.");
    private ObjectResult ListingUnavailable() => Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Listing operation is temporarily unavailable.");
}
