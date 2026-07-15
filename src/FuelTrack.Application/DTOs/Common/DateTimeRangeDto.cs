namespace FuelTrack.Application.DTOs.Common;

public record DateTimeRangeDto(
    DateTime? From,
    DateTime? To);

public record IdNameDto(
    int Id,
    string Name);
