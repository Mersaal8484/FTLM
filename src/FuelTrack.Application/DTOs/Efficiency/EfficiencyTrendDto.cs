namespace FuelTrack.Application.DTOs.Efficiency;

public record EfficiencyTrendDto(
    int GeneratorId,
    string GeneratorName,
    List<EfficiencyDataPoint> DataPoints);

public record EfficiencyDataPoint(
    DateTime Date,
    double EnergyKWh,
    double FuelLiters,
    double EfficiencyPercent,
    double RunningHours);
