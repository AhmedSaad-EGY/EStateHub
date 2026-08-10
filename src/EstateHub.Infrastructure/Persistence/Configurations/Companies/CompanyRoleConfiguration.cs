using EstateHub.Domain.Entities.Companies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstateHub.Infrastructure.Persistence.Configurations.Companies;

public sealed class CompanyRoleConfiguration : IEntityTypeConfiguration<CompanyRole>
{
    public void Configure(EntityTypeBuilder<CompanyRole> builder)
    {
        builder.HasKey(role => role.Id);

        builder.HasAlternateKey(role => new { role.Id, role.CompanyId });

        builder.Property(role => role.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(role => role.NormalizedName)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(role => new { role.CompanyId, role.NormalizedName })
            .HasDatabaseName("UX_CompanyRoles_Company_NormalizedName")
            .IsUnique();

        builder.HasOne(role => role.Company)
            .WithMany(company => company.Roles)
            .HasForeignKey(role => role.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
