namespace FuelTrack.Domain.Services;

public static class TankVolumeService
{
    public static decimal UsableLitres(decimal measuredLitres, decimal deadStockLitres) =>
        Math.Max(0m, measuredLitres - deadStockLitres);

    public static bool IsAtOrBelowDeadStock(decimal measuredLitres, decimal deadStockLitres) =>
        measuredLitres <= deadStockLitres;
}
