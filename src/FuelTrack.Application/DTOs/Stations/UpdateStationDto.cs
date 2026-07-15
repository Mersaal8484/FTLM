namespace FuelTrack.Application.DTOs.Stations;

public record UpdateStationDto(
    int? BranchId,
    string? Name,
    string? Code,
    string? Notes,
    bool? IsActive);
