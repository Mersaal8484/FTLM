using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Stocktakes;
using FuelTrack.Domain.Enums;

namespace FuelTrack.Application.Interfaces;

public interface IStationStocktakeService
{
    Task<Result<StationStocktakeDto>> CreateAsync(CreateStationStocktakeDto dto, int userId, CancellationToken cancellationToken = default);
    Task<Result<StationStocktakeDto>> GetDraftAsync(DateOnly stocktakeDate, int userId, CancellationToken cancellationToken = default);
    Task<Result<StationStocktakeDto>> GetAsync(int id, int userId, CancellationToken cancellationToken = default);
    Task<Result<StationStocktakeListDto>> ListAsync(StocktakeStatus? status, DateOnly? fromDate, DateOnly? toDate, int page, int pageSize, int userId, CancellationToken cancellationToken = default);
    Task<Result<StationStocktakeDto>> ApproveAsync(int id, int userId, CancellationToken cancellationToken = default);
    Task<Result<StationStocktakeDto>> RejectAsync(int id, string reason, int userId, CancellationToken cancellationToken = default);
    Task<Result<int>> GetPhotoAsync(int stocktakeId, int attachmentId, int userId, CancellationToken cancellationToken = default);
}
