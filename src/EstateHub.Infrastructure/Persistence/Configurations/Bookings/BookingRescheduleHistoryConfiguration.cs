using EstateHub.Domain.Entities.Bookings;
using EstateHub.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstateHub.Infrastructure.Persistence.Configurations.Bookings;

public sealed class BookingRescheduleHistoryConfiguration : IEntityTypeConfiguration<BookingRescheduleHistory>
{
    public void Configure(EntityTypeBuilder<BookingRescheduleHistory> builder)
    {
        builder.HasKey(history => history.Id);

        builder.Property(history => history.Reason)
            .HasMaxLength(1000);

        builder.HasOne(history => history.ViewingBooking)
            .WithMany(booking => booking.RescheduleHistory)
            .HasForeignKey(history => history.ViewingBookingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(history => history.FromViewingSlot)
            .WithMany(slot => slot.ReschedulesFrom)
            .HasForeignKey(history => history.FromViewingSlotId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(history => history.ToViewingSlot)
            .WithMany(slot => slot.ReschedulesTo)
            .HasForeignKey(history => history.ToViewingSlotId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(history => history.ChangedByApplicationUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_BookingRescheduleHistories_DistinctSlots",
                "[FromViewingSlotId] <> [ToViewingSlotId]");
        });
    }
}
