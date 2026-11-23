namespace FuelTrack.Application.Services;

/// <summary>Shared, deterministic oil book-balance arithmetic.</summary>
public static class OilBalanceCalculator
{
    public enum MovementKind
    {
        None,
        Inbound,
        TransferIn,
        Outbound,
        TransferOut,
        Loss
    }

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

    public static MovementKind Classify(string? typeCode) => typeCode switch
    {
        "Purchase" => MovementKind.Inbound,
        "TransferIn" => MovementKind.TransferIn,
        "Sale" or "Consumption" => MovementKind.Outbound,
        "TransferOut" => MovementKind.TransferOut,
        "Loss" => MovementKind.Loss,
        _ => MovementKind.None
    };

    public static bool IsInbound(string? typeCode) => Classify(typeCode) is MovementKind.Inbound or MovementKind.TransferIn;

    /// <summary>All codes which reduce book balance, including transfer-out and loss.</summary>
    public static bool IsOutbound(string? typeCode) => Classify(typeCode) is MovementKind.Outbound or MovementKind.TransferOut or MovementKind.Loss;

    /// <summary>Outbound movements represented by the ledger's direct-outbound column.</summary>
    public static bool IsDirectOutbound(string? typeCode) => Classify(typeCode) == MovementKind.Outbound;
}
