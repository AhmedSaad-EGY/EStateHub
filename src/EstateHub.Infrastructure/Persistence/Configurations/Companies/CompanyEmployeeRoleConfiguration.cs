using EstateHub.Domain.Entities.Companies;
using EstateHub.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstateHub.Infrastructure.Persistence.Configurations.Companies;

public sealed class CompanyEmployeeRoleConfiguration : IEntityTypeConfiguration<CompanyEmployeeRole>
{
    public void Configure(EntityTypeBuilder<CompanyEmployeeRole> builder)
    {
        builder.HasKey(assignment => assignment.Id);

        builder.HasIndex(assignment => new
            {
                assignment.CompanyEmployeeId,
                assignment.CompanyRoleId
            })
            .HasDatabaseName("UX_CompanyEmployeeRoles_ActiveAssignment")
            .IsUnique()
            .HasFilter("[RevokedAt] IS NULL");

        builder.HasOne(assignment => assignment.Company)
            .WithMany(company => company.EmployeeRoleAssignments)
            .HasForeignKey(assignment => assignment.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(assignment => assignment.CompanyEmployee)
            .WithMany(employee => employee.RoleAssignments)
            .HasForeignKey(assignment => new
            {
                assignment.CompanyEmployeeId,
                assignment.CompanyId
            })
            .HasPrincipalKey(employee => new { employee.Id, employee.CompanyId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(assignment => assignment.CompanyRole)
            .WithMany(role => role.EmployeeAssignments)
            .HasForeignKey(assignment => new
            {
                assignment.CompanyRoleId,
                assignment.CompanyId
            })
            .HasPrincipalKey(role => new { role.Id, role.CompanyId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(assignment => assignment.AssignedByApplicationUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
