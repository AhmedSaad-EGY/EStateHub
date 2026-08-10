namespace EstateHub.Domain.Entities.Companies;

public class CompanyEmployeeRole
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid CompanyEmployeeId { get; set; }
    public Guid CompanyRoleId { get; set; }
    public DateTimeOffset AssignedAt { get; set; }
    public Guid AssignedByApplicationUserId { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }

    public Company Company { get; set; } = null!;
    public CompanyEmployee CompanyEmployee { get; set; } = null!;
    public CompanyRole CompanyRole { get; set; } = null!;
}
