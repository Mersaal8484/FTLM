namespace FuelTrack.Application.DTOs.Lookups;

public record LookupDetailDto(
    int Id,
    string Category,
    string Name,
    string? Code,
    int SortOrder,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);
