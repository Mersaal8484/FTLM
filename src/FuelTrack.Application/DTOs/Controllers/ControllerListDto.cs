namespace FuelTrack.Application.DTOs.Controllers;

public record ControllerListDto(
    int Id,
    string Name,
    string Code,
    string ControllerType,
    string? ConnectionStatus,
    string? GeneratorName,
    int? GeneratorId,
    string? SerialNumber,
    string? IpAddress,
    int? Port,
    int? ModbusUnitId,
    double EnergyReading,
    double OperatingHours,
    string? RegisterType,
    bool AutoConnect,
    string? EnergyUnit,
    bool IsActive);
