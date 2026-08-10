using EstateHub.Domain.Entities.Catalog;
using EstateHub.Domain.Enums;

namespace EstateHub.Domain.Entities.Bookings;

public class ViewingSlot
{
    public Guid Id { get; set; }
    public Guid ListingId { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public int Capacity { get; set; } = 1;
    public string MeetingPoint { get; set; } = string.Empty;
    public ViewingSlotStatus Status { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public Listing Listing { get; set; } = null!;
    public ICollection<ViewingBooking> Bookings { get; set; } = [];
    public ICollection<BookingRescheduleHistory> ReschedulesFrom { get; set; } = [];
    public ICollection<BookingRescheduleHistory> ReschedulesTo { get; set; } = [];
}
