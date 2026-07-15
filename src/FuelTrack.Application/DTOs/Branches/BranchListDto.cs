namespace FuelTrack.Application.DTOs.Branches;

public record BranchListDto(
    int Id,
    string Name,
    string Code,
    bool IsGeneration,
    int StationCount);
