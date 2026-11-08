using FuelTrack.Application.DTOs.Operations;
using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.API.Controllers;

/// <summary>
/// Phase 3: Tier-2 review queue — approve / reject Pending submissions.
/// </summary>
[Authorize]
[ApiController]
[Route("api/operational-review")]
public sealed class OperationalReviewController(IOperationalReviewService reviewService) : BaseApiController
{
    /// <summary>GET pending Tier-2 submissions visible to the current reviewer.</summary>
    [HttpGet]
    public async Task<IActionResult> GetPendingSubmissions(
        [FromQuery] int? subStationId, CancellationToken cancellationToken)
    {
        var result = await reviewService.GetPendingSubmissionsAsync(GetUserId(), subStationId, cancellationToken);
        return Ok(result);
    }

    /// <summary>GET full detail of one submission including per-line values, photos and delta preview.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetSubmissionDetail(int id, CancellationToken cancellationToken)
    {
        var result = await reviewService.GetSubmissionDetailAsync(id, GetUserId(), cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value)
            : StatusCode(result.StatusCode, new { error = result.Error });
    }

    /// <summary>Atomically approve all non-rejected Pending lines in a submission.</summary>
    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> ApproveSubmission(int id, CancellationToken cancellationToken)
    {
        var result = await reviewService.ApproveSubmissionAsync(id, GetUserId(), cancellationToken);
        return result.IsSuccess
            ? Ok(new { message = "Submission approved." })
            : StatusCode(result.StatusCode, new { error = result.Error });
    }

    /// <summary>Reject a single reading line; reason is mandatory.</summary>
    [HttpPost("lines/{lineType}/{lineId:int}/reject")]
    public async Task<IActionResult> RejectLine(
        string lineType, int lineId,
        [FromBody] RejectLineDto dto,
        CancellationToken cancellationToken)
    {
        var result = await reviewService.RejectLineAsync(lineType, lineId, dto, GetUserId(), cancellationToken);
        return result.IsSuccess
            ? Ok(new { message = "Line rejected." })
            : StatusCode(result.StatusCode, new { error = result.Error });
    }
}
