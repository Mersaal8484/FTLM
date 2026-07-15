using FuelTrack.Application.DTOs.SubStations;
using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.API.Controllers;

[Authorize]
[ApiController]
[Route("api/sub-stations")]
public class SubStationsController : BaseApiController
{
    private readonly ISubStationService _subStationService;
    private readonly IRealtimeNotifier _notifier;

    public SubStationsController(ISubStationService subStationService, IRealtimeNotifier notifier)
    {
        _subStationService = subStationService;
        _notifier = notifier;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSubStationDto dto)
    {
        var userId = GetUserId();
        var result = await _subStationService.CreateAsync(dto, userId);
        if (!result.IsSuccess) return BadRequest(result.Error);
        await _notifier.NotifyAllAsync("SubStationCreated", new { result.Value?.Id, result.Value?.Name });
        return CreatedAtAction(nameof(GetById), new { id = result.Value?.Id }, result.Value);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSubStationDto dto)
    {
        var userId = GetUserId();
        var result = await _subStationService.UpdateAsync(id, dto, userId);
        await _notifier.NotifyAllAsync("SubStationUpdated", new { id });
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _subStationService.DeleteAsync(id);
        await _notifier.NotifyAllAsync("SubStationDeleted", new { id });
        return NoContent();
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _subStationService.GetByIdAsync(id);
        if (result is null) return NotFound();
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
    {
        var result = await _subStationService.GetAllAsync(includeInactive);
        return Ok(result);
    }

    [HttpGet("by-station/{stationId:int}")]
    public async Task<IActionResult> GetByStation(int stationId, [FromQuery] bool includeInactive = false)
    {
        var result = await _subStationService.GetByStationAsync(stationId, includeInactive);
        return Ok(result);
    }

}
