using EstateHub.Domain.Entities.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstateHub.Infrastructure.Persistence.Configurations.Billing;

public sealed class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.HasKey(plan => plan.Id);

        builder.Property(plan => plan.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(plan => plan.MonthlyPrice)
            .HasPrecision(18, 2);

        builder.Property(plan => plan.CurrencyCode)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(plan => plan.BookingFeeAmount)
            .HasPrecision(18, 2);

        builder.HasIndex(plan => plan.Name)
            .HasDatabaseName("UX_SubscriptionPlans_Name")
            .IsUnique();

        builder.HasOne(plan => plan.Currency)
            .WithMany(currency => currency.SubscriptionPlans)
            .HasForeignKey(plan => plan.CurrencyCode)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_SubscriptionPlans_MonthlyPrice_NonNegative",
                "[MonthlyPrice] >= 0");
            table.HasCheckConstraint(
                "CK_SubscriptionPlans_BookingFeeAmount_NonNegative",
                "[BookingFeeAmount] >= 0");
            table.HasCheckConstraint(
                "CK_SubscriptionPlans_MaxPublishedListings_NonNegative",
                "[MaxPublishedListings] IS NULL OR [MaxPublishedListings] >= 0");
        });
    }
}
