using EstateHub.Domain.Enums;

namespace EstateHub.Domain.Entities.Catalog;

public class Amenity
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public AmenityScope Scope { get; set; }
    public string? IconKey { get; set; }
    public bool IsActive { get; set; }

    public ICollection<ProjectAmenity> Projects { get; set; } = [];
    public ICollection<UnitAmenity> Units { get; set; } = [];
}
