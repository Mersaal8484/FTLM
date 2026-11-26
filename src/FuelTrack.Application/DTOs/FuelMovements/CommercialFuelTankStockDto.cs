namespace FuelTrack.Application.DTOs.FuelMovements;

/// <summary>
/// Commercial-only fuel view. It deliberately keeps main-tank supply separate
/// from a generator daily tank's operating consumption.
/// </summary>
public sealed record CommercialFuelTankStockDto(
    int TankId,
    string TankName,
    string TankCode,
    string TankRole,
    int FuelLookupId,
    string FuelLookupName,
    int? GeneratorId,
    string? GeneratorName,
    int? ParentTankId,
    decimal CapacityLitres,
    decimal DeadStockLitres,
    decimal CurrentLitres,
    decimal UsableLitres,
    decimal ExternalReceiptLitres,
    decimal TransferredOutLitres,
    decimal TransferredInLitres,
    decimal ConsumedLitres,
    string AlertLevel,
    decimal? CurrentLevelCm = null);

public sealed record CommercialFuelStockOverviewDto(
    IReadOnlyList<CommercialFuelTankStockDto> MainTanks,
    IReadOnlyList<CommercialFuelTankStockDto> DailyTanks);
