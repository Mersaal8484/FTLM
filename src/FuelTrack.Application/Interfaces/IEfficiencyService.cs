using FuelTrack.Application.DTOs.Efficiency;

namespace FuelTrack.Application.Interfaces;

public interface IEfficiencyService
{
    Task<EfficiencyReportDto> GetEfficiencyReportAsync(int generatorId, DateTime startDate, DateTime endDate, CancellationToken ct = default);
    Task<EfficiencyTrendDto> GetEfficiencyTrendAsync(int generatorId, DateTime startDate, DateTime endDate, CancellationToken ct = default);
}
