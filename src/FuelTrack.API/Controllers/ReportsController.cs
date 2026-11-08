using FuelTrack.Application.Interfaces;
using FuelTrack.Application.DTOs.Reports;
using FuelTrack.API.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using FuelTrack.API.Services;

namespace FuelTrack.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("ApiRead")]
public class ReportsController : BaseApiController
{
    private readonly IReportService _service;
    private readonly IHubContext<DashboardHub> _hub;
    private readonly IConfiguration _configuration;
    private readonly ITenantRequestContext? _tenant;

    public ReportsController(IReportService service, IHubContext<DashboardHub> hub, IConfiguration configuration, ITenantRequestContext? tenant = null)
    {
        _service = service;
        _hub = hub;
        _configuration = configuration;
        _tenant = tenant;
    }

    // ─── Dashboard ────────────────────────────────────────────────

    [HttpGet("dashboard-stats")]
    [OutputCache(PolicyName = "Dashboard")]
    public async Task<IActionResult> GetDashboardStats(
        [FromQuery] int? branchId,
        [FromQuery] int? stationId,
        [FromQuery] int? subStationId,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] string timeZoneId = "Asia/Riyadh")
    {
        var result = await _service.GetDashboardStatsAsync(branchId, stationId, subStationId, dateFrom, dateTo, timeZoneId);
        return Ok(result);
    }

    [HttpGet("fuel-oil-dashboard")]
    [OutputCache(PolicyName = "Dashboard")]
    public async Task<IActionResult> GetFuelOilDashboard(
        [FromQuery] int? branchId,
        [FromQuery] int? stationId,
        [FromQuery] int? subStationId,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] string timeZoneId = "Asia/Riyadh")
    {
        var result = await _service.GetFuelOilDashboardAsync(branchId, stationId, subStationId, dateFrom, dateTo, timeZoneId);
        return Ok(result);
    }

    // ─── Specialized Reports ──────────────────────────────────────

    [HttpGet("generators")]
    public async Task<IActionResult> GetGeneratorsReport(
        [FromQuery] int? branchId,
        [FromQuery] int? stationId,
        [FromQuery] int? subStationId,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string timeZoneId = "Asia/Riyadh")
    {
        var result = await _service.GetGeneratorsReportAsync(branchId, stationId, subStationId, dateFrom, dateTo, page, pageSize, timeZoneId);
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
        [FromQuery] int pageSize = 50,
        [FromQuery] string timeZoneId = "Asia/Riyadh")
    {
        var result = await _service.GetStationReadingsReportAsync(branchId, stationId, subStationId, dateFrom, dateTo, page, pageSize, timeZoneId);
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
        [FromQuery] int pageSize = 50,
        [FromQuery] string timeZoneId = "Asia/Riyadh")
    {
        var result = await _service.GetFuelTransactionsReportAsync(branchId, stationId, subStationId, dateFrom, dateTo, page, pageSize, timeZoneId);
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
        [FromQuery] int pageSize = 50,
        [FromQuery] string timeZoneId = "Asia/Riyadh")
    {
        var result = await _service.GetOilTransactionsReportAsync(branchId, stationId, subStationId, dateFrom, dateTo, page, pageSize, timeZoneId);
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
        [FromQuery] int pageSize = 50,
        [FromQuery] string timeZoneId = "Asia/Riyadh")
    {
        var result = await _service.GetPeriodicInventoryReportAsync(branchId, stationId, subStationId, dateFrom, dateTo, page, pageSize, timeZoneId);
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
        [FromQuery] int pageSize = 50,
        [FromQuery] string timeZoneId = "Asia/Riyadh")
    {
        var result = await _service.GetEfficiencyReportAsync(branchId, stationId, subStationId, dateFrom, dateTo, page, pageSize, timeZoneId);
        return Ok(result);
    }

    // ─── Daily Reports Sub-Tabs ────────────────────────────────────

    [HttpGet("fuel-incoming")]
    public async Task<IActionResult> GetFuelIncomingReport(
        [FromQuery] int? branchId,
        [FromQuery] int? stationId,
        [FromQuery] int? subStationId,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string timeZoneId = "Asia/Riyadh")
    {
        var result = await _service.GetFuelIncomingReportAsync(branchId, stationId, subStationId, dateFrom, dateTo, page, pageSize, timeZoneId);
        return Ok(result);
    }

    [HttpGet("fuel-transfer")]
    public async Task<IActionResult> GetFuelTransferReport(
        [FromQuery] int? branchId,
        [FromQuery] int? stationId,
        [FromQuery] int? subStationId,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string timeZoneId = "Asia/Riyadh")
    {
        var result = await _service.GetFuelTransferReportAsync(branchId, stationId, subStationId, dateFrom, dateTo, page, pageSize, timeZoneId);
        return Ok(result);
    }

    [HttpGet("fuel-loss")]
    public async Task<IActionResult> GetFuelLossReport(
        [FromQuery] int? branchId,
        [FromQuery] int? stationId,
        [FromQuery] int? subStationId,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string timeZoneId = "Asia/Riyadh")
    {
        var result = await _service.GetFuelLossReportAsync(branchId, stationId, subStationId, dateFrom, dateTo, page, pageSize, timeZoneId);
        return Ok(result);
    }

    // ─── Inventory Summary ─────────────────────────────────────────

    [HttpGet("inventory-summary")]
    public async Task<IActionResult> GetInventorySummary(
        [FromQuery] int? branchId,
        [FromQuery] int? stationId,
        [FromQuery] int? subStationId,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] string timeZoneId = "Asia/Riyadh")
    {
        var result = await _service.GetInventorySummaryAsync(branchId, stationId, subStationId, dateFrom, dateTo, timeZoneId);
        return Ok(result);
    }

    // ─── SignalR Push ─────────────────────────────────────────────

    [HttpPost("push-stats")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> PushDashboardStats()
    {
        var stats = await _service.GetDashboardStatsAsync(null, null, null, null, null, "Asia/Riyadh");
        var tenantId = TenantRealtimeScope.GetTenant(_configuration, _tenant);
        await _hub.Clients.Group(TenantRealtimeScope.Group(tenantId, "dashboard")).SendAsync("DashboardStatsUpdated", stats);
        return Ok(new { message = "Dashboard stats pushed to all connected clients." });
    }
}
