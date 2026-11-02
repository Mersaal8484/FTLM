namespace FuelTrack.Application.Interfaces;

public interface IAlertGeneratorService
{
    Task<int> CheckFuelStockLevelsAsync(CancellationToken ct = default);
    Task<int> CheckOilStockLevelsAsync(CancellationToken ct = default);
    Task<int> CheckInstalledPartsReplacementAsync(CancellationToken ct = default);
}
