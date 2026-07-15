namespace FuelTrack.Application.DTOs.Readings;

public record ReadingListDto(
    int Id,
    int GeneratorId,
    string GeneratorName,
    DateTime ReadingDate,
    double CurrentRunningHours,
    double CurrentEnergyKWh,
    string FuelLookupName,
    int ReadingStatusLookupId,
    string ReadingStatusLookupName,
    DateTime CreatedAt,
    int? ControllerId,
    string? ControllerName);
