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
    private readonly ICommercialStationModeService _commercialMode;

    public StationsController(IStationService stationService, IRealtimeNotifier notifier, ICommercialStationModeService commercialMode)
    {
        _stationService = stationService;
        _notifier = notifier;
        _commercialMode = commercialMode;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStationDto dto)
    {
        if (await _commercialMode.IsCommercialModeAsync()) return NotFound();
        var userId = GetUserId();
        var result = await _stationService.CreateAsync(dto, userId);
        if (!result.IsSuccess) return BadRequest(result.Error);
        await _notifier.NotifyAllAsync("StationCreated", new { result.Value?.Id, result.Value?.Name });
        return CreatedAtAction(nameof(GetById), new { id = result.Value?.Id }, result.Value);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateStationDto dto)
    {
        if (await _commercialMode.IsCommercialModeAsync()) return NotFound();
        var userId = GetUserId();
        var result = await _stationService.UpdateAsync(id, dto, userId);
        await _notifier.NotifyAllAsync("StationUpdated", new { id });
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (await _commercialMode.IsCommercialModeAsync()) return NotFound();
        await _stationService.DeleteAsync(id);
        await _notifier.NotifyAllAsync("StationDeleted", new { id });
        return NoContent();
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        if (await _commercialMode.IsCommercialModeAsync()) return NotFound();
        var result = await _stationService.GetByIdAsync(id);
        if (result is null) return NotFound();
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        if (await _commercialMode.IsCommercialModeAsync()) return NotFound();
        var result = await _stationService.GetAllAsync();
        return Ok(result);
    }

    [HttpGet("by-branch/{branchId:int}")]
    public async Task<IActionResult> GetByBranch(int branchId)
    {
        if (await _commercialMode.IsCommercialModeAsync()) return NotFound();
        var result = await _stationService.GetByBranchAsync(branchId);
        return Ok(result);
    }

}
