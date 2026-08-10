using EstateHub.Domain.Enums;

namespace EstateHub.Domain.Entities.Companies;

public class CompanyApplication
{
    public Guid Id { get; set; }
    public Guid SubmittedByApplicationUserId { get; set; }
    public Guid? ApprovedCompanyId { get; set; }
    public string LegalName { get; set; } = string.Empty;
    public string BusinessEmail { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public string? TaxId { get; set; }
    public string? Website { get; set; }
    public CompanyType CompanyType { get; set; }
    public string OfficeAddress { get; set; } = string.Empty;
    public string LocationText { get; set; } = string.Empty;
    public string? EstimatedPropertyRange { get; set; }
    public CompanyApplicationStatus Status { get; set; }
    public DateTimeOffset? SubmittedAt { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public Guid? ReviewedByApplicationUserId { get; set; }
    public string? DecisionReason { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public Company? ApprovedCompany { get; set; }
    public ICollection<CompanyDocument> Documents { get; set; } = [];
    public ICollection<ApplicationStatusHistory> StatusHistory { get; set; } = [];
}
