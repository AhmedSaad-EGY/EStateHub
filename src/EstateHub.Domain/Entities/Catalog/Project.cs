using EstateHub.Domain.Entities.Companies;
using EstateHub.Domain.Enums;

namespace EstateHub.Domain.Entities.Catalog;

public class Project
{
    public Guid Id { get; set; }
    public Guid DeveloperCompanyId { get; set; }
    public Guid LocationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ProjectDeliveryStatus DeliveryStatus { get; set; }
    public DateOnly? ExpectedDeliveryDate { get; set; }
    public ProjectStatus ProjectStatus { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public Company DeveloperCompany { get; set; } = null!;
    public Location Location { get; set; } = null!;
    public ICollection<Unit> Units { get; set; } = [];
    public ICollection<ProjectAmenity> Amenities { get; set; } = [];
    public ICollection<ProjectMedia> Media { get; set; } = [];
    public ICollection<NearbyPlace> NearbyPlaces { get; set; } = [];
}
