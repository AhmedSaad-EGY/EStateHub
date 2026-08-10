using EstateHub.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstateHub.Infrastructure.Persistence.Configurations.Catalog;

public sealed class ProjectAmenityConfiguration : IEntityTypeConfiguration<ProjectAmenity>
{
    public void Configure(EntityTypeBuilder<ProjectAmenity> builder)
    {
        builder.HasKey(projectAmenity => new
        {
            projectAmenity.ProjectId,
            projectAmenity.AmenityId
        });

        builder.HasOne(projectAmenity => projectAmenity.Project)
            .WithMany(project => project.Amenities)
            .HasForeignKey(projectAmenity => projectAmenity.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(projectAmenity => projectAmenity.Amenity)
            .WithMany(amenity => amenity.Projects)
            .HasForeignKey(projectAmenity => projectAmenity.AmenityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
