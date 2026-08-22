using EstateHub.Api.Contracts.Files;
using EstateHub.Application.Files;
using EstateHub.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EstateHub.Api.Controllers;

[ApiController]
[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/me/files")]
public sealed class MyFilesController(IFileAssetService service) : ControllerBase
{
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestFormLimits(MultipartBodyLengthLimit = 22020096)]
    public async Task<ActionResult<FileAssetMetadataResponse>> Upload([FromForm] UploadFileAssetRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (!Request.HasFormContentType || Request.Form.Files.Count != 1 || request.File is null || request.File.Length == 0) ModelState.AddModelError("file", "Exactly one non-empty file is required.");
        if (!TryParseFileType(request.FileType, out var fileType)) ModelState.AddModelError("fileType", "FileType must be Image or Document without surrounding whitespace.");
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        await using var stream = request.File!.OpenReadStream();
        var result = await service.UploadAsync(userId, new FileAssetUpload(stream, request.File.FileName, fileType), cancellationToken);
        return result.Status switch
        {
            FileAssetOperationStatus.Succeeded => CreatedAtAction(nameof(GetMetadata), new { fileAssetId = result.Value!.Id }, FileAssetMetadataResponse.From(result.Value)),
            FileAssetOperationStatus.InvalidRequest => InvalidUpload(),
            FileAssetOperationStatus.TooLarge => FileTooLarge(),
            _ => FileUnavailable()
        };
    }

    [HttpGet]
    public async Task<ActionResult<FileAssetDirectoryResponse>> GetFiles([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] string? fileType = null, [FromQuery] string? search = null, CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (pageNumber is < 1 or > 10000) ModelState.AddModelError("pageNumber", "PageNumber must be between 1 and 10000.");
        if (pageSize is < 1 or > 50) ModelState.AddModelError("pageSize", "PageSize must be between 1 and 50.");
        FileType? type = null;
        if (fileType is not null) { if (TryParseFileType(fileType, out var parsed)) type = parsed; else ModelState.AddModelError("fileType", "FileType must be Image or Document without surrounding whitespace."); }
        var normalizedSearch = search?.Trim();
        if (normalizedSearch?.Length == 0) normalizedSearch = null;
        if (normalizedSearch?.Length > 100) ModelState.AddModelError("search", "Search must not exceed 100 characters.");
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = await service.GetOwnedFilesAsync(userId, new FileAssetDirectoryQuery(pageNumber, pageSize, type, normalizedSearch), cancellationToken);
        return result.Status switch { FileAssetOperationStatus.Succeeded => Ok(FileAssetDirectoryResponse.From(result.Value!)), _ => FileUnavailable() };
    }

    [HttpGet("{fileAssetId}")]
    public async Task<ActionResult<FileAssetMetadataResponse>> GetMetadata(Guid fileAssetId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (fileAssetId == Guid.Empty) { ModelState.AddModelError("fileAssetId", "FileAssetId is required."); return ValidationProblem(ModelState); }
        var result = await service.GetOwnedFileAsync(userId, fileAssetId, cancellationToken);
        return result.Status switch { FileAssetOperationStatus.Succeeded => Ok(FileAssetMetadataResponse.From(result.Value!)), FileAssetOperationStatus.NotFound => FileNotFound(), _ => FileUnavailable() };
    }

    [HttpGet("{fileAssetId}/content")]
    public async Task<IActionResult> GetContent(Guid fileAssetId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (fileAssetId == Guid.Empty) { ModelState.AddModelError("fileAssetId", "FileAssetId is required."); return ValidationProblem(ModelState); }
        var result = await service.GetOwnedContentAsync(userId, fileAssetId, cancellationToken);
        if (result.Status == FileAssetOperationStatus.NotFound) return FileNotFound();
        if (result.Status != FileAssetOperationStatus.Succeeded || result.Value is null) return FileUnavailable();
        Response.Headers.XContentTypeOptions = "nosniff";
        return File(result.Value.Content, result.Value.ContentType, result.Value.DownloadFileName, enableRangeProcessing: true);
    }

    [HttpDelete("{fileAssetId}")]
    public async Task<IActionResult> Delete(Guid fileAssetId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (fileAssetId == Guid.Empty) { ModelState.AddModelError("fileAssetId", "FileAssetId is required."); return ValidationProblem(ModelState); }
        var status = await service.DeleteOwnedFileAsync(userId, fileAssetId, cancellationToken);
        return status switch { FileAssetOperationStatus.Succeeded => NoContent(), FileAssetOperationStatus.NotFound => FileNotFound(), FileAssetOperationStatus.Conflict => FileConflict(), _ => FileUnavailable() };
    }

    private ActionResult InvalidUpload() { ModelState.AddModelError("file", "The uploaded file is invalid or unsupported."); return ValidationProblem(ModelState); }
    private ObjectResult FileTooLarge() => Problem(statusCode: StatusCodes.Status413PayloadTooLarge, title: "The uploaded file is too large.");
    private bool TryGetUserId(out Guid userId) => Guid.TryParse(User.FindFirst("sub")?.Value, out userId) && userId != Guid.Empty;
    private static bool TryParseFileType(string? value, out FileType fileType)
    {
        if (value is not null && value.Length > 0 && value == value.Trim())
        {
            if (string.Equals(value, "Image", StringComparison.OrdinalIgnoreCase)) { fileType = FileType.Image; return true; }
            if (string.Equals(value, "Document", StringComparison.OrdinalIgnoreCase)) { fileType = FileType.Document; return true; }
        }
        fileType = default; return false;
    }
    private ObjectResult FileNotFound() => Problem(statusCode: StatusCodes.Status404NotFound, title: "File not found.");
    private ObjectResult FileConflict() => Problem(statusCode: StatusCodes.Status409Conflict, title: "File cannot be deleted.");
    private ObjectResult FileUnavailable() => Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "File operation is temporarily unavailable.");
}
