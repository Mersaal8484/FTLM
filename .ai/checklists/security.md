# Security Checklist

## Description
Verify authentication, authorization, input validation, data protection, and infrastructure security are correctly implemented.

## Sections

### 1. Authentication
| # | Item | Severity | Verification | Status |
|---|------|----------|--------------|--------|
| 1.1 | JWT tokens signed with strong secret key | Critical | Verify `JwtSettings:SecretKey` length ≥ 32 chars | [ ] |
| 1.2 | Access token expiration configured (≤ 60 min) | High | Check `ExpiresInMinutes` setting | [ ] |
| 1.3 | Refresh token rotation implemented | High | Verify new refresh token issued on refresh | [ ] |
| 1.4 | Refresh tokens stored securely (httpOnly cookie) | High | Check `Cookie.HttpOnly = true` | [ ] |
| 1.5 | Password requirements enforced (min length, complexity) | Critical | Verify `PasswordOptions` in `Identity` config | [ ] |
| 1.6 | Account lockout after failed attempts | Medium | Check `LockoutEnabledByDefault` setting | [ ] |
| 1.7 | Token revocation on logout | High | Verify `RefreshTokenRepository` revokes tokens | [ ] |

### 2. Authorization
| # | Item | Severity | Verification | Status |
|---|------|----------|--------------|--------|
| 2.1 | Role-based permissions enforced on all endpoints | Critical | Check `[Authorize(Roles = "...")]` on controllers | [ ] |
| 2.2 | Permissions stored as JSON in `Role.Permissions` | High | Verify `PermissionHelper.Deserialize` usage | [ ] |
| 2.3 | Default roles seeded with correct permissions | High | Check `RoleService.InitAsync()` defaults | [ ] |
| 2.4 | Users cannot escalate their own permissions | Critical | Verify no self-modify on roles endpoint | [ ] |
| 2.5 | Data scope filtering applied (Branch/Station/SubStation) | High | Check `UserScope` filtering in service queries | [ ] |
| 2.6 | Admin-only operations restricted to Admin role | Critical | Verify `[Authorize(Roles = "Admin")]` on admin endpoints | [ ] |

### 3. Input Validation
| # | Item | Severity | Verification | Status |
|---|------|----------|--------------|--------|
| 3.1 | FluentValidation validators for all DTOs | High | Check validator classes in `Application/Validators/` | [ ] |
| 3.2 | `[Required]` attributes on mandatory fields | High | Verify DTO annotations | [ ] |
| 3.3 | String length limits enforced (`MaxLength`) | Medium | Check `HasMaxLength` in validators | [ ] |
| 3.4 | Numeric ranges validated (`Range`) | Medium | Verify `InclusiveBetween` in validators | [ ] |
| 3.5 | No raw SQL with user input (SQL injection) | Critical | Search for `FromSqlRaw` / string interpolation | [ ] |
| 3.6 | File upload size limits configured | Medium | Check `MultipartBodyLengthLimit` | [ ] |

### 4. Data Protection
| # | Item | Severity | Verification | Status |
|---|------|----------|--------------|--------|
| 4.1 | Passwords hashed with bcrypt/Argon2 | Critical | Verify `PasswordHasher<User>` usage (Identity default) | [ ] |
| 4.2 | Sensitive fields masked in API responses | High | Check DTOs don't expose `PasswordHash`, `RefreshToken` | [ ] |
| 4.3 | Audit logs capture who did what | Medium | Verify `AuditSaveChangesInterceptor` records `UserId` | [ ] |
| 4.4 | PII not logged in structured logs | High | Review `ILogger` calls for sensitive data | [ ] |
| 4.5 | HTTPS enforced in production | Critical | Verify `app.UseHttpsRedirection()` | [ ] |

### 5. Infrastructure
| # | Item | Severity | Verification | Status |
|---|------|----------|--------------|--------|
| 5.1 | CORS restricted to known origins | High | Verify `Cors:Origins` doesn't include `*` | [ ] |
| 5.2 | Rate limiting configured on auth endpoints | Medium | Check rate limit middleware | [ ] |
| 5.3 | Security headers middleware active | High | Verify `SecurityHeadersMiddleware` in pipeline | [ ] |
| 5.4 | HSTS enabled for HTTPS | Medium | Check `UseHsts()` in production | [ ] |
| 5.5 | No debug/developer mode in production | High | Verify `EnvironmentName != "Development"` checks | [ ] |
| 5.6 | Docker container runs as non-root user | Medium | Check `USER` directive in `Dockerfile` | [ ] |

## Quick Reference
- **Critical items:** 1.1, 1.5, 2.1, 2.4, 2.6, 3.5, 4.1, 4.5
- **Must-pass before merge:** All Critical + 1.2, 1.3, 1.4, 1.7, 2.2, 2.3, 2.5, 3.1, 3.2, 4.2, 4.4, 5.1, 5.3, 5.5
- **Nice-to-have:** 1.6, 3.3, 3.4, 3.6, 4.3, 5.2, 5.4, 5.6
