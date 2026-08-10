using EstateHub.Domain.Entities.Bookings;
using EstateHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstateHub.Infrastructure.Persistence.Configurations.Bookings;

public sealed class ViewingBookingConfiguration : IEntityTypeConfiguration<ViewingBooking>
{
    public void Configure(EntityTypeBuilder<ViewingBooking> builder)
    {
        const int pendingStatus = (int)ViewingBookingStatus.Pending;
        const int confirmedStatus = (int)ViewingBookingStatus.Confirmed;
        const int checkedInStatus = (int)ViewingBookingStatus.CheckedIn;

        builder.HasKey(booking => booking.Id);

        builder.Property(booking => booking.BookingCode)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(booking => booking.ContactPhone)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(booking => booking.SpecialRequests)
            .HasMaxLength(4000);

        builder.Property(booking => booking.RowVersion)
            .IsRowVersion();

        builder.HasIndex(booking => booking.BookingCode)
            .HasDatabaseName("UX_ViewingBookings_BookingCode")
            .IsUnique();

        builder.HasIndex(booking => new { booking.ViewingSlotId, booking.CustomerProfileId })
            .HasDatabaseName("UX_ViewingBookings_Active_CustomerSlot")
            .IsUnique()
            .HasFilter($"[Status] IN ({pendingStatus}, {confirmedStatus}, {checkedInStatus})");

        builder.HasOne(booking => booking.ViewingSlot)
            .WithMany(slot => slot.Bookings)
            .HasForeignKey(booking => booking.ViewingSlotId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(booking => booking.CustomerProfile)
            .WithMany(profile => profile.ViewingBookings)
            .HasForeignKey(booking => booking.CustomerProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(booking => booking.AssignedEmployee)
            .WithMany(employee => employee.AssignedBookings)
            .HasForeignKey(booking => booking.AssignedEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_ViewingBookings_VisitorCount_Positive",
                "[VisitorCount] > 0");
        });
    }
}
