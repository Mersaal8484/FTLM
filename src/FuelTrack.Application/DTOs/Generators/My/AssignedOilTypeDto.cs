namespace FuelTrack.Application.DTOs.Generators.My;

public record AssignedOilTypeDto(
    int Id,
    string Name,
    decimal CurrentStock,
    decimal MinStock);
