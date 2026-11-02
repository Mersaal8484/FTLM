namespace FuelTrack.Application.DTOs.Controllers;

public record ControllerDetailDto(
    int Id,
    int? GeneratorId,
    string? GeneratorName,
    string Name,
    string Code,
    string ControllerType,
    string? IpAddress,
    int? Port,
    int? ModbusUnitId,
    string? SerialNumber,
    string? FirmwareVersion,
    string? ConnectionStatus,
    double EnergyReading,
    double OperatingHours,
    string? RegisterType,
    bool AutoConnect,
    string? EnergyUnit,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);
