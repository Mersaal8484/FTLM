using FuelTrack.Application.DTOs.Audits;
using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AuditsController : BaseApiController
{
    private readonly IAuditService _auditService;

    public AuditsController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] AuditFilterParams filter)
    {
        var result = await _auditService.GetPagedAsync(filter);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] AuditFilterParams filter)
    {
        var csv = await _auditService.ExportCsvAsync(filter);
        return File(csv, "text/csv", $"auditlog_{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}
