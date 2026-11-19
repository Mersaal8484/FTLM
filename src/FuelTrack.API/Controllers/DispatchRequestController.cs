using FuelTrack.Application.DTOs.Warehouse;
using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FuelTrack.Application.Validators.Warehouse;

namespace FuelTrack.API.Controllers;

[Authorize]
[ApiController]
[Route("api/warehouse/dispatch")]
public class DispatchRequestController : BaseApiController
{
    private readonly IDispatchRequestService _dispatchRequestService;

    public DispatchRequestController(IDispatchRequestService dispatchRequestService)
    {
        _dispatchRequestService = dispatchRequestService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? status = null)
    {
        var result = await _dispatchRequestService.GetAllAsync(page, pageSize, status);
        return Ok(result);
    }

    [HttpGet("by-substation/{subStationId:int}")]
    public async Task<IActionResult> GetBySubStation(int subStationId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _dispatchRequestService.GetBySubStationAsync(subStationId, page, pageSize);
        return Ok(result);
    }

    [HttpGet("by-station/{stationId:int}")]
    public async Task<IActionResult> GetByStation(int stationId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _dispatchRequestService.GetByStationAsync(stationId, page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _dispatchRequestService.GetByIdAsync(id);
        if (result is null)
            return NotFound();
        return Ok(result);
    }

    [Authorize]
    [HttpPost("commercial")]
    public async Task<IActionResult> CreateCommercial([FromBody] CommercialDispatchRequest request,
        [FromServices] ICommercialStationModeService commercial,
        [FromServices] IUserScopeFilterService scopeFilter,
        [FromServices] IWorkPermitService workPermits,
        CancellationToken ct)
    {
        var scope = await commercial.GetScopeAsync(ct);
        if (scope is null) return NotFound();
        var userId = GetUserId();
        if (!User.IsInRole("Admin") && !User.IsInRole("SystemOwner") && !await scopeFilter.HasAccessToSubStationAsync(userId, scope.SubStationId))
            return Forbid();
        if (request.WorkPermitId.HasValue)
        {
            var permit = await workPermits.GetByIdAsync(request.WorkPermitId.Value, ct);
            if (permit is null || permit.SubStationId != scope.SubStationId) return BadRequest(new { error = "Work permit is not available in this station." });
        }
        var dto = new CreateDispatchRequestDto(scope.SubStationId, scope.StationId, request.WorkPermitId, request.Purpose, request.Notes, request.Items);
        var validation = new CreateDispatchRequestDtoValidator().Validate(dto);
        if (!validation.IsValid) return BadRequest(new { error = string.Join("; ", validation.Errors.Select(x => x.ErrorMessage)) });
        var result = await _dispatchRequestService.CreateAsync(dto, userId, ct);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(result.StatusCode, new { error = result.Error });
    }

    public sealed record CommercialDispatchRequest(int? WorkPermitId, string? Purpose, string? Notes, List<CreateDispatchItemDto> Items);

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDispatchRequestDto dto)
    {
        var userId = GetUserId();
        var result = await _dispatchRequestService.CreateAsync(dto, userId);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { error = result.Error });
        return Ok(result.Value);
    }

    [Authorize]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateDispatchRequestDto dto)
    {
        var userId = GetUserId();
        var result = await _dispatchRequestService.UpdateAsync(id, dto, userId);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpGet("{id:int}/approve")]
    public async Task<IActionResult> GetApprovalData(int id)
    {
        var result = await _dispatchRequestService.GetApprovalDataAsync(id);
        return Ok(result);
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id, [FromBody] ApproveDispatchRequestDto dto)
    {
        var userId = GetUserId();
        var result = await _dispatchRequestService.ApproveAsync(id, dto, userId);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { error = result.Error });
        return Ok(result.Value);
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost("{id:int}/reject")]
    public async Task<IActionResult> Reject(int id, [FromBody] RejectDispatchRequestDto dto)
    {
        var userId = GetUserId();
        var result = await _dispatchRequestService.RejectAsync(id, dto.Reason, userId);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { error = result.Error });
        return Ok(result.Value);
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost("{id:int}/issue")]
    public async Task<IActionResult> Issue(int id)
    {
        var userId = GetUserId();
        var result = await _dispatchRequestService.IssueAsync(id, userId);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { error = result.Error });
        return Ok(result.Value);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _dispatchRequestService.DeleteAsync(id);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { error = result.Error });
        return NoContent();
    }
}
