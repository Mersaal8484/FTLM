# Template: API Endpoint (Minimal API)

## Metadata
- **Name**: api-endpoint
- **Purpose**: Minimal API endpoint registration
- **Layer**: API
- **Path**: `src/FuelTrack.API/Endpoints/{Name}Endpoints.cs`

## Template

```csharp
using FuelTrack.Application.DTOs.{Entity};
using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace FuelTrack.API.Endpoints;

public static class {Name}Endpoints
{
    public static void Map{Name}Endpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/{name}")
            .WithTags("{Name}")
            .RequireAuthorization();

        group.MapGet("/", async (
            I{Name}Service service,
            [AsParameters] int page = 1,
            [AsParameters] int pageSize = 20,
            CancellationToken ct = default) =>
        {
            var result = await service.GetAllAsync(page, pageSize, ct);
            return Results.Ok(result);
        });

        group.MapGet("/{id:int}", async (
            int id,
            I{Name}Service service,
            CancellationToken ct) =>
        {
            var result = await service.GetByIdAsync(id, ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        });
    }
}
```

## Notes
- Minimal APIs are used for simple endpoints
- Complex CRUD uses controller pattern
- Use `MapGroup` for route organization
- Register endpoints in `Program.cs`

## Checklist
- [ ] `RequireAuthorization()` on group
- [ ] Proper HTTP method mapping
- [ ] CancellationToken passed through
- [ ] Correct status codes returned
- [ ] Tags set for Swagger
