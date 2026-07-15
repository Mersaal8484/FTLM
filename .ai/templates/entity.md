# Template: Domain Entity

## Metadata
- **Name**: entity
- **Purpose**: New domain entity creation
- **Layer**: Domain
- **Path**: `src/FuelTrack.Domain/Models/{Name}.cs`

## Template

```csharp
using FuelTrack.Domain.Common;
using FuelTrack.Domain.Enums;

namespace FuelTrack.Domain.Models;

public class {Name} : BaseEntity
{
    // Properties
    public int PropertyId { get; set; }

    // Navigation Properties
    public int RelatedEntityId { get; set; }
    public virtual RelatedEntity? RelatedEntity { get; set; }

    // Enum Properties
    public {EnumType} Status { get; set; }
}
```

## Checklist
- [ ] Inherits `BaseEntity`
- [ ] All PKs are `int`
- [ ] All FKs are `int`
- [ ] No `Guid` properties
- [ ] Nullable annotations match DB intent
- [ ] Navigation properties are `virtual` for lazy loading
- [ ] Explicit FK + navigation property pattern
- [ ] No business logic in entity
