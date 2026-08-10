using EstateHub.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstateHub.Infrastructure.Persistence.Configurations.Catalog;

public sealed class AmenityConfiguration : IEntityTypeConfiguration<Amenity>
{
    public void Configure(EntityTypeBuilder<Amenity> builder)
    {
        builder.HasKey(amenity => amenity.Id);

        builder.Property(amenity => amenity.Code)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(amenity => amenity.NameEn)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(amenity => amenity.NameAr)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(amenity => amenity.IconKey)
            .HasMaxLength(100);

        builder.HasIndex(amenity => amenity.Code)
            .HasDatabaseName("UX_Amenities_Code")
            .IsUnique();
    }
}
