using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.API.Controllers;

/// <summary>Phase 3: station energy balance board.</summary>
[Authorize]
[ApiController]
[Route("api/energy-balance")]
public sealed class EnergyBalanceController(IEnergyBalanceService balanceService) : BaseApiController
{
    /// <summary>
    /// Returns the energy balance for a station on a given date (station-local date).
    /// Uses official (IsOfficial) data only.
    /// </summary>
    [HttpGet("{stationId:int}")]
    public async Task<IActionResult> GetBalance(
        int stationId,
        [FromQuery] DateOnly? date,
        CancellationToken cancellationToken)
    {
        var effectiveDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var result = await balanceService.GetStationBalanceAsync(stationId, effectiveDate, cancellationToken);
        return Ok(result);
    }
}
