using FuelTrack.Application.DTOs.Reports;

namespace FuelTrack.Application.Interfaces;

public interface IReportService
{
    // Dashboard
    Task<DashboardStatsResponseDto> GetDashboardStatsAsync(
        int? branchId, int? stationId, int? subStationId,
        DateTime? dateFrom, DateTime? dateTo, string timeZoneId = "Asia/Riyadh");

    // Fuel & Oil Dashboard
    Task<FuelOilDashboardDto> GetFuelOilDashboardAsync(
        int? branchId, int? stationId, int? subStationId,
        DateTime? dateFrom, DateTime? dateTo, string timeZoneId = "Asia/Riyadh");

    // Specialized Reports (all paginated)
    Task<GeneratorsReportDto> GetGeneratorsReportAsync(
        int? branchId, int? stationId, int? subStationId,
        DateTime? dateFrom, DateTime? dateTo,
        int page, int pageSize, string timeZoneId = "Asia/Riyadh");

    Task<StationReadingsReportDto> GetStationReadingsReportAsync(
        int? branchId, int? stationId, int? subStationId,
        DateTime? dateFrom, DateTime? dateTo,
        int page, int pageSize, string timeZoneId = "Asia/Riyadh");

    Task<FuelTransactionsReportDto> GetFuelTransactionsReportAsync(
        int? branchId, int? stationId, int? subStationId,
        DateTime? dateFrom, DateTime? dateTo,
        int page, int pageSize, string timeZoneId = "Asia/Riyadh",
        string? transactionTypeCode = null);

    Task<OilTransactionsReportDto> GetOilTransactionsReportAsync(
        int? branchId, int? stationId, int? subStationId,
        DateTime? dateFrom, DateTime? dateTo,
        int page, int pageSize, string timeZoneId = "Asia/Riyadh");

    Task<PeriodicInventoryReportDto> GetPeriodicInventoryReportAsync(
        int? branchId, int? stationId, int? subStationId,
        DateTime? dateFrom, DateTime? dateTo,
        int page, int pageSize, string timeZoneId = "Asia/Riyadh");

    Task<EfficiencyReportDto> GetEfficiencyReportAsync(
        int? branchId, int? stationId, int? subStationId,
        DateTime? dateFrom, DateTime? dateTo,
        int page, int pageSize, string timeZoneId = "Asia/Riyadh");

    Task<FuelTransactionsReportDto> GetFuelIncomingReportAsync(
        int? branchId, int? stationId, int? subStationId,
        DateTime? dateFrom, DateTime? dateTo,
        int page, int pageSize, string timeZoneId = "Asia/Riyadh");

    Task<FuelTransactionsReportDto> GetFuelTransferReportAsync(
        int? branchId, int? stationId, int? subStationId,
        DateTime? dateFrom, DateTime? dateTo,
        int page, int pageSize, string timeZoneId = "Asia/Riyadh");

    Task<FuelTransactionsReportDto> GetFuelLossReportAsync(
        int? branchId, int? stationId, int? subStationId,
        DateTime? dateFrom, DateTime? dateTo,
        int page, int pageSize, string timeZoneId = "Asia/Riyadh");

    Task<InventorySummaryDto> GetInventorySummaryAsync(
        int? branchId, int? stationId, int? subStationId,
        DateTime? dateFrom, DateTime? dateTo,
        string timeZoneId = "Asia/Riyadh");
}
