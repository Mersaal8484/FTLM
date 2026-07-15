using FuelTrack.Application.DTOs.Lookups;

namespace FuelTrack.Application.Interfaces;

public interface ILookupService
{
    Task<LookupDetailDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<List<LookupListDto>> GetAllAsync(CancellationToken ct = default);
    Task<List<LookupListDto>> GetByCategoryAsync(string category, CancellationToken ct = default);
    Task<LookupListDto> CreateAsync(CreateLookupDto dto, CancellationToken ct = default);
    Task<LookupDetailDto> UpdateAsync(int id, UpdateLookupDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
