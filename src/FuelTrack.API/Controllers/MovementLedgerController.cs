using FuelTrack.Application.DTOs.Movements;
using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.API.Controllers;

[Authorize]
[ApiController]
[Route("api/movement-ledger")]
public sealed class MovementLedgerController(IMovementLedgerService service) : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int? stationId,
        [FromQuery] int? subStationId,
        [FromQuery] string? kind,
        [FromQuery] string? movementType,
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] string? sort = "date",
        [FromQuery] string? sortDirection = "desc",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await service.GetAsync(new MovementLedgerFilterDto(
            dateFrom, dateTo, stationId, subStationId, kind, movementType,
            status, search, sort, sortDirection, page, pageSize), cancellationToken);
        return Ok(result);
    }
}
