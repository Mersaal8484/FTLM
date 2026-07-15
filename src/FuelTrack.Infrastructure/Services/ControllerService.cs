using FuelTrack.Application.DTOs.Controllers;
using FuelTrack.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using DomainController = FuelTrack.Domain.Models.Controller;

namespace FuelTrack.Infrastructure.Services;

public class ControllerService : IControllerService
{
    private readonly FuelTrackDbContext _db;

    public ControllerService(FuelTrackDbContext db)
    {
        _db = db;
    }

    public async Task<ControllerDetailDto> CreateAsync(CreateControllerDto dto)
    {
        if (dto.GeneratorId.HasValue)
        {
            var generator = await _db.Generators.FindAsync(dto.GeneratorId.Value)
                ?? throw new KeyNotFoundException($"Generator {dto.GeneratorId.Value} not found.");

            // R2: Check if generator is already assigned to another controller
            var existingController = await _db.Controllers
                .FirstOrDefaultAsync(c => c.GeneratorId == dto.GeneratorId);
            if (existingController != null)
            {
                throw new InvalidOperationException(
                    $"Generator {dto.GeneratorId} is already assigned to controller {existingController.Id} ({existingController.Name}).");
            }
        }

        var controller = new DomainController
        {
            GeneratorId = dto.GeneratorId,
            Name = dto.Name,
            Code = dto.Code,
            ControllerType = dto.ControllerType,
            IpAddress = dto.IpAddress,
            Port = dto.Port,
            ModbusUnitId = dto.ModbusUnitId,
            SerialNumber = dto.SerialNumber,
            FirmwareVersion = dto.FirmwareVersion,
            EnergyReading = dto.EnergyReading,
            OperatingHours = dto.OperatingHours,
            RegisterType = dto.RegisterType,
            AutoConnect = dto.AutoConnect,
            EnergyUnit = dto.EnergyUnit,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _db.Controllers.Add(controller);
        await _db.SaveChangesAsync();

        return (await MapToDetailAsync(controller.Id))!;
    }

    public async Task<ControllerDetailDto> UpdateAsync(int id, CreateControllerDto dto)
    {
        var controller = await _db.Controllers.FindAsync(id)
            ?? throw new KeyNotFoundException($"Controller {id} not found.");

        if (dto.GeneratorId.HasValue)
        {
            var generator = await _db.Generators.FindAsync(dto.GeneratorId.Value)
                ?? throw new KeyNotFoundException($"Generator {dto.GeneratorId.Value} not found.");

            // R2: Check if generator is already assigned to another controller
            var existingController = await _db.Controllers
                .FirstOrDefaultAsync(c => c.GeneratorId == dto.GeneratorId && c.Id != id);
            if (existingController != null)
            {
                throw new InvalidOperationException(
                    $"Generator {dto.GeneratorId} is already assigned to controller {existingController.Id} ({existingController.Name}).");
            }

            // R1: Check if controller already has a generator in a different SubStation
            if (controller.GeneratorId.HasValue && controller.GeneratorId != dto.GeneratorId)
            {
                var currentGenerator = await _db.Generators.FindAsync(controller.GeneratorId.Value);
                if (currentGenerator != null && currentGenerator.SubStationId != generator.SubStationId)
                {
                    throw new InvalidOperationException(
                        $"Cannot assign generator from a different SubStation. Current generator is in SubStation {currentGenerator.SubStationId}, new generator is in SubStation {generator.SubStationId}.");
                }
            }
        }

        // R3: If unlinking (generatorId is null), sync readings from last DailyReading
        if (controller.GeneratorId.HasValue && !dto.GeneratorId.HasValue)
        {
            var lastReading = await _db.DailyReadings
                .Where(d => d.GeneratorId == controller.GeneratorId.Value)
                .OrderByDescending(d => d.ReadingDate)
                .FirstOrDefaultAsync();

            if (lastReading != null)
            {
                controller.EnergyReading = lastReading.CurrentEnergyKWh;
                controller.OperatingHours = lastReading.CurrentRunningHours;
            }
        }

        controller.GeneratorId = dto.GeneratorId;
        controller.Name = dto.Name;
        controller.Code = dto.Code;
        controller.ControllerType = dto.ControllerType;
        controller.IpAddress = dto.IpAddress;
        controller.Port = dto.Port;
        controller.ModbusUnitId = dto.ModbusUnitId;
        controller.SerialNumber = dto.SerialNumber;
        controller.FirmwareVersion = dto.FirmwareVersion;
        controller.EnergyReading = dto.EnergyReading;
        controller.OperatingHours = dto.OperatingHours;
        controller.RegisterType = dto.RegisterType;
        controller.AutoConnect = dto.AutoConnect;
        controller.EnergyUnit = dto.EnergyUnit;
        controller.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return (await MapToDetailAsync(controller.Id))!;
    }

    public async Task<ControllerDetailDto?> GetByIdAsync(int id)
    {
        return await MapToDetailAsync(id);
    }

    public async Task<List<ControllerListDto>> GetAllAsync()
    {
        return await _db.Controllers
            .Include(c => c.Generator)
            .OrderBy(c => c.Name)
            .Select(c => new ControllerListDto(
                c.Id, c.Name, c.Code, c.ControllerType, c.ConnectionStatus,
                c.Generator == null ? null : c.Generator.Name,
                c.GeneratorId, c.SerialNumber, c.IpAddress, c.Port, c.ModbusUnitId,
                c.EnergyReading, c.OperatingHours,
                c.RegisterType, c.AutoConnect, c.EnergyUnit))
            .ToListAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var controller = await _db.Controllers.FindAsync(id)
            ?? throw new KeyNotFoundException($"Controller {id} not found.");
        _db.Controllers.Remove(controller);
        await _db.SaveChangesAsync();
    }

    public async Task<ControllerDetailDto> UpdateConnectionStatusAsync(int id, string status)
    {
        var controller = await _db.Controllers.FindAsync(id)
            ?? throw new KeyNotFoundException($"Controller {id} not found.");

        controller.ConnectionStatus = status;
        controller.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return (await MapToDetailAsync(controller.Id))!;
    }

    public async Task<ControllerDetailDto?> AssignGeneratorAsync(int controllerId, int? generatorId)
    {
        var controller = await _db.Controllers.FindAsync(controllerId)
            ?? throw new KeyNotFoundException($"Controller {controllerId} not found.");

        if (generatorId.HasValue)
        {
            var generator = await _db.Generators.FindAsync(generatorId.Value)
                ?? throw new KeyNotFoundException($"Generator {generatorId.Value} not found.");

            // R2: Check if generator is already assigned to another controller
            var existingController = await _db.Controllers
                .FirstOrDefaultAsync(c => c.GeneratorId == generatorId && c.Id != controllerId);
            if (existingController != null)
            {
                throw new InvalidOperationException(
                    $"Generator {generatorId} is already assigned to controller {existingController.Id} ({existingController.Name}).");
            }

            // R1: Check if controller already has a generator in a different SubStation
            if (controller.GeneratorId.HasValue && controller.GeneratorId != generatorId)
            {
                var currentGenerator = await _db.Generators.FindAsync(controller.GeneratorId.Value);
                if (currentGenerator != null && currentGenerator.SubStationId != generator.SubStationId)
                {
                    throw new InvalidOperationException(
                        $"Cannot assign generator from a different SubStation. Current generator is in SubStation {currentGenerator.SubStationId}, new generator is in SubStation {generator.SubStationId}.");
                }
            }
        }

        // R3: If unlinking (generatorId is null), sync readings from last DailyReading
        if (controller.GeneratorId.HasValue && !generatorId.HasValue)
        {
            var lastReading = await _db.DailyReadings
                .Where(d => d.GeneratorId == controller.GeneratorId.Value)
                .OrderByDescending(d => d.ReadingDate)
                .FirstOrDefaultAsync();

            if (lastReading != null)
            {
                controller.EnergyReading = lastReading.CurrentEnergyKWh;
                controller.OperatingHours = lastReading.CurrentRunningHours;
            }
        }

        controller.GeneratorId = generatorId;
        controller.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return await MapToDetailAsync(controller.Id);
    }

    private async Task<ControllerDetailDto?> MapToDetailAsync(int id)
    {
        return await _db.Controllers
            .Include(c => c.Generator)
            .Where(c => c.Id == id)
            .Select(c => new ControllerDetailDto(
                c.Id, c.GeneratorId, c.Generator == null ? null : c.Generator.Name,
                c.Name, c.Code, c.ControllerType, c.IpAddress,
                c.Port, c.ModbusUnitId, c.SerialNumber, c.FirmwareVersion,
                c.ConnectionStatus, c.EnergyReading, c.OperatingHours,
                c.RegisterType, c.AutoConnect, c.EnergyUnit,
                c.CreatedAt, c.UpdatedAt))
            .FirstOrDefaultAsync();
    }
}
