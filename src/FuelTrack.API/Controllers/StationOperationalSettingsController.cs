using FuelTrack.Application.DTOs.Operations;
using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.API.Controllers;

[Authorize]
[ApiController]
[Route("api/stations/{stationId:int}/operational-settings")]
public sealed class StationOperationalSettingsController(IStationOperationalSettingsService service) : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> Get(int stationId, CancellationToken cancellationToken)
    {
        var settings = await service.GetAsync(stationId, cancellationToken);
        return settings is null ? NotFound() : Ok(settings);
    }

    [HttpPut]
    public async Task<IActionResult> Put(int stationId, UpdateStationOperationalSettingsDto dto, CancellationToken cancellationToken)
    {
        var result = await service.UpsertAsync(stationId, dto, GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(result.StatusCode, new { error = result.Error });
    }
}
