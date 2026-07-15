# Add Entity Prompt

## Description
Guide for adding a new domain entity with proper EF Core configuration, DTOs, validation, and test setup.

## System Prompt
**Role:** Domain-driven design specialist for ASP.NET Core with EF Core.
**Context:** FuelTrack uses Clean Architecture. All entities inherit `BaseEntity` (int Id, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy). Audit fields auto-populated by `AuditSaveChangesInterceptor`. FK properties must be `int`. Domain models live in `src/FuelTrack.Domain/Models/`.
**Constraints:**
- Do NOT modify existing entity files.
- Use only `int` for PK/FK — never Guid.
- Navigation properties must be nullable (`Lookup?`) unless collection (`List<T>`).
- Follow existing naming: singular entity names, PascalCase properties.
- Fluent API configuration goes in `FuelTrackDbContext.OnModelCreating`.
- No data annotations on domain models — use Fluent API only.

## User Prompt Template
Add a new entity {{entity_name}} with the following properties:

**Properties:**
{{property_list}}

**Relationships:**
{{relationships}}

**Business Rules:**
{{business_rules}}

Provide:
1. Domain model class
2. EF Core Fluent API configuration
3. DbSet registration
4. DTOs (List, Detail, Create, Update)
5. FluentValidation rules
6. Test file with correct constructor and field names

## Examples

### Example 1: SubStation Entity
```csharp
// Domain: src/FuelTrack.Domain/Models/SubStation.cs
public class SubStation : BaseEntity
{
    public int StationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public double? CapacityKW { get; set; }
    
    // Navigation
    public Station? Station { get; set; }
    public List<Generator>? Generators { get; set; }
}

// DbContext Fluent API
builder.Entity<SubStation>(entity =>
{
    entity.HasOne(e => e.Station)
          .WithMany(s => s.SubStations)
          .HasForeignKey(e => e.StationId)
          .OnDelete(DeleteBehavior.Restrict);
    
    entity.Property(e => e.Name)
          .IsRequired()
          .HasMaxLength(200);
});

// DTOs
public record SubStationListDto(int Id, string Name, string StationName, double? CapacityKW);
public record SubStationDto(int Id, string Name, int StationId, string StationName, double? CapacityKW);
public record CreateSubStationDto(string Name, int StationId, double? CapacityKW);
public record UpdateSubStationDto(string Name, int StationId, double? CapacityKW);
```

### Example 2: Meter Entity
```csharp
// Domain: src/FuelTrack.Domain/Models/Meter.cs
public class Meter : BaseEntity
{
    public int? SubStationId { get; set; }
    public int? GeneratorId { get; set; }
    public int? FuelTankId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    
    // Navigation
    public SubStation? SubStation { get; set; }
    public Generator? Generator { get; set; }
    public FuelTank? FuelTank { get; set; }
}

// DbContext Fluent API
builder.Entity<Meter>(entity =>
{
    entity.HasOne(e => e.SubStation)
          .WithMany()
          .HasForeignKey(e => e.SubStationId)
          .OnDelete(DeleteBehavior.Restrict);
    
    entity.HasOne(e => e.Generator)
          .WithMany()
          .HasForeignKey(e => e.GeneratorId)
          .OnDelete(DeleteBehavior.Restrict);
    
    entity.HasOne(e => e.FuelTank)
          .WithMany()
          .HasForeignKey(e => e.FuelTankId)
          .OnDelete(DeleteBehavior.Restrict);
});
```

## Completion Checklist
- [ ] Entity class created in `src/FuelTrack.Domain/Models/`
- [ ] Inherits `BaseEntity` (int Id + audit fields)
- [ ] All FK properties are `int` or `int?`
- [ ] Navigation properties are nullable (`Type?`) or collections (`List<T>`)
- [ ] Fluent API config added to `FuelTrackDbContext.OnModelCreating`
- [ ] `DbSet<Entity>` registered in context
- [ ] FK relationships use `.OnDelete(DeleteBehavior.Restrict)` or appropriate behavior
- [ ] String properties have `.HasMaxLength()` configured
- [ ] DTOs created: List, Detail, Create, Update
- [ ] FluentValidation validators created
- [ ] Test file uses `int` IDs, not `Guid.NewGuid()`
- [ ] Build succeeds with 0 errors
- [ ] No references to deleted/nonexistent models (StationReading, FuelMovement, etc.)
