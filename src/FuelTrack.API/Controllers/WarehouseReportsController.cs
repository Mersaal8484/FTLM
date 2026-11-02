using FuelTrack.Application.DTOs.Warehouse;
using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.API.Controllers;

[Authorize]
[ApiController]
[Route("api/warehouse/reports")]
public class WarehouseReportsController : BaseApiController
{
    private readonly IWarehouseReportService _warehouseReportService;

    public WarehouseReportsController(IWarehouseReportService warehouseReportService)
    {
        _warehouseReportService = warehouseReportService;
    }

    [HttpGet("stock")]
    public async Task<IActionResult> GetStockReport([FromQuery] int? stationId = null)
    {
        var result = await _warehouseReportService.GetStockReportAsync(stationId);
        return Ok(result);
    }

    [HttpGet("movements")]
    public async Task<IActionResult> GetMovementReport(
        [FromQuery] int stationId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        var result = await _warehouseReportService.GetMovementReportAsync(stationId, startDate, endDate);
        return Ok(result);
    }

    [HttpGet("low-stock-alert")]
    public async Task<IActionResult> GetLowStockAlert()
    {
        var result = await _warehouseReportService.GetLowStockAlertAsync();
        return Ok(result);
    }

    [HttpGet("by-generator/{generatorId:int}")]
    public async Task<IActionResult> GetDispatchByGenerator(int generatorId)
    {
        var result = await _warehouseReportService.GetDispatchByGeneratorAsync(generatorId, null, null);
        return Ok(result);
    }

    [HttpGet("by-substation/{subStationId:int}")]
    public async Task<IActionResult> GetDispatchBySubStation(int subStationId)
    {
        var result = await _warehouseReportService.GetDispatchBySubStationAsync(subStationId, null, null);
        return Ok(result);
    }

    [HttpGet("supply")]
    public async Task<IActionResult> GetSupplyReport(
        [FromQuery] int stationId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        var result = await _warehouseReportService.GetSupplyReportAsync(stationId, startDate, endDate);
        return Ok(result);
    }

    [HttpGet("periodic")]
    public async Task<IActionResult> GetPeriodicInventory(
        [FromQuery] int stationId,
        [FromQuery] string type,
        [FromQuery] int year,
        [FromQuery] int period)
    {
        var result = await _warehouseReportService.GetPeriodicInventoryReportAsync(stationId, type, year, period);
        return Ok(result);
    }
}
