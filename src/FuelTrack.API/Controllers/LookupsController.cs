using FuelTrack.Application.DTOs.Lookups;
using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;

namespace FuelTrack.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("ApiRead")]
public class LookupsController : BaseApiController
{
    private readonly ILookupService _lookupService;
    private readonly IRealtimeNotifier _notifier;

    public LookupsController(ILookupService lookupService, IRealtimeNotifier notifier)
    {
        _lookupService = lookupService;
        _notifier = notifier;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLookupDto dto)
    {
        var userId = GetUserId();
        var result = await _lookupService.CreateAsync(dto);
        await _notifier.NotifyAllAsync("LookupCreated", new { id = result.Id, result.Category });
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateLookupDto dto)
    {
        var userId = GetUserId();
        var result = await _lookupService.UpdateAsync(id, dto);
        await _notifier.NotifyAllAsync("LookupUpdated", new { id });
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _lookupService.DeleteAsync(id);
        await _notifier.NotifyAllAsync("LookupDeleted", new { id });
        return NoContent();
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _lookupService.GetByIdAsync(id);
        if (result is null) return NotFound();
        return Ok(result);
    }

    [HttpGet]
    [OutputCache(PolicyName = "Lookups")]
    public async Task<IActionResult> GetAll()
    {
        var result = await _lookupService.GetAllAsync();
        return Ok(result);
    }

    [HttpGet("by-category/{category}")]
    [OutputCache(PolicyName = "Lookups")]
    public async Task<IActionResult> GetByCategory(string category)
    {
        var result = await _lookupService.GetByCategoryAsync(category);
        return Ok(result);
    }

}
