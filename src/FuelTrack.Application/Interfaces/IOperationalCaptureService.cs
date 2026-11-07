using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Operations;

namespace FuelTrack.Application.Interfaces;

public interface IOperationalCaptureService
{
    Task<Result<OperationalSubmissionDto>> SubmitAsync(SubmitOperationalCaptureDto dto, int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OperationalHistoryItemDto>> GetMyHistoryAsync(int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OperationalCaptureReferenceDto>> GetCaptureReferencesAsync(int userId, CancellationToken cancellationToken = default);
    Task<Result> VoidRecordedLineAsync(string lineType, int lineId, VoidOperationalReadingDto dto, int userId, CancellationToken cancellationToken = default);
}
