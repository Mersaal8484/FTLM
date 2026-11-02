using FuelTrack.Application.DTOs.Maintenance;
using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.API.Controllers;

[Authorize]
[ApiController]
[Route("api/installed-parts")]
public class InstalledPartsController : BaseApiController
{
    private readonly IInstalledPartService _installedPartService;

    public InstalledPartsController(IInstalledPartService installedPartService)
    {
        _installedPartService = installedPartService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? generatorId = null)
    {
        var result = await _installedPartService.GetAllAsync(generatorId);
        return Ok(result);
    }

    [HttpGet("by-generator/{generatorId:int}")]
    public async Task<IActionResult> GetByGenerator(
        int generatorId,
        [FromQuery] bool activeOnly = false)
    {
        var result = await _installedPartService.GetByGeneratorAsync(generatorId, activeOnly);
        return Ok(result);
    }

    [HttpGet("overdue")]
    public async Task<IActionResult> GetOverdue([FromQuery] int? subStationId = null)
    {
        var result = await _installedPartService.GetOverdueAsync(subStationId);
        return Ok(result);
    }

    [HttpGet("upcoming")]
    public async Task<IActionResult> GetUpcomingReplacements(
        [FromQuery] int? subStationId = null,
        [FromQuery] double hoursThreshold = 200)
    {
        var result = await _installedPartService.GetUpcomingReplacementsAsync(subStationId, hoursThreshold);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("install")]
    public async Task<IActionResult> Install([FromBody] CreateInstalledPartDto dto)
    {
        var userId = GetUserId();
        var result = await _installedPartService.InstallAsync(dto, userId);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [Authorize]
    [HttpPost("{id:int}/remove")]
    public async Task<IActionResult> Remove(int id, [FromBody] RemoveInstalledPartDto dto)
    {
        var userId = GetUserId();
        var result = await _installedPartService.RemoveAsync(id, dto, userId);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }
}
