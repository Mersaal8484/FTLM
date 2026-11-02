using FuelTrack.Application.DTOs.Generators;
using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class GeneratorsController : BaseApiController
{
    private readonly IGeneratorService _generatorService;
    private readonly IRealtimeNotifier _notifier;

    public GeneratorsController(IGeneratorService generatorService, IRealtimeNotifier notifier)
    {
        _generatorService = generatorService;
        _notifier = notifier;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateGeneratorDto dto)
    {
        var userId = GetUserId();
        var result = await _generatorService.CreateAsync(dto, userId);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        await _notifier.NotifyAllAsync("GeneratorCreated", new { result.Value?.Id, result.Value?.Name });
        return Ok(result.Value);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateGeneratorDto dto)
    {
        var userId = GetUserId();
        var result = await _generatorService.UpdateAsync(id, dto, userId);
        await _notifier.NotifyAllAsync("GeneratorUpdated", new { id });
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();
        await _generatorService.DeleteAsync(id, userId);
        await _notifier.NotifyAllAsync("GeneratorDeleted", new { id });
        return NoContent();
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = GetUserId();
        var result = await _generatorService.GetByIdAsync(id, userId);
        if (result is null)
            return NotFound();
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var userId = GetUserId();
        var result = await _generatorService.GetAllAsync(userId);
        var total = result.Count;
        var paged = result.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Ok(new
        {
            data = paged,
            pagination = new { total, page, pageSize, pages = (int)Math.Ceiling(total / (double)pageSize) }
        });
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyGenerators()
    {
        var userId = GetUserId();
        var result = await _generatorService.GetMyGeneratorsAsync(userId);
        return Ok(result);
    }

    [HttpGet("my/{id:int}")]
    public async Task<IActionResult> GetMyGeneratorById(int id)
    {
        var userId = GetUserId();
        var result = await _generatorService.GetMyGeneratorByIdAsync(id, userId);
        if (result is null)
            return NotFound();
        return Ok(result);
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActiveWithControllers()
    {
        var userId = GetUserId();
        var result = await _generatorService.GetActiveWithControllersAsync(userId);
        return Ok(result);
    }

    [HttpGet("my/{id:int}/last-reading")]
    public async Task<IActionResult> GetMyGeneratorLastReading(int id)
    {
        var result = await _generatorService.GetLastReadingAsync(id);
        if (result is null)
            return NotFound();
        return Ok(result);
    }

}
