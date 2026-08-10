namespace EstateHub.Domain.Entities.Bookings;

public class BookingRescheduleHistory
{
    public Guid Id { get; set; }
    public Guid ViewingBookingId { get; set; }
    public Guid FromViewingSlotId { get; set; }
    public Guid ToViewingSlotId { get; set; }
    public Guid? ChangedByApplicationUserId { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
    public string? Reason { get; set; }

    public ViewingBooking ViewingBooking { get; set; } = null!;
    public ViewingSlot FromViewingSlot { get; set; } = null!;
    public ViewingSlot ToViewingSlot { get; set; } = null!;
}
