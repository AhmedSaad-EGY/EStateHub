using EstateHub.Domain.Entities.Bookings;
using EstateHub.Domain.Entities.Companies;
using EstateHub.Domain.Entities.Users;
using EstateHub.Domain.Enums;

namespace EstateHub.Domain.Entities.Discovery;

public class Lead
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid? CustomerProfileId { get; set; }
    public Guid? SourceBookingId { get; set; }
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    public string Source { get; set; } = string.Empty;
    public LeadStage Stage { get; set; }
    public LeadPriority Priority { get; set; }
    public Guid? OwnerEmployeeId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastActivityAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public Company Company { get; set; } = null!;
    public CustomerProfile? CustomerProfile { get; set; }
    public ViewingBooking? SourceBooking { get; set; }
    public CompanyEmployee? OwnerEmployee { get; set; }
    public ICollection<CustomerRequirement> Requirements { get; set; } = [];
    public ICollection<LeadInterest> Interests { get; set; } = [];
    public ICollection<CRMActivity> Activities { get; set; } = [];
}
