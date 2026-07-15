namespace FuelTrack.Application.DTOs.Oils;

public record OilListDto(
    int Id,
    int OilLookupId,
    string OilLookupName,
    decimal CurrentStock,
    decimal MinStock,
    int? StationId,
    string? StationName,
    int? SubStationId,
    string? SubStationName)
{
    public string Name => OilLookupName;
}
