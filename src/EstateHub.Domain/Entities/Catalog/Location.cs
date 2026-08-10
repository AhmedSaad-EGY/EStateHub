using EstateHub.Domain.Entities.Companies;
using EstateHub.Domain.Enums;

namespace EstateHub.Domain.Entities.Catalog;

public class Location
{
    public Guid Id { get; set; }
    public Guid? ParentLocationId { get; set; }
    public LocationType Type { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    public Location? ParentLocation { get; set; }
    public ICollection<Location> ChildLocations { get; set; } = [];
    public ICollection<Address> Addresses { get; set; } = [];
    public ICollection<Project> Projects { get; set; } = [];
    public ICollection<Unit> Units { get; set; } = [];
}
