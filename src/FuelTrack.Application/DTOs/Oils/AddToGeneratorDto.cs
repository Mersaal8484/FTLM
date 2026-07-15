namespace FuelTrack.Application.DTOs.Oils;

public record AddToGeneratorDto(
    int GeneratorId,
    int OilId,
    double Quantity,
    string? Notes);
