using FuelTrack.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.API.Controllers;

/// <summary>Read-only gap and average-load indicators for the Phase 3b log views.</summary>
[Authorize]
[ApiController]
[Route("api/operational-completeness")]
public sealed class OperationalCompletenessController(OperationalCompletenessService service) : BaseApiController
{
    [HttpGet("generators/{generatorId:int}/{businessDate}")]
    public async Task<IActionResult> Get(int generatorId, DateOnly businessDate, CancellationToken ct)
    {
        var result = await service.GetAsync(generatorId, businessDate, GetUserId(), ct);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(result.StatusCode, new { error = result.Error });
    }

    [HttpGet("feeders/{feederId:int}/{businessDate}")]
    public async Task<IActionResult> GetFeeder(int feederId, DateOnly businessDate, CancellationToken ct)
    {
        var result = await service.GetFeederAsync(feederId, businessDate, GetUserId(), ct);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(result.StatusCode, new { error = result.Error });
    }
}
