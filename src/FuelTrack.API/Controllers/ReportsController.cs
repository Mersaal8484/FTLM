using FuelTrack.Application.Interfaces;
using FuelTrack.Application.DTOs.Reports;
using FuelTrack.API.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;

namespace FuelTrack.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("ApiRead")]
public class ReportsController : BaseApiController
{
    private readonly IReportService _service;
    private readonly IHubContext<DashboardHub> _hub;

    public ReportsController(IReportService service, IHubContext<DashboardHub> hub)
    {
        _service = service;
        _hub = hub;
    }

    // ─── Dashboard ────────────────────────────────────────────────────────

    [HttpGet("dashboard-stats")]
    [OutputCache(PolicyName = "Dashboard")]
    public async Task<IActionResult> GetDashboardStats(
        [FromQuery] int? branchId,
        [FromQuery] int? stationId,
        [FromQuery] int? subStationId,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo)
    {
        var result = await _service.GetDashboardStatsAsync(branchId, stationId, subStationId, dateFrom, dateTo);
        return Ok(result);
    }

    [HttpGet("fuel-oil-dashboard")]
    [OutputCache(PolicyName = "Dashboard")]
    public async Task<IActionResult> GetFuelOilDashboard(
        [FromQuery] int? branchId,
        [FromQuery] int? stationId,
        [FromQuery] int? subStationId,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo)
    {
        var result = await _service.GetFuelOilDashboardAsync(branchId, stationId, subStationId, dateFrom, dateTo);
        return Ok(result);
    }

    // ─── Specialized Reports ──────────────────────────────────────────────

    [HttpGet("generators")]
    public async Task<IActionResult> GetGeneratorsReport(
        [FromQuery] int? branchId,
        [FromQuery] int? stationId,
        [FromQuery] int? subStationId,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await _service.GetGeneratorsReportAsync(branchId, stationId, subStationId, dateFrom, dateTo, page, pageSize);
        return Ok(result);
    }

    [HttpGet("station-readings")]
    public async Task<IActionResult> GetStationReadingsReport(
        [FromQuery] int? branchId,
        [FromQuery] int? stationId,
        [FromQuery] int? subStationId,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await _service.GetStationReadingsReportAsync(branchId, stationId, subStationId, dateFrom, dateTo, page, pageSize);
        return Ok(result);
    }

    [HttpGet("fuel-transactions")]
    public async Task<IActionResult> GetFuelTransactionsReport(
        [FromQuery] int? branchId,
        [FromQuery] int? stationId,
        [FromQuery] int? subStationId,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await _service.GetFuelTransactionsReportAsync(branchId, stationId, subStationId, dateFrom, dateTo, page, pageSize);
        return Ok(result);
    }

    [HttpGet("oil-transactions")]
    public async Task<IActionResult> GetOilTransactionsReport(
        [FromQuery] int? branchId,
        [FromQuery] int? stationId,
        [FromQuery] int? subStationId,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await _service.GetOilTransactionsReportAsync(branchId, stationId, subStationId, dateFrom, dateTo, page, pageSize);
        return Ok(result);
    }

    [HttpGet("periodic-inventory")]
    public async Task<IActionResult> GetPeriodicInventoryReport(
        [FromQuery] int? branchId,
        [FromQuery] int? stationId,
        [FromQuery] int? subStationId,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await _service.GetPeriodicInventoryReportAsync(branchId, stationId, subStationId, dateFrom, dateTo, page, pageSize);
        return Ok(result);
    }

    [HttpGet("efficiency")]
    public async Task<IActionResult> GetEfficiencyReport(
        [FromQuery] int? branchId,
        [FromQuery] int? stationId,
        [FromQuery] int? subStationId,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await _service.GetEfficiencyReportAsync(branchId, stationId, subStationId, dateFrom, dateTo, page, pageSize);
        return Ok(result);
    }

    // ─── SignalR Push ─────────────────────────────────────────────────────

    [HttpPost("push-stats")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> PushDashboardStats()
    {
        var stats = await _service.GetDashboardStatsAsync(null, null, null, null, null);
        await _hub.Clients.Group("dashboard").SendAsync("DashboardStatsUpdated", stats);
        return Ok(new { message = "Dashboard stats pushed to all connected clients." });
    }
}
