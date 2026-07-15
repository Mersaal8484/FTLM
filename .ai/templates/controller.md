# Template: Controller

## Metadata
- **Name**: controller
- **Purpose**: API Controller for API layer
- **Layer**: API
- **Path**: `src/FuelTrack.API/Controllers/{Name}Controller.cs`

## Template

```csharp
using FuelTrack.Application.DTOs.{Entity};
using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class {Name}Controller : BaseApiController
{
    private readonly I{Name}Service _service;

    public {Name}Controller(I{Name}Service service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<{Entity}Dto>), 200)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.GetAllAsync(page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof({Entity}DetailDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken = default)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        if (result is null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof({Entity}DetailDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create(
        [FromBody] Create{Entity}Dto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.CreateAsync(dto, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Manager")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof({Entity}DetailDto), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] Update{Entity}Dto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.UpdateAsync(id, dto, cancellationToken);
        if (result is null) return NotFound();
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var result = await _service.DeleteAsync(id, cancellationToken);
        if (!result) return NotFound();
        return NoContent();
    }
}
```

## Checklist
- [ ] Inherits `BaseApiController`
- [ ] `[Authorize]` at class level
- [ ] `[Authorize(Roles = "...")]` on write endpoints
- [ ] `[ValidateAntiForgeryToken]` on POST/PUT/DELETE
- [ ] `[ProducesResponseType]` for Swagger
- [ ] `Ok(result)` pattern (not `CreatedAtAction`)
- [ ] `NoContent()` for successful delete
- [ ] `NotFound()` when entity not found
- [ ] `BadRequest()` for validation errors
- [ ] No business logic in controller
- [ ] `CancellationToken` on all methods
