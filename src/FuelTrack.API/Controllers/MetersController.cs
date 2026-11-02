using FuelTrack.Application.DTOs.Meters;
using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class MetersController : BaseApiController
{
    private readonly IMeterService _service;
    private readonly IRealtimeNotifier _notifier;

    public MetersController(IMeterService service, IRealtimeNotifier notifier)
    {
        _service = service;
        _notifier = notifier;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMeterDto dto)
    {
        var result = await _service.CreateAsync(dto);
        await _notifier.NotifyAllAsync("MeterCreated", new { id = result.Id });
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateMeterDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        await _notifier.NotifyAllAsync("MeterUpdated", new { id });
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result is null) return NotFound();
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAllAsync();
        return Ok(result);
    }

    [HttpGet("by-substation/{subStationId:int}")]
    public async Task<IActionResult> GetBySubStation(int subStationId)
    {
        var result = await _service.GetBySubStationAsync(subStationId);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        await _notifier.NotifyAllAsync("MeterDeleted", new { id });
        return NoContent();
    }
}
