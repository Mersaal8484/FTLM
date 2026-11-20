namespace FuelTrack.Application.DTOs.OilLedger;

public sealed record OilLedgerDayDto(
    DateOnly Date,
    int OilLookupId,
    string OilLookupName,
    decimal Opening,
    decimal Inbound,
    decimal Outbound,
    decimal TransferIn,
    decimal TransferOut,
    decimal Loss,
    decimal Closing);

public sealed record OilLedgerMonthDto(int Year, int Month, IReadOnlyList<OilLedgerDayDto> Rows);

public sealed record OilLedgerTransactionDto(
    int Id,
    DateTime TransactionDate,
    int OilLookupId,
    string OilLookupName,
    string TypeCode,
    string TypeName,
    decimal QuantityLiters,
    string? Reference,
    string? Supplier,
    string? Notes);
