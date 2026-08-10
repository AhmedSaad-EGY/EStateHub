using EstateHub.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstateHub.Infrastructure.Persistence.Configurations.Catalog;

public sealed class PaymentPlanConfiguration : IEntityTypeConfiguration<PaymentPlan>
{
    public void Configure(EntityTypeBuilder<PaymentPlan> builder)
    {
        builder.HasKey(plan => plan.Id);

        builder.Property(plan => plan.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(plan => plan.TotalPrice)
            .HasPrecision(18, 2);

        builder.Property(plan => plan.CurrencyCode)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(plan => plan.DownPaymentPercentage)
            .HasPrecision(5, 2);

        builder.Property(plan => plan.CashDiscountPercentage)
            .HasPrecision(5, 2);

        builder.HasOne(plan => plan.Listing)
            .WithMany(listing => listing.PaymentPlans)
            .HasForeignKey(plan => plan.ListingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(plan => plan.Currency)
            .WithMany(currency => currency.PaymentPlans)
            .HasForeignKey(plan => plan.CurrencyCode)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_PaymentPlans_TotalPrice_Positive",
                "[TotalPrice] > 0");
            table.HasCheckConstraint(
                "CK_PaymentPlans_DurationMonths_Positive",
                "[DurationMonths] > 0");
            table.HasCheckConstraint(
                "CK_PaymentPlans_DownPaymentPercentage_Range",
                "[DownPaymentPercentage] >= 0 AND [DownPaymentPercentage] <= 100");
            table.HasCheckConstraint(
                "CK_PaymentPlans_CashDiscountPercentage_Range",
                "[CashDiscountPercentage] IS NULL OR ([CashDiscountPercentage] >= 0 AND [CashDiscountPercentage] <= 100)");
        });
    }
}
