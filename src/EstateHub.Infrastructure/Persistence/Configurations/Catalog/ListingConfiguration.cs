using EstateHub.Domain.Entities.Catalog;
using EstateHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstateHub.Infrastructure.Persistence.Configurations.Catalog;

public sealed class ListingConfiguration : IEntityTypeConfiguration<Listing>
{
    public void Configure(EntityTypeBuilder<Listing> builder)
    {
        const int publishedStatus = (int)ListingPublicationStatus.Published;

        builder.HasKey(listing => listing.Id);

        builder.HasAlternateKey(listing => new { listing.Id, listing.CompanyId });

        builder.Property(listing => listing.CurrencyCode)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(listing => listing.ListingCode)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(listing => listing.Slug)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(listing => listing.Title)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(listing => listing.Description)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(listing => listing.AskingPrice)
            .HasPrecision(18, 2);

        builder.Property(listing => listing.RowVersion)
            .IsRowVersion();

        builder.HasIndex(listing => listing.Slug)
            .HasDatabaseName("UX_Listings_Slug")
            .IsUnique();

        builder.HasIndex(listing => new { listing.CompanyId, listing.ListingCode })
            .HasDatabaseName("UX_Listings_CompanyId_ListingCode")
            .IsUnique();

        builder.HasIndex(listing => listing.UnitId)
            .HasDatabaseName("UX_Listings_Published_UnitId")
            .IsUnique()
            .HasFilter($"[PublicationStatus] = {publishedStatus}");

        builder.HasOne(listing => listing.Unit)
            .WithMany(unit => unit.Listings)
            .HasForeignKey(listing => new { listing.UnitId, listing.CompanyId })
            .HasPrincipalKey(unit => new { unit.Id, unit.ManagingCompanyId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(listing => listing.Company)
            .WithMany(company => company.Listings)
            .HasForeignKey(listing => listing.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(listing => listing.Currency)
            .WithMany(currency => currency.Listings)
            .HasForeignKey(listing => listing.CurrencyCode)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(listing => listing.CreatedByEmployee)
            .WithMany(employee => employee.CreatedListings)
            .HasForeignKey(listing => new
            {
                listing.CreatedByEmployeeId,
                listing.CompanyId
            })
            .HasPrincipalKey(employee => new { employee.Id, employee.CompanyId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_Listings_AskingPrice_Positive",
                "[AskingPrice] > 0");
        });
    }
}
