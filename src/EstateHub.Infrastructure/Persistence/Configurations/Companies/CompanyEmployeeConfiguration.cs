using EstateHub.Domain.Entities.Companies;
using EstateHub.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstateHub.Infrastructure.Persistence.Configurations.Companies;

public sealed class CompanyEmployeeConfiguration : IEntityTypeConfiguration<CompanyEmployee>
{
    public void Configure(EntityTypeBuilder<CompanyEmployee> builder)
    {
        builder.HasKey(employee => employee.Id);

        builder.HasAlternateKey(employee => new { employee.Id, employee.CompanyId });

        builder.Property(employee => employee.FullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(employee => employee.JobTitle)
            .HasMaxLength(150);

        builder.Property(employee => employee.RowVersion)
            .IsRowVersion();

        builder.HasIndex(employee => employee.ApplicationUserId)
            .HasDatabaseName("UX_CompanyEmployees_OpenMembership")
            .IsUnique()
            .HasFilter("[EndedAt] IS NULL");

        builder.HasIndex(employee => employee.CompanyId)
            .HasDatabaseName("UX_CompanyEmployees_OpenPrimaryContact")
            .IsUnique()
            .HasFilter("[IsPrimaryContact] = 1 AND [EndedAt] IS NULL");

        builder.HasOne(employee => employee.Company)
            .WithMany(company => company.Employees)
            .HasForeignKey(employee => employee.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(employee => employee.ApplicationUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
