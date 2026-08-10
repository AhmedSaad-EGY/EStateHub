using EstateHub.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstateHub.Infrastructure.Persistence.Configurations.Catalog;

public sealed class ListingMediaConfiguration : IEntityTypeConfiguration<ListingMedia>
{
    public void Configure(EntityTypeBuilder<ListingMedia> builder)
    {
        builder.HasKey(media => media.Id);

        builder.Property(media => media.Caption)
            .HasMaxLength(500);

        builder.HasIndex(media => new { media.ListingId, media.FileAssetId })
            .HasDatabaseName("UX_ListingMedia_ListingId_FileAssetId")
            .IsUnique();

        builder.HasIndex(media => media.ListingId)
            .HasDatabaseName("UX_ListingMedia_Cover_ListingId")
            .IsUnique()
            .HasFilter("[IsCover] = 1");

        builder.HasOne(media => media.Listing)
            .WithMany(listing => listing.Media)
            .HasForeignKey(media => media.ListingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(media => media.FileAsset)
            .WithMany(file => file.ListingMedia)
            .HasForeignKey(media => media.FileAssetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_ListingMedia_SortOrder_NonNegative",
                "[SortOrder] >= 0");
        });
    }
}
