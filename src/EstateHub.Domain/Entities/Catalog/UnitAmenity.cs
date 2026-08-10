namespace EstateHub.Domain.Entities.Catalog;

public class UnitAmenity
{
    public Guid UnitId { get; set; }
    public Guid AmenityId { get; set; }

    public Unit Unit { get; set; } = null!;
    public Amenity Amenity { get; set; } = null!;
}
