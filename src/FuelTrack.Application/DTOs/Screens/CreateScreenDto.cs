namespace FuelTrack.Application.DTOs.Screens;

public record CreateScreenDto(
    string Name,
    string Code,
    string? Description);
