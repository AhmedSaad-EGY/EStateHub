namespace EstateHub.Domain.Entities.Companies;

public class CompanyRolePermission
{
    public Guid Id { get; set; }
    public Guid CompanyRoleId { get; set; }
    public Guid PermissionId { get; set; }
    public DateTimeOffset GrantedAt { get; set; }
    public Guid GrantedByApplicationUserId { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }

    public CompanyRole CompanyRole { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
}
