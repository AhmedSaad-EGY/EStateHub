using EstateHub.Domain.Entities.Billing;
using EstateHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstateHub.Infrastructure.Persistence.Configurations.Billing;

public sealed class BillingInvoiceLineConfiguration : IEntityTypeConfiguration<BillingInvoiceLine>
{
    public void Configure(EntityTypeBuilder<BillingInvoiceLine> builder)
    {
        const int subscriptionLineType = (int)BillingLineType.Subscription;
        const int bookingChargeLineType = (int)BillingLineType.BookingCharge;
        const int promotionLineType = (int)BillingLineType.Promotion;

        builder.HasKey(line => line.Id);

        builder.Property(line => line.DescriptionSnapshot)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(line => line.Quantity)
            .HasPrecision(18, 2);

        builder.Property(line => line.UnitAmount)
            .HasPrecision(18, 2);

        builder.Property(line => line.LineTotal)
            .HasPrecision(18, 2);

        builder.HasIndex(line => line.BookingChargeId)
            .HasDatabaseName("UX_BillingInvoiceLines_BookingChargeId")
            .IsUnique()
            .HasFilter("[BookingChargeId] IS NOT NULL");

        builder.HasIndex(line => line.ListingPromotionId)
            .HasDatabaseName("UX_BillingInvoiceLines_ListingPromotionId")
            .IsUnique()
            .HasFilter("[ListingPromotionId] IS NOT NULL");

        builder.HasOne(line => line.BillingInvoice)
            .WithMany(invoice => invoice.Lines)
            .HasForeignKey(line => line.BillingInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(line => line.CompanySubscription)
            .WithMany(subscription => subscription.InvoiceLines)
            .HasForeignKey(line => line.CompanySubscriptionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(line => line.BookingCharge)
            .WithOne(charge => charge.InvoiceLine)
            .HasForeignKey<BillingInvoiceLine>(line => line.BookingChargeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(line => line.ListingPromotion)
            .WithOne(promotion => promotion.InvoiceLine)
            .HasForeignKey<BillingInvoiceLine>(line => line.ListingPromotionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_BillingInvoiceLines_Source_Xor",
                "(([CompanySubscriptionId] IS NOT NULL AND [BookingChargeId] IS NULL AND [ListingPromotionId] IS NULL) OR " +
                "([CompanySubscriptionId] IS NULL AND [BookingChargeId] IS NOT NULL AND [ListingPromotionId] IS NULL) OR " +
                "([CompanySubscriptionId] IS NULL AND [BookingChargeId] IS NULL AND [ListingPromotionId] IS NOT NULL))");
            table.HasCheckConstraint(
                "CK_BillingInvoiceLines_LineType_Source",
                $"(([LineType] = {subscriptionLineType} AND [CompanySubscriptionId] IS NOT NULL AND [BookingChargeId] IS NULL AND [ListingPromotionId] IS NULL) OR " +
                $"([LineType] = {bookingChargeLineType} AND [CompanySubscriptionId] IS NULL AND [BookingChargeId] IS NOT NULL AND [ListingPromotionId] IS NULL) OR " +
                $"([LineType] = {promotionLineType} AND [CompanySubscriptionId] IS NULL AND [BookingChargeId] IS NULL AND [ListingPromotionId] IS NOT NULL))");
            table.HasCheckConstraint(
                "CK_BillingInvoiceLines_Quantity_Positive",
                "[Quantity] > 0");
            table.HasCheckConstraint(
                "CK_BillingInvoiceLines_UnitAmount_NonNegative",
                "[UnitAmount] >= 0");
            table.HasCheckConstraint(
                "CK_BillingInvoiceLines_LineTotal_NonNegative",
                "[LineTotal] >= 0");
        });
    }
}
