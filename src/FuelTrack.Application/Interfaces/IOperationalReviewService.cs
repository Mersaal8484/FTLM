using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Operations;

namespace FuelTrack.Application.Interfaces;

public interface IOperationalReviewService
{
    Task<IReadOnlyList<PendingSubmissionDto>> GetPendingSubmissionsAsync(
        int userId, int? subStationId, CancellationToken cancellationToken = default);

    Task<Result<SubmissionDetailDto>> GetSubmissionDetailAsync(
        int submissionId, int userId, CancellationToken cancellationToken = default);

    Task<Result> ApproveSubmissionAsync(
        int submissionId, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Approves one end-of-day review bundle. A generator bundle includes its
    /// generator readings, daily-tank level and generator counters; a feeder
    /// bundle includes its feeder reading and feeder counter.
    /// </summary>
    Task<Result> ApproveBundleAsync(
        int submissionId, string bundleType, int subjectId, int userId,
        CancellationToken cancellationToken = default);

    /// <summary>Rejects every reviewable line in one end-of-day bundle.</summary>
    Task<Result> RejectBundleAsync(
        int submissionId, string bundleType, int subjectId, RejectLineDto dto, int userId,
        CancellationToken cancellationToken = default);

    Task<Result> RejectLineAsync(
        string lineType, int lineId, RejectLineDto dto, int userId,
        CancellationToken cancellationToken = default);
}
