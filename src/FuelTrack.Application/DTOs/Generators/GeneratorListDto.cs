namespace FuelTrack.Application.DTOs.Generators;

public record GeneratorListDto(
    int Id,
    int SubStationId,
    string SubStationName,
    string Name,
    string SerialNumber,
    string? Model,
    string? Manufacturer,
    double NominalPower,
    int ActualPowerKW,
    decimal FuelConsumptionRatePerHour,
    string? OwnerName,
    int? GeneratorTypeLookupId,
    string? GeneratorTypeLookupName,
    int? VoltageLookupId,
    string? VoltageLookupName,
    int? FrequencyLookupId,
    string? FrequencyLookupName,
    int? OwnershipTypeLookupId,
    string? OwnershipTypeLookupName,
    int? StatusLookupId,
    string? StatusLookupName);
