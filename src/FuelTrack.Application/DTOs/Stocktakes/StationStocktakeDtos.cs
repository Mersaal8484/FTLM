using FuelTrack.Domain.Enums;

namespace FuelTrack.Application.DTOs.Stocktakes;

public sealed record CreateStationStocktakeDto(
    DateOnly StocktakeDate,
    string? Notes,
    Guid ClientRequestId,
    IReadOnlyList<CreateStationStocktakeFuelLineDto> FuelLines,
    IReadOnlyList<CreateStationStocktakeOilLineDto> OilLines,
    IReadOnlyList<int> AttachmentIds);

public sealed record CreateStationStocktakeFuelLineDto(int FuelTankId, decimal CountedLiters, decimal? DipLevelCm);
public sealed record CreateStationStocktakeOilLineDto(int OilId, decimal CountedLiters);
public sealed record RejectStationStocktakeDto(string Reason);

public sealed record StationStocktakeFuelLineDto(
    int FuelTankId, string TankName, decimal CountedLiters, decimal BookLiters,
    decimal DeadStockLiters, decimal UsableCountedLiters, decimal VarianceLiters, decimal? DipLevelCm);

public sealed record StationStocktakeOilLineDto(
    int OilId, int OilLookupId, string OilName, decimal CountedLiters, decimal BookLiters, decimal VarianceLiters);

public sealed record StationStocktakeDto(
    int Id, DateOnly StocktakeDate, StocktakeStatus Status, int SubmittedByUserId,
    DateTime SubmittedAt, int? ReviewedByUserId, DateTime? ReviewedAt, string? RejectionReason,
    string? Notes, IReadOnlyList<int> AttachmentIds, IReadOnlyList<StationStocktakeFuelLineDto> FuelLines,
    IReadOnlyList<StationStocktakeOilLineDto> OilLines);

public sealed record StationStocktakeListItemDto(
    int Id, DateOnly StocktakeDate, StocktakeStatus Status, int SubmittedByUserId,
    DateTime SubmittedAt, DateTime? ReviewedAt, decimal TotalFuelVarianceLiters, decimal TotalOilVarianceLiters);

public sealed record StationStocktakeListDto(
    IReadOnlyList<StationStocktakeListItemDto> Items, int TotalCount, int Page, int PageSize);
