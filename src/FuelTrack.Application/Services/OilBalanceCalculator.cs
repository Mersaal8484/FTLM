using FuelTrack.Application.Constants;

namespace FuelTrack.Application.Services;

/// <summary>Shared, deterministic oil book-balance arithmetic.</summary>
public static class OilBalanceCalculator
{
    public static decimal Round(decimal value) => Math.Round(value, 3, MidpointRounding.AwayFromZero);

    public static decimal ToDecimal(double value) => Round(Convert.ToDecimal(value));

    public static decimal CalculateClosing(
        decimal opening,
        decimal inbound,
        decimal outbound,
        decimal transferIn,
        decimal transferOut,
        decimal loss,
        decimal consumed = 0m) =>
        Round(opening + inbound + transferIn - outbound - transferOut - loss - consumed);

    public static bool IsInbound(string? typeCode) => typeCode is "Purchase" or "TransferIn";

    public static bool IsOutbound(string? typeCode) => typeCode is "Sale" or "TransferOut" or "Loss" or "Consumption" or OilTransactionTypeCodes.MaintenanceUse or OilTransactionTypeCodes.Consumption;
}
