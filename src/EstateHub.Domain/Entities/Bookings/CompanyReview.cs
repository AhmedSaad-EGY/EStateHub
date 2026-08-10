using EstateHub.Domain.Enums;

namespace EstateHub.Domain.Entities.Bookings;

public class CompanyReview
{
    public Guid Id { get; set; }
    public Guid ViewingBookingId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public CompanyReviewStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? ModeratedByApplicationUserId { get; set; }
    public DateTimeOffset? ModeratedAt { get; set; }
    public string? ModerationReason { get; set; }

    public ViewingBooking ViewingBooking { get; set; } = null!;
}
