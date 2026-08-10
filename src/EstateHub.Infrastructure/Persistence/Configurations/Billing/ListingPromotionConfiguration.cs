using EstateHub.Domain.Entities.Billing;
using EstateHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstateHub.Infrastructure.Persistence.Configurations.Billing;

public sealed class ListingPromotionConfiguration : IEntityTypeConfiguration<ListingPromotion>
{
    public void Configure(EntityTypeBuilder<ListingPromotion> builder)
    {
        const int activeStatus = (int)ListingPromotionStatus.Active;

        builder.HasKey(promotion => promotion.Id);

        builder.Property(promotion => promotion.AmountSnapshot)
            .HasPrecision(18, 2);

        builder.Property(promotion => promotion.CurrencyCode)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(promotion => promotion.PlacementSnapshot)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(promotion => promotion.RowVersion)
            .IsRowVersion();

        builder.HasIndex(promotion => new { promotion.ListingId, promotion.PlacementSnapshot })
            .HasDatabaseName("UX_ListingPromotions_Active_ListingId_PlacementSnapshot")
            .IsUnique()
            .HasFilter($"[Status] = {activeStatus}");

        builder.HasOne(promotion => promotion.Listing)
            .WithMany(listing => listing.Promotions)
            .HasForeignKey(promotion => new { promotion.ListingId, promotion.CompanyId })
            .HasPrincipalKey(listing => new { listing.Id, listing.CompanyId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(promotion => promotion.Company)
            .WithMany(company => company.ListingPromotions)
            .HasForeignKey(promotion => promotion.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(promotion => promotion.PromotionPackage)
            .WithMany(package => package.ListingPromotions)
            .HasForeignKey(promotion => promotion.PromotionPackageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(promotion => promotion.Currency)
            .WithMany(currency => currency.ListingPromotions)
            .HasForeignKey(promotion => promotion.CurrencyCode)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_ListingPromotions_AmountSnapshot_NonNegative",
                "[AmountSnapshot] >= 0");
            table.HasCheckConstraint(
                "CK_ListingPromotions_TimeRange",
                "[StartsAt] < [EndsAt]");
        });
    }
}
