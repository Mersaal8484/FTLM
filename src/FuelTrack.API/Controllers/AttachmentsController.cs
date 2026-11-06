using FuelTrack.Application.DTOs.Operations;
using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.API.Controllers;

[Authorize]
[ApiController]
[Route("api/attachments")]
public sealed class AttachmentsController(IAttachmentService service) : BaseApiController
{
    [HttpPost]
    [RequestSizeLimit(75 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 70 * 1024)]
    public async Task<IActionResult> Upload([FromForm] IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null) return BadRequest(new { error = "An image file is required." });

        await using var content = new MemoryStream();
        await file.CopyToAsync(content, cancellationToken);
        var result = await service.StoreAsync(
            new UploadAttachmentDto(content.ToArray(), file.ContentType, file.FileName),
            GetUserId(),
            cancellationToken);
        return result.IsSuccess
            ? StatusCode(result.StatusCode, result.Value)
            : StatusCode(result.StatusCode, new { error = result.Error });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Download(int id, CancellationToken cancellationToken)
    {
        var attachment = await service.OpenReadAsync(id, cancellationToken);
        if (attachment is null) return NotFound();
        Response.Headers.ContentDisposition = $"inline; filename=\"{attachment.Value.Metadata.OriginalFileName.Replace("\"", string.Empty)}\"";
        Response.Headers.XContentTypeOptions = "nosniff";
        return File(attachment.Value.Content, attachment.Value.Metadata.ContentType);
    }
}
