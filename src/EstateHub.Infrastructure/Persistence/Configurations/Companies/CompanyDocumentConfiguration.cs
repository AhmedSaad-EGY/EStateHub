using EstateHub.Domain.Entities.Companies;
using EstateHub.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstateHub.Infrastructure.Persistence.Configurations.Companies;

public sealed class CompanyDocumentConfiguration : IEntityTypeConfiguration<CompanyDocument>
{
    public void Configure(EntityTypeBuilder<CompanyDocument> builder)
    {
        builder.HasKey(document => document.Id);

        builder.Property(document => document.DocumentType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(document => document.VerificationStatus)
            .IsRequired();

        builder.Property(document => document.RejectionReason)
            .HasMaxLength(1000);

        builder.HasOne(document => document.CompanyApplication)
            .WithMany(application => application.Documents)
            .HasForeignKey(document => document.CompanyApplicationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(document => document.FileAsset)
            .WithMany(file => file.CompanyDocuments)
            .HasForeignKey(document => document.FileAssetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(document => document.ReviewedByApplicationUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
