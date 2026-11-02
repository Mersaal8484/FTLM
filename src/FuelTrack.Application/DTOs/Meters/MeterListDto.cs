namespace FuelTrack.Application.DTOs.Meters;

    public record MeterListDto(
        int Id,
        string Name,
        int? SubStationId,
        string? SubStationName,
        int? StationId,
        string? StationName,
        int? BranchId,
        string? BranchName,
        int? MeterTypeLookupId,
        string? MeterTypeLookupName,
        string? SerialNumber,
        int? GeneratorId,
        string? GeneratorName,
        double? HoursReading,
        double? EnergyReading,
        bool IsActive);
