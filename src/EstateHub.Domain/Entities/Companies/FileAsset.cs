using EstateHub.Domain.Entities.Catalog;
using EstateHub.Domain.Enums;

namespace EstateHub.Domain.Entities.Companies;

public class FileAsset
{
    public Guid Id { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    public FileType FileType { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public Guid UploadedByApplicationUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<CompanyDocument> CompanyDocuments { get; set; } = [];
    public ICollection<Company> CompaniesUsingAsLogo { get; set; } = [];
    public ICollection<Company> CompaniesUsingAsCover { get; set; } = [];
    public ICollection<ProjectMedia> ProjectMedia { get; set; } = [];
    public ICollection<ListingMedia> ListingMedia { get; set; } = [];
}
