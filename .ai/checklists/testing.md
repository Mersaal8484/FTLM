# Testing Checklist

## Description
Ensure comprehensive test coverage across unit, integration, and E2E layers with reliable test data management.

## Sections

### 1. Unit Tests
| # | Item | Severity | Verification | Status |
|---|------|----------|--------------|--------|
| 1.1 | Service layer methods have unit tests | High | Check test files under `tests/FuelTrack.Tests/Services/` | [ ] |
| 1.2 | DTOs tested for validation rules | Medium | Verify FluentValidation test classes | [ ] |
| 1.3 | Permission/authorization logic tested | High | Check role-based access tests | [ ] |
| 1.4 | Edge cases covered (null, empty, max values) | Medium | Review test methods for boundary conditions | [ ] |
| 1.5 | Tests use Arrange-Act-Assert pattern | Low | Consistent test structure across files | [ ] |
| 1.6 | No test interdependencies (isolated execution) | High | Run single test class — no side effects | [ ] |

### 2. Integration Tests
| # | Item | Severity | Verification | Status |
|---|------|----------|--------------|--------|
| 2.1 | API endpoints tested with `WebApplicationFactory` | High | Check `IntegrationTestBase` usage | [ ] |
| 2.2 | Database integration tests use InMemory or test SQL | High | Verify test DB configuration | [ ] |
| 2.3 | Authentication tested (valid/invalid/expired tokens) | Critical | Check JWT test scenarios | [ ] |
| 2.4 | Pagination tested with multiple page sizes | Medium | Verify page 1, middle, last page scenarios | [ ] |
| 2.5 | Error responses tested (400, 401, 403, 404, 500) | High | Check status code assertions | [ ] |
| 2.6 | SignalR hub tested for connection/message flow | Medium | Verify hub test classes exist | [ ] |

### 3. E2E Tests
| # | Item | Severity | Verification | Status |
|---|------|----------|--------------|--------|
| 3.1 | Login flow tested end-to-end | Critical | Playwright test for auth flow | [ ] |
| 3.2 | CRUD operations tested via UI | High | Playwright tests for each entity page | [ ] |
| 3.3 | Form validation tested in browser | Medium | Submit empty forms, verify error display | [ ] |
| 3.4 | Navigation between pages tested | Medium | Verify menu/routing works | [ ] |
| 3.5 | Responsive layout tested (desktop/mobile) | Low | Check viewport breakpoints | [ ] |

### 4. Coverage
| # | Item | Severity | Verification | Status |
|---|------|----------|--------------|--------|
| 4.1 | Unit test coverage ≥ 70% for service layer | High | Run `dotnet test /p:CollectCoverage=true` | [ ] |
| 4.2 | Integration test coverage for all controllers | High | Check each controller has test class | [ ] |
| 4.3 | Critical paths (auth, CRUD) have 100% coverage | Medium | Verify auth and main entity tests exist | [ ] |
| 4.4 | No coverage regressions from baseline | Medium | Compare before/after coverage reports | [ ] |

### 5. Test Data
| # | Item | Severity | Verification | Status |
|---|------|----------|--------------|--------|
| 5.1 | Test fixtures defined for common entities | Medium | Check `TestData` or `Fixtures` classes | [ ] |
| 5.2 | Tests clean up after themselves (no DB pollution) | High | Verify `Dispose`/`IAsyncLifetime` cleanup | [ ] |
| 5.3 | AutoFixture used for random data generation | Low | Check `new Fixture()` usage in tests | [ ] |
| 5.4 | No hardcoded production IDs in test data | Medium | Search for real production entity IDs | [ ] |

## Quick Reference
- **Critical items:** 2.3, 3.1
- **Must-pass before merge:** All Critical + 1.1, 1.3, 1.6, 2.1, 2.2, 2.5, 4.1, 4.2, 5.2
- **Nice-to-have:** 1.5, 3.5, 5.3
