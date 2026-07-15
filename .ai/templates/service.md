# Template: Service

## Metadata
- **Name**: service
- **Purpose**: Service implementation for Infrastructure layer
- **Layer**: Infrastructure
- **Path**: `src/FuelTrack.Infrastructure/Services/{Name}Service.cs`

## Template

```csharp
using FuelTrack.Application.DTOs.{Entity};
using FuelTrack.Application.Interfaces;
using FuelTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FuelTrack.Infrastructure.Services;

public class {Name}Service : I{Name}Service
{
    private readonly FuelTrackDbContext _context;
    private readonly ILogger<{Name}Service> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public {Name}Service(
        FuelTrackDbContext context,
        ILogger<{Name}Service> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<{Entity}Dto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.{Entities}
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new {Entity}Dto
            {
                Id = e.Id,
                Name = e.Name,
                CreatedBy = e.CreatedBy,
                CreatedAt = e.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        return entity;
    }

    public async Task<IEnumerable<{Entity}Dto>> GetAllAsync(
        int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        return await _context.{Entities}
            .AsNoTracking()
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new {Entity}Dto
            {
                Id = e.Id,
                Name = e.Name,
                CreatedBy = e.CreatedBy,
                CreatedAt = e.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<{Entity}DetailDto> CreateAsync(Create{Entity}Dto dto, CancellationToken cancellationToken = default)
    {
        var userId = int.Parse(_httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value ?? "0");

        var entity = new {Entity}
        {
            Name = dto.Name,
            RelatedEntityId = dto.RelatedEntityId,
            CreatedBy = userId,
            UpdatedBy = userId
        };

        _context.{Entities}.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created {Entity} {Id}", nameof({Entity}), entity.Id);

        return MapToDetail(entity);
    }

    public async Task<{Entity}DetailDto?> UpdateAsync(int id, Update{Entity}Dto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _context.{Entities}
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (entity is null) return null;

        var userId = int.Parse(_httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value ?? "0");

        entity.Name = dto.Name;
        entity.RelatedEntityId = dto.RelatedEntityId;
        entity.UpdatedBy = userId;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated {Entity} {Id}", nameof({Entity}), entity.Id);

        return MapToDetail(entity);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.{Entities}
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (entity is null) return false;

        _context.{Entities}.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted {Entity} {Id}", nameof({Entity}), id);

        return true;
    }

    private static {Entity}DetailDto MapToDetail({Entity} entity)
    {
        return new {Entity}DetailDto
        {
            Id = entity.Id,
            Name = entity.Name,
            RelatedEntityId = entity.RelatedEntityId,
            CreatedBy = entity.CreatedBy,
            CreatedAt = entity.CreatedAt,
            UpdatedBy = entity.UpdatedBy,
            UpdatedAt = entity.UpdatedAt
        };
    }
}
```

## Checklist
- [ ] Injects `FuelTrackDbContext`, `ILogger<T>`, `IHttpContextAccessor`
- [ ] All read queries use `AsNoTracking()`
- [ ] All methods accept `CancellationToken`
- [ ] All navigation properties loaded with `.Include()` or `.Select()`
- [ ] List methods have pagination (`.Skip().Take()`)
- [ ] Manual DTO mapping (no AutoMapper)
- [ ] Structured logging via `ILogger<T>`
- [ ] Error handling with specific exception types
- [ ] No N+1 queries
