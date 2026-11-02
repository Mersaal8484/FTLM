using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Warehouse;

namespace FuelTrack.Application.Interfaces;

public interface ISparePartService
{
    Task<Result<SparePartDetailDto>> CreateAsync(CreateSparePartDto dto, int userId, CancellationToken ct = default);
    Task<Result<SparePartDetailDto>> UpdateAsync(int id, UpdateSparePartDto dto, int userId, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
    Task<SparePartDetailDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<List<SparePartListDto>> GetAllAsync(int? stationId, CancellationToken ct = default);
    Task<PagedResult<SparePartListDto>> GetPagedAsync(int page, int pageSize, string? search, int? categoryId, string? category, CancellationToken ct = default);
    Task<List<WarehouseStockDto>> GetStockByPartAsync(int sparePartId, CancellationToken ct = default);
    Task<string> ExportCsvAsync(string? search, int? categoryId, string? category, CancellationToken ct = default);
}
