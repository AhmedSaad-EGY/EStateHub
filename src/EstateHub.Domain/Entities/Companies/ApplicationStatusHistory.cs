using EstateHub.Domain.Enums;

namespace EstateHub.Domain.Entities.Companies;

public class ApplicationStatusHistory
{
    public Guid Id { get; set; }
    public Guid CompanyApplicationId { get; set; }
    public CompanyApplicationStatus? FromStatus { get; set; }
    public CompanyApplicationStatus ToStatus { get; set; }
    public Guid? ChangedByApplicationUserId { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
    public string? Reason { get; set; }

    public CompanyApplication CompanyApplication { get; set; } = null!;
}
