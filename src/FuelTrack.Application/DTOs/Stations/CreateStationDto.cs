namespace FuelTrack.Application.DTOs.Stations;

public record CreateStationDto(
    int BranchId,
    string Name,
    string Code,
    string? Notes,
    bool? IsActive);
