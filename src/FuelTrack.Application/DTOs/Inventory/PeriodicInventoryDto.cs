namespace FuelTrack.Application.DTOs.Inventory;

public record PeriodicInventoryDto(
    int Id,
    int SubStationId,
    string? SubStationName,
    int Year,
    int Month,
    int PeriodTypeLookupId,
    string? PeriodTypeLookupName,
    DateTime InventoryDate,
    string? Notes,
    bool IsApproved,
    int? ApprovedById,
    string? ApprovedByName,
    DateTime? ApprovedAt,
    int CreatedBy,
    int UpdatedBy,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<FuelReadingResultDto> FuelReadings,
    List<OilReadingResultDto> OilReadings);

public record FuelReadingResultDto(
    int Id,
    int? FuelTankId,
    string? FuelTankCode,
    int FuelLookupId,
    string? FuelLookupName,
    double ExpectedQtyLiters,
    double ActualQtyLiters,
    double DifferenceLiters);

public record OilReadingResultDto(
    int Id,
    int? OilId,
    string? OilName,
    int OilLookupId,
    string? OilLookupName,
    double ExpectedQtyLiters,
    double ActualQtyLiters,
    double DifferenceLiters);
