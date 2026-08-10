namespace EstateHub.Domain.Entities.Companies;

public class Permission
{
    public Guid Id { get; set; }
    public Guid PermissionGroupId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }

    public PermissionGroup PermissionGroup { get; set; } = null!;
    public ICollection<CompanyRolePermission> RoleAssignments { get; set; } = [];
}
