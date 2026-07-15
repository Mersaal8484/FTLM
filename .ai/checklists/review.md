# Code Review Checklist

## Description
Ensure code quality, architectural compliance, security, and performance before merging to main branch.

## Sections

### 1. Build
| # | Item | Severity | Verification | Status |
|---|------|----------|--------------|--------|
| 1.1 | Solution compiles with zero errors | Critical | `dotnet build` passes | [ ] |
| 1.2 | No new compiler warnings introduced | High | Compare warning count before/after changes | [ ] |
| 1.3 | Frontend builds without errors | High | `npm run build` passes in `frontend/` | [ ] |
| 1.4 | No unused imports or references | Low | IDE unused-using detection | [ ] |

### 2. Tests
| # | Item | Severity | Verification | Status |
|---|------|----------|--------------|--------|
| 2.1 | All existing tests still pass | Critical | `dotnet test` — 0 failures | [ ] |
| 2.2 | New code has corresponding unit tests | High | Check test coverage for changed files | [ ] |
| 2.3 | Integration tests cover new endpoints | High | Verify test classes for new controllers | [ ] |
| 2.4 | No flaky tests introduced | Medium | Run tests 3x, confirm consistent results | [ ] |

### 3. Architecture
| # | Item | Severity | Verification | Status |
|---|------|----------|--------------|--------|
| 3.1 | Clean Architecture layers respected (Domain → Application → Infrastructure → API) | Critical | No upward references (e.g., Domain referencing API) | [ ] |
| 3.2 | Domain entities have no DTOs or HTTP dependencies | Critical | Verify `Domain` project references only `System.*` | [ ] |
| 3.3 | Services in `Infrastructure/Services`, not in `API/Controllers` | High | Controllers delegate to injected services | [ ] |
| 3.4 | No business logic in controllers | High | Controllers thin, services thick | [ ] |
| 3.5 | Interface segregation (small, focused interfaces) | Medium | Check `IUserService` vs god-interfaces | [ ] |
| 3.6 | No circular dependencies between projects | Medium | Review `.csproj` references | [ ] |

### 4. Security
| # | Item | Severity | Verification | Status |
|---|------|----------|--------------|--------|
| 4.1 | No secrets, keys, or connection strings in source code | Critical | `git diff` — no hardcoded secrets | [ ] |
| 4.2 | User input validated before processing | High | Check FluentValidation or `[Required]` usage | [ ] |
| 4.3 | SQL injection prevented (parameterized queries / EF Core) | Critical | No raw SQL with string concatenation | [ ] |
| 4.4 | XSS prevention (output encoding, sanitized inputs) | High | Review any `dangerouslySetInnerHTML` or raw HTML | [ ] |
| 4.5 | Authentication/authorization not bypassed | Critical | `[Authorize]` on all non-public endpoints | [ ] |

### 5. Performance
| # | Item | Severity | Verification | Status |
|---|------|----------|--------------|--------|
| 5.1 | No N+1 query patterns | High | Check for lazy loading or loops with DB calls | [ ] |
| 5.2 | `AsNoTracking()` on read-only queries | Medium | Verify in service layer | [ ] |
| 5.3 | Pagination on list endpoints | Medium | Verify `Skip`/`Take` or equivalent | [ ] |
| 5.4 | No synchronous blocking (`Result`, `Wait()`) | High | Search for `.Result` or `.Wait()` on tasks | [ ] |
| 5.5 | Appropriate data structure for lookups (Dictionary vs List) | Low | Review loops that could use hash lookups | [ ] |

### 6. Documentation
| # | Item | Severity | Verification | Status |
|---|------|----------|--------------|--------|
| 6.1 | Public methods have XML doc comments | Medium | Check `///` on service/controller methods | [ ] |
| 6.2 | API endpoints documented in Swagger | Medium | Verify `[ProducesResponseType]` and `[SwaggerOperation]` | [ ] |
| 6.3 | README updated if setup steps changed | Low | Check `README.md` for new dependencies | [ ] |
| 6.4 | AGENTS.md session summary updated | Low | Add entry for significant changes | [ ] |

## Quick Reference
- **Critical items:** 1.1, 2.1, 3.1, 3.2, 4.1, 4.3, 4.5
- **Must-pass before merge:** All Critical + 1.2, 1.3, 2.2, 2.3, 3.3, 3.4, 4.2, 5.1, 5.4
- **Nice-to-have:** 1.4, 3.5, 3.6, 5.5, 6.3, 6.4
