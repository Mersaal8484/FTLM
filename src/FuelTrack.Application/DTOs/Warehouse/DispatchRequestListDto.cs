namespace FuelTrack.Application.DTOs.Warehouse;

public record DispatchRequestListDto(
    int Id,
    string RequestNumber,
    string SubStationName,
    string StationName,
    DateTime RequestDate,
    string StatusName,
    string RequestedByName,
    int RequestedById,
    int ItemCount,
    string? WorkPermitNumber,
    int? WorkPermitId,
    string? Purpose,
    DateTime CreatedAt);

public record DispatchRequestDetailDto(
    int Id,
    string RequestNumber,
    int SubStationId,
    string SubStationName,
    int StationId,
    string StationName,
    DateTime RequestDate,
    int RequestedById,
    string RequestedByName,
    int? ApprovedById,
    string? ApprovedByName,
    DateTime? ApprovalDate,
    int? WorkPermitId,
    string? WorkPermitNumber,
    int StatusLookupId,
    string StatusName,
    string? Purpose,
    string? Notes,
    DateTime CreatedAt,
    List<DispatchItemDetailDto> Items);
