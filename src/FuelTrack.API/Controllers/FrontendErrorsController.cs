using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace FuelTrack.API.Controllers;

/// <summary>
/// Receives frontend JS runtime errors and persists them to AuditLog.
/// No authentication required so error reporting works even after session expiry.
/// Rate-limited by the global rate limiter (100 req/min).
/// </summary>
[AllowAnonymous]
[ApiController]
[Route("api/[controller]")]
public class FrontendErrorsController : ControllerBase
{
    private readonly FuelTrackDbContext _db;
    private readonly ILogger<FrontendErrorsController> _logger;

    public FrontendErrorsController(FuelTrackDbContext db, ILogger<FrontendErrorsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>POST /api/frontenderrors — log a client-side runtime error.</summary>
    [HttpPost]
    public async Task<IActionResult> LogError([FromBody] FrontendErrorDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        _logger.LogWarning(
            "[FrontendError] source={Source} message={Message} url={Url} userAgent={UA}",
            dto.Source, dto.Message, dto.Url, dto.UserAgent);

        var detail = $"[{dto.Source}] {dto.Message} | URL: {dto.Url} | Stack: {dto.Stack?.Substring(0, Math.Min(dto.Stack?.Length ?? 0, 2000))}";

        var log = new AuditLog
        {
            EntityName = "FrontendError",
            EntityId = 0,
            Action = "RuntimeError",
            NewValues = detail,
            UserId = dto.UserId ?? 0,
            UserAgent = dto.UserAgent,
            Timestamp = DateTime.UtcNow
        };

        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync();

        return Ok(new { logged = true });
    }
}

public record FrontendErrorDto
{
    [Required, MaxLength(100)]
    public string Source { get; init; } = string.Empty;

    [Required, MaxLength(1000)]
    public string Message { get; init; } = string.Empty;

    public string? Stack { get; init; }

    [MaxLength(500)]
    public string? Url { get; init; }

    [MaxLength(300)]
    public string? UserAgent { get; init; }

    public int? UserId { get; init; }
}
