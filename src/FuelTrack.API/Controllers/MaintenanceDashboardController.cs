using FuelTrack.Application.DTOs.Maintenance;
using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.API.Controllers;

[Authorize]
[ApiController]
[Route("api/maintenance/dashboard")]
public class MaintenanceDashboardController : BaseApiController
{
    private readonly IMaintenanceDashboardService _maintenanceDashboardService;

    public MaintenanceDashboardController(IMaintenanceDashboardService maintenanceDashboardService)
    {
        _maintenanceDashboardService = maintenanceDashboardService;
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboard([FromQuery] int? subStationId = null, [FromQuery] int? stationId = null)
    {
        var result = await _maintenanceDashboardService.GetDashboardAsync(subStationId, stationId);
        return Ok(result);
    }
}
