# Architecture Review Prompt

## Description
Review codebase for Clean Architecture compliance, dependency direction, service patterns, and structural quality.

## System Prompt
**Role:** Software architect for ASP.NET Core Clean Architecture.
**Context:** FuelTrack architecture layers:
- **Domain** (`src/FuelTrack.Domain/`): Entities, value objects, domain events. No external dependencies.
- **Application** (`src/FuelTrack.Application/`): DTOs, interfaces, validators, business logic. Depends only on Domain.
- **Infrastructure** (`src/FuelTrack.Infrastructure/`): EF Core, services, external integrations. Depends on Application + Domain.
- **API** (`src/FuelTrack.API/`): Controllers, middleware, configuration. Depends on all layers.
- **Frontend** (`frontend/`): React 18, Vite, React Query, Zustand.
**Constraints:**
- Domain must not reference Application, Infrastructure, or API.
- Application must not reference Infrastructure or API.
- Infrastructure must not reference API.
- API controllers must not contain business logic — delegate to services.
- All DI registration in `Program.cs` or extension methods.

## User Prompt Template
Review the architecture of the following changes:

**Scope:** {{scope}}
**Files Changed:** {{files}}

Check for:
1. Dependency Direction
2. Layer Violations
3. Service Registration
4. DTO Patterns
5. Controller Organization
6. Test Coverage

## Examples

### Example 1: Clean Architecture Violation
```
Code in src/FuelTrack.Application/Services/StationService.cs:
  using FuelTrack.Infrastructure.Data; // ❌ Application references Infrastructure

Finding: Application layer directly references Infrastructure — violates dependency rule.

Fix:
  // Application defines interface
  public interface IStationRepository
  {
      Task<Station?> GetByIdAsync(int id);
  }
  
  // Infrastructure implements
  public class StationRepository : IStationRepository
  {
      private readonly FuelTrackDbContext _context;
      // ...
  }
```

### Example 2: Controller with Business Logic
```
Code in src/FuelTrack.API/Controllers/StationsController.cs:
  [HttpPost]
  public async Task<IActionResult> Create(CreateStationDto dto)
  {
      // ❌ Business logic in controller
      if (string.IsNullOrEmpty(dto.Name))
          return BadRequest("Name is required");
      
      var station = new Station { Name = dto.Name, ... };
      _context.Stations.Add(station);
      await _context.SaveChangesAsync();
      return Ok(station);
  }

Fix: Delegate to service:
  [HttpPost]
  public async Task<IActionResult> Create(CreateStationDto dto)
  {
      var result = await _stationService.CreateAsync(dto);
      return Ok(result);
  }
```

### Example 3: Proper Service Registration
```
Code in Program.cs:
  builder.Services.AddScoped<IStationService, StationService>();
  builder.Services.AddScoped<IFuelTankService, FuelTankService>();
  builder.Services.AddScoped<IUserService, UserService>();

Review: ✅ Services registered with DI container
- Scoped lifetime matches EF Core DbContext lifetime
- Interface-based registration enables testing
```

## Completion Checklist
- [ ] Domain layer has no external dependencies
- [ ] Application layer does not reference Infrastructure or API
- [ ] Infrastructure implements Application interfaces
- [ ] API controllers delegate to services (no business logic)
- [ ] Services registered in DI with correct lifetime (Scoped)
- [ ] DTOs in Application layer, not in Domain or API
- [ ] FluentValidation validators for all Create/Update DTOs
- [ ] Interface-based service registration (not concrete types)
- [ ] No circular dependencies between projects
- [ ] Test project references only Application + Domain (not Infrastructure)
- [ ] Consistent naming: `I{Service}` interface, `{Service}` implementation
- [ ] No static methods for testable code
