namespace FuelTrack.Application.DTOs.SubStations;

public record CreateSubStationDto(
    int StationId,
    string Name,
    string Code,
    string? Location,
    double? CapacityKW,
    bool IsActive = true);
