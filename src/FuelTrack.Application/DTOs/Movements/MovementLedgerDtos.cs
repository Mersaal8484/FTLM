namespace FuelTrack.Application.DTOs.Movements;

public sealed record MovementLedgerRowDto(
    int Id,
    string Kind,
    DateTime Date,
    int StationId,
    string Station,
    int SubStationId,
    string SubStation,
    string MovementType,
    string? MovementTypeCode,
    double Quantity,
    string Unit,
    string? Reference,
    string Status,
    string? CreatedBy,
    string? ApprovedBy,
    DateTime CreatedAt);

public sealed record MovementLedgerFilterDto(
    DateTime? DateFrom,
    DateTime? DateTo,
    int? StationId,
    int? SubStationId,
    string? Kind,
    string? MovementType,
    string? Status,
    string? Search,
    string? Sort,
    string? SortDirection,
    int Page = 1,
    int PageSize = 20);
