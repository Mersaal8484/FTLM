# Template: Validator

## Metadata
- **Name**: validator
- **Purpose**: FluentValidation validator for DTOs
- **Layer**: Application
- **Path**: `src/FuelTrack.Application/Validators/{Entity}/`

## Templates

### Create Validator
```csharp
using FluentValidation;
using FuelTrack.Application.DTOs.{Entity};

namespace FuelTrack.Application.Validators.{Entity};

public class Create{Entity}Validator : AbstractValidator<Create{Entity}Dto>
{
    public Create{Entity}Validator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters");

        RuleFor(x => x.RelatedEntityId)
            .GreaterThan(0).WithMessage("Related entity is required");
    }
}
```

### Update Validator
```csharp
using FluentValidation;
using FuelTrack.Application.DTOs.{Entity};

namespace FuelTrack.Application.Validators.{Entity};

public class Update{Entity}Validator : AbstractValidator<Update{Entity}Dto>
{
    public Update{Entity}Validator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters");

        RuleFor(x => x.RelatedEntityId)
            .GreaterThan(0).WithMessage("Related entity is required");
    }
}
```

## Checklist
- [ ] Inherits `AbstractValidator<T>`
- [ ] All required fields have `NotEmpty()`
- [ ] String fields have `MaximumLength()`
- [ ] Numeric fields have `GreaterThan()` or `InclusiveBetween()`
- [ ] Business rules added as custom validators
- [ ] Error messages are user-friendly
