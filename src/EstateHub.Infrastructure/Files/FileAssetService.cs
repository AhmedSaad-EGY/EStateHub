using System.Data;
using System.Data.Common;
using EstateHub.Application.Common;
using EstateHub.Application.Files;
using EstateHub.Domain.Entities.Catalog;
using EstateHub.Domain.Entities.Companies;
using EstateHub.Domain.Enums;
using EstateHub.Infrastructure.Listings;
using EstateHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EstateHub.Infrastructure.Files;

internal sealed class FileAssetService(EstateHubDbContext dbContext, ILocalFileStorage storage, FileStorageOptions options, TimeProvider timeProvider) : IFileAssetService
{
    public async Task<FileAssetResult<FileAssetMetadata>> UploadAsync(Guid applicationUserId, FileAssetUpload upload, CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeFileName(upload.OriginalFileName, out var originalFileName)) return new(FileAssetOperationStatus.InvalidRequest);
        var maximumBytes = upload.FileType == FileType.Image ? options.MaximumImageBytes : options.MaximumDocumentBytes;
        var write = await storage.StoreAsync(upload.Content, upload.FileType, maximumBytes, cancellationToken);
        if (write.Status == StorageWriteStatus.InvalidFormat) return new(FileAssetOperationStatus.InvalidRequest);
        if (write.Status == StorageWriteStatus.TooLarge) return new(FileAssetOperationStatus.TooLarge);
        if (write.Status != StorageWriteStatus.Succeeded || write.File is null) return new(FileAssetOperationStatus.ServiceUnavailable);
        var stored = write.File;
        var entity = new FileAsset { Id = Guid.NewGuid(), UploadedByApplicationUserId = applicationUserId, FileType = upload.FileType, OriginalFileName = originalFileName, StorageKey = stored.StorageKey, ContentType = stored.ContentType, SizeBytes = stored.SizeBytes, Width = null, Height = null, CreatedAt = timeProvider.GetUtcNow() };
        try
        {
            dbContext.Add(entity);
            await dbContext.SaveChangesAsync(cancellationToken);
            return new(FileAssetOperationStatus.Succeeded, Map(entity));
        }
        catch (OperationCanceledException) { await storage.DeleteFinalAsync(stored.StorageKey, CancellationToken.None); throw; }
        catch (DbException) { await storage.DeleteFinalAsync(stored.StorageKey, CancellationToken.None); return new(FileAssetOperationStatus.ServiceUnavailable); }
        catch (DbUpdateException) { await storage.DeleteFinalAsync(stored.StorageKey, CancellationToken.None); return new(FileAssetOperationStatus.ServiceUnavailable); }
        catch { await storage.DeleteFinalAsync(stored.StorageKey, CancellationToken.None); return new(FileAssetOperationStatus.ServiceUnavailable); }
    }

    public async Task<FileAssetResult<PagedResult<FileAssetMetadata>>> GetOwnedFilesAsync(Guid applicationUserId, FileAssetDirectoryQuery query, CancellationToken cancellationToken = default)
    {
        try
        {
            var files = dbContext.Set<FileAsset>().AsNoTracking().Where(item => item.UploadedByApplicationUserId == applicationUserId);
            if (query.FileType is not null) files = files.Where(item => item.FileType == query.FileType.Value);
            if (query.Search is not null) files = files.Where(item => item.OriginalFileName != null && item.OriginalFileName.Contains(query.Search));
            var total = await files.CountAsync(cancellationToken);
            var items = await files.OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.Id).Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize)
                .Select(item => new FileAssetMetadata(item.Id, item.FileType, item.OriginalFileName, item.ContentType, item.SizeBytes, item.Width, item.Height, item.CreatedAt)).ToListAsync(cancellationToken);
            return new(FileAssetOperationStatus.Succeeded, new PagedResult<FileAssetMetadata>(items, query.PageNumber, query.PageSize, total));
        }
        catch (OperationCanceledException) { throw; }
        catch (DbException) { return new(FileAssetOperationStatus.ServiceUnavailable); }
    }

    public async Task<FileAssetResult<FileAssetMetadata>> GetOwnedFileAsync(Guid applicationUserId, Guid fileAssetId, CancellationToken cancellationToken = default)
    {
        try
        {
            var item = await dbContext.Set<FileAsset>().AsNoTracking().Where(file => file.Id == fileAssetId && file.UploadedByApplicationUserId == applicationUserId)
                .Select(file => new FileAssetMetadata(file.Id, file.FileType, file.OriginalFileName, file.ContentType, file.SizeBytes, file.Width, file.Height, file.CreatedAt)).SingleOrDefaultAsync(cancellationToken);
            return item is null ? new(FileAssetOperationStatus.NotFound) : new(FileAssetOperationStatus.Succeeded, item);
        }
        catch (OperationCanceledException) { throw; }
        catch (DbException) { return new(FileAssetOperationStatus.ServiceUnavailable); }
    }

    public async Task<FileAssetResult<FileAssetContent>> GetOwnedContentAsync(Guid applicationUserId, Guid fileAssetId, CancellationToken cancellationToken = default)
    {
        var stored = await GetStoredOwnedAsync(applicationUserId, fileAssetId, cancellationToken);
        if (stored.Status != FileAssetOperationStatus.Succeeded || stored.Value is null) return new(stored.Status);
        var stream = await storage.OpenReadAsync(stored.Value.StorageKey, stored.Value.FileType, stored.Value.ContentType, stored.Value.SizeBytes, cancellationToken);
        if (stream is null) return new(FileAssetOperationStatus.ServiceUnavailable);
        return new(FileAssetOperationStatus.Succeeded, new FileAssetContent(stream, stored.Value.ContentType, SafeDownloadName(stored.Value.OriginalFileName, fileAssetId, stored.Value.ContentType)));
    }

    public async Task<FileAssetResult<FileAssetContent>> GetPublicImageAsync(Guid fileAssetId, CancellationToken cancellationToken = default)
    {
        try
        {
            var now = timeProvider.GetUtcNow();
            var publicListings = dbContext.Set<Listing>().AsNoTracking().WherePublic(now);
            var stored = await dbContext.Set<FileAsset>().AsNoTracking().Where(file => file.Id == fileAssetId && file.FileType == FileType.Image
                    && (file.CompaniesUsingAsLogo.Any(company => company.Status == CompanyStatus.Active && company.VerifiedAt != null)
                        || file.CompaniesUsingAsCover.Any(company => company.Status == CompanyStatus.Active && company.VerifiedAt != null)
                        || file.ProjectMedia.Any(media => media.Project.ProjectStatus == ProjectStatus.Published && media.Project.DeveloperCompany.Status == CompanyStatus.Active && media.Project.DeveloperCompany.VerifiedAt != null && media.Project.DeveloperCompany.CompanyType == CompanyType.Developer)
                        || file.ListingMedia.Any(media => publicListings.Any(listing => listing.Id == media.ListingId))))
                .Select(file => new StoredMetadata(file.StorageKey, file.FileType, file.OriginalFileName, file.ContentType, file.SizeBytes)).SingleOrDefaultAsync(cancellationToken);
            if (stored is null) return new(FileAssetOperationStatus.NotFound);
            var stream = await storage.OpenReadAsync(stored.StorageKey, stored.FileType, stored.ContentType, stored.SizeBytes, cancellationToken);
            if (stream is null) return new(FileAssetOperationStatus.ServiceUnavailable);
            return new(FileAssetOperationStatus.Succeeded, new FileAssetContent(stream, stored.ContentType, SafeDownloadName(null, fileAssetId, stored.ContentType)));
        }
        catch (OperationCanceledException) { throw; }
        catch (DbException) { return new(FileAssetOperationStatus.ServiceUnavailable); }
    }

    public async Task<FileAssetOperationStatus> DeleteOwnedFileAsync(Guid applicationUserId, Guid fileAssetId, CancellationToken cancellationToken = default)
    {
        StagedDeletion? staged = null;
        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var entity = await dbContext.Set<FileAsset>().Where(file => file.Id == fileAssetId && file.UploadedByApplicationUserId == applicationUserId).SingleOrDefaultAsync(cancellationToken);
            if (entity is null) return FileAssetOperationStatus.NotFound;
            var referenced = await dbContext.Set<FileAsset>().AsNoTracking().Where(file => file.Id == fileAssetId)
                .Select(file => file.CompanyDocuments.Any() || file.CompaniesUsingAsLogo.Any() || file.CompaniesUsingAsCover.Any() || file.ProjectMedia.Any() || file.ListingMedia.Any()).SingleAsync(cancellationToken);
            if (referenced) return FileAssetOperationStatus.Conflict;
            staged = await storage.StageDeletionAsync(entity.StorageKey, cancellationToken);
            if (staged is null) return FileAssetOperationStatus.ServiceUnavailable;
            dbContext.Remove(entity);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            await storage.PurgeDeletionAsync(staged, CancellationToken.None);
            return FileAssetOperationStatus.Succeeded;
        }
        catch (OperationCanceledException) { if (staged is not null) await TryRestore(staged); throw; }
        catch (DbUpdateConcurrencyException) { if (staged is not null) await TryRestore(staged); return FileAssetOperationStatus.Conflict; }
        catch (DbUpdateException) { if (staged is not null) await TryRestore(staged); return FileAssetOperationStatus.Conflict; }
        catch (DbException) { if (staged is not null) await TryRestore(staged); return FileAssetOperationStatus.ServiceUnavailable; }
        catch { if (staged is not null) await TryRestore(staged); return FileAssetOperationStatus.ServiceUnavailable; }
    }

    private async Task<FileAssetResult<StoredMetadata>> GetStoredOwnedAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var item = await dbContext.Set<FileAsset>().AsNoTracking().Where(file => file.Id == id && file.UploadedByApplicationUserId == userId)
                .Select(file => new StoredMetadata(file.StorageKey, file.FileType, file.OriginalFileName, file.ContentType, file.SizeBytes)).SingleOrDefaultAsync(cancellationToken);
            return item is null ? new(FileAssetOperationStatus.NotFound) : new(FileAssetOperationStatus.Succeeded, item);
        }
        catch (OperationCanceledException) { throw; }
        catch (DbException) { return new(FileAssetOperationStatus.ServiceUnavailable); }
    }
    private async Task TryRestore(StagedDeletion staged) { try { await storage.RestoreDeletionAsync(staged, CancellationToken.None); } catch { } }
    private static FileAssetMetadata Map(FileAsset item) => new(item.Id, item.FileType, item.OriginalFileName, item.ContentType, item.SizeBytes, item.Width, item.Height, item.CreatedAt);
    internal static bool TryNormalizeFileName(string? submitted, out string normalized)
    {
        var candidate = submitted?.Replace('\\', '/');
        candidate = candidate?[(candidate.LastIndexOf('/') + 1)..].Trim();
        if (string.IsNullOrEmpty(candidate) || candidate.Length > 255 || candidate.Any(char.IsControl)) { normalized = string.Empty; return false; }
        normalized = candidate; return true;
    }
    private static string SafeDownloadName(string? original, Guid id, string contentType) => TryNormalizeFileName(original, out var normalized) ? normalized : $"file-{id:D}{ExtensionFor(contentType)}";
    private static string ExtensionFor(string contentType) => contentType switch { "image/jpeg" => ".jpg", "image/png" => ".png", "image/webp" => ".webp", "application/pdf" => ".pdf", _ => ".bin" };
    private sealed record StoredMetadata(string StorageKey, FileType FileType, string? OriginalFileName, string ContentType, long SizeBytes);
}
