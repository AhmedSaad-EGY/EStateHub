using EstateHub.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstateHub.Infrastructure.Persistence.Configurations.Catalog;

public sealed class UnitConfiguration : IEntityTypeConfiguration<Unit>
{
    public void Configure(EntityTypeBuilder<Unit> builder)
    {
        builder.HasKey(unit => unit.Id);

        builder.HasAlternateKey(unit => new { unit.Id, unit.ManagingCompanyId });

        builder.Property(unit => unit.UnitCode)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(unit => unit.BuiltUpArea)
            .HasPrecision(18, 2);

        builder.Property(unit => unit.LandArea)
            .HasPrecision(18, 2);

        builder.Property(unit => unit.RowVersion)
            .IsRowVersion();

        builder.HasIndex(unit => new
            {
                unit.ManagingCompanyId,
                unit.ProjectId,
                unit.UnitCode
            })
            .HasDatabaseName("UX_Units_ManagingCompanyId_ProjectId_UnitCode")
            .IsUnique()
            .HasFilter("[ProjectId] IS NOT NULL");

        builder.HasIndex(unit => new { unit.ManagingCompanyId, unit.UnitCode })
            .HasDatabaseName("UX_Units_ManagingCompanyId_StandaloneUnitCode")
            .IsUnique()
            .HasFilter("[ProjectId] IS NULL");

        builder.HasOne(unit => unit.ManagingCompany)
            .WithMany(company => company.Units)
            .HasForeignKey(unit => unit.ManagingCompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(unit => unit.Project)
            .WithMany(project => project.Units)
            .HasForeignKey(unit => unit.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(unit => unit.Location)
            .WithMany(location => location.Units)
            .HasForeignKey(unit => unit.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(unit => unit.UnitType)
            .WithMany(unitType => unitType.Units)
            .HasForeignKey(unit => unit.UnitTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_Units_Bedrooms_NonNegative",
                "[Bedrooms] >= 0");
            table.HasCheckConstraint(
                "CK_Units_Bathrooms_NonNegative",
                "[Bathrooms] >= 0");
            table.HasCheckConstraint(
                "CK_Units_BuiltUpArea_Positive",
                "[BuiltUpArea] > 0");
            table.HasCheckConstraint(
                "CK_Units_LandArea_Positive",
                "[LandArea] IS NULL OR [LandArea] > 0");
            table.HasCheckConstraint(
                "CK_Units_TotalFloors_Positive",
                "[TotalFloors] IS NULL OR [TotalFloors] > 0");
        });
    }
}
