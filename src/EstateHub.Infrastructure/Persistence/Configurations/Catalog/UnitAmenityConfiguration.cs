using EstateHub.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstateHub.Infrastructure.Persistence.Configurations.Catalog;

public sealed class UnitAmenityConfiguration : IEntityTypeConfiguration<UnitAmenity>
{
    public void Configure(EntityTypeBuilder<UnitAmenity> builder)
    {
        builder.HasKey(unitAmenity => new
        {
            unitAmenity.UnitId,
            unitAmenity.AmenityId
        });

        builder.HasOne(unitAmenity => unitAmenity.Unit)
            .WithMany(unit => unit.Amenities)
            .HasForeignKey(unitAmenity => unitAmenity.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(unitAmenity => unitAmenity.Amenity)
            .WithMany(amenity => amenity.Units)
            .HasForeignKey(unitAmenity => unitAmenity.AmenityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
