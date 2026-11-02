using FuelTrack.Application.DTOs.Modbus;
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
    private readonly IWorkflowNotificationService _workflowNotifications;

    public ReadingsController(IReadingService readingService, IRealtimeNotifier notifier, IWorkflowNotificationService workflowNotifications)
    {
        _readingService = readingService;
        _notifier = notifier;
        _workflowNotifications = workflowNotifications;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReadingDto dto)
    {
        var userId = GetUserId();
        var isAdmin = GetUserRoles().Contains("Admin");
        var result = await _readingService.CreateAsync(dto, userId, isAdmin);
        await _workflowNotifications.NotifyReadingAsync(result.GeneratorId, result.Id, result.GeneratorName, "reading_pending", userId);
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

    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id, [FromBody] ApproveReadingDto? dto = null)
    {
        try
        {
            var userId = GetUserId();
            var result = await _readingService.ApproveAsync(id, dto ?? new ApproveReadingDto(userId, null));
            await _workflowNotifications.NotifyReadingAsync(result.GeneratorId, result.Id, result.GeneratorName, "reading_approved", userId);
            await _notifier.NotifyAllAsync("ReadingApproved", new { result.Id, result.GeneratorName, result.ReadingDate, ApprovedById = dto?.ApprovedById ?? userId });
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new Dictionary<string, string>
            {
                ["msg"] = ex.Message,
                ["message"] = ex.Message
            });
        }
    }

    [HttpPost("{id:int}/reject")]
    public async Task<IActionResult> Reject(int id, [FromBody] RejectReadingDto dto)
    {
        try
        {
            var result = await _readingService.RejectAsync(id, dto);
            await _workflowNotifications.NotifyReadingAsync(result.GeneratorId, result.Id, result.GeneratorName, "reading_rejected", GetUserId(), dto.RejectionReason);
            await _notifier.NotifyAllAsync("ReadingRejected", new { result.Id, result.GeneratorName, dto.RejectionReason });
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new Dictionary<string, string>
            {
                ["msg"] = ex.Message,
                ["message"] = ex.Message
            });
        }
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

    [HttpGet("pending")]
    public async Task<IActionResult> GetPending()
    {
        var result = await _readingService.GetPendingAsync();
        return Ok(result);
    }

    [HttpGet("{id:int}/consumption")]
    public async Task<IActionResult> GetConsumption(int id)
    {
        var result = await _readingService.GetConsumptionAsync(id);
        return Ok(result);
    }

    [HttpPost("modbus")]
    [AllowAnonymous]
    public async Task<IActionResult> ReceiveModbusReading([FromBody] ModbusReadingDto dto)
    {
        var result = await _readingService.SaveModbusReadingAsync(dto.GeneratorId, dto.FuelLevelLiters, dto.ReadingTimestamp);
        return Ok(result);
    }

}
