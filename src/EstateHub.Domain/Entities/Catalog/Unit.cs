using EstateHub.Domain.Entities.Companies;
using EstateHub.Domain.Enums;

namespace EstateHub.Domain.Entities.Catalog;

public class Unit
{
    public Guid Id { get; set; }
    public Guid ManagingCompanyId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid LocationId { get; set; }
    public Guid UnitTypeId { get; set; }
    public string UnitCode { get; set; } = string.Empty;
    public FinishingType? FinishingType { get; set; }
    public int Bedrooms { get; set; }
    public int Bathrooms { get; set; }
    public int? FloorNumber { get; set; }
    public int? TotalFloors { get; set; }
    public decimal BuiltUpArea { get; set; }
    public decimal? LandArea { get; set; }
    public FurnishedStatus FurnishedStatus { get; set; }
    public UnitStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public Company ManagingCompany { get; set; } = null!;
    public Project? Project { get; set; }
    public Location Location { get; set; } = null!;
    public UnitType UnitType { get; set; } = null!;
    public ICollection<Listing> Listings { get; set; } = [];
    public ICollection<UnitAmenity> Amenities { get; set; } = [];
    public ICollection<NearbyPlace> NearbyPlaces { get; set; } = [];
}
