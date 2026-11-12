using FuelTrack.Application.DTOs.FuelMovements;
using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace FuelTrack.API.Controllers;

[Authorize]
[ApiController]
[Route("api/fuel-movements")]
public class FuelMovementsController : BaseApiController
{
    private readonly IFuelMovementService _service;
    private readonly IRealtimeNotifier _notifier;
    private readonly ISubStationService _subStationService;
    private readonly IStockSummaryService _stockSummaryService;

    public FuelMovementsController(
        IFuelMovementService service,
        IRealtimeNotifier notifier,
        ISubStationService subStationService,
        IStockSummaryService stockSummaryService)
    {
        _service = service;
        _notifier = notifier;
        _subStationService = subStationService;
        _stockSummaryService = stockSummaryService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFuelMovementDto dto)
    {
        var userId = GetUserId();
        var result = await _service.CreateAsync(dto, userId);
        await _notifier.NotifyAllAsync("FuelMovementCreated", new { result.Id });
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateFuelMovementDto dto)
    {
        var userId = GetUserId();
        var result = await _service.UpdateAsync(id, dto, userId);
        await _notifier.NotifyAllAsync("FuelMovementUpdated", new { result.Id });
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result is null) return NotFound();
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] string? type = null)
    {
        var result = await _service.GetPagedAsync(page, pageSize, search, status, type);
        return Ok(result);
    }

    [HttpGet("by-substation/{subStationId:int}")]
    public async Task<IActionResult> GetBySubStation(int subStationId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _service.GetPagedBySubStationAsync(subStationId, page, pageSize);
        return Ok(result);
    }

    [HttpGet("by-fuel-tank/{fuelTankId:int}")]
    public async Task<IActionResult> GetByFuelTank(int fuelTankId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _service.GetPagedByFuelTankAsync(fuelTankId, page, pageSize);
        return Ok(result);
    }

    [HttpGet("last-by-substation/{subStationId:int}")]
    public async Task<IActionResult> GetLastBySubStation(int subStationId)
    {
        var result = await _service.GetLastBySubStationAsync(subStationId);
        if (result is null) return Ok(null);
        return Ok(result);
    }

    [HttpPost("{id:int}/submit")]
    public async Task<IActionResult> Submit(int id)
    {
        var userId = GetUserId();
        var result = await _service.SubmitForApprovalAsync(id, userId);
        await _notifier.NotifyAllAsync("FuelMovementSubmitted", new { result.Id });
        return Ok(result);
    }

    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        var userId = GetUserId();
        var result = await _service.ApproveMovementAsync(id, userId);
        await _notifier.NotifyAllAsync("FuelMovementApproved", new { result.Id });
        return Ok(result);
    }

    [HttpPost("{id:int}/reject")]
    public async Task<IActionResult> Reject(int id, [FromBody] FuelTrack.Application.DTOs.Readings.RejectReadingDto? dto = null)
    {
        var userId = GetUserId();
        var result = await _service.RejectMovementAsync(id, userId, dto?.RejectionReason);
        await _notifier.NotifyAllAsync("FuelMovementRejected", new { result.Id });
        return Ok(result);
    }

    [HttpGet("transfer-destinations")]
    public async Task<IActionResult> GetTransferDestinations([FromQuery] int? excludeSubStationId = null)
    {
        var subStations = await _subStationService.GetAllAsync();
        if (excludeSubStationId.HasValue)
            subStations = subStations.Where(s => s.Id != excludeSubStationId.Value).ToList();
        return Ok(subStations);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        await _notifier.NotifyAllAsync("FuelMovementDeleted", new { Id = id });
        return NoContent();
    }

    [HttpGet("fuel-stock-summary")]
    public async Task<IActionResult> GetFuelStockSummary([FromQuery] string from, [FromQuery] string to)
    {
        if (!DateTime.TryParseExact(from, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fromDate)
            || !DateTime.TryParseExact(to, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var toDate))
        {
            return BadRequest(new { message = "Invalid date format. Use yyyy-MM-dd.", receivedFrom = from, receivedTo = to });
        }

        var userId = GetUserId();
        var result = await _stockSummaryService.GetFuelStockSummaryAsync(fromDate, toDate, userId);

        var etag = Convert.ToBase64String(System.Security.Cryptography.MD5.HashData(
            System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(result)));

        if (Request.Headers.IfNoneMatch == $"\"{etag}\"")
            return StatusCode(StatusCodes.Status304NotModified);

        Response.Headers.ETag = $"\"{etag}\"";
        Response.Headers.CacheControl = "no-store";
        return Ok(result);
    }

    [HttpGet("oil-stock-summary")]
    public async Task<IActionResult> GetOilStockSummary([FromQuery] string from, [FromQuery] string to)
    {
        if (!DateTime.TryParseExact(from, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fromDate)
            || !DateTime.TryParseExact(to, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var toDate))
        {
            return BadRequest(new { message = "Invalid date format. Use yyyy-MM-dd.", receivedFrom = from, receivedTo = to });
        }

        var userId = GetUserId();
        var result = await _stockSummaryService.GetOilStockSummaryAsync(fromDate, toDate, userId);

        var etag = Convert.ToBase64String(System.Security.Cryptography.MD5.HashData(
            System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(result)));

        if (Request.Headers.IfNoneMatch == $"\"{etag}\"")
            return StatusCode(StatusCodes.Status304NotModified);

        Response.Headers.ETag = $"\"{etag}\"";
        Response.Headers.CacheControl = "no-store";
        return Ok(result);
    }
}
