using EstateHub.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstateHub.Infrastructure.Persistence.Configurations.Catalog;

public sealed class ListingPriceHistoryConfiguration : IEntityTypeConfiguration<ListingPriceHistory>
{
    public void Configure(EntityTypeBuilder<ListingPriceHistory> builder)
    {
        builder.HasKey(history => history.Id);

        builder.Property(history => history.Price)
            .HasPrecision(18, 2);

        builder.Property(history => history.CurrencyCode)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(history => history.ChangeReason)
            .HasMaxLength(500);

        builder.HasIndex(history => history.ListingId)
            .HasDatabaseName("UX_ListingPriceHistories_OpenListingId")
            .IsUnique()
            .HasFilter("[EffectiveTo] IS NULL");

        builder.HasOne(history => history.Listing)
            .WithMany(listing => listing.PriceHistory)
            .HasForeignKey(history => history.ListingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(history => history.Currency)
            .WithMany(currency => currency.ListingPriceHistories)
            .HasForeignKey(history => history.CurrencyCode)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_ListingPriceHistories_Price_Positive",
                "[Price] > 0");
            table.HasCheckConstraint(
                "CK_ListingPriceHistories_EffectiveDates",
                "[EffectiveTo] IS NULL OR [EffectiveTo] > [EffectiveFrom]");
        });
    }
}
