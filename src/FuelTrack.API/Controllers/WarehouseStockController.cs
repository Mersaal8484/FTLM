using FuelTrack.Application.DTOs.Warehouse;
using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.API.Controllers;

[Authorize]
[ApiController]
[Route("api/warehouse/stock")]
public class WarehouseStockController : BaseApiController
{
    private readonly IWarehouseStockService _warehouseStockService;

    public WarehouseStockController(IWarehouseStockService warehouseStockService)
    {
        _warehouseStockService = warehouseStockService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? stationId = null)
    {
        var result = await _warehouseStockService.GetAllAsync(stationId);
        return Ok(result);
    }

    [HttpGet("by-station/{stationId:int}")]
    public async Task<IActionResult> GetByStation(int stationId)
    {
        var result = await _warehouseStockService.GetByStationAsync(stationId);
        return Ok(result);
    }

    [HttpGet("by-part/{partId:int}")]
    public async Task<IActionResult> GetByPart(int partId)
    {
        var result = await _warehouseStockService.GetByPartAsync(partId);
        return Ok(result);
    }

    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStock([FromQuery] int? stationId = null)
    {
        var result = await _warehouseStockService.GetLowStockAsync(stationId);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{partId:int}/adjust")]
    public async Task<IActionResult> AdjustStock(int partId, [FromBody] AdjustStockDto dto)
    {
        var userId = GetUserId();
        await _warehouseStockService.AdjustStockAsync(partId, dto.StationId, dto.Delta, dto.Reason, userId);
        return NoContent();
    }
}
