using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.RBAC;
using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FuelTrack.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("ApiWrite")]
public class RolesController : BaseApiController
{
    private readonly IRoleService _roleService;
    private readonly IRealtimeNotifier _notifier;

    public RolesController(IRoleService roleService, IRealtimeNotifier notifier)
    {
        _roleService = roleService;
        _notifier = notifier;
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _roleService.GetAllAsync();
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _roleService.GetByIdAsync(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRoleDto dto)
    {
        var userId = GetUserId();
        var result = await _roleService.CreateAsync(dto, userId);
        if (!result.IsSuccess) return BadRequest(result.Error);
        await _notifier.NotifyAllAsync("RoleCreated", result.Value);
        return CreatedAtAction(nameof(GetById), new { id = result.Value?.Id }, result.Value);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRoleDto dto)
    {
        var userId = GetUserId();
        var result = await _roleService.UpdateAsync(id, dto, userId);
        if (!result.IsSuccess) return BadRequest(result.Error);
        await _notifier.NotifyAllAsync("RoleUpdated", result.Value);
        return Ok(result.Value);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _roleService.DeleteAsync(id);
        if (!result.IsSuccess)
        {
            if (result.Error?.Contains("protected") == true)
                return Forbid();
            return NotFound(result.Error);
        }
        await _notifier.NotifyAllAsync("RoleDeleted", new { id });
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("{id:int}/permissions")]
    public async Task<IActionResult> GetPermissions(int id)
    {
        var result = await _roleService.GetRolePermissionsAsync(id);
        if (!result.IsSuccess) return BadRequest(result.Error);
        return Ok(result.Value);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}/permissions")]
    public async Task<IActionResult> UpdatePermissions(int id, [FromBody] SaveRolePermissionsRequest request)
    {
        var userId = GetUserId();
        var result = await _roleService.SaveRolePermissionsAsync(
            request with { RoleId = id }, userId);
        if (!result.IsSuccess) return BadRequest(result.Error);
        await _notifier.NotifyAllAsync("RolePermissionsUpdated", new { id });
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("modules")]
    public async Task<IActionResult> GetModules()
    {
        var result = await _roleService.GetAllModulesAsync();
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("init")]
    public async Task<IActionResult> Init()
    {
        await _roleService.InitAsync();
        await _notifier.NotifyAllAsync("RolesInitialized", null);
        return Ok(new { message = "Default roles initialized." });
    }
}
