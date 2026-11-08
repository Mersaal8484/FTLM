using FuelTrack.Application.Interfaces;
using FuelTrack.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.API.Controllers;

/// <summary>
/// Phase 3 §7.6: authorized, scope-checked retrieval of operational photos.
/// Photos are NEVER served as static URLs.
/// </summary>
[Authorize]
[ApiController]
[Route("api/operational-photos")]
public sealed class OperationalPhotosController(
    IAttachmentService attachmentService,
    IUserScopeFilterService scopeFilter,
    FuelTrackDbContext db) : BaseApiController
{
    [HttpGet("{attachmentId:int}")]
    public async Task<IActionResult> GetPhoto(int attachmentId, CancellationToken cancellationToken)
    {
        int userId = GetUserId();

        // Verify scope: attachment must be linked to a submission whose SubStation is accessible
        var accessibleSiteIds = await scopeFilter.GetAccessibleSubStationIdsAsync(userId);

        bool inScope = await db.AttachmentLinks
            .AsNoTracking()
            .AnyAsync(l => l.AttachmentId == attachmentId
                           && l.EntityType == "OperationalSubmission"
                           && db.OperationalSubmissions
                               .Where(s => s.Id == l.EntityId)
                               .Any(s => accessibleSiteIds.Contains(s.SubStationId)),
                      cancellationToken);

        // Also allow if attached directly to a reading line entity inside a scoped submission
        if (!inScope)
        {
            inScope = await db.AttachmentLinks
                .AsNoTracking()
                .AnyAsync(l => l.AttachmentId == attachmentId
                               && (l.EntityType == "OperationalGeneratorReading"
                                   || l.EntityType == "TankLevelReading"
                                   || l.EntityType == "FeederLoadReading"
                                   || l.EntityType == "MeterReading")
                               && db.OperationalSubmissions
                                   .Where(s => db.OperationalGeneratorReadings.Any(r => r.Id == l.EntityId && r.OperationalSubmissionId == s.Id)
                                            || db.TankLevelReadings.Any(r => r.Id == l.EntityId && r.OperationalSubmissionId == s.Id)
                                            || db.FeederLoadReadings.Any(r => r.Id == l.EntityId && r.OperationalSubmissionId == s.Id)
                                            || db.MeterReadings.Any(r => r.Id == l.EntityId && r.OperationalSubmissionId == s.Id))
                                   .Any(s => accessibleSiteIds.Contains(s.SubStationId)),
                      cancellationToken);
        }

        if (!inScope)
            return Forbid();

        var attachment = await attachmentService.OpenReadAsync(attachmentId, cancellationToken);
        if (attachment is null)
            return NotFound();

        Response.Headers.ContentDisposition =
            $"inline; filename=\"{attachment.Value.Metadata.OriginalFileName.Replace("\"", string.Empty)}\"";
        Response.Headers.XContentTypeOptions = "nosniff";
        return File(attachment.Value.Content, attachment.Value.Metadata.ContentType);
    }
}
