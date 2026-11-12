using FuelTrack.Application.DTOs.Movements;

namespace FuelTrack.Application.Interfaces;

public interface IMovementLedgerService
{
    Task<PagedResult<MovementLedgerRowDto>> GetAsync(
        MovementLedgerFilterDto filter,
        CancellationToken cancellationToken = default);
}
