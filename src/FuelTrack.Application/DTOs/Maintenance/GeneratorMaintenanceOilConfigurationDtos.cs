namespace FuelTrack.Application.DTOs.Maintenance;

public sealed record GeneratorMaintenanceOilConfigurationDto(
    int Id,
    int GeneratorId,
    string GeneratorName,
    int ServiceItemId,
    string ServiceItemCode,
    string ServiceItemNameAr,
    string ServiceItemNameEn,
    int OilId,
    int OilLookupId,
    string OilLookupName,
    decimal QuantityLiters);

public sealed record SaveGeneratorMaintenanceOilConfigurationDto(
    int GeneratorId,
    int ServiceItemId,
    int OilId,
    decimal QuantityLiters);
