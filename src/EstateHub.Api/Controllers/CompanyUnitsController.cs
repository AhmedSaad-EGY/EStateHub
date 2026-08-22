using EstateHub.Api.Authorization.CompanyPermissions;
using EstateHub.Api.Contracts.CompanyUnits;
using EstateHub.Application.CompanyAccess;
using EstateHub.Application.CompanyUnits;
using EstateHub.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EstateHub.Api.Controllers;

[ApiController]
[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/company/units")]
public sealed class CompanyUnitsController(ICompanyUnitManagementService service) : ControllerBase
{
    private const decimal MaxDecimal18_2 = 9999999999999999.99m;

    [HttpGet]
    [RequireCompanyPermission(CompanyPermissionCodes.UnitsRead)]
    public async Task<ActionResult<CompanyUnitsResponse>> GetUnits([FromQuery] CompanyUnitDirectoryRequest request, CancellationToken cancellationToken)
    {
        if (!UserId(out var userId)) return Unauthorized();
        var query = DirectoryQuery(request);
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = await service.GetUnitsAsync(userId, query, cancellationToken);
        return result is null ? NotFoundResult() : Ok(CompanyUnitsResponse.From(result));
    }

    [HttpGet("{unitId}")]
    [RequireCompanyPermission(CompanyPermissionCodes.UnitsRead)]
    public async Task<ActionResult<CompanyUnitManagementDetailsResponse>> GetUnit(Guid unitId, CancellationToken cancellationToken)
    {
        if (!UserId(out var userId)) return Unauthorized();
        if (unitId == Guid.Empty) return InvalidRoute("unitId");
        var result = await service.GetUnitAsync(userId, unitId, cancellationToken);
        return result is null ? NotFoundResult() : Ok(CompanyUnitManagementDetailsResponse.From(result));
    }

    [HttpPost]
    [RequireCompanyPermission(CompanyPermissionCodes.UnitsManage)]
    public async Task<ActionResult<CompanyUnitDetailsResponse>> CreateUnit(CreateCompanyUnitRequest request, CancellationToken cancellationToken)
    {
        if (!UserId(out var userId)) return Unauthorized();
        var command = Command(request, null);
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = await service.CreateUnitAsync(userId, command, cancellationToken);
        return Mutation(result, true);
    }

    [HttpPut("{unitId}")]
    [RequireCompanyPermission(CompanyPermissionCodes.UnitsManage)]
    public async Task<ActionResult<CompanyUnitDetailsResponse>> UpdateUnit(Guid unitId, UpdateCompanyUnitRequest request, CancellationToken cancellationToken)
    {
        if (!UserId(out var userId)) return Unauthorized();
        if (unitId == Guid.Empty) ModelState.AddModelError("unitId", "UnitId is required.");
        var command = Command(new CreateCompanyUnitRequest(request.ProjectId, request.LocationId, request.UnitTypeId, request.UnitCode, request.FinishingType, request.Bedrooms, request.Bathrooms, request.FloorNumber, request.TotalFloors, request.BuiltUpArea, request.LandArea, request.FurnishedStatus), request.RowVersion);
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = await service.UpdateUnitAsync(userId, unitId, command, cancellationToken);
        return Mutation(result, false);
    }

    [HttpPut("{unitId}/status")]
    [RequireCompanyPermission(CompanyPermissionCodes.UnitsManage)]
    public async Task<ActionResult<CompanyUnitStatusResponse>> UpdateStatus(Guid unitId, UpdateCompanyUnitStatusRequest request, CancellationToken cancellationToken)
    {
        if (!UserId(out var userId)) return Unauthorized();
        if (unitId == Guid.Empty) ModelState.AddModelError("unitId", "UnitId is required.");
        var rowVersion = RowVersion(request.RowVersion);
        var status = EnumValue<UnitStatus>(request.Status, "status", true);
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = await service.UpdateUnitStatusAsync(userId, unitId, status!.Value, rowVersion!, cancellationToken);
        return result.Status switch
        {
            CompanyUnitOperationStatus.Succeeded => Ok(new CompanyUnitStatusResponse(Text(result.UnitStatus!.Value), result.UpdatedAt!.Value, Convert.ToBase64String(result.RowVersion!))),
            CompanyUnitOperationStatus.NotFound => NotFoundResult(),
            CompanyUnitOperationStatus.Conflict => ConflictResult(),
            _ => UnavailableResult()
        };
    }

    [HttpPut("{unitId}/amenities")]
    [RequireCompanyPermission(CompanyPermissionCodes.UnitsManage)]
    public async Task<ActionResult<ReplaceCompanyUnitAmenitiesResponse>> ReplaceAmenities(Guid unitId, ReplaceCompanyUnitAmenitiesRequest request, CancellationToken cancellationToken)
    {
        if (!UserId(out var userId)) return Unauthorized();
        if (unitId == Guid.Empty) ModelState.AddModelError("unitId", "UnitId is required.");
        var rowVersion = RowVersion(request.RowVersion);
        var amenityIds = request.AmenityIds;
        if (amenityIds is null || amenityIds.Any(x => x == Guid.Empty) || amenityIds.Distinct().Count() != amenityIds.Count) ModelState.AddModelError("amenityIds", "AmenityIds must contain unique non-empty IDs.");
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var result = await service.ReplaceAmenitiesAsync(userId, unitId, amenityIds!, rowVersion!, cancellationToken);
        return result.Status switch
        {
            CompanyUnitOperationStatus.Succeeded => Ok(new ReplaceCompanyUnitAmenitiesResponse(Convert.ToBase64String(result.RowVersion!), result.Amenities!.Select(CompanyUnitAmenityResponse.From).ToList())),
            CompanyUnitOperationStatus.NotFound or CompanyUnitOperationStatus.InvalidReference => NotFoundResult(),
            CompanyUnitOperationStatus.Conflict => ConflictResult(),
            _ => UnavailableResult()
        };
    }

    [HttpPost("{unitId}/nearby-places")]
    [RequireCompanyPermission(CompanyPermissionCodes.UnitsManage)]
    public async Task<ActionResult<CompanyUnitNearbyPlaceMutationResponse>> CreateNearbyPlace(Guid unitId, CompanyUnitNearbyPlaceRequest request, CancellationToken cancellationToken)
    {
        if (!UserId(out var userId)) return Unauthorized();
        if (unitId == Guid.Empty) ModelState.AddModelError("unitId", "UnitId is required.");
        var command = NearbyPlaceCommand(request);
        var rowVersion = RowVersion(request.RowVersion);
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = await service.CreateNearbyPlaceAsync(userId, unitId, command, rowVersion!, cancellationToken);
        return NearbyPlaceResult(result, true);
    }

    [HttpPut("{unitId}/nearby-places/{nearbyPlaceId}")]
    [RequireCompanyPermission(CompanyPermissionCodes.UnitsManage)]
    public async Task<ActionResult<CompanyUnitNearbyPlaceMutationResponse>> UpdateNearbyPlace(Guid unitId, Guid nearbyPlaceId, CompanyUnitNearbyPlaceRequest request, CancellationToken cancellationToken)
    {
        if (!UserId(out var userId)) return Unauthorized();
        if (unitId == Guid.Empty) ModelState.AddModelError("unitId", "UnitId is required.");
        if (nearbyPlaceId == Guid.Empty) ModelState.AddModelError("nearbyPlaceId", "NearbyPlaceId is required.");
        var command = NearbyPlaceCommand(request);
        var rowVersion = RowVersion(request.RowVersion);
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = await service.UpdateNearbyPlaceAsync(userId, unitId, nearbyPlaceId, command, rowVersion!, cancellationToken);
        return NearbyPlaceResult(result, false);
    }

    [HttpDelete("{unitId}/nearby-places/{nearbyPlaceId}")]
    [RequireCompanyPermission(CompanyPermissionCodes.UnitsManage)]
    public async Task<IActionResult> DeleteNearbyPlace(Guid unitId, Guid nearbyPlaceId, [FromQuery] string? rowVersion, CancellationToken cancellationToken)
    {
        if (!UserId(out var userId)) return Unauthorized();
        if (unitId == Guid.Empty) ModelState.AddModelError("unitId", "UnitId is required.");
        if (nearbyPlaceId == Guid.Empty) ModelState.AddModelError("nearbyPlaceId", "NearbyPlaceId is required.");
        var rowVersionBytes = RowVersion(rowVersion);
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = await service.DeleteNearbyPlaceAsync(userId, unitId, nearbyPlaceId, rowVersionBytes!, cancellationToken);
        return result switch
        {
            CompanyUnitOperationStatus.Succeeded => NoContent(),
            CompanyUnitOperationStatus.NotFound => NotFoundResult(),
            CompanyUnitOperationStatus.Conflict => ConflictResult(),
            _ => UnavailableResult()
        };
    }

    private ActionResult<CompanyUnitDetailsResponse> Mutation(CompanyUnitMutationResult result, bool isCreate) => result.Status switch
    {
        CompanyUnitOperationStatus.Succeeded when isCreate => CreatedAtAction(nameof(GetUnit), new { unitId = result.Unit!.Id }, CompanyUnitDetailsResponse.From(result.Unit)),
        CompanyUnitOperationStatus.Succeeded => Ok(CompanyUnitDetailsResponse.From(result.Unit!)),
        CompanyUnitOperationStatus.NotFound or CompanyUnitOperationStatus.InvalidReference => NotFoundResult(),
        CompanyUnitOperationStatus.Conflict => ConflictResult(),
        _ => UnavailableResult()
    };

    private ActionResult<CompanyUnitNearbyPlaceMutationResponse> NearbyPlaceResult(CompanyUnitNearbyPlaceResult result, bool created) => result.Status switch
    {
        CompanyUnitOperationStatus.Succeeded when created => StatusCode(201, new CompanyUnitNearbyPlaceMutationResponse(Convert.ToBase64String(result.RowVersion!), CompanyUnitNearbyPlaceResponse.From(result.NearbyPlace!))),
        CompanyUnitOperationStatus.Succeeded => Ok(new CompanyUnitNearbyPlaceMutationResponse(Convert.ToBase64String(result.RowVersion!), CompanyUnitNearbyPlaceResponse.From(result.NearbyPlace!))),
        CompanyUnitOperationStatus.NotFound => NotFoundResult(),
        CompanyUnitOperationStatus.Conflict => ConflictResult(),
        _ => UnavailableResult()
    };

    private CompanyUnitNearbyPlaceCommand NearbyPlaceCommand(CompanyUnitNearbyPlaceRequest request)
    {
        var category = request.Category?.Trim();
        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(category) || category.Length > 100 || string.IsNullOrWhiteSpace(name) || name.Length > 200 || request.DistanceMeters is < 0m || request.TravelMinutes is < 0 || request.Latitude is < -90m or > 90m || request.Longitude is < -180m or > 180m || !Decimal18_2(request.DistanceMeters) || !Decimal9_6(request.Latitude) || !Decimal9_6(request.Longitude)) ModelState.AddModelError("nearbyPlace", "Nearby place is invalid.");
        return new(category ?? string.Empty, name ?? string.Empty, request.DistanceMeters, request.TravelMinutes, request.Latitude, request.Longitude);
    }

    private CompanyUnitDirectoryQuery DirectoryQuery(CompanyUnitDirectoryRequest request)
    {
        if (request.PageNumber is < 1 or > 10000) ModelState.AddModelError("pageNumber", "PageNumber must be between 1 and 10000.");
        if (request.PageSize is < 1 or > 50) ModelState.AddModelError("pageSize", "PageSize must be between 1 and 50.");
        var search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();
        if (search?.Length > 100) ModelState.AddModelError("search", "Search must not exceed 100 characters.");
        var status = EnumValue<UnitStatus>(request.Status, "status", false);
        OptionalGuid(request.ProjectId, "projectId"); OptionalGuid(request.UnitTypeId, "unitTypeId"); OptionalGuid(request.LocationId, "locationId");
        return new(request.PageNumber, request.PageSize, search, status, request.ProjectId, request.UnitTypeId, request.LocationId);
    }

    private CompanyUnitCommand Command(CreateCompanyUnitRequest request, string? rowVersionValue)
    {
        var code = request.UnitCode?.Trim();
        if (string.IsNullOrWhiteSpace(code) || code.Length > 100) ModelState.AddModelError("unitCode", "UnitCode is required and must not exceed 100 characters.");
        if (request.ProjectId == Guid.Empty) ModelState.AddModelError("projectId", "ProjectId cannot be empty when supplied.");
        if (request.LocationId == Guid.Empty) ModelState.AddModelError("locationId", "LocationId is required.");
        if (request.UnitTypeId == Guid.Empty) ModelState.AddModelError("unitTypeId", "UnitTypeId is required.");
        if (request.Bedrooms < 0 || request.Bathrooms < 0 || request.BuiltUpArea <= 0m || request.LandArea is <= 0m || request.TotalFloors is <= 0 || (request.FloorNumber is > 0 && request.TotalFloors is not null && request.FloorNumber > request.TotalFloors) || !Decimal18_2(request.BuiltUpArea) || !Decimal18_2(request.LandArea)) ModelState.AddModelError("unit", "Unit numeric fields are invalid.");
        var finishing = EnumValue<FinishingType>(request.FinishingType, "finishingType", false);
        var furnished = EnumValue<FurnishedStatus>(request.FurnishedStatus, "furnishedStatus", true);
        return new(request.ProjectId, request.LocationId, request.UnitTypeId, code ?? string.Empty, finishing, request.Bedrooms, request.Bathrooms, request.FloorNumber, request.TotalFloors, request.BuiltUpArea, request.LandArea, furnished ?? default, rowVersionValue is null ? null : RowVersion(rowVersionValue));
    }

    private T? EnumValue<T>(string? value, string key, bool required) where T : struct, Enum
    {
        if (value is null) { if (required) ModelState.AddModelError(key, $"{key} is required."); return null; }
        if (!string.IsNullOrWhiteSpace(value) && Enum.TryParse<T>(value, true, out var candidate) && Enum.IsDefined(candidate) && string.Equals(value, candidate.ToString(), StringComparison.OrdinalIgnoreCase)) return candidate;
        ModelState.AddModelError(key, $"{key} is invalid."); return null;
    }

    private byte[]? RowVersion(string? value)
    {
        try { var bytes = string.IsNullOrWhiteSpace(value) ? null : Convert.FromBase64String(value); if (bytes is { Length: 8 }) return bytes; }
        catch (FormatException) { }
        ModelState.AddModelError("rowVersion", "RowVersion must be Base64 encoded eight bytes."); return null;
    }

    private static bool Decimal18_2(decimal? value) => value is null || (value >= -MaxDecimal18_2 && value <= MaxDecimal18_2 && ((decimal.GetBits(value.Value)[3] >> 16) & 0x7f) <= 2);
    private static bool Decimal9_6(decimal? value) => value is null || (value >= -999.999999m && value <= 999.999999m && ((decimal.GetBits(value.Value)[3] >> 16) & 0x7f) <= 6);
    private void OptionalGuid(Guid? value, string key) { if (value == Guid.Empty) ModelState.AddModelError(key, $"{key} cannot be empty when supplied."); }
    private bool UserId(out Guid id) => Guid.TryParse(User.FindFirst("sub")?.Value, out id) && id != Guid.Empty;
    private ActionResult InvalidRoute(string key) { ModelState.AddModelError(key, $"{key} is required."); return ValidationProblem(ModelState); }
    private ObjectResult NotFoundResult() => Problem(statusCode: 404, title: "Unit not found.");
    private ObjectResult ConflictResult() => Problem(statusCode: 409, title: "Unit operation conflict.");
    private ObjectResult UnavailableResult() => Problem(statusCode: 503, title: "Unit operation is temporarily unavailable.");
    private static string Text(UnitStatus value) => value switch { UnitStatus.Available => "Available", UnitStatus.Reserved => "Reserved", UnitStatus.Sold => "Sold", UnitStatus.Rented => "Rented", UnitStatus.Withdrawn => "Withdrawn", _ => throw new ArgumentOutOfRangeException(nameof(value), value, null) };
}
