namespace FuelTrack.Application.DTOs.Lookups;

public record UpdateLookupDto(
    string? Category,
    string? Name,
    string? Code,
    bool? IsActive);
