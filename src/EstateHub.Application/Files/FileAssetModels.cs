using System.IO;
using EstateHub.Application.Common;
using EstateHub.Domain.Enums;

namespace EstateHub.Application.Files;

public sealed record FileAssetUpload(Stream Content, string OriginalFileName, FileType FileType);
public sealed record FileAssetDirectoryQuery(int PageNumber, int PageSize, FileType? FileType, string? Search);
public sealed record FileAssetMetadata(Guid Id, FileType FileType, string? OriginalFileName, string ContentType, long SizeBytes, int? Width, int? Height, DateTimeOffset CreatedAt);
public sealed record FileAssetContent(Stream Content, string ContentType, string DownloadFileName);
public enum FileAssetOperationStatus { Succeeded, InvalidRequest, TooLarge, NotFound, Conflict, ServiceUnavailable }
public sealed record FileAssetResult<T>(FileAssetOperationStatus Status, T? Value = default);
