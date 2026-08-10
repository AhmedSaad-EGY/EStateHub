using EstateHub.Domain.Entities.Companies;
using EstateHub.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstateHub.Infrastructure.Persistence.Configurations.Companies;

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.HasKey(permission => permission.Id);

        builder.Property(permission => permission.Code)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(permission => permission.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(permission => permission.Description)
            .HasMaxLength(500);

        builder.HasIndex(permission => permission.Code)
            .HasDatabaseName("UX_Permissions_Code")
            .IsUnique();

        builder.HasOne(permission => permission.PermissionGroup)
            .WithMany(group => group.Permissions)
            .HasForeignKey(permission => permission.PermissionGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(CompanyPermissionCatalogSeed.Permissions);
    }
}
