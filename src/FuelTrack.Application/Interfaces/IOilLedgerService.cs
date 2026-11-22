using FuelTrack.Application.DTOs.OilLedger;

namespace FuelTrack.Application.Interfaces;

public interface IOilLedgerService
{
    Task<OilLedgerMonthDto> GetMonthlyLedgerAsync(int year, int month, int? oilLookupId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OilLedgerTransactionDto>> GetDayTransactionsAsync(DateOnly date, int? oilLookupId, CancellationToken cancellationToken = default);
    Task<decimal> GetCurrentBalanceAsync(int subStationId, int oilLookupId, CancellationToken cancellationToken = default);
}
