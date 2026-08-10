using EstateHub.Domain.Entities.Companies;
using EstateHub.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstateHub.Infrastructure.Persistence.Configurations.Companies;

public sealed class CompanyApplicationConfiguration : IEntityTypeConfiguration<CompanyApplication>
{
    public void Configure(EntityTypeBuilder<CompanyApplication> builder)
    {
        builder.HasKey(application => application.Id);

        builder.Property(application => application.LegalName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(application => application.BusinessEmail)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(application => application.PhoneNumber)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(application => application.RegistrationNumber)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(application => application.TaxId)
            .HasMaxLength(100);

        builder.Property(application => application.Website)
            .HasMaxLength(2048);

        builder.Property(application => application.OfficeAddress)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(application => application.LocationText)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(application => application.EstimatedPropertyRange)
            .HasMaxLength(100);

        builder.Property(application => application.DecisionReason)
            .HasMaxLength(1000);

        builder.Property(application => application.RowVersion)
            .IsRowVersion();

        builder.HasIndex(application => application.ApprovedCompanyId)
            .HasDatabaseName("UX_CompanyApplications_ApprovedCompanyId")
            .IsUnique()
            .HasFilter("[ApprovedCompanyId] IS NOT NULL");

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(application => application.SubmittedByApplicationUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(application => application.ReviewedByApplicationUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(application => application.ApprovedCompany)
            .WithOne(company => company.ApprovedFromApplication)
            .HasForeignKey<CompanyApplication>(application => application.ApprovedCompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
