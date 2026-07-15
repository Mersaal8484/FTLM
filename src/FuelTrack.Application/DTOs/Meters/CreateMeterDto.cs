namespace FuelTrack.Application.DTOs.Meters;

public record CreateMeterDto(
    string? MeterName,
    string? Code,
    int? StationId,
    int? SubStationId,
    string? MeterType,
    int? MeterTypeLookupId,
    string? SerialNumber,
    int? GeneratorId,
    int? FuelTankId,
    double? HoursReading,
    double? EnergyReading,
    bool? IsActive = null);
