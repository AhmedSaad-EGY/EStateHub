using EstateHub.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstateHub.Infrastructure.Persistence.Configurations.Catalog;

public sealed class ProjectMediaConfiguration : IEntityTypeConfiguration<ProjectMedia>
{
    public void Configure(EntityTypeBuilder<ProjectMedia> builder)
    {
        builder.HasKey(media => media.Id);

        builder.Property(media => media.Caption)
            .HasMaxLength(500);

        builder.HasIndex(media => new { media.ProjectId, media.FileAssetId })
            .HasDatabaseName("UX_ProjectMedia_ProjectId_FileAssetId")
            .IsUnique();

        builder.HasIndex(media => media.ProjectId)
            .HasDatabaseName("UX_ProjectMedia_Cover_ProjectId")
            .IsUnique()
            .HasFilter("[IsCover] = 1");

        builder.HasOne(media => media.Project)
            .WithMany(project => project.Media)
            .HasForeignKey(media => media.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(media => media.FileAsset)
            .WithMany(file => file.ProjectMedia)
            .HasForeignKey(media => media.FileAssetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_ProjectMedia_SortOrder_NonNegative",
                "[SortOrder] >= 0");
        });
    }
}
