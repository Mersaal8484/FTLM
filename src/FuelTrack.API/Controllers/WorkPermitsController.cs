using FuelTrack.Application.DTOs.Maintenance;
using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.API.Controllers;

[Authorize]
[ApiController]
[Route("api/work-permits")]
public class WorkPermitsController : BaseApiController
{
    private readonly IWorkPermitService _workPermitService;
    private readonly IWorkflowNotificationService _workflowNotifications;

    public WorkPermitsController(IWorkPermitService workPermitService, IWorkflowNotificationService workflowNotifications)
    {
        _workPermitService = workPermitService;
        _workflowNotifications = workflowNotifications;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? status = null,
        [FromQuery] int? priority = null,
        [FromQuery] int? subStationId = null)
    {
        var result = await _workPermitService.GetAllAsync(page, pageSize, status, priority, subStationId);
        return Ok(result);
    }

    [HttpGet("by-substation/{subStationId:int}")]
    public async Task<IActionResult> GetBySubStation(
        int subStationId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? status = null)
    {
        var result = await _workPermitService.GetBySubStationAsync(subStationId, page, pageSize, status);
        return Ok(result);
    }

    [HttpGet("by-generator/{generatorId:int}")]
    public async Task<IActionResult> GetByGenerator(int generatorId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _workPermitService.GetByGeneratorAsync(generatorId, page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _workPermitService.GetByIdAsync(id);
        if (result is null)
            return NotFound();
        return Ok(result);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWorkPermitDto dto)
    {
        var userId = GetUserId();
        var result = await _workPermitService.CreateAsync(dto, userId);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        await _workflowNotifications.NotifyWorkPermitAssignedAsync(result.Value!.Id, result.Value.SubStationId, result.Value.PermitNumber, result.Value.AssignedToId, userId);
        return Ok(result.Value);
    }

    [Authorize]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateWorkPermitDto dto)
    {
        var userId = GetUserId();
        var result = await _workPermitService.UpdateAsync(id, dto, userId);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        await _workflowNotifications.NotifyWorkPermitAssignedAsync(result.Value!.Id, result.Value.SubStationId, result.Value.PermitNumber, result.Value.AssignedToId, userId);
        return Ok(result.Value);
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateWorkPermitStatusDto dto)
    {
        var userId = GetUserId();
        var result = await _workPermitService.UpdateStatusAsync(id, dto, userId);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [Authorize]
    [HttpPost("{id:int}/tasks/{taskId:int}/complete")]
    public async Task<IActionResult> CompleteTask(int id, int taskId)
    {
        var userId = GetUserId();
        var result = await _workPermitService.CompleteTaskAsync(id, taskId, userId);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("{id:int}/log")]
    public async Task<IActionResult> AddLog(int id, [FromBody] AddWorkPermitLogDto dto)
    {
        var userId = GetUserId();
        var result = await _workPermitService.AddLogAsync(id, dto.Action, dto.Notes, userId);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _workPermitService.DeleteAsync(id);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return NoContent();
    }
}
