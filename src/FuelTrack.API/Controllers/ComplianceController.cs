using System.Threading.Tasks;
using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FuelTrack.API.Controllers;

[Authorize(Roles = "Admin,SystemOwner")]
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("ApiWrite")]
public class ComplianceController : BaseApiController
{
    private readonly IComplianceService _complianceService;

    public ComplianceController(IComplianceService complianceService)
    {
        _complianceService = complianceService;
    }

    [HttpGet("export-user/{userId:int}")]
    public async Task<IActionResult> ExportUserData(int userId)
    {
        var requestedBy = User.Identity?.Name ?? $"User-{GetUserId()}";
        var export = await _complianceService.ExportUserDataAsync(userId, requestedBy);
        if (export == null)
        {
            return NotFound(new { message = $"User with ID {userId} not found" });
        }

        return Ok(export);
    }

    [HttpPost("anonymize-user/{userId:int}")]
    public async Task<IActionResult> AnonymizeUser(int userId)
    {
        var requestedBy = User.Identity?.Name ?? $"User-{GetUserId()}";
        var result = await _complianceService.AnonymizeUserPersonalDataAsync(userId, requestedBy);
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpGet("audit-summary")]
    public async Task<IActionResult> GetAuditSummary()
    {
        var summary = await _complianceService.GetComplianceAuditSummaryAsync();
        return Ok(summary);
    }
}
