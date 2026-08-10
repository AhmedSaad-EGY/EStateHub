using EstateHub.Domain.Entities.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstateHub.Infrastructure.Persistence.Configurations.Billing;

public sealed class CompanySubscriptionConfiguration : IEntityTypeConfiguration<CompanySubscription>
{
    public void Configure(EntityTypeBuilder<CompanySubscription> builder)
    {
        builder.HasKey(subscription => subscription.Id);

        builder.Property(subscription => subscription.MonthlyPriceSnapshot)
            .HasPrecision(18, 2);

        builder.Property(subscription => subscription.CurrencyCode)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(subscription => subscription.BookingFeeSnapshot)
            .HasPrecision(18, 2);

        builder.Property(subscription => subscription.RowVersion)
            .IsRowVersion();

        builder.HasIndex(subscription => subscription.CompanyId)
            .HasDatabaseName("UX_CompanySubscriptions_OpenCompanyId")
            .IsUnique()
            .HasFilter("[EndedAt] IS NULL");

        builder.HasOne(subscription => subscription.Company)
            .WithMany(company => company.Subscriptions)
            .HasForeignKey(subscription => subscription.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(subscription => subscription.SubscriptionPlan)
            .WithMany(plan => plan.Subscriptions)
            .HasForeignKey(subscription => subscription.SubscriptionPlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(subscription => subscription.Currency)
            .WithMany(currency => currency.CompanySubscriptions)
            .HasForeignKey(subscription => subscription.CurrencyCode)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_CompanySubscriptions_CurrentPeriod_Range",
                "[CurrentPeriodStart] < [CurrentPeriodEnd]");
            table.HasCheckConstraint(
                "CK_CompanySubscriptions_MonthlyPriceSnapshot_NonNegative",
                "[MonthlyPriceSnapshot] >= 0");
            table.HasCheckConstraint(
                "CK_CompanySubscriptions_BookingFeeSnapshot_NonNegative",
                "[BookingFeeSnapshot] >= 0");
            table.HasCheckConstraint(
                "CK_CompanySubscriptions_MaxPublishedListingsSnapshot_NonNegative",
                "[MaxPublishedListingsSnapshot] IS NULL OR [MaxPublishedListingsSnapshot] >= 0");
        });
    }
}
