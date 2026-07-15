# Template: DTO

## Metadata
- **Name**: dto
- **Purpose**: DTO creation for Application layer
- **Layer**: Application
- **Path**: `src/FuelTrack.Application/DTOs/{Entity}/{Name}Dto.cs`

## Templates

### List DTO
```csharp
namespace FuelTrack.Application.DTOs.{Entity};

public record {Entity}Dto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int CreatedBy { get; init; }
    public DateTime CreatedAt { get; init; }
}
```

### Detail DTO
```csharp
namespace FuelTrack.Application.DTOs.{Entity};

public record {Entity}DetailDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int RelatedEntityId { get; init; }
    public string? RelatedEntityName { get; init; }
    public int CreatedBy { get; init; }
    public DateTime CreatedAt { get; init; }
    public int UpdatedBy { get; init; }
    public DateTime UpdatedAt { get; init; }
}
```

### Create DTO
```csharp
using System.ComponentModel.DataAnnotations;

namespace FuelTrack.Application.DTOs.{Entity};

public record Create{Entity}Dto
{
    [Required]
    [StringLength(100)]
    public string Name { get; init; } = string.Empty;

    [Required]
    public int RelatedEntityId { get; init; }
}
```

### Update DTO
```csharp
using System.ComponentModel.DataAnnotations;

namespace FuelTrack.Application.DTOs.{Entity};

public record Update{Entity}Dto
{
    [Required]
    [StringLength(100)]
    public string Name { get; init; } = string.Empty;

    [Required]
    public int RelatedEntityId { get; init; }
}
```

## Checklist
- [ ] All IDs are `int` (not `Guid`)
- [ ] `[Required]` on mandatory fields
- [ ] `[StringLength]` on string fields
- [ ] `[Range]` on numeric fields
- [ ] No internal fields exposed
- [ ] Record types used (immutable)
