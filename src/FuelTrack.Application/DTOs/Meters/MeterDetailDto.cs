namespace FuelTrack.Application.DTOs.Meters;

    public record MeterDetailDto(
        int Id,
        string Name,
        int? SubStationId,
        string? SubStationName,
        int? StationId,
        string? StationName,
        int? BranchId,
        string? BranchName,
        string? MeterTypeLookupName,
        string? SerialNumber,
        int? GeneratorId,
        string? GeneratorName,
        double? HoursReading,
        double? EnergyReading,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        bool IsActive);
