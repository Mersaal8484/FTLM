# AI Development Guidelines — FuelTrack-Rebuild

## Overview

Hard rules and constraints for AI working on this codebase. These override default AI behavior. Violations must be caught before code is committed.

## Table of Contents

- [Hard Constraints](#hard-constraints)
- [Code Style](#code-style)
- [Architecture Rules](#architecture-rules)
- [Security Rules](#security-rules)
- [Testing Rules](#testing-rules)
- [Git Rules](#git-rules)
- [Quick Reference](#quick-reference)

---

## Hard Constraints

| Rule | Description |
|------|-------------|
| DO NOT modify app source code | `.ai/docs/`, `.ai/skills/`, `.ai/agents/`, `.ai/prompts/` only — unless explicitly directed |
| DO NOT invent architecture | Use existing Clean Architecture pattern (Domain → Application → Infrastructure → API) |
| DO NOT add dependencies | No new NuGet/npm packages without explicit approval |
| DO NOT commit without request | User must explicitly ask for commits |
| DO NOT add comments | Unless user explicitly requests them |
| DO NOT add emojis | Unless user explicitly requests them |
| Use existing technologies only | React 18, Bootstrap, React Query, Zustand, xUnit, FluentAssertions, AutoFixture |

---

## Code Style

### Backend (C# / ASP.NET Core)

- Follow existing naming conventions in neighboring files
- Use `int` for all PK/FK properties (not `Guid`)
- DTOs use positional constructor parameters
- Validators use FluentValidation
- Services use async/await with `CancellationToken`
- Use `AsNoTracking()` for read-only queries
- No `var` when type is not obvious from right side

### Frontend (React / JavaScript)

- Functional components with hooks
- Bootstrap classes for styling (no custom CSS unless necessary)
- React Query (`useQuery`, `useMutation`) for server state
- Zustand stores for client state
- `client` import from `../api/client` for API calls (never raw `axios`)
- Default exports for page components
- Named exports for utility functions

### Testing

- **Backend**: xUnit, FluentAssertions, AutoFixture, NSubstitute
- **Frontend**: Vitest
- All tests must pass before delivery
- No `Guid` for IDs in tests — use `int`
- Constructor parameters must match current DTO signatures

---

## Architecture Rules

### Layer Boundaries

```
Domain (no dependencies)
  ↓
Application (depends on Domain)
  ↓
Infrastructure (depends on Application + Domain)
  ↓
API (depends on all)
```

- Domain: entities, value objects, domain events
- Application: DTOs, interfaces, validators, helpers
- Infrastructure: EF Core DbContext, services, migrations
- API: controllers, middleware, auth configuration

### Naming Conventions

| Layer | Pattern |
|-------|---------|
| Entity | `src/FuelTrack.Domain/Models/[Entity].cs` |
| DTO | `src/FuelTrack.Application/DTOs/[Feature]/[Entity]Dto.cs` |
| Service interface | `src/FuelTrack.Application/Interfaces/I[Entity]Service.cs` |
| Service impl | `src/FuelTrack.Infrastructure/Services/[Entity]Service.cs` |
| Controller | `src/FuelTrack.API/Controllers/[Entity]Controller.cs` |
| Frontend page | `frontend/src/pages/[Entity].js` |
| Test | `tests/FuelTrack.Tests/[Feature]/[Entity]Tests.cs` |

---

## Security Rules

### Permission Format

Permissions MUST be stored as JSON array format:

```json
[{"module":"Dashboard","canView":true},{"module":"Readings","canView":true,"canCreate":true}]
```

NOT flat strings like `"stations.read,stations.write"`.

### API Security

- Admin-only endpoints: `[Authorize(Roles = "Admin")]`
- Use `GetUserId()` from `BaseApiController` (safe `int.TryParse`)
- Never return tokens in responses (except login/register)
- HSTS only when HTTPS is detected

### Data Isolation

- Use `UserScope` for hierarchical data access (Branch → Station → SubStation)
- Generator access is derived from SubStation access
- Empty scopes = no access (not "all access")

---

## Testing Rules

### When Tests Fail

1. Load `fueltrack-test-fix` skill
2. Categorize: syntax error vs domain model mismatch vs constructor mismatch
3. Fix root cause (not the symptom)
4. Run full suite — no regressions

### Test Data

- Use `int` for all IDs (not `Guid`)
- Match current entity field names exactly
- Use AutoFixture for test data generation
- Mock external dependencies with NSubstitute

---

## Git Rules

- Use conventional commits: `feat:`, `fix:`, `refactor:`, `test:`, `docs:`
- Subject line ≤ 50 characters
- Body only when "why" isn't obvious from subject
- Use `caveman-commit` skill for message generation
- Never commit secrets, keys, or connection strings

---

## Quick Reference

| Situation | Rule |
|-----------|------|
| Adding a new entity | Follow `fueltrack-crud-checklist` (70+ steps) |
| Changing permissions | Follow `fueltrack-permission-gate` (JSON array format) |
| Modifying database | Follow `fueltrack-migration` (sequential workflow) |
| Tests failing | Follow `fueltrack-test-fix` (xUnit + Vitest) |
| Code review | Follow `fueltrack-code-review` (CRITICAL/REQUIRED/WARNING/INFO) |
| Business logic | Follow `fueltrack-business-rules` (domain constraints) |
| Context too long | Use `caveman-compress` to shrink memory files |

## Related Files

- `AI_WORKFLOW.md` — End-to-end development workflow
- `CHECKLISTS.md` — All available checklists
- `SKILLS_OVERVIEW.md` — Local skill definitions
