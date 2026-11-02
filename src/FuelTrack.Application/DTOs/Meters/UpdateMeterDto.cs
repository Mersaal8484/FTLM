namespace FuelTrack.Application.DTOs.Meters;

    public record UpdateMeterDto(
        int? SubStationId,
        int? StationId,
        string? Name,
        int? MeterTypeLookupId,
        string? SerialNumber,
        int? GeneratorId);
