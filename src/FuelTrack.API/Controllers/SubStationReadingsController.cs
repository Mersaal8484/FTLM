using System;
using System.Security.Claims;
using FuelTrack.Application.DTOs.Readings;
using FuelTrack.Application.DTOs.SubStationReadings;
using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.API.Controllers;

[Authorize]
[ApiController]
[Route("api/substation-readings")]
public class SubStationReadingsController : BaseApiController
{
    private readonly ISubStationReadingService _service;
    private readonly IRealtimeNotifier _notifier;

    public SubStationReadingsController(ISubStationReadingService service, IRealtimeNotifier notifier)
    {
        _service = service;
        _notifier = notifier;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSubStationReadingDto dto)
    {
        try
        {
            var result = await _service.CreateAsync(dto);
            await _notifier.NotifyAllAsync("SubStationReadingCreated", new { result.Id, result.SubStationId });
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            return BadRequest(new Dictionary<string, string>
            {
                ["msg"] = ex.Message,
                ["message"] = ex.Message
            });
        }
    }

    [HttpGet("last/{stationId:int}")]
    public async Task<IActionResult> GetLast(int stationId, [FromQuery] int? subStation = null)
    {
        var latest = await _service.GetLastByStationAsync(stationId, subStation);
        if (latest is null) return Ok(null);

        return Ok(latest);
    }

    [HttpGet("production-summary")]
    public async Task<IActionResult> GetProductionSummary(
        [FromQuery] int stationId, [FromQuery] string date, [FromQuery] int? subStation = null)
    {
        var targetDate = DateTime.TryParse(date, out var d) ? d : DateTime.UtcNow.Date;
        var result = await _service.GetProductionSummaryAsync(stationId, targetDate, subStation);
        return Ok(result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateSubStationReadingDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        await _notifier.NotifyAllAsync("SubStationReadingUpdated", new { result.Id, result.SubStationId });
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result is null) return NotFound();
        return Ok(result);
    }

    [HttpGet("by-station/{stationId:int}")]
    public async Task<IActionResult> GetByStation(int stationId)
    {
        var result = await _service.GetByStationAsync(stationId);
        return Ok(result);
    }

    [HttpGet("pending")]
    public async Task<IActionResult> GetPending()
    {
        var result = await _service.GetPendingAsync();
        return Ok(result);
    }

    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        try
        {
            var userId = GetUserId();
            await _service.ApproveAsync(id, userId);
            await _notifier.NotifyAllAsync("SubStationReadingApproved", new { Id = id });
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new Dictionary<string, string>
            {
                ["msg"] = ex.Message,
                ["message"] = ex.Message
            });
        }
    }

    [HttpPost("{id:int}/reject")]
    public async Task<IActionResult> Reject(int id, [FromBody] RejectReadingDto? dto = null)
    {
        try
        {
            var userId = GetUserId();
            await _service.RejectAsync(id, userId, dto?.RejectionReason);
            await _notifier.NotifyAllAsync("SubStationReadingRejected", new { Id = id, Reason = dto?.RejectionReason });
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new Dictionary<string, string>
            {
                ["msg"] = ex.Message,
                ["message"] = ex.Message
            });
        }
    }

}
