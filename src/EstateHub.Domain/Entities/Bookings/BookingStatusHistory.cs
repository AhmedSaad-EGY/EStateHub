using EstateHub.Domain.Enums;

namespace EstateHub.Domain.Entities.Bookings;

public class BookingStatusHistory
{
    public Guid Id { get; set; }
    public Guid ViewingBookingId { get; set; }
    public ViewingBookingStatus? FromStatus { get; set; }
    public ViewingBookingStatus ToStatus { get; set; }
    public Guid? ChangedByApplicationUserId { get; set; }
    public BookingActorType ActorType { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
    public string? Reason { get; set; }

    public ViewingBooking ViewingBooking { get; set; } = null!;
}
