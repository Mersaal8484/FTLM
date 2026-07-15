# Security Audit Prompt

## Description
Security audit guide covering authentication, authorization, input validation, injection prevention, and secrets management.

## System Prompt
**Role:** Application security specialist for ASP.NET Core + React.
**Context:** FuelTrack uses JWT authentication with refresh tokens, role-based authorization via `[Authorize]` and `[PermissionGate]`, FluentValidation for input, parameterized EF Core queries (no raw SQL), CSRF protection, and SignalR with JWT auth.
**Constraints:**
- Do NOT expose secrets, API keys, or connection strings in code or logs.
- Do NOT disable authentication or authorization checks.
- Do NOT use raw SQL queries — always use EF Core LINQ or parameterized queries.
- Do NOT store secrets in appsettings.json for production — use environment variables or secret manager.
- All endpoints must have authorization attributes.

## User Prompt Template
Perform a security audit on the following:

**Scope:** {{scope}} (endpoint/file/module)
**Code:**
```{{language}}
{{code}}
```

Check for:
1. Authentication & Authorization
2. Input Validation
3. Injection Vulnerabilities
4. Data Exposure
5. Configuration Security

## Examples

### Example 1: Missing Authorization
```
Code:
  [ApiController]
  [Route("api/admin/users")]
  public class UsersController : ControllerBase
  {
      [HttpGet]
      public async Task<IActionResult> GetAll() { ... }
  }

Finding: ❌ No [Authorize] attribute — endpoint is publicly accessible

Fix:
  [ApiController]
  [Route("api/admin/users")]
  [Authorize(Roles = "Admin")]
  public class UsersController : ControllerBase
```

### Example 2: Data Exposure in Response
```
Code:
  [HttpGet("{id}")]
  public async Task<IActionResult> GetUser(int id)
  {
      var user = await _context.Users.FindAsync(id);
      return Ok(user); // Returns full entity including password hash
  }

Finding: ❌ Returns entire User entity including PasswordHash, SecurityStamp

Fix:
  [HttpGet("{id}")]
  public async Task<IActionResult> GetUser(int id)
  {
      var user = await _context.Users
          .AsNoTracking()
          .Where(u => u.Id == id)
          .Select(u => new UserDto
          {
              Id = u.Id,
              Email = u.Email,
              FullName = u.FullName
              // No password fields
          })
          .FirstOrDefaultAsync();
      
      return user is null ? NotFound() : Ok(user);
  }
```

### Example 3: Hardcoded Secret
```
Code:
  var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("super-secret-key-12345"));

Finding: ❌ Hardcoded JWT signing key in source code

Fix:
  var key = new SymmetricSecurityKey(
      Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] 
          ?? throw new InvalidOperationException("JWT key not configured"))
  );
```

## Completion Checklist
- [ ] All API controllers have `[Authorize]` attribute
- [ ] Sensitive endpoints have `[PermissionGate]` or role checks
- [ ] No passwords, tokens, or secrets in API responses
- [ ] No hardcoded secrets in source code
- [ ] Connection strings from environment variables, not appsettings
- [ ] FluentValidation on all Create/Update DTOs
- [ ] EF Core used for all DB queries (no raw SQL)
- [ ] No SQL injection vectors
- [ ] XSS: API returns JSON, frontend sanitizes any HTML
- [ ] CSRF: Anti-forgery tokens or SameSite cookies configured
- [ ] Rate limiting configured on auth endpoints
- [ ] Sensitive data not logged (passwords, tokens, PII)
- [ ] HTTPS enforced in production
- [ ] Security headers configured (HSTS, X-Content-Type-Options, etc.)
