namespace FuelTrack.Application.DTOs.Branches;

public record CreateBranchDto(
    string Name,
    string Code,
    bool IsGeneration,
    string? Notes);
