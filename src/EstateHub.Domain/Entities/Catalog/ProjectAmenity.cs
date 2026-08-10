namespace EstateHub.Domain.Entities.Catalog;

public class ProjectAmenity
{
    public Guid ProjectId { get; set; }
    public Guid AmenityId { get; set; }

    public Project Project { get; set; } = null!;
    public Amenity Amenity { get; set; } = null!;
}
