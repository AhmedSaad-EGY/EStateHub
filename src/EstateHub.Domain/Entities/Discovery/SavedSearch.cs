using EstateHub.Domain.Entities.Users;

namespace EstateHub.Domain.Entities.Discovery;

public class SavedSearch
{
    public Guid Id { get; set; }
    public Guid CustomerProfileId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FilterJson { get; set; } = string.Empty;
    public int FilterSchemaVersion { get; set; }
    public bool AlertsEnabled { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public CustomerProfile CustomerProfile { get; set; } = null!;
}
