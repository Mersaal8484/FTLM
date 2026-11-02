namespace FuelTrack.Application.DTOs.Meters;

    public record CreateMeterDto(
        string? MeterName,
        int? SubStationId,
        int? StationId,
        int? MeterTypeLookupId,
        string? SerialNumber,
        int? GeneratorId,
        double? HoursReading,
        double? EnergyReading,
        bool? IsActive = null);
