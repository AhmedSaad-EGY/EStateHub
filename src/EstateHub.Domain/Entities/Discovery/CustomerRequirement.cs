using EstateHub.Domain.Entities.Catalog;
using EstateHub.Domain.Entities.Companies;
using EstateHub.Domain.Enums;

namespace EstateHub.Domain.Entities.Discovery;

public class CustomerRequirement
{
    public Guid Id { get; set; }
    public Guid LeadId { get; set; }
    public CustomerIntent Intent { get; set; }
    public string? OriginalQuery { get; set; }
    public string StructuredCriteriaJson { get; set; } = string.Empty;
    public int CriteriaSchemaVersion { get; set; }
    public decimal? MinBudget { get; set; }
    public decimal? MaxBudget { get; set; }
    public string? CurrencyCode { get; set; }
    public int? MinBedrooms { get; set; }
    public int? MinBathrooms { get; set; }
    public decimal? MinArea { get; set; }
    public decimal? MaxArea { get; set; }
    public int? PaymentPlanMonths { get; set; }
    public string? Notes { get; set; }
    public bool ExtractedByAI { get; set; }
    public Guid? ConfirmedByEmployeeId { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Lead Lead { get; set; } = null!;
    public Currency? Currency { get; set; }
    public CompanyEmployee? ConfirmedByEmployee { get; set; }
}
