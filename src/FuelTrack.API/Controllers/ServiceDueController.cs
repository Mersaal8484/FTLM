using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.API.Controllers;

[Authorize]
[ApiController]
[Route("api/service-due")]
public class ServiceDueController : BaseApiController
{
    private readonly IServiceDueService _dueService;
    private readonly IServiceDueEvaluationService _evaluationService;

    public ServiceDueController(
        IServiceDueService dueService,
        IServiceDueEvaluationService evaluationService)
    {
        _dueService = dueService;
        _evaluationService = evaluationService;
    }

    [HttpGet("generator/{generatorId:int}")]
    public async Task<IActionResult> GetGeneratorMaintenanceTable(int generatorId, CancellationToken ct = default)
    {
        var result = await _dueService.GetGeneratorMaintenanceTableAsync(generatorId, ct);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(result.StatusCode, new { error = result.Error });
    }

    [HttpGet("station")]
    public async Task<IActionResult> GetStationMaintenanceTables([FromQuery] int? subStationId = null, CancellationToken ct = default)
    {
        var result = await _dueService.GetStationMaintenanceTablesAsync(subStationId, ct);
        return Ok(result);
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboardSummary([FromQuery] int? subStationId = null, CancellationToken ct = default)
    {
        var result = await _dueService.GetDueDashboardSummaryAsync(subStationId, ct);
        return Ok(result);
    }

    [HttpPost("evaluate")]
    public async Task<IActionResult> Evaluate(
        [FromQuery] int? generatorId = null,
        CancellationToken ct = default)
    {
        if (generatorId.HasValue)
        {
            var gen = await _dueService.GetGeneratorMaintenanceTableAsync(generatorId.Value, ct);
            if (!gen.IsSuccess)
                return StatusCode(gen.StatusCode, new { error = gen.Error });

            var count = await _evaluationService.EvaluateGeneratorAsync(generatorId.Value, ct);
            return Ok(new { alertsGenerated = count });
        }

        // Evaluate only within user scope (B6, W1)
        var tables = await _dueService.GetStationMaintenanceTablesAsync(null, ct);
        var inScopeGenIds = tables.Select(t => t.GeneratorId).ToList();

        var totalAlerts = 0;
        foreach (var id in inScopeGenIds)
        {
            totalAlerts += await _evaluationService.EvaluateGeneratorAsync(id, ct);
        }

        return Ok(new { alertsGenerated = totalAlerts });
    }
}
