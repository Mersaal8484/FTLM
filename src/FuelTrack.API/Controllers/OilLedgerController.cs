using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.API.Controllers;

[Authorize]
[ApiController]
[Route("api/oil-ledger")]
public sealed class OilLedgerController(IOilLedgerService ledger) : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> GetMonth(
        [FromQuery] int year,
        [FromQuery] int month,
        [FromQuery] int? oilLookupId,
        CancellationToken cancellationToken)
    {
        if (year is < 2000 or > 2100 || month is < 1 or > 12)
            return BadRequest("Year must be 2000-2100 and month must be 1-12.");
        return Ok(await ledger.GetMonthlyLedgerAsync(year, month, oilLookupId, cancellationToken));
    }

    [HttpGet("day")]
    public async Task<IActionResult> GetDay(
        [FromQuery] DateOnly date,
        [FromQuery] int? oilLookupId,
        CancellationToken cancellationToken) =>
        Ok(await ledger.GetDayTransactionsAsync(date, oilLookupId, cancellationToken));
}
