using EstateHub.Application.Files;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EstateHub.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/files")]
public sealed class FilesController(IFileAssetService service) : ControllerBase
{
    [HttpGet("{fileAssetId}")]
    public async Task<IActionResult> GetPublicImage(Guid fileAssetId, CancellationToken cancellationToken)
    {
        if (fileAssetId == Guid.Empty) return FileNotFound();
        var result = await service.GetPublicImageAsync(fileAssetId, cancellationToken);
        if (result.Status == FileAssetOperationStatus.NotFound) return FileNotFound();
        if (result.Status != FileAssetOperationStatus.Succeeded || result.Value is null) return FileUnavailable();
        Response.Headers.XContentTypeOptions = "nosniff";
        Response.Headers.CacheControl = "public, max-age=3600";
        Response.Headers.ContentDisposition = $"inline; filename=\"{result.Value.DownloadFileName}\"";
        return File(result.Value.Content, result.Value.ContentType, enableRangeProcessing: true);
    }

    private ObjectResult FileNotFound() => Problem(statusCode: StatusCodes.Status404NotFound, title: "File not found.");
    private ObjectResult FileUnavailable() => Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "File operation is temporarily unavailable.");
}
