using EstateHub.Domain.Enums;

namespace EstateHub.Domain.Entities.Companies;

public class CompanyDocument
{
    public Guid Id { get; set; }
    public Guid CompanyApplicationId { get; set; }
    public Guid FileAssetId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public DocumentVerificationStatus VerificationStatus { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public Guid? ReviewedByApplicationUserId { get; set; }
    public string? RejectionReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public CompanyApplication CompanyApplication { get; set; } = null!;
    public FileAsset FileAsset { get; set; } = null!;
}
