using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class EfficiencyController : BaseApiController
{
    private readonly IEfficiencyService _service;

    public EfficiencyController(IEfficiencyService service)
    {
        _service = service;
    }

    [HttpGet("report/{generatorId:int}")]
    public async Task<IActionResult> GetReport(int generatorId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        from ??= DateTime.UtcNow.AddDays(-7);
        to ??= DateTime.UtcNow;
        var result = await _service.GetEfficiencyReportAsync(generatorId, from.Value, to.Value);
        return Ok(result);
    }

    [HttpGet("trend/{generatorId:int}")]
    public async Task<IActionResult> GetTrend(int generatorId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        from ??= DateTime.UtcNow.AddDays(-30);
        to ??= DateTime.UtcNow;
        var result = await _service.GetEfficiencyTrendAsync(generatorId, from.Value, to.Value);
        return Ok(result);
    }
}
