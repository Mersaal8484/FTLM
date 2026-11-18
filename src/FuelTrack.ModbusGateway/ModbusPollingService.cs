using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Sockets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NModbus;

namespace FuelTrack.ModbusGateway;

public class ModbusPollingService : BackgroundService
{
    private readonly ILogger<ModbusPollingService> _logger;
    private readonly IConfiguration _configuration;

    public ModbusPollingService(ILogger<ModbusPollingService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        int intervalSeconds = _configuration.GetValue<int>("ModbusGateway:PollIntervalSeconds", 5);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollDevicesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during Modbus poll cycle.");
            }

            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
        }
    }

    private async Task PollDevicesAsync(CancellationToken cancellationToken)
    {
        var devices = _configuration.GetSection("ModbusGateway:Devices").GetChildren();
        var factory = new ModbusFactory();

        foreach (var device in devices)
        {
            if (cancellationToken.IsCancellationRequested) break;

            var ip = device["IpAddress"];
            var port = device.GetValue<int>("Port", 502);
            var deviceId = device.GetValue<byte>("DeviceId", 1);
            var registerAddress = device.GetValue<ushort>("RegisterAddress", 0);
            var generatorId = device.GetValue<int?>("GeneratorId");
            
            if (string.IsNullOrEmpty(ip)) continue;

            try
            {
                using var client = new TcpClient(ip, port);
                var master = factory.CreateMaster(client);
                
                master.Transport.ReadTimeout = 2000;
                
                ushort[] registers = master.ReadHoldingRegisters(deviceId, registerAddress, 2);
                float value = InterpretFloat(registers);

                _logger.LogInformation("Device {Ip}:{Port} Unit {Id} => Register {Reg} Value: {Val}", 
                    ip, port, deviceId, registerAddress, value);

                if (generatorId.HasValue)
                {
                    await SaveReadingToApiAsync(generatorId.Value, value, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to read from device {Ip}:{Port}. Error: {Message}", ip, port, ex.Message);
            }
        }
    }

    private async Task SaveReadingToApiAsync(int generatorId, float fuelValue, CancellationToken ct)
    {
        try
        {
            var apiUrl = _configuration["ApiUrl"] ?? "http://localhost:5248";
            var apiKey = _configuration["ModbusGateway:ApiKey"];

            using var httpClient = new HttpClient();
            if (!string.IsNullOrEmpty(apiKey))
            {
                httpClient.DefaultRequestHeaders.Add("X-Modbus-Key", apiKey);
            }

            var payload = new
            {
                GeneratorId = generatorId,
                FuelLevelLiters = fuelValue,
                ReadingTimestamp = DateTime.UtcNow
            };

            var response = await httpClient.PostAsJsonAsync($"{apiUrl}/api/readings/modbus", payload, ct);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save Modbus reading to API for generator {GeneratorId}", generatorId);
        }
    }

    private static float InterpretFloat(ushort[] registers)
    {
        if (registers == null || registers.Length < 2) return 0f;
        byte[] bytes = new byte[4];
        BitConverter.GetBytes(registers[0]).CopyTo(bytes, 0);
        BitConverter.GetBytes(registers[1]).CopyTo(bytes, 2);
        return BitConverter.ToSingle(bytes, 0);
    }
}
