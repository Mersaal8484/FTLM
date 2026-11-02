namespace FuelTrack.Application.DTOs.Modbus;

public record ModbusReadingDto(
    int GeneratorId,
    float FuelLevelLiters,
    DateTime ReadingTimestamp);
