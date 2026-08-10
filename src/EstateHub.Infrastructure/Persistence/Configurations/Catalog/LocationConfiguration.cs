using EstateHub.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstateHub.Infrastructure.Persistence.Configurations.Catalog;

public sealed class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.HasKey(location => location.Id);

        builder.Property(location => location.NameEn)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(location => location.NameAr)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(location => location.Slug)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(location => new
            {
                location.ParentLocationId,
                location.Type,
                location.Slug
            })
            .HasDatabaseName("UX_Locations_ParentLocationId_Type_Slug")
            .IsUnique()
            .HasFilter(null);

        builder.HasOne(location => location.ParentLocation)
            .WithMany(location => location.ChildLocations)
            .HasForeignKey(location => location.ParentLocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
