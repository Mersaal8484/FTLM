using FuelTrack.Application.DTOs.Inventory;
using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class InventoryController : BaseApiController
{
    private readonly IPeriodicInventoryService _service;
    private readonly IRealtimeNotifier _notifier;

    public InventoryController(IPeriodicInventoryService service, IRealtimeNotifier notifier)
    {
        _service = service;
        _notifier = notifier;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var result = await _service.GetAllAsync();
        var total = result.Count;
        var paged = result.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Ok(new
        {
            data = paged,
            pagination = new { total, page, pageSize, pages = (int)Math.Ceiling(total / (double)pageSize) }
        });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result is null) return NotFound();
        return Ok(result);
    }

    [HttpGet("by-substation/{subStationId:int}")]
    public async Task<IActionResult> GetBySubStation(int subStationId)
    {
        var result = await _service.GetBySubStationAsync(subStationId);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePeriodicInventoryDto dto)
    {
        var userId = GetUserId();
        var result = await _service.CreateAsync(dto, userId);
        await _notifier.NotifyAllAsync("InventoryCreated", result);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPost("{id:int}/approve")]
    [Authorize(Policy = "AdminOrManager")]
    public async Task<IActionResult> Approve(int id)
    {
        var userId = GetUserId();
        await _service.ApproveAsync(id, userId);
        await _notifier.NotifyAllAsync("InventoryApproved", new { Id = id });
        return Ok(new { message = "Inventory approved successfully" });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        await _notifier.NotifyAllAsync("InventoryDeleted", new { Id = id });
        return NoContent();
    }
}
