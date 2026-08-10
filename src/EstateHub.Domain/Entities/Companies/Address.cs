using EstateHub.Domain.Entities.Catalog;

namespace EstateHub.Domain.Entities.Companies;

public class Address
{
    public Guid Id { get; set; }
    public Guid LocationId { get; set; }
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string? PostalCode { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    public Location Location { get; set; } = null!;
    public Company? Company { get; set; }
}
