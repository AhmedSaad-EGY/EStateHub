namespace EstateHub.Domain.Entities.Catalog;

public class NearbyPlace
{
    public Guid Id { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? UnitId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal? DistanceMeters { get; set; }
    public int? TravelMinutes { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    public Project? Project { get; set; }
    public Unit? Unit { get; set; }
}
