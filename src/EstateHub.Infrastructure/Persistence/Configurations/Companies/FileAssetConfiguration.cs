using EstateHub.Domain.Entities.Companies;
using EstateHub.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstateHub.Infrastructure.Persistence.Configurations.Companies;

public sealed class FileAssetConfiguration : IEntityTypeConfiguration<FileAsset>
{
    public void Configure(EntityTypeBuilder<FileAsset> builder)
    {
        builder.HasKey(file => file.Id);

        builder.Property(file => file.StorageKey)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(file => file.ContentType)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(file => file.OriginalFileName)
            .HasMaxLength(255);

        builder.HasIndex(file => file.StorageKey)
            .HasDatabaseName("UX_FileAssets_StorageKey")
            .IsUnique();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(file => file.UploadedByApplicationUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_FileAssets_SizeBytes_Positive",
                "[SizeBytes] > 0");
            table.HasCheckConstraint(
                "CK_FileAssets_Width_Positive",
                "[Width] IS NULL OR [Width] > 0");
            table.HasCheckConstraint(
                "CK_FileAssets_Height_Positive",
                "[Height] IS NULL OR [Height] > 0");
        });
    }
}
