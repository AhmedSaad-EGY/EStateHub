using System.Text.Json;
using EstateHub.Application.Common;
using EstateHub.Domain.Enums;

namespace EstateHub.Application.CompanyLeads;

public sealed record CompanyLeadCustomerSummary(Guid Id, string FullName, CustomerPersona Persona);
public sealed record CompanyLeadBookingSummary(Guid Id, string BookingCode, ViewingBookingStatus Status);
public sealed record CompanyLeadEmployeeSummary(Guid Id, string FullName, string? JobTitle);

public sealed record CompanyLeadSummary(
    Guid Id, LeadStage Stage, LeadPriority Priority, string? ContactName, string? ContactPhone,
    string? ContactEmail, string Source, DateTimeOffset CreatedAt, DateTimeOffset? LastActivityAt,
    DateTimeOffset? ClosedAt, byte[] RowVersion, int RequirementCount, int InterestCount,
    int ActivityCount, CompanyLeadCustomerSummary? Customer, CompanyLeadBookingSummary? SourceBooking,
    CompanyLeadEmployeeSummary? Owner);

public sealed record CompanyLeadRequirement(
    Guid Id, CustomerIntent Intent, string? OriginalQuery, JsonElement StructuredCriteria,
    int CriteriaSchemaVersion, decimal? MinBudget, decimal? MaxBudget, string? CurrencyCode,
    int? MinBedrooms, int? MinBathrooms, decimal? MinArea, decimal? MaxArea,
    int? PaymentPlanMonths, string? Notes, bool ExtractedByAI,
    CompanyLeadEmployeeSummary? ConfirmedByEmployee, DateTimeOffset? ConfirmedAt,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public sealed record CompanyLeadInterest(
    Guid Id, Guid ListingId, string ListingCode, string ListingSlug, string ListingTitle,
    string InterestType, DateTimeOffset CreatedAt);

public sealed record CompanyLeadDetails(
    CompanyLeadSummary Header, IReadOnlyList<CompanyLeadRequirement> Requirements,
    IReadOnlyList<CompanyLeadInterest> Interests);

public sealed record CompanyLeadActivity(
    Guid Id, string ActivityType, DateTimeOffset OccurredAt, string? Notes,
    JsonElement? Metadata, CompanyLeadEmployeeSummary? Performer);

public sealed record CompanyLeadDirectoryQuery(
    int PageNumber, int PageSize, string? Search, LeadStage? Stage, LeadPriority? Priority,
    Guid? OwnerEmployeeId, bool? UnassignedOnly, string? Source,
    DateTimeOffset? CreatedFrom, DateTimeOffset? CreatedTo);

public sealed record CompanyLeadActivityQuery(
    int PageNumber, int PageSize, string? ActivityType, DateTimeOffset? From, DateTimeOffset? To);

public sealed record CreateCompanyLeadCommand(
    string? ContactName, string? ContactPhone, string? ContactEmail,
    LeadPriority Priority, Guid? OwnerEmployeeId);

public sealed record UpdateCompanyLeadCommand(
    string? ContactName, string? ContactPhone, string? ContactEmail,
    LeadPriority Priority, Guid? OwnerEmployeeId, byte[] RowVersion);

public sealed record ChangeCompanyLeadStageCommand(LeadStage Stage, byte[] RowVersion, string? Reason);

public sealed record CompanyLeadRequirementCommand(
    CustomerIntent Intent, string? OriginalQuery, string StructuredCriteriaJson,
    int CriteriaSchemaVersion, decimal? MinBudget, decimal? MaxBudget, string? CurrencyCode,
    int? MinBedrooms, int? MinBathrooms, decimal? MinArea, decimal? MaxArea,
    int? PaymentPlanMonths, string? Notes, bool ExtractedByAI, byte[] LeadRowVersion);

public sealed record CompanyLeadInterestCommand(Guid ListingId, string InterestType);
public sealed record ReplaceCompanyLeadInterestsCommand(byte[] LeadRowVersion, IReadOnlyList<CompanyLeadInterestCommand> Items);
public sealed record CreateCompanyLeadActivityCommand(
    string ActivityType, DateTimeOffset? OccurredAt, string? Notes, string? MetadataJson,
    byte[] LeadRowVersion);

public enum CompanyLeadOperationStatus { Succeeded, NotFound, Conflict, InvalidRequest, ServiceUnavailable }
public sealed record CompanyLeadQueryResult<T>(CompanyLeadOperationStatus Status, T? Value = default);
public sealed record CompanyLeadMutationResult(CompanyLeadOperationStatus Status, CompanyLeadDetails? Lead = null);
public sealed record CompanyLeadRequirementResult(CompanyLeadOperationStatus Status, CompanyLeadRequirement? Requirement = null, byte[]? LeadRowVersion = null);
public sealed record CompanyLeadInterestsResult(CompanyLeadOperationStatus Status, IReadOnlyList<CompanyLeadInterest>? Interests = null, byte[]? LeadRowVersion = null);
public sealed record CompanyLeadActivityResult(CompanyLeadOperationStatus Status, CompanyLeadActivity? Activity = null, byte[]? LeadRowVersion = null);
