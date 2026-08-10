namespace EstateHub.Domain.Entities.Companies;

public class CompanyRole
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public bool IsBuiltIn { get; set; }
    public bool IsActive { get; set; }

    public Company Company { get; set; } = null!;
    public ICollection<CompanyEmployeeRole> EmployeeAssignments { get; set; } = [];
    public ICollection<CompanyRolePermission> PermissionAssignments { get; set; } = [];
}
