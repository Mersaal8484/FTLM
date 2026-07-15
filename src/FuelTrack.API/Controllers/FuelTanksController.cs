using FuelTrack.Application.DTOs.FuelTanks;
using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.API.Controllers;

[Authorize]
[ApiController]
[Route("api/fuel-tanks")]
public class FuelTanksController : BaseApiController
{
    private readonly IFuelTankService _fuelTankService;
    private readonly IRealtimeNotifier _notifier;

    public FuelTanksController(IFuelTankService fuelTankService, IRealtimeNotifier notifier)
    {
        _fuelTankService = fuelTankService;
        _notifier = notifier;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFuelTankDto dto)
    {
        var userId = GetUserId();
        var result = await _fuelTankService.CreateAsync(dto, userId);
        await _notifier.NotifyAllAsync("FuelTankCreated", new { result.Id, result.Name });
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateFuelTankDto dto)
    {
        var userId = GetUserId();
        var result = await _fuelTankService.UpdateAsync(id, dto, userId);
        await _notifier.NotifyAllAsync("FuelTankUpdated", new { id });
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _fuelTankService.DeleteAsync(id);
        await _notifier.NotifyAllAsync("FuelTankDeleted", new { id });
        return NoContent();
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _fuelTankService.GetByIdAsync(id);
        if (result is null) return NotFound();
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _fuelTankService.GetAllAsync();
        return Ok(result);
    }

    [HttpGet("by-substation/{subStationId:int}")]
    public async Task<IActionResult> GetBySubStation(int subStationId)
    {
        var result = await _fuelTankService.GetBySubStationAsync(subStationId);
        return Ok(result);
    }

}
