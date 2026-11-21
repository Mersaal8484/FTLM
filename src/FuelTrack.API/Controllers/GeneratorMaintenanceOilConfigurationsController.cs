using FuelTrack.Application.DTOs.Maintenance;
using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.API.Controllers;

[Authorize]
[ApiController]
[Route("api/generator-maintenance-oil-configurations")]
public sealed class GeneratorMaintenanceOilConfigurationsController : BaseApiController
{
    private readonly IGeneratorMaintenanceOilConfigurationService _service;

    public GeneratorMaintenanceOilConfigurationsController(IGeneratorMaintenanceOilConfigurationService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? generatorId = null, CancellationToken ct = default) =>
        Ok(await _service.GetAllAsync(generatorId, ct));

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] SaveGeneratorMaintenanceOilConfigurationDto dto, CancellationToken ct = default)
    {
        var result = await _service.SaveAsync(dto, GetUserId(), ct);
        return result.IsSuccess ? StatusCode(result.StatusCode, result.Value) : StatusCode(result.StatusCode, new { error = result.Error });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
    {
        var result = await _service.DeleteAsync(id, GetUserId(), ct);
        return result.IsSuccess ? Ok(new { success = true }) : StatusCode(result.StatusCode, new { error = result.Error });
    }
}
