using System.Security.Claims;
using FuelTrack.Application.DTOs.OilTransactions;
using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class OilTransactionsController : BaseApiController
{
    private readonly IOilTransactionService _oilTransactionService;

    public OilTransactionsController(IOilTransactionService oilTransactionService)
    {
        _oilTransactionService = oilTransactionService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOilTransactionDto dto)
    {
        var userId = GetUserId();
        var result = await _oilTransactionService.CreateAsync(dto, userId);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateOilTransactionDto dto)
    {
        var userId = GetUserId();
        var result = await _oilTransactionService.UpdateAsync(id, dto, userId);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _oilTransactionService.GetByIdAsync(id);
        if (result is null) return NotFound();
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var result = await _oilTransactionService.GetAllAsync();
        var total = result.Count;
        var paged = result.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Ok(new
        {
            data = paged,
            pagination = new { total, page, pageSize, pages = (int)Math.Ceiling(total / (double)pageSize) }
        });
    }

    [HttpGet("by-substation/{subStationId:int}")]
    public async Task<IActionResult> GetBySubStation(int subStationId)
    {
        var result = await _oilTransactionService.GetBySubStationAsync(subStationId);
        return Ok(result);
    }

    [HttpGet("by-station/{stationId:int}")]
    public async Task<IActionResult> GetByStation(int stationId)
    {
        var result = await _oilTransactionService.GetByStationAsync(stationId);
        return Ok(result);
    }

    [HttpGet("generator/{generatorId:int}")]
    public async Task<IActionResult> GetByGenerator(int generatorId)
    {
        var result = await _oilTransactionService.GetByGeneratorAsync(generatorId);
        return Ok(result);
    }

}
