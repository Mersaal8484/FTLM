namespace FuelTrack.Application.DTOs.Screens;

public record ScreenDetailDto(
    int Id,
    string Name,
    string Code,
    string? Description,
    DateTime CreatedAt,
    DateTime UpdatedAt);
