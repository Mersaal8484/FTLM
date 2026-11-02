using FuelTrack.Application.DTOs.Warehouse;
using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.API.Controllers;

[Authorize]
[ApiController]
[Route("api/warehouse/transactions")]
public class WarehouseTransactionsController : BaseApiController
{
    private readonly IWarehouseTransactionService _warehouseTransactionService;

    public WarehouseTransactionsController(IWarehouseTransactionService warehouseTransactionService)
    {
        _warehouseTransactionService = warehouseTransactionService;
    }

    [HttpGet("by-part/{partId:int}")]
    public async Task<IActionResult> GetByPart(
        int partId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _warehouseTransactionService.GetByPartAsync(partId, page, pageSize);
        return Ok(result);
    }

    [HttpGet("by-station/{stationId:int}")]
    public async Task<IActionResult> GetByStation(
        int stationId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _warehouseTransactionService.GetByStationAsync(stationId, startDate, endDate, page, pageSize);
        return Ok(result);
    }
}
