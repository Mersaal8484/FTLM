namespace FuelTrack.Application.DTOs.Efficiency;

public record EfficiencyReportDto(
    int GeneratorId,
    string GeneratorName,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    double TotalEnergyKWh,
    double TotalFuelLiters,
    double RunningHours,
    double AverageEfficiencyPercent,
    double AverageLoadKw,
    int ReadingCount);
