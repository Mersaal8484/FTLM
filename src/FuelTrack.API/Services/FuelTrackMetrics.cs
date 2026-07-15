using System.Diagnostics.Metrics;

namespace FuelTrack.API.Services;

public class FuelTrackMetrics
{
    private readonly Counter<long> _fuelMovementsCreated;
    private readonly Counter<long> _readingsSubmitted;
    private readonly Counter<long> _inventoryCreated;
    private readonly Histogram<double> _apiRequestDuration;
    private readonly Counter<long> _authAttempts;

    public FuelTrackMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("FuelTrack.API");

        _fuelMovementsCreated = meter.CreateCounter<long>(
            "fueltrack.fuel_movements.created",
            description: "Total fuel movements created");

        _readingsSubmitted = meter.CreateCounter<long>(
            "fueltrack.readings.submitted",
            description: "Total readings submitted");

        _inventoryCreated = meter.CreateCounter<long>(
            "fueltrack.inventory.created",
            description: "Total periodic inventories created");

        _apiRequestDuration = meter.CreateHistogram<double>(
            "fueltrack.api.request_duration_ms",
            unit: "ms",
            description: "API request duration in milliseconds");

        _authAttempts = meter.CreateCounter<long>(
            "fueltrack.auth.attempts",
            description: "Total authentication attempts");
    }

    public void FuelMovementCreated(string subStation) =>
        _fuelMovementsCreated.Add(1, [new KeyValuePair<string, object?>("sub_station", subStation)]);

    public void ReadingSubmitted(string generator) =>
        _readingsSubmitted.Add(1, [new KeyValuePair<string, object?>("generator", generator)]);

    public void InventoryCreated(string subStation, string periodType) =>
        _inventoryCreated.Add(1,
        [
            new KeyValuePair<string, object?>("sub_station", subStation),
            new KeyValuePair<string, object?>("period", periodType)
        ]);

    public void RecordApiRequest(double durationMs, string method, string endpoint, int statusCode) =>
        _apiRequestDuration.Record(durationMs,
        [
            new KeyValuePair<string, object?>("method", method),
            new KeyValuePair<string, object?>("endpoint", endpoint),
            new KeyValuePair<string, object?>("status", statusCode.ToString())
        ]);

    public void AuthAttempt(string result) =>
        _authAttempts.Add(1, [new KeyValuePair<string, object?>("result", result)]);
}
