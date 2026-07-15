using FuelTrack.Application.DTOs.Reports;

namespace FuelTrack.Application.Interfaces;

public interface IReportService
{
    // Dashboard
    Task<DashboardStatsResponseDto> GetDashboardStatsAsync(
        int? branchId, int? stationId, int? subStationId,
        DateTime? dateFrom, DateTime? dateTo);

    // Fuel & Oil Dashboard
    Task<FuelOilDashboardDto> GetFuelOilDashboardAsync(
        int? branchId, int? stationId, int? subStationId,
        DateTime? dateFrom, DateTime? dateTo);

    // Specialized Reports (all paginated)
    Task<GeneratorsReportDto> GetGeneratorsReportAsync(
        int? branchId, int? stationId, int? subStationId,
        DateTime? dateFrom, DateTime? dateTo,
        int page, int pageSize);

    Task<StationReadingsReportDto> GetStationReadingsReportAsync(
        int? branchId, int? stationId, int? subStationId,
        DateTime? dateFrom, DateTime? dateTo,
        int page, int pageSize);

    Task<FuelTransactionsReportDto> GetFuelTransactionsReportAsync(
        int? branchId, int? stationId, int? subStationId,
        DateTime? dateFrom, DateTime? dateTo,
        int page, int pageSize);

    Task<OilTransactionsReportDto> GetOilTransactionsReportAsync(
        int? branchId, int? stationId, int? subStationId,
        DateTime? dateFrom, DateTime? dateTo,
        int page, int pageSize);

    Task<PeriodicInventoryReportDto> GetPeriodicInventoryReportAsync(
        int? branchId, int? stationId, int? subStationId,
        DateTime? dateFrom, DateTime? dateTo,
        int page, int pageSize);

    Task<EfficiencyReportDto> GetEfficiencyReportAsync(
        int? branchId, int? stationId, int? subStationId,
        DateTime? dateFrom, DateTime? dateTo,
        int page, int pageSize);
}
