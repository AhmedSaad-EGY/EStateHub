using EstateHub.Application.Common;

namespace EstateHub.Application.Files;

public interface IFileAssetService
{
    Task<FileAssetResult<FileAssetMetadata>> UploadAsync(Guid applicationUserId, FileAssetUpload upload, CancellationToken cancellationToken = default);
    Task<FileAssetResult<PagedResult<FileAssetMetadata>>> GetOwnedFilesAsync(Guid applicationUserId, FileAssetDirectoryQuery query, CancellationToken cancellationToken = default);
    Task<FileAssetResult<FileAssetMetadata>> GetOwnedFileAsync(Guid applicationUserId, Guid fileAssetId, CancellationToken cancellationToken = default);
    Task<FileAssetResult<FileAssetContent>> GetOwnedContentAsync(Guid applicationUserId, Guid fileAssetId, CancellationToken cancellationToken = default);
    Task<FileAssetResult<FileAssetContent>> GetPublicImageAsync(Guid fileAssetId, CancellationToken cancellationToken = default);
    Task<FileAssetOperationStatus> DeleteOwnedFileAsync(Guid applicationUserId, Guid fileAssetId, CancellationToken cancellationToken = default);
}
