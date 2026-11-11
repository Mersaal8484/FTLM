using FuelTrack.Application.DTOs.Maintenance;
using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.API.Controllers;

[Authorize]
[ApiController]
[Route("api/service-replacements")]
public class ServiceReplacementsController : BaseApiController
{
    private readonly IServiceReplacementService _service;

    public ServiceReplacementsController(IServiceReplacementService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> RecordReplacement(
        [FromBody] RecordReplacementRequestDto dto,
        CancellationToken ct = default)
    {
        var result = await _service.RecordReplacementAsync(dto, GetUserId(), ct);
        return result.IsSuccess
            ? StatusCode(result.StatusCode, result.Value)
            : StatusCode(result.StatusCode, new { error = result.Error });
    }

    [HttpPost("bulk-baseline")]
    public async Task<IActionResult> RecordBulkBaseline(
        [FromBody] BulkBaselineRequestDto dto,
        CancellationToken ct = default)
    {
        // The detailed contract preserves per-row failures while allowing valid
        // baseline rows to be committed in the same submission.
        var result = await _service.RecordBulkBaselineDetailedAsync(dto, GetUserId(), ct);
        return result.IsSuccess
            ? StatusCode(result.StatusCode, result.Value)
            : StatusCode(result.StatusCode, new { error = result.Error });
    }

    [HttpPost("multi")]
    public async Task<IActionResult> RecordMultiItemReplacement(
        [FromBody] MultiItemReplacementRequestDto dto,
        CancellationToken ct = default)
    {
        var result = await _service.RecordMultiItemReplacementAsync(dto, GetUserId(), ct);
        return result.IsSuccess
            ? StatusCode(result.StatusCode, result.Value)
            : StatusCode(result.StatusCode, new { error = result.Error });
    }

    [HttpPost("{id:int}/void")]
    public async Task<IActionResult> Void(
        int id,
        [FromBody] VoidReplacementRecordDto dto,
        CancellationToken ct = default)
    {
        var result = await _service.VoidAsync(id, dto, GetUserId(), ct);
        return result.IsSuccess
            ? Ok(new { success = true })
            : StatusCode(result.StatusCode, new { error = result.Error });
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(
        [FromQuery] int generatorId,
        [FromQuery] int? serviceItemId = null,
        CancellationToken ct = default)
    {
        var result = await _service.GetHistoryAsync(generatorId, serviceItemId, ct);
        return Ok(result);
    }
}
