using System.Text.Json;
using EstateHub.Application.Common;
using EstateHub.Application.CompanyLeads;
using EstateHub.Domain.Enums;

namespace EstateHub.Api.Contracts.CompanyLeads;

public sealed class CompanyLeadDirectoryRequest { public int PageNumber { get; init; } = 1; public int PageSize { get; init; } = 20; public string? Search { get; init; } public string? Stage { get; init; } public string? Priority { get; init; } public Guid? OwnerEmployeeId { get; init; } public bool? UnassignedOnly { get; init; } public string? Source { get; init; } public DateTimeOffset? CreatedFrom { get; init; } public DateTimeOffset? CreatedTo { get; init; } }
public sealed class CompanyLeadActivityDirectoryRequest { public int PageNumber { get; init; } = 1; public int PageSize { get; init; } = 20; public string? ActivityType { get; init; } public DateTimeOffset? From { get; init; } public DateTimeOffset? To { get; init; } }
public sealed record CreateCompanyLeadRequest(string? ContactName, string? ContactPhone, string? ContactEmail, string? Priority, Guid? OwnerEmployeeId);
public sealed record UpdateCompanyLeadRequest(string? ContactName, string? ContactPhone, string? ContactEmail, string? Priority, Guid? OwnerEmployeeId, string? RowVersion);
public sealed record ChangeCompanyLeadStageRequest(string? Stage, string? RowVersion, string? Reason);
public sealed record CompanyLeadRequirementRequest(string? Intent, string? OriginalQuery, JsonElement StructuredCriteria, int CriteriaSchemaVersion, decimal? MinBudget, decimal? MaxBudget, string? CurrencyCode, int? MinBedrooms, int? MinBathrooms, decimal? MinArea, decimal? MaxArea, int? PaymentPlanMonths, string? Notes, bool ExtractedByAI, string? LeadRowVersion);
public sealed record ConfirmCompanyLeadRequirementRequest(string? LeadRowVersion);
public sealed record CompanyLeadInterestRequest(Guid ListingId, string? InterestType);
public sealed record ReplaceCompanyLeadInterestsRequest(string? LeadRowVersion, IReadOnlyList<CompanyLeadInterestRequest>? Items);
public sealed record CreateCompanyLeadActivityRequest(string? ActivityType, DateTimeOffset? OccurredAt, string? Notes, JsonElement? Metadata, string? LeadRowVersion);

public sealed record CompanyLeadEmployeeResponse(Guid Id, string FullName, string? JobTitle);
public sealed record CompanyLeadCustomerResponse(Guid Id, string FullName, string Persona);
public sealed record CompanyLeadBookingResponse(Guid Id, string BookingCode, string Status);
public sealed record CompanyLeadResponse(Guid Id, string Stage, string Priority, string? ContactName, string? ContactPhone, string? ContactEmail, string Source, DateTimeOffset CreatedAt, DateTimeOffset? LastActivityAt, DateTimeOffset? ClosedAt, string RowVersion, int RequirementCount, int InterestCount, int ActivityCount, CompanyLeadCustomerResponse? Customer, CompanyLeadBookingResponse? SourceBooking, CompanyLeadEmployeeResponse? Owner)
{
    public static CompanyLeadResponse From(CompanyLeadSummary x) => new(x.Id, LeadText.Stage(x.Stage), LeadText.Priority(x.Priority), x.ContactName, x.ContactPhone, x.ContactEmail, x.Source, x.CreatedAt, x.LastActivityAt, x.ClosedAt, Convert.ToBase64String(x.RowVersion), x.RequirementCount, x.InterestCount, x.ActivityCount, x.Customer is null ? null : new(x.Customer.Id, x.Customer.FullName, LeadText.Persona(x.Customer.Persona)), x.SourceBooking is null ? null : new(x.SourceBooking.Id, x.SourceBooking.BookingCode, LeadText.Booking(x.SourceBooking.Status)), x.Owner is null ? null : Employee(x.Owner));
    internal static CompanyLeadEmployeeResponse Employee(CompanyLeadEmployeeSummary x) => new(x.Id, x.FullName, x.JobTitle);
}
public sealed record CompanyLeadsResponse(IReadOnlyList<CompanyLeadResponse> Items, int PageNumber, int PageSize, int TotalCount, int TotalPages, bool HasPreviousPage, bool HasNextPage)
{ public static CompanyLeadsResponse From(PagedResult<CompanyLeadSummary> x) => new(x.Items.Select(CompanyLeadResponse.From).ToList(), x.PageNumber, x.PageSize, x.TotalCount, x.TotalPages, x.HasPreviousPage, x.HasNextPage); }
public sealed record CompanyLeadRequirementResponse(Guid Id, string Intent, string? OriginalQuery, JsonElement StructuredCriteria, int CriteriaSchemaVersion, decimal? MinBudget, decimal? MaxBudget, string? CurrencyCode, int? MinBedrooms, int? MinBathrooms, decimal? MinArea, decimal? MaxArea, int? PaymentPlanMonths, string? Notes, bool ExtractedByAI, CompanyLeadEmployeeResponse? ConfirmedByEmployee, DateTimeOffset? ConfirmedAt, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt)
{ public static CompanyLeadRequirementResponse From(CompanyLeadRequirement x) => new(x.Id, LeadText.Intent(x.Intent), x.OriginalQuery, x.StructuredCriteria, x.CriteriaSchemaVersion, x.MinBudget, x.MaxBudget, x.CurrencyCode, x.MinBedrooms, x.MinBathrooms, x.MinArea, x.MaxArea, x.PaymentPlanMonths, x.Notes, x.ExtractedByAI, x.ConfirmedByEmployee is null ? null : CompanyLeadResponse.Employee(x.ConfirmedByEmployee), x.ConfirmedAt, x.CreatedAt, x.UpdatedAt); }
public sealed record CompanyLeadInterestResponse(Guid Id, Guid ListingId, string ListingCode, string ListingSlug, string ListingTitle, string InterestType, DateTimeOffset CreatedAt)
{ public static CompanyLeadInterestResponse From(CompanyLeadInterest x) => new(x.Id, x.ListingId, x.ListingCode, x.ListingSlug, x.ListingTitle, x.InterestType, x.CreatedAt); }
public sealed record CompanyLeadDetailsResponse(Guid Id, string Stage, string Priority, string? ContactName, string? ContactPhone, string? ContactEmail, string Source, DateTimeOffset CreatedAt, DateTimeOffset? LastActivityAt, DateTimeOffset? ClosedAt, string RowVersion, int RequirementCount, int InterestCount, int ActivityCount, CompanyLeadCustomerResponse? Customer, CompanyLeadBookingResponse? SourceBooking, CompanyLeadEmployeeResponse? Owner, IReadOnlyList<CompanyLeadRequirementResponse> Requirements, IReadOnlyList<CompanyLeadInterestResponse> Interests)
{
    public static CompanyLeadDetailsResponse From(CompanyLeadDetails x)
    {
        var h = CompanyLeadResponse.From(x.Header);
        return new(h.Id, h.Stage, h.Priority, h.ContactName, h.ContactPhone, h.ContactEmail, h.Source, h.CreatedAt, h.LastActivityAt, h.ClosedAt, h.RowVersion, h.RequirementCount, h.InterestCount, h.ActivityCount, h.Customer, h.SourceBooking, h.Owner, x.Requirements.Select(CompanyLeadRequirementResponse.From).ToList(), x.Interests.Select(CompanyLeadInterestResponse.From).ToList());
    }
}
public sealed record CompanyLeadRequirementMutationResponse(CompanyLeadRequirementResponse Requirement, string LeadRowVersion);
public sealed record CompanyLeadInterestsMutationResponse(IReadOnlyList<CompanyLeadInterestResponse> Interests, string LeadRowVersion);
public sealed record CompanyLeadActivityResponse(Guid Id, string ActivityType, DateTimeOffset OccurredAt, string? Notes, JsonElement? Metadata, CompanyLeadEmployeeResponse? Performer)
{ public static CompanyLeadActivityResponse From(CompanyLeadActivity x) => new(x.Id, x.ActivityType, x.OccurredAt, x.Notes, x.Metadata, x.Performer is null ? null : CompanyLeadResponse.Employee(x.Performer)); }
public sealed record CompanyLeadActivitiesResponse(IReadOnlyList<CompanyLeadActivityResponse> Items, int PageNumber, int PageSize, int TotalCount, int TotalPages, bool HasPreviousPage, bool HasNextPage)
{ public static CompanyLeadActivitiesResponse From(PagedResult<CompanyLeadActivity> x) => new(x.Items.Select(CompanyLeadActivityResponse.From).ToList(), x.PageNumber, x.PageSize, x.TotalCount, x.TotalPages, x.HasPreviousPage, x.HasNextPage); }
public sealed record CompanyLeadActivityMutationResponse(CompanyLeadActivityResponse Activity, string LeadRowVersion);

internal static class LeadText
{
    public static string Stage(LeadStage x) => x switch { LeadStage.New => "New", LeadStage.Contacted => "Contacted", LeadStage.Qualified => "Qualified", LeadStage.ViewingScheduled => "ViewingScheduled", LeadStage.Negotiation => "Negotiation", LeadStage.Won => "Won", LeadStage.Lost => "Lost", _ => throw new ArgumentOutOfRangeException(nameof(x), x, null) };
    public static string Priority(LeadPriority x) => x switch { LeadPriority.Low => "Low", LeadPriority.Medium => "Medium", LeadPriority.High => "High", _ => throw new ArgumentOutOfRangeException(nameof(x), x, null) };
    public static string Intent(CustomerIntent x) => x switch { CustomerIntent.Buy => "Buy", CustomerIntent.Rent => "Rent", CustomerIntent.Invest => "Invest", _ => throw new ArgumentOutOfRangeException(nameof(x), x, null) };
    public static string Persona(CustomerPersona x) => x switch { CustomerPersona.Buyer => "Buyer", CustomerPersona.Renter => "Renter", CustomerPersona.Agent => "Agent", _ => throw new ArgumentOutOfRangeException(nameof(x), x, null) };
    public static string Booking(ViewingBookingStatus x) => x switch { ViewingBookingStatus.Pending => "Pending", ViewingBookingStatus.Confirmed => "Confirmed", ViewingBookingStatus.CheckedIn => "CheckedIn", ViewingBookingStatus.Completed => "Completed", ViewingBookingStatus.Rejected => "Rejected", ViewingBookingStatus.Cancelled => "Cancelled", ViewingBookingStatus.NoShow => "NoShow", _ => throw new ArgumentOutOfRangeException(nameof(x), x, null) };
}
