using EstateHub.Application.Common;
using EstateHub.Application.Files;
using Microsoft.AspNetCore.Http;

namespace EstateHub.Api.Contracts.Files;

public sealed class UploadFileAssetRequest
{
    public IFormFile? File { get; set; }
    public string? FileType { get; set; }
}

public sealed record FileAssetMetadataResponse(Guid Id, string FileType, string? OriginalFileName, string ContentType, long SizeBytes, int? Width, int? Height, DateTimeOffset CreatedAt)
{
    public static FileAssetMetadataResponse From(FileAssetMetadata item) => new(item.Id, item.FileType switch { EstateHub.Domain.Enums.FileType.Image => "Image", EstateHub.Domain.Enums.FileType.Document => "Document", _ => throw new ArgumentOutOfRangeException(nameof(item.FileType), item.FileType, "Unknown file type.") }, item.OriginalFileName, item.ContentType, item.SizeBytes, item.Width, item.Height, item.CreatedAt);
}

public sealed record FileAssetDirectoryResponse(IReadOnlyList<FileAssetMetadataResponse> Items, int PageNumber, int PageSize, int TotalCount, int TotalPages, bool HasPreviousPage, bool HasNextPage)
{
    public static FileAssetDirectoryResponse From(PagedResult<FileAssetMetadata> result) => new(result.Items.Select(FileAssetMetadataResponse.From).ToList(), result.PageNumber, result.PageSize, result.TotalCount, result.TotalPages, result.HasPreviousPage, result.HasNextPage);
}
