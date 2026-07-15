using FuelTrack.Application.DTOs.Stations;
using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class StationsController : BaseApiController
{
    private readonly IStationService _stationService;
    private readonly IRealtimeNotifier _notifier;

    public StationsController(IStationService stationService, IRealtimeNotifier notifier)
    {
        _stationService = stationService;
        _notifier = notifier;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStationDto dto)
    {
        var userId = GetUserId();
        var result = await _stationService.CreateAsync(dto, userId);
        if (!result.IsSuccess) return BadRequest(result.Error);
        await _notifier.NotifyAllAsync("StationCreated", new { result.Value?.Id, result.Value?.Name });
        return CreatedAtAction(nameof(GetById), new { id = result.Value?.Id }, result.Value);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateStationDto dto)
    {
        var userId = GetUserId();
        var result = await _stationService.UpdateAsync(id, dto, userId);
        await _notifier.NotifyAllAsync("StationUpdated", new { id });
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _stationService.DeleteAsync(id);
        await _notifier.NotifyAllAsync("StationDeleted", new { id });
        return NoContent();
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _stationService.GetByIdAsync(id);
        if (result is null) return NotFound();
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _stationService.GetAllAsync();
        return Ok(result);
    }

    [HttpGet("by-branch/{branchId:int}")]
    public async Task<IActionResult> GetByBranch(int branchId)
    {
        var result = await _stationService.GetByBranchAsync(branchId);
        return Ok(result);
    }

}
