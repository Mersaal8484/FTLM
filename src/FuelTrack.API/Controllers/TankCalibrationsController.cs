using FuelTrack.Application.DTOs.Operations;
using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.API.Controllers;

[Authorize]
[ApiController]
[Route("api/fuel-tanks/{fuelTankId:int}/calibration")]
public sealed class TankCalibrationsController(ITankCalibrationService service) : BaseApiController
{
    [HttpPost("preview")]
    public IActionResult Preview(UpdateTankCalibrationDto dto)
    {
        try { return Ok(service.Preview(dto)); }
        catch (ArgumentOutOfRangeException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPut]
    public async Task<IActionResult> Put(int fuelTankId, UpdateTankCalibrationDto dto, CancellationToken cancellationToken)
    {
        var result = await service.UpsertAsync(fuelTankId, dto, GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(result.StatusCode, new { error = result.Error });
    }
}
