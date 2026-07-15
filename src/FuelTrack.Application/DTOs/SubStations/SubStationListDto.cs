namespace FuelTrack.Application.DTOs.SubStations;

public record SubStationListDto(
    int Id,
    int StationId,
    string StationName,
    string Name,
    string Code,
    string? Location,
    double? CapacityKW,
    bool IsActive);
