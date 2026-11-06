using FuelTrack.Application.DTOs.Operations;
using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.API.Controllers;

[Authorize]
[ApiController]
[Route("api/operational-captures")]
public sealed class OperationalCapturesController(IOperationalCaptureService service) : BaseApiController
{
    [HttpGet("references")]
    public async Task<IActionResult> References(CancellationToken cancellationToken) => Ok(await service.GetCaptureReferencesAsync(GetUserId(), cancellationToken));

    [HttpGet("my-history")]
    public async Task<IActionResult> MyHistory(CancellationToken cancellationToken) => Ok(await service.GetMyHistoryAsync(GetUserId(), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Submit(SubmitOperationalCaptureDto dto, CancellationToken cancellationToken)
    {
        var result = await service.SubmitAsync(dto, GetUserId(), cancellationToken);
        return result.IsSuccess ? StatusCode(result.StatusCode, result.Value) : StatusCode(result.StatusCode, new { error = result.Error });
    }
}
