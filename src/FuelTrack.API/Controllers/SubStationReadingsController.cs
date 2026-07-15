using System;
using System.Security.Claims;
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

    public SubStationReadingsController(ISubStationReadingService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSubStationReadingDto dto)
    {
        try
        {
            var result = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { msg = ex.Message, message = ex.Message });
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
        // Lightweight summary used by the Site Energy Monitoring page.
        // TODO: aggregate generator readings for the given station/date when available.
        return Ok(new { totalProduced = 0 });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateSubStationReadingDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
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
        var userId = GetUserId();
        await _service.ApproveAsync(id, userId);
        return NoContent();
    }

    [HttpPost("{id:int}/reject")]
    public async Task<IActionResult> Reject(int id)
    {
        var userId = GetUserId();
        await _service.RejectAsync(id, userId);
        return NoContent();
    }

}
