using FuelTrack.Application.DTOs.Readings;
using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ReadingsController : BaseApiController
{
    private readonly IReadingService _readingService;
    private readonly IRealtimeNotifier _notifier;

    public ReadingsController(IReadingService readingService, IRealtimeNotifier notifier)
    {
        _readingService = readingService;
        _notifier = notifier;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReadingDto dto)
    {
        var userId = GetUserId();
        var isAdmin = GetUserRoles().Contains("Admin");
        var result = await _readingService.CreateAsync(dto, userId, isAdmin);
        await _notifier.NotifyAllAsync("ReadingCreated", new { result.Id, result.GeneratorName, result.ReadingDate });
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateReadingDto dto)
    {
        var userId = GetUserId();
        var isAdmin = GetUserRoles().Contains("Admin");
        var result = await _readingService.UpdateAsync(id, dto, userId, isAdmin);
        await _notifier.NotifyAllAsync("ReadingUpdated", new { result.Id });
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id, [FromBody] ApproveReadingDto dto)
    {
        var result = await _readingService.ApproveAsync(id, dto);
        await _notifier.NotifyAllAsync("ReadingApproved", new { result.Id, result.GeneratorName, result.ReadingDate, ApprovedById = dto.ApprovedById });
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/reject")]
    public async Task<IActionResult> Reject(int id, [FromBody] RejectReadingDto dto)
    {
        var result = await _readingService.RejectAsync(id, dto);
        await _notifier.NotifyAllAsync("ReadingRejected", new { result.Id, result.GeneratorName, dto.RejectionReason });
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/review")]
    public async Task<IActionResult> Review(int id)
    {
        var userId = GetUserId();
        var result = await _readingService.ReviewAsync(id, userId);
        await _notifier.NotifyAllAsync("ReadingReviewed", new { result.Id });
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _readingService.DeleteAsync(id);
        await _notifier.NotifyAllAsync("ReadingDeleted", new { Id = id });
        return NoContent();
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _readingService.GetByIdAsync(id);
        if (result is null)
            return NotFound();
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] ReadingFilterDto filter)
    {
        var userId = GetUserId();
        var isAdmin = GetUserRoles().Contains("Admin");
        var result = await _readingService.ListAsync(filter, userId, isAdmin);
        return Ok(result);
    }

    [HttpGet("{id:int}/consumption")]
    public async Task<IActionResult> GetConsumption(int id)
    {
        var result = await _readingService.GetConsumptionAsync(id);
        return Ok(result);
    }

}
