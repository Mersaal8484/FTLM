using FuelTrack.Application.DTOs.Warehouse;
using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.API.Controllers;

[Authorize]
[ApiController]
[Route("api/warehouse/supply")]
public class WarehouseSupplyController : BaseApiController
{
    private readonly IWarehouseSupplyService _warehouseSupplyService;
    private readonly IWorkflowNotificationService _workflowNotifications;

    public WarehouseSupplyController(IWarehouseSupplyService warehouseSupplyService, IWorkflowNotificationService workflowNotifications)
    {
        _warehouseSupplyService = warehouseSupplyService;
        _workflowNotifications = workflowNotifications;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _warehouseSupplyService.GetAllAsync(page, pageSize);
        return Ok(result);
    }

    [HttpGet("by-station/{stationId:int}")]
    public async Task<IActionResult> GetByStation(int stationId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _warehouseSupplyService.GetByStationAsync(stationId, page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _warehouseSupplyService.GetByIdAsync(id);
        if (result is null)
            return NotFound();
        return Ok(result);
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWarehouseSupplyDto dto)
    {
        var userId = GetUserId();
        var result = await _warehouseSupplyService.CreateAsync(dto, userId);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        await _workflowNotifications.NotifySupplyRequestedAsync(result.Value!.Id, result.Value.StationId, result.Value.SupplyNumber, userId);
        return Ok(result.Value);
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateWarehouseSupplyDto dto)
    {
        var userId = GetUserId();
        var result = await _warehouseSupplyService.UpdateAsync(id, dto, userId);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost("{id:int}/confirm")]
    public async Task<IActionResult> Confirm(int id)
    {
        var userId = GetUserId();
        var result = await _warehouseSupplyService.ConfirmAsync(id, userId);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();
        var result = await _warehouseSupplyService.DeleteAsync(id);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return NoContent();
    }
}
