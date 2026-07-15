using FuelTrack.Application.DTOs.Screens;
using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ScreensController : BaseApiController
{
    private readonly IScreenService _service;
    private readonly IRealtimeNotifier _notifier;

    public ScreensController(IScreenService service, IRealtimeNotifier notifier)
    {
        _service = service;
        _notifier = notifier;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateScreenDto dto)
    {
        var result = await _service.CreateAsync(dto);
        await _notifier.NotifyAllAsync("ScreenCreated", new { id = result.Id });
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateScreenDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        await _notifier.NotifyAllAsync("ScreenUpdated", new { id });
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
        await _notifier.NotifyAllAsync("ScreenDeleted", new { id });
        return NoContent();
    }
}
