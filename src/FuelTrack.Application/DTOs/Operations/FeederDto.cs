namespace FuelTrack.Application.DTOs.Operations;

public sealed record CreateFeederDto(int SubStationId, string Code, string Name, decimal? MeterMultiplier, int? MeterId);
public sealed record FeederDto(int Id, int SubStationId, string Code, string Name, decimal MeterMultiplier, int? MeterId);
