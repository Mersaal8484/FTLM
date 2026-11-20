using FuelTrack.Application.DTOs.Stocktakes;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.API.Controllers;

[Authorize]
[ApiController]
[Route("api/stocktakes")]
public sealed class StocktakesController(IStationStocktakeService stocktakes, IAttachmentService attachments) : BaseApiController
{
    [HttpGet("draft")]
    public async Task<IActionResult> Draft([FromQuery] DateOnly date, CancellationToken cancellationToken)
    {
        var result = await stocktakes.GetDraftAsync(date == default ? DateOnly.FromDateTime(DateTime.UtcNow) : date, GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(result.StatusCode, new { error = result.Error });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStationStocktakeDto dto, CancellationToken cancellationToken)
    {
        var result = await stocktakes.CreateAsync(dto, GetUserId(), cancellationToken);
        return result.IsSuccess ? StatusCode(result.StatusCode, result.Value) : StatusCode(result.StatusCode, new { error = result.Error });
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] StocktakeStatus? status, [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken cancellationToken = default)
    {
        var result = await stocktakes.ListAsync(status, fromDate, toDate, page, pageSize, GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(result.StatusCode, new { error = result.Error });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var result = await stocktakes.GetAsync(id, GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(result.StatusCode, new { error = result.Error });
    }

    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id, CancellationToken cancellationToken)
    {
        var result = await stocktakes.ApproveAsync(id, GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(result.StatusCode, new { error = result.Error });
    }

    [HttpPost("{id:int}/reject")]
    public async Task<IActionResult> Reject(int id, [FromBody] RejectStationStocktakeDto dto, CancellationToken cancellationToken)
    {
        var result = await stocktakes.RejectAsync(id, dto.Reason, GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(result.StatusCode, new { error = result.Error });
    }

    [HttpGet("{id:int}/photos/{attachmentId:int}")]
    public async Task<IActionResult> Photo(int id, int attachmentId, CancellationToken cancellationToken)
    {
        var access = await stocktakes.GetPhotoAsync(id, attachmentId, GetUserId(), cancellationToken);
        if (!access.IsSuccess) return StatusCode(access.StatusCode, new { error = access.Error });
        var photo = await attachments.OpenReadAsync(attachmentId, cancellationToken);
        return photo is null ? NotFound() : File(photo.Value.Content, photo.Value.Metadata.ContentType, photo.Value.Metadata.OriginalFileName);
    }
}
