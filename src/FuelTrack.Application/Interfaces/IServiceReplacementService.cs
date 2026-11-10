using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Maintenance;

namespace FuelTrack.Application.Interfaces;

public interface IServiceReplacementService
{
    Task<Result<ServiceReplacementRecordDto>> RecordReplacementAsync(RecordReplacementRequestDto dto, int userId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ServiceReplacementRecordDto>>> RecordBulkBaselineAsync(BulkBaselineRequestDto dto, int userId, CancellationToken ct = default);
    Task<Result<bool>> VoidAsync(int id, VoidReplacementRecordDto dto, int userId, CancellationToken ct = default);
    Task<IReadOnlyList<ServiceReplacementRecordDto>> GetHistoryAsync(int generatorId, int? serviceItemId = null, CancellationToken ct = default);
}
