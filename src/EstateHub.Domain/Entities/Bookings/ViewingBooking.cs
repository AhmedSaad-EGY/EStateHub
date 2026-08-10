using EstateHub.Domain.Entities.Companies;
using EstateHub.Domain.Entities.Discovery;
using EstateHub.Domain.Entities.Users;
using EstateHub.Domain.Enums;

namespace EstateHub.Domain.Entities.Bookings;

public class ViewingBooking
{
    public Guid Id { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public Guid ViewingSlotId { get; set; }
    public Guid CustomerProfileId { get; set; }
    public int VisitorCount { get; set; }
    public string ContactPhone { get; set; } = string.Empty;
    public string? SpecialRequests { get; set; }
    public ViewingBookingStatus Status { get; set; }
    public Guid? AssignedEmployeeId { get; set; }
    public BookingCancellationSource? CancellationSource { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public DateTimeOffset? CheckedInAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public ViewingSlot ViewingSlot { get; set; } = null!;
    public CustomerProfile CustomerProfile { get; set; } = null!;
    public CompanyEmployee? AssignedEmployee { get; set; }
    public ICollection<BookingStatusHistory> StatusHistory { get; set; } = [];
    public ICollection<BookingRescheduleHistory> RescheduleHistory { get; set; } = [];
    public BookingCharge? Charge { get; set; }
    public CompanyReview? Review { get; set; }
    public Lead? SourceLead { get; set; }
}
