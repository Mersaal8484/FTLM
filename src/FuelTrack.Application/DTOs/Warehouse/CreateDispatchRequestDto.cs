namespace FuelTrack.Application.DTOs.Warehouse;

public record CreateDispatchRequestDto(
    int SubStationId,
    int StationId,
    int? WorkPermitId,
    string? Purpose,
    string? Notes,
    List<CreateDispatchItemDto> Items);

public record UpdateDispatchRequestDto(
    int Id,
    int? WorkPermitId,
    string? Purpose,
    string? Notes);
