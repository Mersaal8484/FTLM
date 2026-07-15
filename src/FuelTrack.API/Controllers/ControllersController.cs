using FuelTrack.Application.DTOs.Controllers;
using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ControllersController : BaseApiController
{
    private readonly IControllerService _service;
    private readonly IRealtimeNotifier _notifier;

    public ControllersController(IControllerService service, IRealtimeNotifier notifier)
    {
        _service = service;
        _notifier = notifier;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateControllerDto dto)
    {
        var result = await _service.CreateAsync(dto);
        await _notifier.NotifyAllAsync("ControllerCreated", new { id = result.Id });
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateControllerDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        await _notifier.NotifyAllAsync("ControllerUpdated", new { id });
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result is null) return NotFound();
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAllAsync();
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        await _notifier.NotifyAllAsync("ControllerDeleted", new { id });
        return NoContent();
    }

    [HttpPost("{id:int}/connection-status")]
    public async Task<IActionResult> UpdateConnectionStatus(int id, [FromBody] ConnectionStatusRequest request)
    {
        var result = await _service.UpdateConnectionStatusAsync(id, request.Status);
        await _notifier.NotifyAllAsync("ControllerConnectionStatusChanged", new { id, request.Status });
        return Ok(result);
    }

    public record ConnectionStatusRequest(string Status);

    [HttpPatch("{id:int}/assign-generator")]
    public async Task<IActionResult> AssignGenerator(int id, [FromBody] AssignGeneratorRequest request)
    {
        var result = await _service.AssignGeneratorAsync(id, request.GeneratorId);
        if (result is null) return NotFound();
        await _notifier.NotifyAllAsync("ControllerUpdated", new { id });
        return Ok(result);
    }

    public record AssignGeneratorRequest(int? GeneratorId);
}
