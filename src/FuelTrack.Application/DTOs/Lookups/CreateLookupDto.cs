namespace FuelTrack.Application.DTOs.Lookups;

public record CreateLookupDto(
    string Category,
    string Name,
    string? Code,
    bool? IsActive);
