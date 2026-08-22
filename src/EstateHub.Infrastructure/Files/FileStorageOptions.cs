namespace EstateHub.Infrastructure.Files;

internal sealed record FileStorageOptions(string RootPath, long MaximumImageBytes, long MaximumDocumentBytes);

internal sealed record StoredFile(string StorageKey, string ContentType, string Extension, long SizeBytes);
internal sealed record StagedDeletion(string StorageKey, string TrashPath);
internal enum StorageWriteStatus { Succeeded, InvalidFormat, TooLarge, Unavailable }
internal sealed record StorageWriteResult(StorageWriteStatus Status, StoredFile? File = null);

internal interface ILocalFileStorage
{
    Task<StorageWriteResult> StoreAsync(Stream source, EstateHub.Domain.Enums.FileType fileType, long maximumBytes, CancellationToken cancellationToken);
    Task<Stream?> OpenReadAsync(string storageKey, EstateHub.Domain.Enums.FileType fileType, string contentType, long sizeBytes, CancellationToken cancellationToken);
    Task<bool> DeleteFinalAsync(string storageKey, CancellationToken cancellationToken);
    Task<StagedDeletion?> StageDeletionAsync(string storageKey, CancellationToken cancellationToken);
    Task RestoreDeletionAsync(StagedDeletion staged, CancellationToken cancellationToken);
    Task PurgeDeletionAsync(StagedDeletion staged, CancellationToken cancellationToken);
}
