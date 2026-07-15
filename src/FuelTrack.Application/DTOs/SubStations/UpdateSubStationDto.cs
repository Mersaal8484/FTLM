namespace FuelTrack.Application.DTOs.SubStations;

public record UpdateSubStationDto(
    int? StationId,
    string? Name,
    string? Code,
    string? Location,
    double? CapacityKW,
    bool? IsActive);
