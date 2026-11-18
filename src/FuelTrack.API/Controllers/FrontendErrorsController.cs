using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.ComponentModel.DataAnnotations;

namespace FuelTrack.API.Controllers;

/// <summary>
/// Receives rate-limited frontend errors even after session expiry.
/// </summary>
[AllowAnonymous]
[EnableRateLimiting("ApiWrite")]
[ApiController]
[Route("api/[controller]")]
public class FrontendErrorsController : ControllerBase
{
    private readonly ILogger<FrontendErrorsController> _logger;

    public FrontendErrorsController(ILogger<FrontendErrorsController> logger)
    {
        _logger = logger;
    }

    /// <summary>POST /api/frontenderrors — log a client-side runtime error.</summary>
    [HttpPost]
    public IActionResult LogError([FromBody] FrontendErrorDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        _logger.LogWarning(
            "[FrontendError] source={Source} message={Message} url={Url} userAgent={UA}",
            dto.Source, dto.Message, dto.Url, dto.UserAgent);

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

}
