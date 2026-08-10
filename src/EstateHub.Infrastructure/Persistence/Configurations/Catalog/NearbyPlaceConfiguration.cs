using EstateHub.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstateHub.Infrastructure.Persistence.Configurations.Catalog;

public sealed class NearbyPlaceConfiguration : IEntityTypeConfiguration<NearbyPlace>
{
    public void Configure(EntityTypeBuilder<NearbyPlace> builder)
    {
        builder.HasKey(place => place.Id);

        builder.Property(place => place.Category)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(place => place.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(place => place.DistanceMeters)
            .HasPrecision(18, 2);

        builder.Property(place => place.Latitude)
            .HasPrecision(9, 6);

        builder.Property(place => place.Longitude)
            .HasPrecision(9, 6);

        builder.HasOne(place => place.Project)
            .WithMany(project => project.NearbyPlaces)
            .HasForeignKey(place => place.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(place => place.Unit)
            .WithMany(unit => unit.NearbyPlaces)
            .HasForeignKey(place => place.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_NearbyPlaces_ProjectOrUnit_Xor",
                "(([ProjectId] IS NOT NULL AND [UnitId] IS NULL) OR ([ProjectId] IS NULL AND [UnitId] IS NOT NULL))");
            table.HasCheckConstraint(
                "CK_NearbyPlaces_DistanceMeters_NonNegative",
                "[DistanceMeters] IS NULL OR [DistanceMeters] >= 0");
            table.HasCheckConstraint(
                "CK_NearbyPlaces_TravelMinutes_NonNegative",
                "[TravelMinutes] IS NULL OR [TravelMinutes] >= 0");
            table.HasCheckConstraint(
                "CK_NearbyPlaces_Latitude_Range",
                "[Latitude] IS NULL OR ([Latitude] >= -90 AND [Latitude] <= 90)");
            table.HasCheckConstraint(
                "CK_NearbyPlaces_Longitude_Range",
                "[Longitude] IS NULL OR ([Longitude] >= -180 AND [Longitude] <= 180)");
        });
    }
}
