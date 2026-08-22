using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using EstateHub.Domain.Enums;

namespace EstateHub.Infrastructure.Files;

internal sealed class LocalFileStorage : ILocalFileStorage
{
    private readonly string rootPath;
    private readonly string stagingPath;
    private readonly string trashPath;
    private readonly string objectsPath;

    public LocalFileStorage(string rootPath)
    {
        this.rootPath = Path.GetFullPath(rootPath);
        stagingPath = Path.Combine(this.rootPath, ".staging");
        trashPath = Path.Combine(this.rootPath, ".trash");
        objectsPath = Path.Combine(this.rootPath, "objects");
        Directory.CreateDirectory(stagingPath);
        Directory.CreateDirectory(trashPath);
        Directory.CreateDirectory(objectsPath);
    }

    public async Task<StorageWriteResult> StoreAsync(Stream source, FileType fileType, long maximumBytes, CancellationToken cancellationToken)
    {
        var temporaryPath = Path.Combine(stagingPath, $"{RandomKey()}.tmp");
        try
        {
            long total = 0;
            StorageWriteStatus? rejected = null;
            await using (var destination = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                var buffer = new byte[81920];
                while (true)
                {
                    var read = await source.ReadAsync(buffer, cancellationToken);
                    if (read == 0) break;
                    total += read;
                    if (total > maximumBytes) { rejected = StorageWriteStatus.TooLarge; break; }
                    await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                }
                if (total == 0) rejected = StorageWriteStatus.InvalidFormat;
                if (rejected is null) await destination.FlushAsync(cancellationToken);
            }
            if (rejected is not null) return await FailWrite(temporaryPath, rejected.Value);

            var detected = await DetectAsync(temporaryPath, cancellationToken);
            if (detected is null || (fileType == FileType.Image) != detected.Value.IsImage) return await FailWrite(temporaryPath, StorageWriteStatus.InvalidFormat);
            var storageKey = $"objects/{RandomKey()}{detected.Value.Extension}";
            var finalPath = ResolveStorageKey(storageKey);
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryPath, finalPath);
            return new(StorageWriteStatus.Succeeded, new StoredFile(storageKey, detected.Value.ContentType, detected.Value.Extension, total));
        }
        catch (OperationCanceledException)
        {
            TryDelete(temporaryPath);
            throw;
        }
        catch
        {
            TryDelete(temporaryPath);
            return new(StorageWriteStatus.Unavailable);
        }
    }

    public async Task<Stream?> OpenReadAsync(string storageKey, FileType fileType, string contentType, long sizeBytes, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var path = ResolveStorageKey(storageKey);
            if (!File.Exists(path) || new FileInfo(path).Length != sizeBytes) return null;
            var detected = await DetectAsync(path, cancellationToken);
            if (detected is null || detected.Value.ContentType != contentType || detected.Value.IsImage != (fileType == FileType.Image) || !storageKey.EndsWith(detected.Value.Extension, StringComparison.Ordinal)) return null;
            return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
        }
        catch (OperationCanceledException) { throw; }
        catch { return null; }
    }

    public Task<bool> DeleteFinalAsync(string storageKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try { var path = ResolveStorageKey(storageKey); if (File.Exists(path)) File.Delete(path); return Task.FromResult(!File.Exists(path)); }
        catch { return Task.FromResult(false); }
    }

    public Task<StagedDeletion?> StageDeletionAsync(string storageKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var source = ResolveStorageKey(storageKey);
            if (!File.Exists(source)) return Task.FromResult<StagedDeletion?>(null);
            var staged = Path.Combine(trashPath, $"{RandomKey()}.trash");
            File.Move(source, staged);
            return Task.FromResult<StagedDeletion?>(new StagedDeletion(storageKey, staged));
        }
        catch { return Task.FromResult<StagedDeletion?>(null); }
    }

    public Task RestoreDeletionAsync(StagedDeletion staged, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var destination = ResolveStorageKey(staged.StorageKey);
        if (File.Exists(staged.TrashPath) && !File.Exists(destination)) File.Move(staged.TrashPath, destination);
        return Task.CompletedTask;
    }

    public Task PurgeDeletionAsync(StagedDeletion staged, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        TryDelete(staged.TrashPath);
        return Task.CompletedTask;
    }

    internal static string ResolveConfiguredRoot(string configuredRoot, string contentRoot, string webRoot)
    {
        if (string.IsNullOrWhiteSpace(configuredRoot)) throw new InvalidOperationException("FileStorage:RootPath is required.");
        var content = Path.GetFullPath(contentRoot);
        var root = Path.IsPathFullyQualified(configuredRoot) ? Path.GetFullPath(configuredRoot) : Path.GetFullPath(Path.Combine(content, configuredRoot));
        var volumeRoot = Path.GetPathRoot(root);
        if (string.IsNullOrWhiteSpace(volumeRoot) || PathEquals(root, volumeRoot) || PathEquals(root, content)) throw new InvalidOperationException("FileStorage:RootPath is unsafe.");
        if (!Path.IsPathFullyQualified(configuredRoot) && !IsWithin(root, content)) throw new InvalidOperationException("Relative FileStorage:RootPath must remain under the content root.");
        var web = Path.GetFullPath(webRoot);
        if (PathEquals(root, web) || IsWithin(root, web)) throw new InvalidOperationException("FileStorage:RootPath cannot be inside wwwroot.");
        return root;
    }

    private string ResolveStorageKey(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey) || Path.IsPathFullyQualified(storageKey)) throw new InvalidOperationException("Invalid storage key.");
        var normalized = storageKey.Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(rootPath, normalized));
        if (!IsWithin(fullPath, rootPath) || !IsWithin(fullPath, objectsPath)) throw new InvalidOperationException("Unsafe storage key.");
        return fullPath;
    }

    private static async Task<DetectedFormat?> DetectAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
        var length = stream.Length;
        var prefix = new byte[Math.Min(16, (int)length)];
        await stream.ReadExactlyAsync(prefix, cancellationToken);
        var tailLength = (int)Math.Min(1024, length);
        var tail = new byte[tailLength];
        stream.Seek(-tailLength, SeekOrigin.End);
        await stream.ReadExactlyAsync(tail, cancellationToken);

        if (prefix.Length >= 3 && prefix[0] == 0xFF && prefix[1] == 0xD8 && prefix[2] == 0xFF && tail.Length >= 2 && tail[^2] == 0xFF && tail[^1] == 0xD9) return new(true, ".jpg", "image/jpeg");
        ReadOnlySpan<byte> pngHeader = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        ReadOnlySpan<byte> pngEnd = [0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82];
        if (prefix.AsSpan().StartsWith(pngHeader) && tail.AsSpan().EndsWith(pngEnd)) return new(true, ".png", "image/png");
        if (prefix.Length >= 16 && Encoding.ASCII.GetString(prefix, 0, 4) == "RIFF" && Encoding.ASCII.GetString(prefix, 8, 4) == "WEBP" && BinaryPrimitives.ReadUInt32LittleEndian(prefix.AsSpan(4, 4)) + 8 == length && Encoding.ASCII.GetString(prefix, 12, 4) is "VP8 " or "VP8L" or "VP8X") return new(true, ".webp", "image/webp");
        if (prefix.Length >= 5 && Encoding.ASCII.GetString(prefix, 0, 5) == "%PDF-" && Encoding.ASCII.GetString(tail).TrimEnd('\0', '\t', '\r', '\n', ' ').EndsWith("%%EOF", StringComparison.Ordinal)) return new(false, ".pdf", "application/pdf");
        return null;
    }

    private static string RandomKey() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
    private static Task<StorageWriteResult> FailWrite(string path, StorageWriteStatus status) { TryDelete(path); return Task.FromResult(new StorageWriteResult(status)); }
    private static void TryDelete(string path) { try { if (File.Exists(path)) File.Delete(path); } catch { } }
    private static bool PathEquals(string left, string right) => string.Equals(Path.TrimEndingDirectorySeparator(left), Path.TrimEndingDirectorySeparator(right), StringComparison.OrdinalIgnoreCase);
    private static bool IsWithin(string path, string parent) => Path.GetFullPath(path).StartsWith(Path.TrimEndingDirectorySeparator(Path.GetFullPath(parent)) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    private readonly record struct DetectedFormat(bool IsImage, string Extension, string ContentType);
}
