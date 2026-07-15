namespace FuelTrack.Application.DTOs.Reports;

public record GeneratorReadingReportDto(
    DateTime Date,
    string GeneratorName,
    string SubStationName,
    string Shift,
    string Status,
    double RunningHours,
    double EnergyKWh,
    double LoadKW,
    double FuelLevelPct,
    string? Notes);

public record FuelMovementReportDto(
    DateTime Date,
    string? From,
    string? To,
    double Quantity,
    string? Reference,
    string Status);

public record OilTransactionReportDto(
    DateTime Date,
    string SubStationName,
    string OilType,
    double QuantityLiters,
    string TransactionType,
    string? Reference);

public record StationReadingReportDto(
    DateTime Date,
    string StationName,
    double? EnergyConsumedKWh,
    int Shift,
    string? Notes);

public record FullDailyReportResultDto(
    List<GeneratorReadingReportDto> Generators,
    List<OilTransactionReportDto> Oil,
    List<FuelMovementReportDto> Fuel,
    List<StationReadingReportDto> StationReadings,
    List<object> InventoryEfficiency);

public record LegacyPeriodicInventoryReportDto(
    string SiteLabel,
    double EnergyProducedKwh,
    int OperatingHours,
    int RatedCapacityKw,
    int ActualCapacityKw);

public record PeriodicFuelSummaryDto(
    string SubStationName,
    double TotalReceivedLiters,
    double TotalTransferredLiters,
    double TotalConsumedLiters);

public record PeriodicOilSummaryDto(
    string SubStationName,
    double TotalQuantity);

public record PeriodicReportResultDto(
    List<LegacyPeriodicInventoryReportDto> SiteGenerationReport,
    List<PeriodicFuelSummaryDto> FuelReport,
    List<PeriodicOilSummaryDto> OilReport);

public record FuelIncomingItemDto(
    int Id,
    DateTime Date,
    string SubStation,
    string FuelType,
    double Quantity,
    string? Reference,
    string Status);

public record FuelTransferItemDto(
    int Id,
    DateTime Date,
    string FromSubStation,
    string ToSubStation,
    string FuelType,
    double Quantity,
    string? Reference,
    string Status);

public record FuelLossItemDto(
    int Id,
    DateTime Date,
    string SubStation,
    string FuelType,
    double Quantity,
    string? Reference,
    string Status,
    string? Notes);
