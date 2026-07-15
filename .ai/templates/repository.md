# Template: Repository

## Metadata
- **Name**: repository
- **Purpose**: Repository interface (Application layer)
- **Layer**: Application
- **Path**: `src/FuelTrack.Application/Interfaces/I{Name}Repository.cs`

## Template

```csharp
using FuelTrack.Domain.Models;
using FuelTrack.Domain.Common;

namespace FuelTrack.Application.Interfaces;

public interface I{Name}Repository : IRepository<{Name}>
{
    Task<{Name}?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<bool> ExistsByCodeAsync(string code, int? excludeId = null, CancellationToken cancellationToken = default);
}
```

## Notes
- FuelTrack uses EF Core directly via `FuelTrackDbContext` in services
- Repository pattern is optional — services can query DbContext directly
- Use repository only for complex data access logic
- Keep repository thin — business logic belongs in services

## Checklist
- [ ] Interface is in Application layer
- [ ] No implementation details leaked
- [ ] Generic `IRepository<T>` base used
- [ ] CancellationToken on all async methods
