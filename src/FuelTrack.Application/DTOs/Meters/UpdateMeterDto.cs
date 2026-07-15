namespace FuelTrack.Application.DTOs.Meters;

public record UpdateMeterDto(
    int? SubStationId,
    string? Name,
    string? Code,
    int? MeterTypeLookupId,
    string? SerialNumber,
    int? GeneratorId,
    int? FuelTankId);
