using FuelTrack.Application.DTOs.Oils;
using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class OilsController : BaseApiController
{
    private readonly IOilService _oilService;
    private readonly IRealtimeNotifier _notifier;

    public OilsController(IOilService oilService, IRealtimeNotifier notifier)
    {
        _oilService = oilService;
        _notifier = notifier;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? stationId = null, [FromQuery] int? subStationId = null)
    {
        if (subStationId.HasValue)
        {
            var filtered = await _oilService.GetAllBySubStationAsync(subStationId.Value);
            return Ok(filtered);
        }

        if (stationId.HasValue)
        {
            var filtered = await _oilService.GetAllByStationAsync(stationId.Value);
            return Ok(filtered);
        }

        var result = await _oilService.GetAllAsync();
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var oil = await _oilService.GetByIdAsync(id);
        if (oil is null) return NotFound();
        return Ok(oil);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOilDto dto)
    {
        var userId = GetUserId();
        var oil = await _oilService.CreateAsync(dto, userId);
        await _notifier.NotifyAllAsync("OilCreated", new { id = oil.Id });
        return CreatedAtAction(nameof(GetById), new { id = oil.Id }, oil);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateOilDto dto)
    {
        var userId = GetUserId();
        var oil = await _oilService.UpdateAsync(id, dto, userId);
        await _notifier.NotifyAllAsync("OilUpdated", new { id });
        return Ok(oil);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();
        await _oilService.DeleteAsync(id, userId);
        await _notifier.NotifyAllAsync("OilDeleted", new { id });
        return NoContent();
    }

    [HttpPost("add-to-generator")]
    public async Task<IActionResult> AddToGenerator([FromBody] AddToGeneratorDto dto)
    {
        var userId = GetUserId();
        var result = await _oilService.AddToGeneratorAsync(dto, userId);
        return Ok(result);
    }
}
