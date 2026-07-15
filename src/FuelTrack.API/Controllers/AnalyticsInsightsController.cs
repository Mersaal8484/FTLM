using System.Security.Claims;
using FuelTrack.Application.DTOs.Intelligence;
using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.API.Controllers;

[Authorize]
[ApiController]
[Route("api/analytics/insights")]
public class AnalyticsInsightsController : BaseApiController
{
    private readonly IAnalyticsInsightService _service;

    public AnalyticsInsightsController(IAnalyticsInsightService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetInsights([FromQuery] int? subStationId, [FromQuery] int? stationId)
    {
        var result = await _service.GetInsightsAsync(subStationId, stationId);
        return Ok(result);
    }

    [HttpPost("feedback")]
    public async Task<IActionResult> AddFeedback([FromBody] CreateInsightFeedbackDto dto)
    {
        var userId = GetUserId();
        await _service.AddFeedbackAsync(dto, userId);
        return NoContent();
    }

}
