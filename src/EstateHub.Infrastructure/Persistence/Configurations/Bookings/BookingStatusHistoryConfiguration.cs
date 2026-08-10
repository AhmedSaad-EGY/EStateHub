using EstateHub.Domain.Entities.Bookings;
using EstateHub.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstateHub.Infrastructure.Persistence.Configurations.Bookings;

public sealed class BookingStatusHistoryConfiguration : IEntityTypeConfiguration<BookingStatusHistory>
{
    public void Configure(EntityTypeBuilder<BookingStatusHistory> builder)
    {
        builder.HasKey(history => history.Id);

        builder.Property(history => history.Reason)
            .HasMaxLength(1000);

        builder.HasOne(history => history.ViewingBooking)
            .WithMany(booking => booking.StatusHistory)
            .HasForeignKey(history => history.ViewingBookingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(history => history.ChangedByApplicationUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
