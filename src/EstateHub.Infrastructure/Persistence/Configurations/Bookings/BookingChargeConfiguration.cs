using EstateHub.Domain.Entities.Bookings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstateHub.Infrastructure.Persistence.Configurations.Bookings;

public sealed class BookingChargeConfiguration : IEntityTypeConfiguration<BookingCharge>
{
    public void Configure(EntityTypeBuilder<BookingCharge> builder)
    {
        builder.HasKey(charge => charge.Id);

        builder.Property(charge => charge.AmountSnapshot)
            .HasPrecision(18, 2);

        builder.Property(charge => charge.CurrencyCode)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(charge => charge.VoidReason)
            .HasMaxLength(1000);

        builder.HasIndex(charge => charge.ViewingBookingId)
            .HasDatabaseName("UX_BookingCharges_ViewingBookingId")
            .IsUnique();

        builder.HasOne(charge => charge.ViewingBooking)
            .WithOne(booking => booking.Charge)
            .HasForeignKey<BookingCharge>(charge => charge.ViewingBookingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(charge => charge.Company)
            .WithMany(company => company.BookingCharges)
            .HasForeignKey(charge => charge.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(charge => charge.Currency)
            .WithMany(currency => currency.BookingCharges)
            .HasForeignKey(charge => charge.CurrencyCode)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(charge => charge.CompanySubscription)
            .WithMany(subscription => subscription.BookingCharges)
            .HasForeignKey(charge => charge.CompanySubscriptionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_BookingCharges_AmountSnapshot_NonNegative",
                "[AmountSnapshot] >= 0");
        });
    }
}
