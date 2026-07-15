using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Generators;
using FuelTrack.Application.DTOs.Generators.My;

namespace FuelTrack.Application.Interfaces;

public interface IGeneratorService
{
    Task<GeneratorDetailDto?> GetByIdAsync(int id, int userId, CancellationToken ct = default);
    Task<List<GeneratorListDto>> GetAllAsync(int userId, CancellationToken ct = default);
    Task<Result<GeneratorListDto>> CreateAsync(CreateGeneratorDto dto, int userId, CancellationToken ct = default);
    Task<Result<GeneratorDetailDto>> UpdateAsync(int id, UpdateGeneratorDto dto, int userId, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, int userId, CancellationToken ct = default);
    Task<List<MyGeneratorListDto>> GetMyGeneratorsAsync(int userId, CancellationToken ct = default);
    Task<MyGeneratorDetailDto?> GetMyGeneratorByIdAsync(int id, int userId, CancellationToken ct = default);
    Task<LastReadingDto?> GetLastReadingAsync(int generatorId, CancellationToken ct = default);
}
