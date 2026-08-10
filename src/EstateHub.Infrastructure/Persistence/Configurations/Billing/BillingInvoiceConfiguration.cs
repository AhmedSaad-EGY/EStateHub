using EstateHub.Domain.Entities.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstateHub.Infrastructure.Persistence.Configurations.Billing;

public sealed class BillingInvoiceConfiguration : IEntityTypeConfiguration<BillingInvoice>
{
    public void Configure(EntityTypeBuilder<BillingInvoice> builder)
    {
        builder.HasKey(invoice => invoice.Id);

        builder.Property(invoice => invoice.InvoiceNumber)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(invoice => invoice.CurrencyCode)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(invoice => invoice.Subtotal)
            .HasPrecision(18, 2);

        builder.Property(invoice => invoice.Total)
            .HasPrecision(18, 2);

        builder.Property(invoice => invoice.RowVersion)
            .IsRowVersion();

        builder.HasIndex(invoice => invoice.InvoiceNumber)
            .HasDatabaseName("UX_BillingInvoices_InvoiceNumber")
            .IsUnique();

        builder.HasOne(invoice => invoice.Company)
            .WithMany(company => company.BillingInvoices)
            .HasForeignKey(invoice => invoice.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(invoice => invoice.Currency)
            .WithMany(currency => currency.BillingInvoices)
            .HasForeignKey(invoice => invoice.CurrencyCode)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_BillingInvoices_Subtotal_NonNegative",
                "[Subtotal] >= 0");
            table.HasCheckConstraint(
                "CK_BillingInvoices_Total_NonNegative",
                "[Total] >= 0");
            table.HasCheckConstraint(
                "CK_BillingInvoices_Period_Range",
                "[PeriodStart] IS NULL OR [PeriodEnd] IS NULL OR [PeriodStart] < [PeriodEnd]");
        });
    }
}
