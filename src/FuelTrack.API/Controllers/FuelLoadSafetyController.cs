using System.Text;
using FuelTrack.Application.DTOs.Operations;
using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.API.Controllers;

[Authorize]
[ApiController]
[Route("api/fuel-load-safety")]
public sealed class FuelLoadSafetyController(IFuelLoadSafetyService service) : BaseApiController
{
    [HttpGet("generators/{generatorId:int}/consumption")]
    public async Task<IActionResult> Consumption(int generatorId, [FromQuery] DateTime fromUtc, [FromQuery] DateTime toUtc, CancellationToken ct)
    {
        var result = await service.GetConsumptionAsync(generatorId, fromUtc, toUtc, GetUserId(), ct);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(result.StatusCode, new { error = result.Error });
    }

    [HttpGet("substations/{subStationId:int}/reconciliation")]
    public async Task<IActionResult> Reconciliation(int subStationId, [FromQuery] DateTime fromUtc, [FromQuery] DateTime toUtc, CancellationToken ct)
    {
        var result = await service.GetReconciliationAsync(subStationId, fromUtc, toUtc, GetUserId(), ct);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(result.StatusCode, new { error = result.Error });
    }

    [HttpGet("overloads")]
    public async Task<IActionResult> Overloads([FromQuery] int? subStationId, [FromQuery] bool includeResolved = false, CancellationToken ct = default) =>
        Ok(await service.GetOverloadsAsync(GetUserId(), subStationId, includeResolved, ct));

    [HttpGet("generators/{generatorId:int}/limits")]
    public async Task<IActionResult> GetLimits(int generatorId, CancellationToken ct)
    {
        var result = await service.GetLimitsAsync(generatorId, GetUserId(), ct);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(result.StatusCode, new { error = result.Error });
    }

    [HttpPut("generators/{generatorId:int}/limits")]
    public async Task<IActionResult> SaveLimit(int generatorId, [FromBody] SaveGeneratorOperatingLimitDto dto, CancellationToken ct)
    {
        var result = await service.SaveLimitAsync(generatorId, dto, GetUserId(), ct);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(result.StatusCode, new { error = result.Error });
    }

    [HttpGet("generators/{generatorId:int}/daily-log/{businessDate}")]
    public async Task<IActionResult> DailyLog(int generatorId, DateOnly businessDate, CancellationToken ct)
    {
        var result = await service.GetDailyLogAsync(generatorId, businessDate, GetUserId(), ct);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(result.StatusCode, new { error = result.Error });
    }

    [HttpGet("generators/{generatorId:int}/consumption.csv")]
    public async Task<IActionResult> ExportConsumption(int generatorId, [FromQuery] DateTime fromUtc, [FromQuery] DateTime toUtc, CancellationToken ct)
    {
        var result = await service.GetConsumptionAsync(generatorId, fromUtc, toUtc, GetUserId(), ct);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, new { error = result.Error });
        var item = result.Value!;
        var csv = $"generator_id,from_utc,to_utc,consumption_litres,refill_litres,produced_kwh,kwh_per_l{Environment.NewLine}" +
                  $"{item.GeneratorId},{item.FromUtc:O},{item.ToUtc:O},{item.ConsumptionLitres},{item.RefillLitres},{item.ProducedKwh},{item.KwhPerL}";
        return File(Encoding.UTF8.GetBytes(csv), "text/csv; charset=utf-8", "generator-consumption.csv");
    }
}
