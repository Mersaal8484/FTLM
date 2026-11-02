namespace FuelTrack.Application.DTOs.SubStationReadings;

public record ProductionSummaryDto(
    double TotalProduced,
    int GeneratorCount,
    DateTime Date);
