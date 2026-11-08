using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Operations;

namespace FuelTrack.Application.Interfaces;

public interface IFuelLoadSafetyService
{
    Task<Result<FuelConsumptionReportDto>> GetConsumptionAsync(int generatorId, DateTime fromUtc, DateTime toUtc, int userId, CancellationToken cancellationToken = default);
    Task<Result<FuelReconciliationReportDto>> GetReconciliationAsync(int subStationId, DateTime fromUtc, DateTime toUtc, int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OverloadEventDto>> GetOverloadsAsync(int userId, int? subStationId, bool includeResolved, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<GeneratorOperatingLimitDto>>> GetLimitsAsync(int generatorId, int userId, CancellationToken cancellationToken = default);
    Task<Result<GeneratorOperatingLimitDto>> SaveLimitAsync(int generatorId, SaveGeneratorOperatingLimitDto dto, int userId, CancellationToken cancellationToken = default);
    Task<Result<OperationalDailyLogDto>> GetDailyLogAsync(int generatorId, DateOnly businessDate, int userId, CancellationToken cancellationToken = default);
    Task EvaluateOnReceiptAsync(int generatorReadingId, CancellationToken cancellationToken = default);
    Task EvaluateOperatingLimitsAsync(CancellationToken cancellationToken = default);
}
