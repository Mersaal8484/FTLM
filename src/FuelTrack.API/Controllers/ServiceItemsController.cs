using FuelTrack.Application.DTOs.Maintenance;
using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.API.Controllers;

[Authorize]
[ApiController]
[Route("api/service-items")]
public class ServiceItemsController : BaseApiController
{
    private readonly IServiceItemService _service;

    public ServiceItemsController(IServiceItemService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false, CancellationToken ct = default)
    {
        var result = await _service.GetAllAsync(includeInactive, ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct = default)
    {
        var result = await _service.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(result.StatusCode, new { error = result.Error });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateServiceItemDto dto, CancellationToken ct = default)
    {
        var result = await _service.CreateAsync(dto, GetUserId(), ct);
        return result.IsSuccess ? StatusCode(result.StatusCode, result.Value) : StatusCode(result.StatusCode, new { error = result.Error });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateServiceItemDto dto, CancellationToken ct = default)
    {
        var result = await _service.UpdateAsync(id, dto, GetUserId(), ct);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(result.StatusCode, new { error = result.Error });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
    {
        var result = await _service.DeleteAsync(id, GetUserId(), ct);
        return result.IsSuccess ? Ok(new { success = true }) : StatusCode(result.StatusCode, new { error = result.Error });
    }
}
