namespace FuelTrack.Application.DTOs.Stations;

public record StationListDto(
    int Id,
    int BranchId,
    string BranchName,
    string Name,
    string Code,
    bool IsActive);
