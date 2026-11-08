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

    [HttpGet("generators/{generatorId:int}/daily-log/{businessDate}.csv")]
    public async Task<IActionResult> ExportDailyLog(int generatorId, DateOnly businessDate, CancellationToken ct)
    {
        var result = await service.GetDailyLogAsync(generatorId, businessDate, GetUserId(), ct);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, new { error = result.Error });
        var sb = new StringBuilder();
        sb.AppendLine("captured_at_utc,active_load_kw,apparent_load_kva,power_factor,frequency_hz,voltage_v,current_a,oil_pressure_bar,water_temp_c,oil_temp_c,peak_load_kw,warnings");
        foreach (var r in result.Value!.Rows)
        {
            var warnings = string.Join("; ", r.Markers ?? []);
            sb.AppendLine($"\"{r.CapturedAtUtc:O}\",{r.ActiveLoadKw},{r.ApparentLoadKva},{r.PowerFactor},{r.FrequencyHz},{r.LineVoltageVolts},{r.CurrentAmps},{r.OilPressure},{r.WaterTemperatureCelsius},{r.OilTemperatureCelsius},{r.PeakLoadKw},\"{warnings}\"");
        }
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv; charset=utf-8", $"daily-log-{generatorId}-{businessDate:yyyy-MM-dd}.csv");
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
