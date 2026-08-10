using EstateHub.Domain.Entities.Bookings;
using EstateHub.Domain.Entities.Discovery;
using EstateHub.Domain.Enums;

namespace EstateHub.Domain.Entities.Users;

public class CustomerProfile
{
    public Guid Id { get; set; }
    public Guid ApplicationUserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public CustomerPersona Persona { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<Favorite> Favorites { get; set; } = [];
    public ICollection<SavedSearch> SavedSearches { get; set; } = [];
    public ICollection<Lead> Leads { get; set; } = [];
    public ICollection<ViewingBooking> ViewingBookings { get; set; } = [];
}
