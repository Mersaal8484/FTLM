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
public class UsersController : BaseApiController
{
    private readonly IUserService _userService;
    private readonly IRealtimeNotifier _notifier;

    public UsersController(IUserService userService, IRealtimeNotifier notifier)
    {
        _userService = userService;
        _notifier = notifier;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var result = await _userService.GetPagedAsync(page, pageSize);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _userService.GetByIdAsync(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
    {
        var userId = GetUserId();
        var result = await _userService.CreateAsync(dto, userId);
        if (!result.IsSuccess) return BadRequest(new { msg = result.Error });
        await _notifier.NotifyAllAsync("UserCreated", result.Value);
        return CreatedAtAction(nameof(GetById), new { id = result.Value?.Id }, result.Value);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserDto dto)
    {
        var userId = GetUserId();
        var result = await _userService.UpdateAsync(id, dto, userId);
        await _notifier.NotifyAllAsync("UserUpdated", result);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _userService.DeleteAsync(id);
        await _notifier.NotifyAllAsync("UserDeleted", new { id });
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/assign-roles")]
    public async Task<IActionResult> AssignRoles(int id, [FromBody] List<string> roles)
    {
        await _userService.AssignRolesAsync(id, roles);
        await _notifier.NotifyAllAsync("UserRolesAssigned", new { id, roles });
        return NoContent();
    }

    [HttpGet("{id:int}/roles")]
    public async Task<IActionResult> GetRoles(int id)
    {
        var roles = await _userService.GetUserRolesAsync(id);
        if (!roles.IsSuccess) return NotFound(roles.Error);
        return Ok(roles.Value);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/activate")]
    public async Task<IActionResult> Activate(int id)
    {
        await _userService.ActivateAsync(id);
        await _notifier.NotifyAllAsync("UserActivated", new { id });
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/deactivate")]
    public async Task<IActionResult> Deactivate(int id)
    {
        await _userService.DeactivateAsync(id);
        await _notifier.NotifyAllAsync("UserDeactivated", new { id });
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{id:int}/reset-password")]
    public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetPasswordRequest request)
    {
        await _userService.ResetPasswordAsync(id, request.Password);
        await _notifier.NotifyAllAsync("UserPasswordReset", new { id });
        return NoContent();
    }

    public record ResetPasswordRequest(string Password);

    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/scopes")]
    public async Task<IActionResult> AssignScopes(int id, [FromBody] AssignScopesDto dto)
    {
        var result = await _userService.AssignScopesAsync(id, dto.Scopes);
        if (!result.IsSuccess) return BadRequest(new { msg = result.Error });
        await _notifier.NotifyAllAsync("UserScopesAssigned", new { userId = id, scopes = dto.Scopes });
        return NoContent();
    }
}
