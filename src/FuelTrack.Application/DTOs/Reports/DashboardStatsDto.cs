namespace FuelTrack.Application.DTOs.Reports;

// ─── Dashboard ───────────────────────────────────────────────────────────────

public record DashboardStatsResponseDto(
    FuelSummary Fuel,
    EnergySummary Energy,
    OilSummary Oil,
    AlertSummary Alerts,
    SystemSummary System)
{
    public FuelSummary Fuel { get; init; } = Fuel;
    public EnergySummary Energy { get; init; } = Energy;
    public OilSummary Oil { get; init; } = Oil;
    public AlertSummary Alerts { get; init; } = Alerts;
    public SystemSummary System { get; init; } = System;
}

public record FuelSummary(
    double TotalReceivedLiters,
    double TotalTransferredLiters,
    double TotalConsumedLiters,
    double NetDeltaLiters,
    List<FuelTankLevelDto> TankLevels);

public record FuelTankLevelDto(
    int TankId,
    string TankName,
    string SubStationName,
    double ActualStockLiters,
    double CapacityLiters,
    double PercentageFull,
    bool IsLow,
    bool IsCritical);

public record EnergySummary(
    double TotalProducedKWh,
    double TotalExportedKWh,
    double NetInternalKWh,
    double AverageLoadKW,
    double MaxLoadKW,
    int ActiveGenerators);

public record OilSummary(
    double TotalReceivedLiters,
    double TotalConsumedLiters,
    List<OilTankLevelDto> TankLevels);

public record OilTankLevelDto(
    int TankId,
    string TankName,
    string SubStationName,
    double ActualStockLiters,
    double CapacityLiters,
    double PercentageFull);

public record AlertSummary(
    int LowFuelCount,
    int CriticalFuelCount,
    int LowOilCount,
    int StaleReadingCount,
    int Total);

public record SystemSummary(
    int TotalBranches,
    int TotalStations,
    int TotalSubStations,
    int TotalGenerators,
    int TotalFuelTanks,
    int TotalOilTanks,
    int OnlineGenerators,
    int OfflineGenerators,
    int MaintenanceGenerators);

// ─── Fuel & Oil Dashboard ────────────────────────────────────────────────────

public record FuelOilDashboardDto(
    FuelOilSummaryDto Summary,
    List<FuelOilConsumptionDto> Consumptions,
    List<FuelOilTrendDto> Trends,
    List<FuelOilAlertDto> Alerts);

public record FuelOilSummaryDto(
    double TotalFuelConsumedLiters,
    double TotalOilConsumedLiters,
    double FuelCost,
    double OilCost,
    int ActiveStations,
    int ActiveGenerators);

public record FuelOilConsumptionDto(
    DateTime Date,
    string SubStationName,
    double FuelLiters,
    double OilLiters,
    double EfficiencyPercent);

public record FuelOilTrendDto(
    DateTime Date,
    double FuelConsumedLiters,
    double OilConsumedLiters,
    double EnergyProducedKWh);

public record FuelOilAlertDto(
    string Type,
    string Severity,
    string Message,
    string SubStationName,
    DateTime? CreatedAt);
