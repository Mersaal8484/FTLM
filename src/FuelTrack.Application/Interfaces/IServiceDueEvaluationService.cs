namespace FuelTrack.Application.Interfaces;

public interface IServiceDueEvaluationService
{
    Task<int> EvaluateGeneratorAsync(int generatorId, CancellationToken ct = default);
    Task<int> EvaluateAllGeneratorsAsync(CancellationToken ct = default);
}
