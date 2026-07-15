namespace FuelTrack.Application.DTOs.Generators.My;

public record MyGeneratorListDto(
    int Id,
    int SubStationId,
    string SubStationName,
    int? StationId,
    string? StationName,
    string Name,
    string SerialNumber,
    double NominalPower,
    int ActualPowerKW,
    decimal FuelConsumptionRatePerHour,
    string? GenType,
    string Status,
    double? LastPowerRecord,
    double? LastHoursRecord,
    List<AssignedOilTypeDto>? AssignedOilTypes,
    int? FuelLookupId,
    decimal? StandardFuelRate,
    int? OilLookupId);
