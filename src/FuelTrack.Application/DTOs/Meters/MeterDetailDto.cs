namespace FuelTrack.Application.DTOs.Meters;

public record MeterDetailDto(
    int Id,
    string Name,
    string? Code,
    int? StationId,
    string? StationName,
    int? SubStationId,
    string? SubStationName,
    string? MeterType,
    string? MeterTypeLookupName,
    string? SerialNumber,
    int? GeneratorId,
    string? GeneratorName,
    int? FuelTankId,
    string? FuelTankName,
    double? HoursReading,
    double? EnergyReading,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    bool IsActive);
