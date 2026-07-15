namespace FuelTrack.Application.DTOs.Stations;

public record StationDetailDto(
    int Id,
    int BranchId,
    string BranchName,
    string Name,
    string Code,
    string? Notes,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);
