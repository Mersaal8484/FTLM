using FuelTrack.Application.DTOs.Rollback;
using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.API.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
public class RollbackController : BaseApiController
{
    private readonly IRollbackService _rollbackService;
    private readonly IRealtimeNotifier _notifier;

    public RollbackController(IRollbackService rollbackService, IRealtimeNotifier notifier)
    {
        _rollbackService = rollbackService;
        _notifier = notifier;
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory()
    {
        var history = await _rollbackService.GetHistoryAsync();
        return Ok(history);
    }

    [HttpPost("check")]
    public async Task<IActionResult> Check([FromBody] RollbackRequestDto dto)
    {
        var result = await _rollbackService.CheckRollbackAsync(dto.AuditLogId);
        return Ok(result);
    }

    [HttpPost("execute")]
    public async Task<IActionResult> Execute([FromBody] RollbackRequestDto dto)
    {
        var userId = GetUserId();
        await _rollbackService.ExecuteRollbackAsync(dto.AuditLogId, userId);
        await _notifier.NotifyAllAsync("RollbackExecuted", new { auditLogId = dto.AuditLogId });
        return Ok(new { message = "Rollback executed successfully" });
    }
}
