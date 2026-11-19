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

    [HttpGet("commercial/{report}")]
    public async Task<IActionResult> GetCommercialReport(
        string report,
        [FromServices] ICommercialStationModeService commercial,
        [FromServices] IUserScopeFilterService scopeFilter,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? type,
        [FromQuery] int? year,
        CancellationToken ct)
    {
        var scope = await commercial.GetScopeAsync(ct);
        if (scope is null) return NotFound();
        if (!User.IsInRole("Admin") && !User.IsInRole("SystemOwner") &&
            !await scopeFilter.HasAccessToSubStationAsync(GetUserId(), scope.SubStationId)) return Forbid();

        switch (report.ToLowerInvariant())
        {
            case "stock": return Ok(await _warehouseReportService.GetStockReportAsync(scope.StationId, ct));
            case "lowstock":
                var lowStock = await _warehouseReportService.GetLowStockAlertAsync(ct);
                return Ok(lowStock.Where(item => item.StationId == scope.StationId));
            case "dispatch": return Ok(await _warehouseReportService.GetDispatchBySubStationAsync(scope.SubStationId, startDate, endDate, ct));
            case "movements" when startDate.HasValue && endDate.HasValue:
                return Ok(await _warehouseReportService.GetMovementReportAsync(scope.StationId, startDate.Value, endDate.Value, ct));
            case "supply" when startDate.HasValue && endDate.HasValue:
                return Ok(await _warehouseReportService.GetSupplyReportAsync(scope.StationId, startDate.Value, endDate.Value, ct));
            case "inventory" when !string.IsNullOrWhiteSpace(type):
                return Ok(await _warehouseReportService.GetPeriodicInventoryReportAsync(scope.StationId, type, year ?? DateTime.UtcNow.Year, 0, ct));
            default: return BadRequest(new { error = "Invalid report or missing report filters." });
        }
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
