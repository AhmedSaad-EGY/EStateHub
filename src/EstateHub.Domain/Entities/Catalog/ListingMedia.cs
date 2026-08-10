using EstateHub.Domain.Entities.Companies;

namespace EstateHub.Domain.Entities.Catalog;

public class ListingMedia
{
    public Guid Id { get; set; }
    public Guid ListingId { get; set; }
    public Guid FileAssetId { get; set; }
    public int SortOrder { get; set; }
    public bool IsCover { get; set; }
    public string? Caption { get; set; }

    public Listing Listing { get; set; } = null!;
    public FileAsset FileAsset { get; set; } = null!;
}
