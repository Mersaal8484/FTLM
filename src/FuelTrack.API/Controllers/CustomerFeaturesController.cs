using FuelTrack.Application.DTOs.Operations;
using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.API.Controllers;

[Authorize]
[ApiController]
[Route("api/customer-features")]
public sealed class CustomerFeaturesController(ICustomerFeatureService service) : BaseApiController
{
    [HttpGet("operational-data-mode")]
    public async Task<IActionResult> GetOperationalDataMode(CancellationToken cancellationToken) =>
        Ok(new { isNewMode = await service.IsOperationalDataNewModeAsync(cancellationToken) });

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) => Ok(await service.GetAllAsync(cancellationToken));

    [HttpPut("{key}")]
    public async Task<IActionResult> Put(string key, UpdateCustomerFeatureSettingDto dto, CancellationToken cancellationToken)
    {
        var result = await service.UpdateAsync(key, dto, GetUserId(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(result.StatusCode, new { error = result.Error });
    }
}
