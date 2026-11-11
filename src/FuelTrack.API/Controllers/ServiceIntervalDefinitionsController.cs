using FuelTrack.Application.DTOs.Maintenance;
using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.API.Controllers;

[Authorize]
[ApiController]
[Route("api/service-interval-definitions")]
public class ServiceIntervalDefinitionsController : BaseApiController
{
    private readonly IServiceIntervalDefinitionService _service;

    public ServiceIntervalDefinitionsController(IServiceIntervalDefinitionService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? model = null, CancellationToken ct = default)
    {
        var result = await _service.GetAllAsync(model, ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct = default)
    {
        var result = await _service.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(result.StatusCode, new { error = result.Error });
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] SaveServiceIntervalDefinitionDto dto, CancellationToken ct = default)
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
