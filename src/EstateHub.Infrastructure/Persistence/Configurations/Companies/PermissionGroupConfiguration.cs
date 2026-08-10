using EstateHub.Domain.Entities.Companies;
using EstateHub.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstateHub.Infrastructure.Persistence.Configurations.Companies;

public sealed class PermissionGroupConfiguration : IEntityTypeConfiguration<PermissionGroup>
{
    public void Configure(EntityTypeBuilder<PermissionGroup> builder)
    {
        builder.HasKey(group => group.Id);

        builder.Property(group => group.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(group => group.Description)
            .HasMaxLength(500);

        builder.HasIndex(group => group.Name)
            .HasDatabaseName("UX_PermissionGroups_Name")
            .IsUnique();

        builder.HasData(CompanyPermissionCatalogSeed.PermissionGroups);
    }
}
