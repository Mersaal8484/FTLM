namespace FuelTrack.Application.DTOs.Branches;

public record BranchDetailDto(
    int Id,
    string Name,
    string Code,
    bool IsGeneration,
    string? Notes,
    int StationCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);
