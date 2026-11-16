using FuelTrack.Application.DTOs.Operations;
using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.API.Controllers;

[Authorize]
[ApiController]
[Route("api/commercial-station-mode")]
public sealed class CommercialStationModeController(ICommercialStationModeService service) : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) => Ok(await service.GetStatusAsync(cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpGet("preflight")]
    public async Task<IActionResult> Preflight(CancellationToken cancellationToken) => Ok(await service.GetActivationPreflightAsync(cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpPost("activate")]
    public async Task<IActionResult> Activate(ActivateCommercialStationModeDto dto, CancellationToken cancellationToken)
    {
        var result = await service.ActivateAsync(dto, GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(result.StatusCode, new { error = result.Error });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("deactivate")]
    public async Task<IActionResult> Deactivate(CancellationToken cancellationToken)
    {
        var result = await service.DeactivateAsync(GetUserId(), cancellationToken);
        return result.IsSuccess ? NoContent() : StatusCode(result.StatusCode, new { error = result.Error });
    }
}
