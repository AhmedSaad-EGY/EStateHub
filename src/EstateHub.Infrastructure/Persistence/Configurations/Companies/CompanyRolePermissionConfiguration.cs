using EstateHub.Domain.Entities.Companies;
using EstateHub.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstateHub.Infrastructure.Persistence.Configurations.Companies;

public sealed class CompanyRolePermissionConfiguration : IEntityTypeConfiguration<CompanyRolePermission>
{
    public void Configure(EntityTypeBuilder<CompanyRolePermission> builder)
    {
        builder.HasKey(assignment => assignment.Id);

        builder.HasIndex(assignment => new
            {
                assignment.CompanyRoleId,
                assignment.PermissionId
            })
            .HasDatabaseName("UX_CompanyRolePermissions_ActiveAssignment")
            .IsUnique()
            .HasFilter("[RevokedAt] IS NULL");

        builder.HasOne(assignment => assignment.CompanyRole)
            .WithMany(role => role.PermissionAssignments)
            .HasForeignKey(assignment => assignment.CompanyRoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(assignment => assignment.Permission)
            .WithMany(permission => permission.RoleAssignments)
            .HasForeignKey(assignment => assignment.PermissionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(assignment => assignment.GrantedByApplicationUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
