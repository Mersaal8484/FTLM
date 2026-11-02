using FuelTrack.Application.DTOs.Readings;

namespace FuelTrack.Application.Interfaces;

public interface IReadingService
{
    Task<ReadingDetailDto> CreateAsync(CreateReadingDto dto, int userId, bool isAdmin = false);
    Task<ReadingDetailDto> UpdateAsync(int id, UpdateReadingDto dto, int userId, bool isAdmin = false);
    Task<ReadingDetailDto> ApproveAsync(int id, ApproveReadingDto dto);
    Task<ReadingDetailDto> RejectAsync(int id, RejectReadingDto dto);
    Task<ReadingDetailDto> ReviewAsync(int id, int userId);
    Task DeleteAsync(int id);
    Task<ReadingDetailDto?> GetByIdAsync(int id);
    Task<PagedResult<ReadingListDto>> ListAsync(ReadingFilterDto filter, int? userId = null, bool isAdmin = false, CancellationToken ct = default);
    Task<List<PendingReadingDto>> GetPendingAsync(CancellationToken ct = default);
    Task<ReadingConsumptionDto> GetConsumptionAsync(int id);
    Task<ReadingDetailDto> SaveModbusReadingAsync(int generatorId, float fuelLevelLiters, DateTime readingTimestamp);
}
