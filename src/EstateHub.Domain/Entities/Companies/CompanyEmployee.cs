using EstateHub.Domain.Entities.Bookings;
using EstateHub.Domain.Entities.Catalog;
using EstateHub.Domain.Entities.Discovery;
using EstateHub.Domain.Enums;

namespace EstateHub.Domain.Entities.Companies;

public class CompanyEmployee
{
    public Guid Id { get; set; }
    public Guid ApplicationUserId { get; set; }
    public Guid CompanyId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? JobTitle { get; set; }
    public bool IsPrimaryContact { get; set; }
    public CompanyEmployeeStatus Status { get; set; }
    public DateTimeOffset JoinedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public Company Company { get; set; } = null!;
    public ICollection<CompanyEmployeeRole> RoleAssignments { get; set; } = [];
    public ICollection<Listing> CreatedListings { get; set; } = [];
    public ICollection<Lead> OwnedLeads { get; set; } = [];
    public ICollection<CustomerRequirement> ConfirmedRequirements { get; set; } = [];
    public ICollection<CRMActivity> PerformedActivities { get; set; } = [];
    public ICollection<ViewingBooking> AssignedBookings { get; set; } = [];
}
