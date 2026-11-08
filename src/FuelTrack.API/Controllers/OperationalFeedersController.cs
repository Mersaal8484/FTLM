using FuelTrack.Application.DTOs.Operations;
using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.API.Controllers;

[Authorize]
[ApiController]
[Route("api/operational-feeders")]
public sealed class OperationalFeedersController(IFeederService service) : BaseApiController
{
    [HttpGet("by-site/{subStationId:int}")]
    public async Task<IActionResult> GetBySite(int subStationId, CancellationToken cancellationToken) => Ok(await service.GetBySiteAsync(subStationId, GetUserId(), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(CreateFeederDto dto, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(dto, GetUserId(), cancellationToken);
        return result.IsSuccess ? StatusCode(result.StatusCode, result.Value) : StatusCode(result.StatusCode, new { error = result.Error });
    }
}
