using EstateHub.Domain.Entities.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstateHub.Infrastructure.Persistence.Configurations.Billing;

public sealed class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.HasKey(transaction => transaction.Id);

        builder.Property(transaction => transaction.Amount)
            .HasPrecision(18, 2);

        builder.Property(transaction => transaction.CurrencyCode)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(transaction => transaction.ProviderReference)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(transaction => transaction.FailureReason)
            .HasMaxLength(1000);

        builder.HasIndex(transaction => new { transaction.Provider, transaction.ProviderReference })
            .HasDatabaseName("UX_PaymentTransactions_Provider_ProviderReference")
            .IsUnique();

        builder.HasOne(transaction => transaction.BillingInvoice)
            .WithMany(invoice => invoice.PaymentTransactions)
            .HasForeignKey(transaction => transaction.BillingInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(transaction => transaction.Currency)
            .WithMany(currency => currency.PaymentTransactions)
            .HasForeignKey(transaction => transaction.CurrencyCode)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_PaymentTransactions_Amount_Positive",
                "[Amount] > 0");
            table.HasCheckConstraint(
                "CK_PaymentTransactions_CompletedAt_NotBeforeCreatedAt",
                "[CompletedAt] IS NULL OR [CompletedAt] >= [CreatedAt]");
        });
    }
}
