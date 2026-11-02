using FuelTrack.Application.DTOs.Warehouse;
using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.API.Controllers;

[Authorize]
[ApiController]
[Route("api/spare-parts")]
public class SparePartsController : BaseApiController
{
    private readonly ISparePartService _sparePartService;

    public SparePartsController(ISparePartService sparePartService)
    {
        _sparePartService = sparePartService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] int? categoryId = null,
        [FromQuery] string? category = null)
    {
        var result = await _sparePartService.GetPagedAsync(page, pageSize, search, categoryId, category);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _sparePartService.GetByIdAsync(id);
        if (result is null)
            return NotFound();
        return Ok(result);
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAll([FromQuery] int? stationId = null)
    {
        var result = await _sparePartService.GetAllAsync(stationId);
        return Ok(result);
    }

    [HttpGet("{id:int}/stock")]
    public async Task<IActionResult> GetStock(int id)
    {
        var result = await _sparePartService.GetStockByPartAsync(id);
        return Ok(result);
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] string? search = null,
        [FromQuery] int? categoryId = null,
        [FromQuery] string? category = null)
    {
        var csv = await _sparePartService.ExportCsvAsync(search, categoryId, category);
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"spare-parts-{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSparePartDto dto)
    {
        var userId = GetUserId();
        var result = await _sparePartService.CreateAsync(dto, userId);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSparePartDto dto)
    {
        var userId = GetUserId();
        var result = await _sparePartService.UpdateAsync(id, dto, userId);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();
        var result = await _sparePartService.DeleteAsync(id);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return NoContent();
    }
}
