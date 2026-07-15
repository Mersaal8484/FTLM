# Security Audit Workflow

## Overview
Workflow for identifying and fixing security vulnerabilities in FuelTrack. Covers authentication, authorization, input validation, and data protection.

## Prerequisites
- Security concern or audit request
- Access to authentication/authorization configuration
- Knowledge of OWASP Top 10

## Flow

```
[Scan] → [Analyze] → [Harden] → [Verify] → [Review] → [Quality Gates]
```

## Steps

### Step 1: Scan
- **Invoke:** explore agent
- **Action:** Identify attack surface in FuelTrack:
  - API endpoints without auth attributes
  - Input validation gaps
  - SQL injection vectors (raw queries)
  - XSS vectors (frontend rendering)
  - CSRF protection gaps
  - Secrets in code or config
  - Overly permissive CORS
- **Check:** Attack surface mapped? Vulnerabilities listed? Risk levels assigned?
- **Output:** Security scan report with specific vulnerability locations.
- **On Failure:** If scan incomplete, use security-specific tools (OWASP ZAP, security skills).

### Step 2: Analyze
- **Invoke:** cavecrew-investigator
- **Action:** Deep-dive into each vulnerability:
  - Trace auth path from request to database
  - Verify authorization checks at each layer
  - Check input sanitization chain
  - Review secret management
- **Check:** Vulnerability confirmed? Impact understood? Fix strategy defined?
- **Output:** Analysis report with specific fix recommendations per vulnerability.
- **On Failure:** If vulnerability unclear, create minimal reproduction to confirm.

### Step 3: Harden
- **Invoke:** cavecrew-builder
- **Action:** Apply security fixes:
  - Add missing `[Authorize]` attributes
  - Add input validation (FluentValidation)
  - Parameterize raw SQL queries
  - Sanitize output for XSS prevention
  - Add CSRF tokens where needed
  - Remove secrets from code/config
  - Tighten CORS configuration
  - Add rate limiting to sensitive endpoints
- **Check:** Fix applied? Minimal change? No behavior regression? Security improved?
- **Output:** Hardened code with security fixes applied.
- **On Failure:** If fix breaks functionality, find alternative approach that maintains security.

### Step 4: Verify
- **Invoke:** fueltrack-test-fix
- **Action:** Run full test suite. Add security-specific tests:
  - Verify unauthorized access is blocked
  - Verify input validation rejects malicious input
  - Verify authorization prevents privilege escalation
- **Check:** All tests pass? Security tests added? No regressions?
- **Output:** Test results confirming security fixes work and no regressions.
- **On Failure:** If security tests fail, fix test or fix implementation.

### Step 5: Review
- **Invoke:** fueltrack-code-review + cavecrew-reviewer
- **Action:** Review security fixes for:
  - Completeness (all vulnerabilities addressed)
  - Correctness (fixes actually prevent attack)
  - No bypasses (check for common bypass patterns)
  - Defense in depth (multiple layers of protection)
- **Check:** All vulns fixed? No bypasses? Defense in depth? No new vulns introduced?
- **Output:** Security review report with verification of each fix.
- **On Failure:** Address review comments, potentially add additional security layers.

### Step 6: Quality Gates
- **Invoke:** (Build commands + test runner)
- **Action:**
  1. `dotnet build FuelTrack.sln` — zero errors
  2. `dotnet test FuelTrack.Tests` — all tests pass
  3. No secrets in source code
  4. All write endpoints require authorization
  5. Input validation on all user-facing endpoints
- **Check:** Build clean? Tests pass? No secrets? Auth required? Validation present?
- **Output:** All gates green, security fixes ready for PR.
- **On Failure:** Fix build errors or address remaining security gaps.

## Quality Gates
- [ ] `dotnet build FuelTrack.sln` — 0 errors
- [ ] `dotnet test FuelTrack.Tests` — all pass
- [ ] No secrets in source code (check `.gitignore` covers config files)
- [ ] All API endpoints have appropriate `[Authorize]` attributes
- [ ] Input validation on all user-facing endpoints
- [ ] No raw SQL queries without parameterization
- [ ] Code review approved
- [ ] Security scan report with all findings addressed

## Common Issues
| Issue | Solution |
|-------|----------|
| Auth attribute missing | Add `[Authorize(Roles = "...")]` to controller/action |
| Raw SQL injection | Use parameterized queries or EF Core LINQ |
| XSS in frontend | Sanitize output, use React's default escaping |
| Secrets in appsettings | Move to User Secrets or environment variables |
| Overly permissive CORS | Restrict to known origins |
| Missing rate limiting | Add ASP.NET Core rate limiting middleware |
