namespace EstateHub.Api.Contracts.Listings;

public sealed class ListingDirectoryRequest
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string? ListingType { get; init; }
    public Guid? LocationId { get; init; }
    public Guid? UnitTypeId { get; init; }
    public Guid? CompanyId { get; init; }
    public Guid? ProjectId { get; init; }
    public string? CurrencyCode { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public int? MinBedrooms { get; init; }
    public int? MaxBedrooms { get; init; }
    public int? MinBathrooms { get; init; }
    public int? MaxBathrooms { get; init; }
    public decimal? MinArea { get; init; }
    public decimal? MaxArea { get; init; }
    public string? Sort { get; init; }
}
