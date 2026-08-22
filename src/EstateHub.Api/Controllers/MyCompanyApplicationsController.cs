using System.Net.Mail;
using EstateHub.Api.Contracts.CompanyApplications;
using EstateHub.Application.CompanyApplications;
using EstateHub.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EstateHub.Api.Controllers;

[ApiController]
[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/me/company-applications")]
public sealed class MyCompanyApplicationsController(
    ICompanyApplicationService applicationService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CompanyApplicationDirectoryResponse>> GetApplications(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var applicationUserId))
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

        CompanyApplicationStatus? parsedStatus = null;
        if (status is not null)
        {
            if (TryParseStatus(status, out var value))
            {
                parsedStatus = value;
            }
            else
            {
                ModelState.AddModelError(
                    "status",
                    "Status must be Draft, Submitted, UnderReview, NeedsChanges, Approved, or Rejected.");
            }
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await applicationService.GetApplicationsAsync(
            applicationUserId,
            new CompanyApplicationDirectoryQuery(pageNumber, pageSize, parsedStatus),
            cancellationToken);
        return Ok(CompanyApplicationDirectoryResponse.From(result));
    }

    [HttpGet("{applicationId}")]
    public async Task<ActionResult<CompanyApplicationDetailsResponse>> GetApplication(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var applicationUserId))
        {
            return Unauthorized();
        }

        if (applicationId == Guid.Empty)
        {
            return InvalidRouteId();
        }

        var application = await applicationService.GetApplicationAsync(
            applicationUserId,
            applicationId,
            cancellationToken);
        return application is null
            ? ApplicationNotFound()
            : Ok(CompanyApplicationDetailsResponse.From(application));
    }

    [HttpPost]
    public async Task<ActionResult<CompanyApplicationDetailsResponse>> CreateApplication(
        CreateCompanyApplicationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var applicationUserId))
        {
            return Unauthorized();
        }

        var command = ValidateFields(request, null);
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await applicationService.CreateApplicationAsync(
            applicationUserId,
            command,
            cancellationToken);
        return result.Status switch
        {
            CompanyApplicationOperationStatus.Succeeded => CreatedAtAction(
                nameof(GetApplication),
                new { applicationId = result.Application!.Id },
                CompanyApplicationDetailsResponse.From(result.Application)),
            CompanyApplicationOperationStatus.Conflict => ApplicationConflict(),
            _ => ApplicationUnavailable()
        };
    }

    [HttpPut("{applicationId}")]
    public async Task<ActionResult<CompanyApplicationDetailsResponse>> UpdateApplication(
        Guid applicationId,
        UpdateCompanyApplicationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var applicationUserId))
        {
            return Unauthorized();
        }

        if (applicationId == Guid.Empty)
        {
            ModelState.AddModelError("applicationId", "ApplicationId is required.");
        }

        var rowVersion = ParseRowVersion(request.RowVersion);
        var command = ValidateFields(request, rowVersion);
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await applicationService.UpdateApplicationAsync(
            applicationUserId,
            applicationId,
            command,
            cancellationToken);
        return MutationResponse(result);
    }

    [HttpPut("{applicationId}/documents")]
    public async Task<ActionResult<CompanyApplicationDocumentsResponse>> ReplaceDocuments(
        Guid applicationId,
        ReplaceCompanyApplicationDocumentsRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var applicationUserId))
        {
            return Unauthorized();
        }

        if (applicationId == Guid.Empty)
        {
            ModelState.AddModelError("applicationId", "ApplicationId is required.");
        }

        var rowVersion = ParseRowVersion(request.RowVersion);
        var documents = ValidateDocuments(request.Documents);
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await applicationService.ReplaceDocumentsAsync(
            applicationUserId,
            applicationId,
            documents,
            rowVersion!,
            cancellationToken);
        return result.Status switch
        {
            CompanyApplicationOperationStatus.Succeeded => Ok(CompanyApplicationDocumentsResponse.From(result)),
            CompanyApplicationOperationStatus.NotFound => ApplicationNotFound(),
            CompanyApplicationOperationStatus.Conflict => ApplicationConflict(),
            _ => ApplicationUnavailable()
        };
    }

    [HttpPost("{applicationId}/submit")]
    public async Task<IActionResult> SubmitApplication(
        Guid applicationId,
        SubmitCompanyApplicationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var applicationUserId))
        {
            return Unauthorized();
        }

        if (applicationId == Guid.Empty)
        {
            ModelState.AddModelError("applicationId", "ApplicationId is required.");
        }

        var rowVersion = ParseRowVersion(request.RowVersion);
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var status = await applicationService.SubmitApplicationAsync(
            applicationUserId,
            applicationId,
            rowVersion!,
            cancellationToken);
        return status switch
        {
            CompanyApplicationOperationStatus.Succeeded => NoContent(),
            CompanyApplicationOperationStatus.NotFound => ApplicationNotFound(),
            CompanyApplicationOperationStatus.Conflict => ApplicationConflict(),
            _ => ApplicationUnavailable()
        };
    }

    private CompanyApplicationCommand ValidateFields(
        CompanyApplicationFieldsRequest request,
        byte[]? rowVersion)
    {
        var legalName = RequiredText(request.LegalName, 200, "legalName", "LegalName");
        var businessEmail = RequiredText(request.BusinessEmail, 256, "businessEmail", "BusinessEmail");
        var phoneNumber = RequiredText(request.PhoneNumber, 32, "phoneNumber", "PhoneNumber");
        var registrationNumber = RequiredText(request.RegistrationNumber, 100, "registrationNumber", "RegistrationNumber");
        var taxId = OptionalText(request.TaxId, 100, "taxId", "TaxId");
        var website = OptionalText(request.Website, 2048, "website", "Website");
        var officeAddress = RequiredText(request.OfficeAddress, 500, "officeAddress", "OfficeAddress");
        var locationText = RequiredText(request.LocationText, 250, "locationText", "LocationText");
        var estimatedPropertyRange = OptionalText(
            request.EstimatedPropertyRange,
            100,
            "estimatedPropertyRange",
            "EstimatedPropertyRange");

        if (businessEmail is not null
            && (!MailAddress.TryCreate(businessEmail, out var address)
                || !string.Equals(address.Address, businessEmail, StringComparison.OrdinalIgnoreCase)))
        {
            ModelState.AddModelError("businessEmail", "BusinessEmail is invalid.");
        }

        if (website is not null
            && (!Uri.TryCreate(website, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)))
        {
            ModelState.AddModelError("website", "Website must be an absolute HTTP or HTTPS URL.");
        }

        if (!TryParseCompanyType(request.CompanyType, out var companyType))
        {
            ModelState.AddModelError("companyType", "CompanyType must be Developer or BrokerAgency.");
        }

        return new CompanyApplicationCommand(
            legalName ?? string.Empty,
            businessEmail ?? string.Empty,
            phoneNumber ?? string.Empty,
            registrationNumber ?? string.Empty,
            taxId,
            website,
            companyType,
            officeAddress ?? string.Empty,
            locationText ?? string.Empty,
            estimatedPropertyRange,
            rowVersion);
    }

    private IReadOnlyList<CompanyApplicationDocumentCommand> ValidateDocuments(
        IReadOnlyList<CompanyApplicationDocumentRequest>? requests)
    {
        if (requests is null)
        {
            ModelState.AddModelError("documents", "Documents are required.");
            return [];
        }

        if (requests.Count > 20)
        {
            ModelState.AddModelError("documents", "No more than 20 documents are allowed.");
        }

        var documents = new List<CompanyApplicationDocumentCommand>(requests.Count);
        var fileAssetIds = new HashSet<Guid>();
        var pairs = new HashSet<(string DocumentType, Guid FileAssetId)>();
        for (var index = 0; index < requests.Count; index++)
        {
            var request = requests[index];
            var documentType = request.DocumentType?.Trim();
            if (string.IsNullOrWhiteSpace(documentType))
            {
                ModelState.AddModelError($"documents[{index}].documentType", "DocumentType is required.");
                documentType = string.Empty;
            }
            else if (documentType.Length > 100)
            {
                ModelState.AddModelError($"documents[{index}].documentType", "DocumentType must not exceed 100 characters.");
            }

            if (request.FileAssetId == Guid.Empty)
            {
                ModelState.AddModelError($"documents[{index}].fileAssetId", "FileAssetId is required.");
            }
            else if (!fileAssetIds.Add(request.FileAssetId))
            {
                ModelState.AddModelError("documents", "Duplicate FileAssetIds are not allowed.");
            }

            if (!pairs.Add((documentType, request.FileAssetId)))
            {
                ModelState.AddModelError("documents", "Duplicate document entries are not allowed.");
            }

            documents.Add(new CompanyApplicationDocumentCommand(request.FileAssetId, documentType));
        }

        return documents;
    }

    private string? RequiredText(
        string? value,
        int maximumLength,
        string key,
        string displayName)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            ModelState.AddModelError(key, $"{displayName} is required.");
        }
        else if (normalized.Length > maximumLength)
        {
            ModelState.AddModelError(key, $"{displayName} must not exceed {maximumLength} characters.");
        }

        return normalized;
    }

    private string? OptionalText(
        string? value,
        int maximumLength,
        string key,
        string displayName)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        if (normalized.Length > maximumLength)
        {
            ModelState.AddModelError(key, $"{displayName} must not exceed {maximumLength} characters.");
        }

        return normalized;
    }

    private byte[]? ParseRowVersion(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            ModelState.AddModelError("rowVersion", "RowVersion is required.");
            return null;
        }

        try
        {
            var rowVersion = Convert.FromBase64String(value);
            if (rowVersion.Length == 8)
            {
                return rowVersion;
            }
        }
        catch (FormatException)
        {
        }

        ModelState.AddModelError("rowVersion", "RowVersion must be valid Base64 representing exactly 8 bytes.");
        return null;
    }

    private static bool TryParseStatus(
        string value,
        out CompanyApplicationStatus status)
    {
        var normalized = value.Trim();
        if (string.Equals(normalized, "Draft", StringComparison.OrdinalIgnoreCase))
        {
            status = CompanyApplicationStatus.Draft;
            return true;
        }

        if (string.Equals(normalized, "Submitted", StringComparison.OrdinalIgnoreCase))
        {
            status = CompanyApplicationStatus.Submitted;
            return true;
        }

        if (string.Equals(normalized, "UnderReview", StringComparison.OrdinalIgnoreCase))
        {
            status = CompanyApplicationStatus.UnderReview;
            return true;
        }

        if (string.Equals(normalized, "NeedsChanges", StringComparison.OrdinalIgnoreCase))
        {
            status = CompanyApplicationStatus.NeedsChanges;
            return true;
        }

        if (string.Equals(normalized, "Approved", StringComparison.OrdinalIgnoreCase))
        {
            status = CompanyApplicationStatus.Approved;
            return true;
        }

        if (string.Equals(normalized, "Rejected", StringComparison.OrdinalIgnoreCase))
        {
            status = CompanyApplicationStatus.Rejected;
            return true;
        }

        status = default;
        return false;
    }

    private static bool TryParseCompanyType(string? value, out CompanyType companyType)
    {
        var normalized = value?.Trim();
        if (string.Equals(normalized, "Developer", StringComparison.OrdinalIgnoreCase))
        {
            companyType = CompanyType.Developer;
            return true;
        }

        if (string.Equals(normalized, "BrokerAgency", StringComparison.OrdinalIgnoreCase))
        {
            companyType = CompanyType.BrokerAgency;
            return true;
        }

        companyType = default;
        return false;
    }

    private ActionResult<CompanyApplicationDetailsResponse> MutationResponse(
        CompanyApplicationMutationResult result) => result.Status switch
        {
            CompanyApplicationOperationStatus.Succeeded => Ok(
                CompanyApplicationDetailsResponse.From(result.Application!)),
            CompanyApplicationOperationStatus.NotFound => ApplicationNotFound(),
            CompanyApplicationOperationStatus.Conflict => ApplicationConflict(),
            _ => ApplicationUnavailable()
        };

    private bool TryGetUserId(out Guid applicationUserId) =>
        Guid.TryParse(User.FindFirst("sub")?.Value, out applicationUserId)
        && applicationUserId != Guid.Empty;

    private ActionResult InvalidRouteId()
    {
        ModelState.AddModelError("applicationId", "ApplicationId is required.");
        return ValidationProblem(ModelState);
    }

    private ObjectResult ApplicationNotFound() => Problem(
        statusCode: StatusCodes.Status404NotFound,
        title: "Company application not found.");

    private ObjectResult ApplicationConflict() => Problem(
        statusCode: StatusCodes.Status409Conflict,
        title: "Company application conflict.");

    private ObjectResult ApplicationUnavailable() => Problem(
        statusCode: StatusCodes.Status503ServiceUnavailable,
        title: "Company application service is temporarily unavailable.");
}
