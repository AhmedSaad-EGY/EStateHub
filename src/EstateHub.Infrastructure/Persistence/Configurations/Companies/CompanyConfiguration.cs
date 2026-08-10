using EstateHub.Domain.Entities.Companies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstateHub.Infrastructure.Persistence.Configurations.Companies;

public sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.HasKey(company => company.Id);

        builder.Property(company => company.Slug)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(company => company.LegalName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(company => company.DisplayName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(company => company.RegistrationNumber)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(company => company.TaxId)
            .HasMaxLength(100);

        builder.Property(company => company.BusinessEmail)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(company => company.SupportPhone)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(company => company.Website)
            .HasMaxLength(2048);

        builder.Property(company => company.TimeZoneId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(company => company.BaseCurrencyCode)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(company => company.RowVersion)
            .IsRowVersion();

        builder.HasIndex(company => company.Slug)
            .HasDatabaseName("UX_Companies_Slug")
            .IsUnique();

        builder.HasIndex(company => company.RegistrationNumber)
            .HasDatabaseName("UX_Companies_RegistrationNumber")
            .IsUnique();

        builder.HasIndex(company => company.TaxId)
            .HasDatabaseName("UX_Companies_TaxId")
            .IsUnique()
            .HasFilter("[TaxId] IS NOT NULL");

        builder.HasIndex(company => company.AddressId)
            .HasDatabaseName("UX_Companies_AddressId")
            .IsUnique();

        builder.HasOne(company => company.Address)
            .WithOne(address => address.Company)
            .HasForeignKey<Company>(company => company.AddressId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(company => company.BaseCurrency)
            .WithMany(currency => currency.CompaniesUsingAsBaseCurrency)
            .HasForeignKey(company => company.BaseCurrencyCode)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(company => company.LogoFileAsset)
            .WithMany(file => file.CompaniesUsingAsLogo)
            .HasForeignKey(company => company.LogoFileAssetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(company => company.CoverFileAsset)
            .WithMany(file => file.CompaniesUsingAsCover)
            .HasForeignKey(company => company.CoverFileAssetId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
