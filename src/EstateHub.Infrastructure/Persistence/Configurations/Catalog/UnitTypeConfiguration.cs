using EstateHub.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstateHub.Infrastructure.Persistence.Configurations.Catalog;

public sealed class UnitTypeConfiguration : IEntityTypeConfiguration<UnitType>
{
    public void Configure(EntityTypeBuilder<UnitType> builder)
    {
        builder.HasKey(unitType => unitType.Id);

        builder.Property(unitType => unitType.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(unitType => unitType.NameEn)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(unitType => unitType.NameAr)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(unitType => unitType.Code)
            .HasDatabaseName("UX_UnitTypes_Code")
            .IsUnique();
    }
}
