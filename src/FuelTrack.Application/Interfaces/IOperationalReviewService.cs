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

    Task<Result> RejectLineAsync(
        string lineType, int lineId, RejectLineDto dto, int userId,
        CancellationToken cancellationToken = default);
}
