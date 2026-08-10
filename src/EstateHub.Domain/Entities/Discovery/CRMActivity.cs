using EstateHub.Domain.Entities.Companies;

namespace EstateHub.Domain.Entities.Discovery;

public class CRMActivity
{
    public Guid Id { get; set; }
    public Guid LeadId { get; set; }
    public Guid? PerformedByEmployeeId { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
    public string? Notes { get; set; }
    public string? MetadataJson { get; set; }

    public Lead Lead { get; set; } = null!;
    public CompanyEmployee? PerformedByEmployee { get; set; }
}
