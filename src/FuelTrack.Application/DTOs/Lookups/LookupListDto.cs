namespace FuelTrack.Application.DTOs.Lookups;

public record LookupListDto(
    int Id,
    string Category,
    string Name,
    string? Code,
    int SortOrder,
    bool IsActive);
