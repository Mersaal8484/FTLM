using FuelTrack.Application.DTOs.Operations;

namespace FuelTrack.Application.Interfaces;

public interface IEnergyBalanceService
{
    /// <summary>
    /// Computes the station energy balance for a given date from official (IsOfficial) data only.
    /// Import = generator energy produced + solar import.
    /// Export = feeder energy sent.
    /// Internal = configured internal-consumption meter delta (null when unconfigured).
    /// Unexplained = Import - Export - Internal (or Import - Export when Internal is null).
    /// Warning threshold fires only when internal consumption IS measured.
    /// </summary>
    Task<EnergyBalanceDto> GetStationBalanceAsync(
        int stationId,
        DateOnly date,
        CancellationToken cancellationToken = default);
}
