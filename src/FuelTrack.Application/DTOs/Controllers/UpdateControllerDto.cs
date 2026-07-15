namespace FuelTrack.Application.DTOs.Controllers;

public record UpdateControllerDto(
    int? GeneratorId,
    string? Name,
    string? Code,
    string? ControllerType,
    string? IpAddress,
    int? Port,
    int? ModbusUnitId,
    string? SerialNumber,
    string? FirmwareVersion);
