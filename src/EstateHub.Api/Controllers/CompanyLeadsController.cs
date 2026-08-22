using System.Net.Mail;
using System.Text.Json;
using EstateHub.Api.Authorization.CompanyPermissions;
using EstateHub.Api.Contracts.CompanyLeads;
using EstateHub.Application.CompanyAccess;
using EstateHub.Application.CompanyLeads;
using EstateHub.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EstateHub.Api.Controllers;

[ApiController]
[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/company/leads")]
public sealed class CompanyLeadsController(ICompanyLeadManagementService service) : ControllerBase
{
    [HttpGet]
    [RequireCompanyPermission(CompanyPermissionCodes.LeadsRead)]
    public async Task<ActionResult<CompanyLeadsResponse>> GetLeads([FromQuery] CompanyLeadDirectoryRequest request, CancellationToken token)
    {
        if (!UserId(out var userId)) return Unauthorized();
        Page(request.PageNumber, request.PageSize); var search = Text(request.Search, 100, "search"); var source = Text(request.Source, 100, "source");
        var stage = EnumText<LeadStage>(request.Stage, "stage"); var priority = EnumText<LeadPriority>(request.Priority, "priority");
        if (request.OwnerEmployeeId == Guid.Empty) ModelState.AddModelError("ownerEmployeeId", "OwnerEmployeeId cannot be empty.");
        if (request.OwnerEmployeeId is not null && request.UnassignedOnly == true) ModelState.AddModelError("unassignedOnly", "OwnerEmployeeId and UnassignedOnly cannot be combined.");
        if (request.CreatedFrom is not null && request.CreatedTo is not null && request.CreatedFrom >= request.CreatedTo) ModelState.AddModelError("createdTo", "CreatedTo must be later than CreatedFrom.");
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = await service.GetLeadsAsync(userId, new(request.PageNumber, request.PageSize, search, stage, priority, request.OwnerEmployeeId, request.UnassignedOnly, source, request.CreatedFrom, request.CreatedTo), token);
        return Query(result, CompanyLeadsResponse.From);
    }

    [HttpGet("{leadId}")]
    [RequireCompanyPermission(CompanyPermissionCodes.LeadsRead)]
    public async Task<ActionResult<CompanyLeadDetailsResponse>> GetLead(Guid leadId, CancellationToken token)
    {
        if (!UserId(out var userId)) return Unauthorized(); if (!RouteId(leadId, "leadId")) return ValidationProblem(ModelState);
        var result = await service.GetLeadAsync(userId, leadId, token); return Query(result, CompanyLeadDetailsResponse.From);
    }

    [HttpPost]
    [RequireCompanyPermission(CompanyPermissionCodes.LeadsManage)]
    public async Task<ActionResult<CompanyLeadDetailsResponse>> CreateLead(CreateCompanyLeadRequest request, CancellationToken token)
    {
        if (!UserId(out var userId)) return Unauthorized(); var contact = Contact(request.ContactName, request.ContactPhone, request.ContactEmail, false); var priority = RequiredEnum<LeadPriority>(request.Priority, "priority");
        if (request.OwnerEmployeeId == Guid.Empty) ModelState.AddModelError("ownerEmployeeId", "OwnerEmployeeId cannot be empty."); if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = await service.CreateLeadAsync(userId, new(contact.Name, contact.Phone, contact.Email, priority!.Value, request.OwnerEmployeeId), token);
        return result.Status switch { CompanyLeadOperationStatus.Succeeded => CreatedAtAction(nameof(GetLead), new { leadId = result.Lead!.Header.Id }, CompanyLeadDetailsResponse.From(result.Lead)), CompanyLeadOperationStatus.NotFound => NotFoundProblem(), CompanyLeadOperationStatus.Conflict => ConflictProblem(), _ => UnavailableProblem() };
    }

    [HttpPut("{leadId}")]
    [RequireCompanyPermission(CompanyPermissionCodes.LeadsManage)]
    public async Task<ActionResult<CompanyLeadDetailsResponse>> UpdateLead(Guid leadId, UpdateCompanyLeadRequest request, CancellationToken token)
    {
        if (!UserId(out var userId)) return Unauthorized(); RouteId(leadId, "leadId"); var contact = Contact(request.ContactName, request.ContactPhone, request.ContactEmail, true); var priority = RequiredEnum<LeadPriority>(request.Priority, "priority"); var version = Version(request.RowVersion, "rowVersion");
        if (request.OwnerEmployeeId == Guid.Empty) ModelState.AddModelError("ownerEmployeeId", "OwnerEmployeeId cannot be empty."); if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = await service.UpdateLeadAsync(userId, leadId, new(contact.Name, contact.Phone, contact.Email, priority!.Value, request.OwnerEmployeeId, version!), token); return Mutation(result);
    }

    [HttpPut("{leadId}/stage")]
    [RequireCompanyPermission(CompanyPermissionCodes.LeadsManage)]
    public async Task<IActionResult> ChangeStage(Guid leadId, ChangeCompanyLeadStageRequest request, CancellationToken token)
    {
        if (!UserId(out var userId)) return Unauthorized(); RouteId(leadId, "leadId"); var stage = RequiredEnum<LeadStage>(request.Stage, "stage"); var version = Version(request.RowVersion, "rowVersion"); var reason = Text(request.Reason, 4000, "reason"); if (!ModelState.IsValid) return ValidationProblem(ModelState);
        return Status(await service.ChangeStageAsync(userId, leadId, new(stage!.Value, version!, reason), token));
    }

    [HttpPost("{leadId}/requirements")]
    [RequireCompanyPermission(CompanyPermissionCodes.LeadsManage)]
    public async Task<ActionResult<CompanyLeadRequirementMutationResponse>> CreateRequirement(Guid leadId, CompanyLeadRequirementRequest request, CancellationToken token)
    {
        if (!UserId(out var userId)) return Unauthorized(); RouteId(leadId, "leadId"); var command = Requirement(request); if (!ModelState.IsValid) return ValidationProblem(ModelState); var result = await service.CreateRequirementAsync(userId, leadId, command, token); return RequirementResult(result, true, leadId);
    }

    [HttpPut("{leadId}/requirements/{requirementId}")]
    [RequireCompanyPermission(CompanyPermissionCodes.LeadsManage)]
    public async Task<ActionResult<CompanyLeadRequirementMutationResponse>> UpdateRequirement(Guid leadId, Guid requirementId, CompanyLeadRequirementRequest request, CancellationToken token)
    {
        if (!UserId(out var userId)) return Unauthorized(); RouteId(leadId, "leadId"); RouteId(requirementId, "requirementId"); var command = Requirement(request); if (!ModelState.IsValid) return ValidationProblem(ModelState); var result = await service.UpdateRequirementAsync(userId, leadId, requirementId, command, token); return RequirementResult(result, false, leadId);
    }

    [HttpPost("{leadId}/requirements/{requirementId}/confirm")]
    [RequireCompanyPermission(CompanyPermissionCodes.LeadsManage)]
    public async Task<IActionResult> ConfirmRequirement(Guid leadId, Guid requirementId, ConfirmCompanyLeadRequirementRequest request, CancellationToken token)
    {
        if (!UserId(out var userId)) return Unauthorized(); RouteId(leadId, "leadId"); RouteId(requirementId, "requirementId"); var version = Version(request.LeadRowVersion, "leadRowVersion"); if (!ModelState.IsValid) return ValidationProblem(ModelState); return Status(await service.ConfirmRequirementAsync(userId, leadId, requirementId, version!, token));
    }

    [HttpDelete("{leadId}/requirements/{requirementId}")]
    [RequireCompanyPermission(CompanyPermissionCodes.LeadsManage)]
    public async Task<IActionResult> DeleteRequirement(Guid leadId, Guid requirementId, [FromQuery] string? leadRowVersion, CancellationToken token)
    {
        if (!UserId(out var userId)) return Unauthorized(); RouteId(leadId, "leadId"); RouteId(requirementId, "requirementId"); var version = Version(leadRowVersion, "leadRowVersion"); if (!ModelState.IsValid) return ValidationProblem(ModelState); return Status(await service.DeleteRequirementAsync(userId, leadId, requirementId, version!, token));
    }

    [HttpPut("{leadId}/interests")]
    [RequireCompanyPermission(CompanyPermissionCodes.LeadsManage)]
    public async Task<ActionResult<CompanyLeadInterestsMutationResponse>> ReplaceInterests(Guid leadId, ReplaceCompanyLeadInterestsRequest request, CancellationToken token)
    {
        if (!UserId(out var userId)) return Unauthorized(); RouteId(leadId, "leadId"); var version = Version(request.LeadRowVersion, "leadRowVersion"); var items = new List<CompanyLeadInterestCommand>();
        if (request.Items is null) ModelState.AddModelError("items", "Items is required."); else if (request.Items.Count > 100) ModelState.AddModelError("items", "Items must not exceed 100 entries."); else { var keys = new HashSet<string>(StringComparer.Ordinal); foreach (var item in request.Items) { if (item.ListingId == Guid.Empty) ModelState.AddModelError("items", "ListingId is required."); var type = Text(item.InterestType, 100, "items.interestType", true) ?? string.Empty; if (!keys.Add($"{item.ListingId:D}\0{type}")) ModelState.AddModelError("items", "Duplicate listing and interest type pairs are not allowed."); items.Add(new(item.ListingId, type)); } }
        if (!ModelState.IsValid) return ValidationProblem(ModelState); var result = await service.ReplaceInterestsAsync(userId, leadId, new(version!, items), token);
        return result.Status switch { CompanyLeadOperationStatus.Succeeded => Ok(new CompanyLeadInterestsMutationResponse(result.Interests!.Select(CompanyLeadInterestResponse.From).ToList(), Convert.ToBase64String(result.LeadRowVersion!))), CompanyLeadOperationStatus.NotFound => NotFoundProblem(), CompanyLeadOperationStatus.Conflict => ConflictProblem(), _ => UnavailableProblem() };
    }

    [HttpGet("{leadId}/activities")]
    [RequireCompanyPermission(CompanyPermissionCodes.LeadsRead)]
    public async Task<ActionResult<CompanyLeadActivitiesResponse>> GetActivities(Guid leadId, [FromQuery] CompanyLeadActivityDirectoryRequest request, CancellationToken token)
    {
        if (!UserId(out var userId)) return Unauthorized(); RouteId(leadId, "leadId"); Page(request.PageNumber, request.PageSize); var type = Text(request.ActivityType, 100, "activityType"); if (request.From is not null && request.To is not null && request.From >= request.To) ModelState.AddModelError("to", "To must be later than From."); if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = await service.GetActivitiesAsync(userId, leadId, new(request.PageNumber, request.PageSize, type, request.From, request.To), token); return Query(result, CompanyLeadActivitiesResponse.From);
    }

    [HttpPost("{leadId}/activities")]
    [RequireCompanyPermission(CompanyPermissionCodes.LeadsManage)]
    public async Task<ActionResult<CompanyLeadActivityMutationResponse>> CreateActivity(Guid leadId, CreateCompanyLeadActivityRequest request, CancellationToken token)
    {
        if (!UserId(out var userId)) return Unauthorized(); RouteId(leadId, "leadId"); var type = Text(request.ActivityType, 100, "activityType", true) ?? string.Empty; var notes = Text(request.Notes, 4000, "notes"); var version = Version(request.LeadRowVersion, "leadRowVersion"); string? metadata = null;
        if (request.Metadata is { } m && m.ValueKind is not JsonValueKind.Null) { if (m.ValueKind != JsonValueKind.Object) ModelState.AddModelError("metadata", "Metadata must be a JSON object."); else { metadata = JsonSerializer.Serialize(m); if (metadata.Length > 16000) ModelState.AddModelError("metadata", "Canonical metadata must not exceed 16000 characters."); } }
        if (!ModelState.IsValid) return ValidationProblem(ModelState); var result = await service.CreateActivityAsync(userId, leadId, new(type, request.OccurredAt, notes, metadata, version!), token);
        if (result.Status == CompanyLeadOperationStatus.InvalidRequest) { ModelState.AddModelError("occurredAt", "OccurredAt must not be later than the current UTC time."); return ValidationProblem(ModelState); }
        return result.Status switch { CompanyLeadOperationStatus.Succeeded => CreatedAtAction(nameof(GetActivities), new { leadId }, new CompanyLeadActivityMutationResponse(CompanyLeadActivityResponse.From(result.Activity!), Convert.ToBase64String(result.LeadRowVersion!))), CompanyLeadOperationStatus.NotFound => NotFoundProblem(), CompanyLeadOperationStatus.Conflict => ConflictProblem(), _ => UnavailableProblem() };
    }

    private CompanyLeadRequirementCommand Requirement(CompanyLeadRequirementRequest r)
    {
        var intent = RequiredEnum<CustomerIntent>(r.Intent, "intent"); var original = Text(r.OriginalQuery, 4000, "originalQuery"); var notes = Text(r.Notes, 4000, "notes"); var version = Version(r.LeadRowVersion, "leadRowVersion");
        string json = string.Empty; if (r.StructuredCriteria.ValueKind != JsonValueKind.Object) ModelState.AddModelError("structuredCriteria", "StructuredCriteria must be a JSON object."); else { json = JsonSerializer.Serialize(r.StructuredCriteria); if (json.Length > 16000) ModelState.AddModelError("structuredCriteria", "Canonical criteria must not exceed 16000 characters."); }
        if (r.CriteriaSchemaVersion <= 0) ModelState.AddModelError("criteriaSchemaVersion", "CriteriaSchemaVersion must be positive."); if (!Decimal18_2(r.MinBudget) || !Decimal18_2(r.MaxBudget) || r.MinBudget < 0 || r.MaxBudget < 0 || (r.MinBudget is not null && r.MaxBudget is not null && r.MinBudget > r.MaxBudget)) ModelState.AddModelError("budget", "Budget values must be non-negative decimal(18,2) values in a valid range.");
        var currency = string.IsNullOrWhiteSpace(r.CurrencyCode) ? null : r.CurrencyCode.Trim().ToUpperInvariant(); if (r.MinBudget is not null || r.MaxBudget is not null) { if (currency is null) ModelState.AddModelError("currencyCode", "CurrencyCode is required with a budget."); } if (currency is not null && (currency.Length != 3 || currency.Any(c => c is not (>= 'A' and <= 'Z')))) ModelState.AddModelError("currencyCode", "CurrencyCode must contain exactly three ASCII letters.");
        if (r.MinBedrooms < 0 || r.MinBathrooms < 0) ModelState.AddModelError("bedrooms", "Bedroom and bathroom minimums must be non-negative."); if (!Decimal18_2(r.MinArea) || !Decimal18_2(r.MaxArea) || r.MinArea <= 0 || r.MaxArea <= 0 || (r.MinArea is not null && r.MaxArea is not null && r.MinArea > r.MaxArea)) ModelState.AddModelError("area", "Area values must be positive decimal(18,2) values in a valid range."); if (r.PaymentPlanMonths <= 0) ModelState.AddModelError("paymentPlanMonths", "PaymentPlanMonths must be positive.");
        return new(intent ?? default, original, json, r.CriteriaSchemaVersion, r.MinBudget, r.MaxBudget, currency, r.MinBedrooms, r.MinBathrooms, r.MinArea, r.MaxArea, r.PaymentPlanMonths, notes, r.ExtractedByAI, version ?? []);
    }

    private (string? Name, string? Phone, string? Email) Contact(string? nameValue, string? phoneValue, string? emailValue, bool customerMaySupplyContact)
    {
        var name = Text(nameValue, 200, "contactName"); var phone = Text(phoneValue, 32, "contactPhone"); var email = Text(emailValue, 256, "contactEmail"); if (email is not null && (!MailAddress.TryCreate(email, out var address) || !string.Equals(address.Address, email, StringComparison.OrdinalIgnoreCase))) ModelState.AddModelError("contactEmail", "ContactEmail is invalid."); if (!customerMaySupplyContact && phone is null && email is null) ModelState.AddModelError("contact", "ContactPhone or ContactEmail is required."); return (name, phone, email);
    }
    private T? EnumText<T>(string? value, string key) where T : struct, Enum { if (value is null) return null; return RequiredEnum<T>(value, key); }
    private T? RequiredEnum<T>(string? value, string key) where T : struct, Enum { var x = value?.Trim(); if (!string.IsNullOrEmpty(x) && Enum.TryParse<T>(x, true, out var candidate) && Enum.IsDefined(candidate) && string.Equals(x, candidate.ToString(), StringComparison.OrdinalIgnoreCase)) return candidate; ModelState.AddModelError(key, $"{key} is invalid."); return null; }
    private string? Text(string? value, int max, string key, bool required = false) { var x = string.IsNullOrWhiteSpace(value) ? null : value.Trim(); if (required && x is null) ModelState.AddModelError(key, $"{key} is required."); if (x?.Length > max) ModelState.AddModelError(key, $"{key} must not exceed {max} characters."); return x; }
    private byte[]? Version(string? value, string key) { if (string.IsNullOrEmpty(value) || value.Any(char.IsWhiteSpace)) { ModelState.AddModelError(key, $"{key} must be Base64 encoded eight bytes."); return null; } try { var b = Convert.FromBase64String(value); if (b.Length == 8) return b; } catch (FormatException) { } ModelState.AddModelError(key, $"{key} must be Base64 encoded eight bytes."); return null; }
    private void Page(int number, int size) { if (number is < 1 or > 10000) ModelState.AddModelError("pageNumber", "PageNumber must be between 1 and 10000."); if (size is < 1 or > 50) ModelState.AddModelError("pageSize", "PageSize must be between 1 and 50."); }
    private static bool Decimal18_2(decimal? value) => value is null || (decimal.Round(value.Value, 2) == value.Value && decimal.Abs(value.Value) <= 9999999999999999.99m);
    private bool RouteId(Guid id, string key) { if (id != Guid.Empty) return true; ModelState.AddModelError(key, $"{key} is required."); return false; }
    private bool UserId(out Guid id) => Guid.TryParse(User.FindFirst("sub")?.Value, out id) && id != Guid.Empty;
    private ActionResult<TResponse> Query<TValue, TResponse>(CompanyLeadQueryResult<TValue> x, Func<TValue, TResponse> map) => x.Status switch { CompanyLeadOperationStatus.Succeeded => Ok(map(x.Value!)), CompanyLeadOperationStatus.NotFound => NotFoundProblem(), _ => UnavailableProblem() };
    private ActionResult<CompanyLeadDetailsResponse> Mutation(CompanyLeadMutationResult x)
    {
        if (x.Status == CompanyLeadOperationStatus.InvalidRequest) { ModelState.AddModelError("contact", "The Lead must retain a customer profile, contact phone, or contact email."); return ValidationProblem(ModelState); }
        return x.Status switch { CompanyLeadOperationStatus.Succeeded => Ok(CompanyLeadDetailsResponse.From(x.Lead!)), CompanyLeadOperationStatus.NotFound => NotFoundProblem(), CompanyLeadOperationStatus.Conflict => ConflictProblem(), _ => UnavailableProblem() };
    }
    private ActionResult<CompanyLeadRequirementMutationResponse> RequirementResult(CompanyLeadRequirementResult x, bool created, Guid leadId) => x.Status switch { CompanyLeadOperationStatus.Succeeded when created => CreatedAtAction(nameof(GetLead), new { leadId }, new CompanyLeadRequirementMutationResponse(CompanyLeadRequirementResponse.From(x.Requirement!), Convert.ToBase64String(x.LeadRowVersion!))), CompanyLeadOperationStatus.Succeeded => Ok(new CompanyLeadRequirementMutationResponse(CompanyLeadRequirementResponse.From(x.Requirement!), Convert.ToBase64String(x.LeadRowVersion!))), CompanyLeadOperationStatus.NotFound => NotFoundProblem(), CompanyLeadOperationStatus.Conflict => ConflictProblem(), _ => UnavailableProblem() };
    private IActionResult Status(CompanyLeadOperationStatus x) => x switch { CompanyLeadOperationStatus.Succeeded => NoContent(), CompanyLeadOperationStatus.NotFound => NotFoundProblem(), CompanyLeadOperationStatus.Conflict => ConflictProblem(), CompanyLeadOperationStatus.InvalidRequest => InvalidProblem(), _ => UnavailableProblem() };
    private ObjectResult InvalidProblem() => Problem(400, "Lead request is invalid."); private ObjectResult NotFoundProblem() => Problem(404, "Lead not found."); private ObjectResult ConflictProblem() => Problem(409, "Lead operation conflict."); private ObjectResult UnavailableProblem() => Problem(503, "Lead operation is temporarily unavailable.");
    private ObjectResult Problem(int status, string title) => base.Problem(statusCode: status, title: title);
}
