using System.Text.RegularExpressions;
using EstateHub.Api.Contracts.PlatformCompanyApplications;
using EstateHub.Application.PlatformAccess;
using EstateHub.Application.PlatformCompanyApplications;
using EstateHub.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EstateHub.Api.Controllers;

[ApiController]
[Authorize(Policy = PlatformRoleNames.PlatformAdmin)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/platform/company-applications")]
public sealed class PlatformCompanyApplicationsController(
    IPlatformCompanyApplicationService applicationService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PlatformCompanyApplicationDirectoryResponse>> GetApplications(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] string? companyType = null,
        [FromQuery] DateTimeOffset? submittedFrom = null,
        [FromQuery] DateTimeOffset? submittedTo = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetPlatformAdminId(out _))
        {
            return Unauthorized();
        }

        if (pageNumber is < 1 or > 10000)
        {
            ModelState.AddModelError("pageNumber", "PageNumber must be between 1 and 10000.");
        }

        if (pageSize is < 1 or > 50)
        {
            ModelState.AddModelError("pageSize", "PageSize must be between 1 and 50.");
        }

        var normalizedSearch = search?.Trim();
        if (normalizedSearch?.Length == 0)
        {
            normalizedSearch = null;
        }
        else if (normalizedSearch?.Length > 100)
        {
            ModelState.AddModelError("search", "Search must not exceed 100 characters.");
        }

        CompanyApplicationStatus? parsedStatus = null;
        if (status is not null)
        {
            if (TryParseApplicationStatus(status, out var value))
            {
                parsedStatus = value;
            }
            else
            {
                ModelState.AddModelError("status", "Status is invalid.");
            }
        }

        CompanyType? parsedCompanyType = null;
        if (companyType is not null)
        {
            if (TryParseCompanyType(companyType, out var value))
            {
                parsedCompanyType = value;
            }
            else
            {
                ModelState.AddModelError("companyType", "CompanyType must be Developer or BrokerAgency.");
            }
        }

        if (submittedFrom is not null && submittedFrom.Value.Offset != TimeSpan.Zero)
        {
            ModelState.AddModelError("submittedFrom", "SubmittedFrom must use UTC.");
        }

        if (submittedTo is not null && submittedTo.Value.Offset != TimeSpan.Zero)
        {
            ModelState.AddModelError("submittedTo", "SubmittedTo must use UTC.");
        }

        if (submittedFrom > submittedTo)
        {
            ModelState.AddModelError("submittedFrom", "SubmittedFrom must not be later than SubmittedTo.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await applicationService.GetApplicationsAsync(
            new PlatformCompanyApplicationDirectoryQuery(
                pageNumber,
                pageSize,
                normalizedSearch,
                parsedStatus,
                parsedCompanyType,
                submittedFrom,
                submittedTo),
            cancellationToken);
        return result.Status == PlatformCompanyApplicationOperationStatus.Succeeded
            ? Ok(PlatformCompanyApplicationDirectoryResponse.From(result.Value!))
            : ApplicationUnavailable();
    }

    [HttpGet("{applicationId}")]
    public async Task<ActionResult<PlatformCompanyApplicationDetailsResponse>> GetApplication(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        if (!TryGetPlatformAdminId(out _)) return Unauthorized();
        if (applicationId == Guid.Empty) return InvalidRoute("applicationId", "ApplicationId is required.");
        var result = await applicationService.GetApplicationAsync(applicationId, cancellationToken);
        return result.Status switch
        {
            PlatformCompanyApplicationOperationStatus.Succeeded => Ok(PlatformCompanyApplicationDetailsResponse.From(result.Value!)),
            PlatformCompanyApplicationOperationStatus.NotFound => ApplicationNotFound(),
            _ => ApplicationUnavailable()
        };
    }

    [HttpGet("{applicationId}/documents/{documentId}/content")]
    public async Task<IActionResult> GetDocumentContent(
        Guid applicationId,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        if (!TryGetPlatformAdminId(out _)) return Unauthorized();
        if (applicationId == Guid.Empty) ModelState.AddModelError("applicationId", "ApplicationId is required.");
        if (documentId == Guid.Empty) ModelState.AddModelError("documentId", "DocumentId is required.");
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = await applicationService.GetDocumentContentAsync(applicationId, documentId, cancellationToken);
        if (result.Status == PlatformCompanyApplicationOperationStatus.NotFound) return ApplicationNotFound();
        if (result.Status != PlatformCompanyApplicationOperationStatus.Succeeded || result.Value is null) return ApplicationUnavailable();
        Response.Headers.XContentTypeOptions = "nosniff";
        return File(result.Value.Content, result.Value.ContentType, result.Value.DownloadFileName, enableRangeProcessing: true);
    }

    [HttpPost("{applicationId}/start-review")]
    public async Task<IActionResult> StartReview(
        Guid applicationId,
        PlatformRowVersionRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetPlatformAdminId(out var adminId)) return Unauthorized();
        if (applicationId == Guid.Empty) ModelState.AddModelError("applicationId", "ApplicationId is required.");
        var rowVersion = ParseRowVersion(request.RowVersion, "rowVersion");
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = await applicationService.StartReviewAsync(adminId, applicationId, rowVersion!, cancellationToken);
        return LifecycleResponse(result);
    }

    [HttpPut("{applicationId}/documents/{documentId}/verification")]
    public async Task<ActionResult<PlatformDocumentDecisionResponse>> VerifyDocument(
        Guid applicationId,
        Guid documentId,
        PlatformDocumentVerificationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetPlatformAdminId(out var adminId)) return Unauthorized();
        if (applicationId == Guid.Empty) ModelState.AddModelError("applicationId", "ApplicationId is required.");
        if (documentId == Guid.Empty) ModelState.AddModelError("documentId", "DocumentId is required.");
        var rowVersion = ParseRowVersion(request.ApplicationRowVersion, "applicationRowVersion");
        var decision = ParseDocumentDecision(request.VerificationStatus);
        var reason = NormalizeOptional(request.RejectionReason, 1000, "rejectionReason", "RejectionReason");
        if (decision == DocumentVerificationStatus.Approved && reason is not null)
        {
            ModelState.AddModelError("rejectionReason", "RejectionReason must be omitted when approving a document.");
        }
        else if (decision == DocumentVerificationStatus.Rejected && reason is null)
        {
            ModelState.AddModelError("rejectionReason", "RejectionReason is required when rejecting a document.");
        }

        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = await applicationService.VerifyDocumentAsync(
            adminId,
            applicationId,
            documentId,
            new PlatformDocumentDecisionCommand(decision, reason, rowVersion!),
            cancellationToken);
        return result.Status switch
        {
            PlatformCompanyApplicationOperationStatus.Succeeded => Ok(PlatformDocumentDecisionResponse.From(result.Value!)),
            PlatformCompanyApplicationOperationStatus.NotFound => ApplicationNotFound(),
            PlatformCompanyApplicationOperationStatus.Conflict => ApplicationConflict(),
            _ => ApplicationUnavailable()
        };
    }

    [HttpPost("{applicationId}/request-changes")]
    public Task<IActionResult> RequestChanges(
        Guid applicationId,
        PlatformDecisionRequest request,
        CancellationToken cancellationToken) =>
        ExecuteReasonedDecision(
            applicationId,
            request,
            applicationService.RequestChangesAsync,
            cancellationToken);

    [HttpPost("{applicationId}/reject")]
    public Task<IActionResult> Reject(
        Guid applicationId,
        PlatformDecisionRequest request,
        CancellationToken cancellationToken) =>
        ExecuteReasonedDecision(
            applicationId,
            request,
            applicationService.RejectAsync,
            cancellationToken);

    [HttpPost("{applicationId}/approve")]
    public async Task<ActionResult<PlatformApprovedCompanyResponse>> Approve(
        Guid applicationId,
        PlatformCompanyApprovalRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetPlatformAdminId(out var adminId)) return Unauthorized();
        if (applicationId == Guid.Empty) ModelState.AddModelError("applicationId", "ApplicationId is required.");
        var command = ValidateApproval(request);
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = await applicationService.ApproveAsync(adminId, applicationId, command, cancellationToken);
        return result.Status switch
        {
            PlatformCompanyApplicationOperationStatus.Succeeded => Ok(PlatformApprovedCompanyResponse.From(result.Value!)),
            PlatformCompanyApplicationOperationStatus.NotFound => ApplicationNotFound(),
            PlatformCompanyApplicationOperationStatus.Conflict => ApplicationConflict(),
            _ => ApplicationUnavailable()
        };
    }

    private async Task<IActionResult> ExecuteReasonedDecision(
        Guid applicationId,
        PlatformDecisionRequest request,
        Func<Guid, Guid, byte[], string, CancellationToken, Task<PlatformCompanyApplicationOperationStatus>> operation,
        CancellationToken cancellationToken)
    {
        if (!TryGetPlatformAdminId(out var adminId)) return Unauthorized();
        if (applicationId == Guid.Empty) ModelState.AddModelError("applicationId", "ApplicationId is required.");
        var rowVersion = ParseRowVersion(request.RowVersion, "rowVersion");
        var reason = RequiredText(request.Reason, 1000, "reason", "Reason");
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        return LifecycleResponse(await operation(adminId, applicationId, rowVersion!, reason!, cancellationToken));
    }

    private PlatformCompanyApprovalCommand ValidateApproval(PlatformCompanyApprovalRequest request)
    {
        var rowVersion = ParseRowVersion(request.RowVersion, "rowVersion");
        var slug = request.Slug?.Trim();
        if (string.IsNullOrWhiteSpace(slug)
            || slug.Length > 200
            || slug != slug.ToLowerInvariant()
            || !Regex.IsMatch(slug, "^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant))
        {
            ModelState.AddModelError("slug", "Slug must be lowercase kebab-case and must not exceed 200 characters.");
        }

        var displayName = RequiredText(request.DisplayName, 200, "displayName", "DisplayName");
        if (request.LocationId == Guid.Empty) ModelState.AddModelError("locationId", "LocationId is required.");
        var addressLine1 = RequiredText(request.AddressLine1, 300, "addressLine1", "AddressLine1");
        var addressLine2 = NormalizeOptional(request.AddressLine2, 300, "addressLine2", "AddressLine2");
        var postalCode = NormalizeOptional(request.PostalCode, 20, "postalCode", "PostalCode");
        if ((request.Latitude is null) != (request.Longitude is null))
        {
            ModelState.AddModelError("coordinates", "Latitude and Longitude must both be supplied or both be omitted.");
        }

        ValidateCoordinate(request.Latitude, -90m, 90m, "latitude", "Latitude");
        ValidateCoordinate(request.Longitude, -180m, 180m, "longitude", "Longitude");
        var timeZoneId = RequiredText(request.TimeZoneId, 100, "timeZoneId", "TimeZoneId");
        var baseCurrencyCode = request.BaseCurrencyCode?.Trim().ToUpperInvariant();
        if (baseCurrencyCode is null
            || baseCurrencyCode.Length != 3
            || baseCurrencyCode.Any(character => character is < 'A' or > 'Z'))
        {
            ModelState.AddModelError("baseCurrencyCode", "BaseCurrencyCode must contain exactly three ASCII letters.");
        }

        return new PlatformCompanyApprovalCommand(
            rowVersion ?? [],
            slug ?? string.Empty,
            displayName ?? string.Empty,
            request.LocationId,
            addressLine1 ?? string.Empty,
            addressLine2,
            postalCode,
            request.Latitude,
            request.Longitude,
            timeZoneId ?? string.Empty,
            baseCurrencyCode ?? string.Empty);
    }

    private DocumentVerificationStatus ParseDocumentDecision(string? value)
    {
        var normalized = value?.Trim();
        if (string.Equals(normalized, "Approved", StringComparison.OrdinalIgnoreCase)) return DocumentVerificationStatus.Approved;
        if (string.Equals(normalized, "Rejected", StringComparison.OrdinalIgnoreCase)) return DocumentVerificationStatus.Rejected;
        ModelState.AddModelError("verificationStatus", "VerificationStatus must be Approved or Rejected.");
        return default;
    }

    private static bool TryParseApplicationStatus(string value, out CompanyApplicationStatus status)
    {
        var normalized = value.Trim();
        if (string.Equals(normalized, "Draft", StringComparison.OrdinalIgnoreCase)) { status = CompanyApplicationStatus.Draft; return true; }
        if (string.Equals(normalized, "Submitted", StringComparison.OrdinalIgnoreCase)) { status = CompanyApplicationStatus.Submitted; return true; }
        if (string.Equals(normalized, "UnderReview", StringComparison.OrdinalIgnoreCase)) { status = CompanyApplicationStatus.UnderReview; return true; }
        if (string.Equals(normalized, "NeedsChanges", StringComparison.OrdinalIgnoreCase)) { status = CompanyApplicationStatus.NeedsChanges; return true; }
        if (string.Equals(normalized, "Approved", StringComparison.OrdinalIgnoreCase)) { status = CompanyApplicationStatus.Approved; return true; }
        if (string.Equals(normalized, "Rejected", StringComparison.OrdinalIgnoreCase)) { status = CompanyApplicationStatus.Rejected; return true; }
        status = default;
        return false;
    }

    private static bool TryParseCompanyType(string value, out CompanyType companyType)
    {
        var normalized = value.Trim();
        if (string.Equals(normalized, "Developer", StringComparison.OrdinalIgnoreCase)) { companyType = CompanyType.Developer; return true; }
        if (string.Equals(normalized, "BrokerAgency", StringComparison.OrdinalIgnoreCase)) { companyType = CompanyType.BrokerAgency; return true; }
        companyType = default;
        return false;
    }

    private void ValidateCoordinate(decimal? value, decimal minimum, decimal maximum, string key, string name)
    {
        if (value is null) return;
        var scale = (decimal.GetBits(value.Value)[3] >> 16) & 0x7F;
        if (value < minimum || value > maximum || scale > 6)
        {
            ModelState.AddModelError(key, $"{name} must be within range and use no more than six decimal places.");
        }
    }

    private byte[]? ParseRowVersion(string? value, string key)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            try
            {
                var rowVersion = Convert.FromBase64String(value);
                if (rowVersion.Length == 8) return rowVersion;
            }
            catch (FormatException)
            {
            }
        }

        ModelState.AddModelError(key, "RowVersion must be valid Base64 representing exactly 8 bytes.");
        return null;
    }

    private string? RequiredText(string? value, int maximumLength, string key, string name)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized)) ModelState.AddModelError(key, $"{name} is required.");
        else if (normalized.Length > maximumLength) ModelState.AddModelError(key, $"{name} must not exceed {maximumLength} characters.");
        return normalized;
    }

    private string? NormalizeOptional(string? value, int maximumLength, string key, string name)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized)) return null;
        if (normalized.Length > maximumLength) ModelState.AddModelError(key, $"{name} must not exceed {maximumLength} characters.");
        return normalized;
    }

    private bool TryGetPlatformAdminId(out Guid applicationUserId) =>
        Guid.TryParse(User.FindFirst("sub")?.Value, out applicationUserId)
        && applicationUserId != Guid.Empty;

    private IActionResult LifecycleResponse(PlatformCompanyApplicationOperationStatus status) => status switch
    {
        PlatformCompanyApplicationOperationStatus.Succeeded => NoContent(),
        PlatformCompanyApplicationOperationStatus.NotFound => ApplicationNotFound(),
        PlatformCompanyApplicationOperationStatus.Conflict => ApplicationConflict(),
        _ => ApplicationUnavailable()
    };

    private ActionResult InvalidRoute(string key, string message)
    {
        ModelState.AddModelError(key, message);
        return ValidationProblem(ModelState);
    }

    private ObjectResult ApplicationNotFound() => Problem(statusCode: StatusCodes.Status404NotFound, title: "Company application not found.");
    private ObjectResult ApplicationConflict() => Problem(statusCode: StatusCodes.Status409Conflict, title: "Company application conflict.");
    private ObjectResult ApplicationUnavailable() => Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Company application service is temporarily unavailable.");
}
