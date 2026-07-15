namespace FuelTrack.Application.DTOs.Controllers;

public record CreateControllerDto(
    int? GeneratorId,
    string Name,
    string Code,
    string ControllerType,
    string? IpAddress,
    int? Port,
    int? ModbusUnitId,
    string? SerialNumber,
    string? FirmwareVersion,
    double EnergyReading,
    double OperatingHours,
    string? RegisterType,
    bool AutoConnect,
    string? EnergyUnit);
