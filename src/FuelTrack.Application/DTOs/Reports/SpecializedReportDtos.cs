namespace FuelTrack.Application.DTOs.Reports;

// ─── Generators Report ───────────────────────────────────────────────────────

public record GeneratorsReportDto(
    List<GeneratorReportItemDto> Items,
    GeneratorReportSummaryDto Summary);

public record GeneratorReportItemDto(
    int Id,
    string Name,
    string SerialNumber,
    string SubStationName,
    string StationName,
    string BranchName,
    double? NominalPower,
    double ActualPowerKW,
    double FuelConsumptionRatePerHour,
    double TotalEnergyProducedKWh,
    double TotalFuelConsumedLiters,
    double OperatingHours,
    double EfficiencyPercent,
    string StatusName,
    DateTime? LastReadingDate);

public record GeneratorReportSummaryDto(
    int TotalGenerators,
    int ActiveGenerators,
    int InactiveGenerators,
    double TotalNominalPower,
    double TotalActualPower,
    double TotalEnergyProducedKWh,
    double TotalFuelConsumedLiters,
    double AverageEfficiencyPercent);

// ─── Station Readings Report ─────────────────────────────────────────────────

public record StationReadingsReportDto(
    List<StationReadingReportItemDto> Items,
    StationReadingReportSummaryDto Summary);

public record StationReadingReportItemDto(
    int Id,
    DateTime ReadingDate,
    string SubStationName,
    string StationName,
    string BranchName,
    string MeterCode,
    double PreviousExportedKWh,
    double CurrentExportedKWh,
    double DeltaExportedKWh,
    string StatusName);

public record StationReadingReportSummaryDto(
    int TotalReadings,
    int VerifiedCount,
    int PendingCount,
    int RejectedCount,
    double TotalEnergyExportedKWh);

// ─── Fuel Transactions Report ────────────────────────────────────────────────

public record FuelTransactionsReportDto(
    List<FuelTransactionReportItemDto> Items,
    FuelTransactionReportSummaryDto Summary);

public record FuelTransactionReportItemDto(
    int Id,
    DateTime TransactionDate,
    string SubStationName,
    string StationName,
    string FuelTypeName,
    double QuantityLiters,
    string TransactionTypeName,
    string? DriverName,
    string? SupplierCompany,
    string? VehiclePlate,
    string? SourceSubStationName,
    string? DestinationSubStationName,
    string StatusName,
    string? Notes);

public record FuelTransactionReportSummaryDto(
    int TotalTransactions,
    double TotalIncomingLiters,
    double TotalInternalTransferLiters,
    double TotalConsumptionLiters,
    int IncomingCount,
    int TransferCount,
    int ConsumptionCount);

// ─── Oil Transactions Report ─────────────────────────────────────────────────

public record OilTransactionsReportDto(
    List<OilTransactionReportItemDto> Items,
    OilTransactionReportSummaryDto Summary);

public record OilTransactionReportItemDto(
    int Id,
    DateTime TransactionDate,
    string SubStationName,
    string StationName,
    string OilTypeName,
    double QuantityLiters,
    string TransactionTypeName,
    string? SupplierCompany,
    string StatusName,
    string? Notes);

public record OilTransactionReportSummaryDto(
    int TotalTransactions,
    double TotalIncomingLiters,
    double TotalInternalTransferLiters,
    double TotalConsumptionLiters,
    int IncomingCount,
    int TransferCount,
    int ConsumptionCount);

// ─── Periodic Inventory Report ───────────────────────────────────────────────

public record PeriodicInventoryReportDto(
    List<PeriodicInventoryReportItemDto> Items,
    PeriodicInventoryReportSummaryDto Summary);

public record PeriodicInventoryReportItemDto(
    int Id,
    int Year,
    int Month,
    string PeriodTypeName,
    string SubStationName,
    string StationName,
    DateTime InventoryDate,
    double FuelReceivedLiters,
    double FuelConsumedLiters,
    double FuelNetDeltaLiters,
    double OilReceivedLiters,
    double OilConsumedLiters,
    double OilNetDeltaLiters,
    string StatusName);

public record PeriodicInventoryReportSummaryDto(
    int TotalRecords,
    int VerifiedCount,
    int PendingCount,
    double TotalFuelReceivedLiters,
    double TotalFuelConsumedLiters,
    double TotalOilReceivedLiters,
    double TotalOilConsumedLiters);

// ─── Efficiency Report ───────────────────────────────────────────────────────

public record EfficiencyReportDto(
    List<EfficiencyReportItemDto> Items,
    EfficiencyReportSummaryDto Summary);

public record EfficiencyReportItemDto(
    int SubStationId,
    string SubStationName,
    string StationName,
    string BranchName,
    double TotalEnergyProducedKWh,
    double TotalEnergyExportedKWh,
    double TotalFuelConsumedLiters,
    double EfficiencyPercent,
    double FuelPerKWh,
    double LoadFactor);

public record EfficiencyReportSummaryDto(
    int TotalSubStations,
    double TotalEnergyProducedKWh,
    double TotalFuelConsumedLiters,
    double AverageEfficiencyPercent,
    string MostEfficientSubStation,
    string LeastEfficientSubStation);

// ─── Hierarchical filter query ───────────────────────────────────────────────

public record ReportFilterQueryDto(
    int? BranchId,
    int? StationId,
    int? SubStationId,
    DateTime? DateFrom,
    DateTime? DateTo,
    int Page = 1,
    int PageSize = 50);

// ─── Inventory Summary ──────────────────────────────────────────────────

public record InventorySummaryDto(
    int TotalFuelTanks,
    int TotalOilTanks,
    double TotalFuelStockLiters,
    double TotalOilStockLiters,
    int LowFuelTanks,
    int LowOilTanks,
    int ActiveGenerators,
    int TotalGenerators);
