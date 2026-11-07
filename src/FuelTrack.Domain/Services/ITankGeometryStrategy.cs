using FuelTrack.Domain.Enums;

namespace FuelTrack.Domain.Services;

/// <summary>Extensibility seam for future tank shapes; current operations use only the confirmed vertical-cone shape.</summary>
public interface ITankGeometryStrategy
{
    TankShape Shape { get; }
    decimal CalculateLitres(decimal diameterCm, decimal cylinderHeightCm, decimal coneHeightCm, decimal factor, decimal liquidLevelCm);
}
