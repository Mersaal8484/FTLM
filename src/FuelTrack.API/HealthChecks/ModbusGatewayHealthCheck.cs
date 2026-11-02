using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FuelTrack.API.HealthChecks;

public class ModbusGatewayHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _gatewayUrl;

    public ModbusGatewayHealthCheck(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _gatewayUrl = configuration["ModbusGateway:Url"] ?? string.Empty;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_gatewayUrl))
            return HealthCheckResult.Healthy("Modbus gateway URL not configured");

        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"{_gatewayUrl}/health", cancellationToken);
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("Modbus gateway is healthy")
                : HealthCheckResult.Unhealthy($"Modbus gateway returned status {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Modbus gateway check failed", ex);
        }
    }
}
