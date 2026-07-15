namespace FuelTrack.Application.DTOs.Branches;

public record UpdateBranchDto(
    string? Name,
    string? Code,
    bool? IsGeneration,
    string? Notes);
