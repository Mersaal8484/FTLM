# CRUD Implementation Checklist

## Description
Verify end-to-end CRUD operations are correctly implemented across Entity, DTO, Service, Controller, and React layers.

## Sections

### 1. Entity
| # | Item | Severity | Verification | Status |
|---|------|----------|--------------|--------|
| 1.1 | Entity inherits from `BaseEntity` (int PK) | Critical | Verify `: BaseEntity` in class declaration | [ ] |
| 1.2 | Foreign key types match referenced entity PKs (int) | Critical | Check FK properties are `int` or `int?` | [ ] |
| 1.3 | Navigation properties configured in `DbContext` | High | Verify `HasOne`/`HasMany` in `OnModelCreating` | [ ] |
| 1.4 | Audit fields (`CreatedAt`, `UpdatedAt`) inherited from `BaseEntity` | High | Confirm no duplicate audit field declarations | [ ] |
| 1.5 | Soft-delete via `IsDeleted` flag if applicable | Medium | Check `IsDeleted` property exists where needed | [ ] |
| 1.6 | Default values set for non-nullable fields | Medium | Verify constructor or property initializers | [ ] |

### 2. DTO
| # | Item | Severity | Verification | Status |
|---|------|----------|--------------|--------|
| 2.1 | DTO properties match entity fields (no extra/missing) | High | Side-by-side comparison of DTO vs entity | [ ] |
| 2.2 | Nullable DTO fields (`int?`, `string?`) for optional entity fields | High | Check nullability annotations | [ ] |
| 2.3 | Validation attributes present (`Required`, `MaxLength`, `Range`) | High | Verify `[Required]` on mandatory fields | [ ] |
| 2.4 | Separate DTOs for Create/Update vs Read/List | Medium | Confirm distinct DTOs per operation | [ ] |
| 2.5 | No navigation properties exposed in DTOs | Medium | DTOs should have flat scalar values only | [ ] |
| 2.6 | `int` IDs, not `Guid`, in all DTOs | Critical | Search for `Guid` in DTO files | [ ] |

### 3. Service
| # | Item | Severity | Verification | Status |
|---|------|----------|--------------|--------|
| 3.1 | All CRUD methods are `async` with `CancellationToken` | High | Verify `async Task` + `CancellationToken` param | [ ] |
| 3.2 | Error handling uses try-catch with structured logging | High | Check `ILogger` usage in catch blocks | [ ] |
| 3.3 | Permission checks via `[Authorize]` or manual `UserManager` calls | Critical | Verify authorization before data access | [ ] |
| 3.4 | `AsNoTracking()` on read-only queries | Medium | Check read methods use `AsNoTracking()` | [ ] |
| 3.5 | `SaveChangesAsync` returns affected row count | Medium | Verify return value is checked | [ ] |
| 3.6 | No direct `DbContext` exposure from service to controller | Medium | Service returns DTOs, not entities | [ ] |

### 4. Controller
| # | Item | Severity | Verification | Status |
|---|------|----------|--------------|--------|
| 4.1 | Correct HTTP verbs: `GET` (list/get), `POST` (create), `PUT/PATCH` (update), `DELETE` (delete) | Critical | Verify `[HttpGet]`, `[HttpPost]`, etc. | [ ] |
| 4.2 | `[Authorize]` attribute on controller or actions | Critical | Check authorization attributes | [ ] |
| 4.3 | `[ProducesResponseType]` for Swagger documentation | Medium | Verify 200, 400, 404 response types | [ ] |
| 4.4 | Route parameters match method signatures | Medium | Check `[Route]` and parameter binding | [ ] |
| 4.5 | Pagination parameters on list endpoints (`page`, `pageSize`) | Medium | Verify `PageNumber`, `PageSize` params | [ ] |
| 4.6 | SignalR notification after mutations | Low | Check `HubContext` calls on create/update/delete | [ ] |

### 5. React Page
| # | Item | Severity | Verification | Status |
|---|------|----------|--------------|--------|
| 5.1 | Form validation with error messages | High | Test required field submission without values | [ ] |
| 5.2 | Loading spinner/state during API calls | High | Verify `<Spinner>` or skeleton during fetch | [ ] |
| 5.3 | Error toast/alert on API failure | High | Trigger 400/500 and confirm error display | [ ] |
| 5.4 | Table with pagination for list views | Medium | Check `react-table` or equivalent with page controls | [ ] |
| 5.5 | `useMutation` from React Query for create/update/delete | Medium | Verify `useMutation` hook usage | [ ] |
| 5.6 | Optimistic updates or cache invalidation after mutations | Medium | Check `queryClient.invalidateQueries` calls | [ ] |
| 5.7 | i18n keys for all user-facing strings | Low | Search for hardcoded English strings | [ ] |

## Quick Reference
- **Critical items:** 1.1, 1.2, 1.6, 2.6, 3.3, 4.1, 4.2
- **Must-pass before merge:** All Critical + 1.3, 2.1, 2.3, 3.1, 3.2, 5.1, 5.2, 5.3
- **Nice-to-have:** 1.5, 4.6, 5.6, 5.7
