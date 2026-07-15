# Deployment Checklist

## Description
Verify build artifacts, configuration, database state, and monitoring are ready before deploying to production.

## Sections

### 1. Build
| # | Item | Severity | Verification | Status |
|---|------|----------|--------------|--------|
| 1.1 | Docker image builds successfully | Critical | `docker build` completes without errors | [ ] |
| 1.2 | Multi-stage build used (build → runtime) | Medium | Verify `FROM` stages in `Dockerfile` | [ ] |
| 1.3 | Image tagged with version/commit hash | Medium | Check `docker build -t` tag format | [ ] |
| 1.4 | No dev dependencies in production image | Medium | Verify `COPY --from=build` excludes `node_modules` dev | [ ] |
| 1.5 | Frontend assets included in image (Vite build output) | High | Confirm `frontend/dist` copied to nginx/static | [ ] |

### 2. Configuration
| # | Item | Severity | Verification | Status |
|---|------|----------|--------------|--------|
| 2.1 | Environment variables set for target environment | Critical | Verify `appsettings.Production.json` or env vars | [ ] |
| 2.2 | Connection string points to correct database | Critical | Confirm `Server=tcp:...,port` format | [ ] |
| 2.3 | JWT secret and signing key configured | Critical | Verify `JwtSettings:SecretKey` is set | [ ] |
| 2.4 | CORS origins include production frontend URL | High | Check `Cors:Origins` in production config | [ ] |
| 2.5 | No Docker `${}` placeholders in production config | High | Search for `${` in `appsettings.Production.json` | [ ] |
| 2.6 | SignalR hub URL configured for production | Medium | Verify `HubUrl` setting | [ ] |

### 3. Database
| # | Item | Severity | Verification | Status |
|---|------|----------|--------------|--------|
| 3.1 | All pending migrations applied | Critical | `dotnet ef database update` — no pending migrations | [ ] |
| 3.2 | Migrations tested on staging database first | High | Verify staging migration completed | [ ] |
| 3.3 | Seed data applied (roles, permissions, admin user) | Medium | Check `RoleService.InitAsync()` runs on startup | [ ] |
| 3.4 | Database backup taken before migration | High | Confirm backup exists and is restorable | [ ] |

### 4. Health Checks
| # | Item | Severity | Verification | Status |
|---|------|----------|--------------|--------|
| 4.1 | API health endpoint responds | Critical | `GET /health` returns 200 OK | [ ] |
| 4.2 | Database connectivity verified | Critical | Health check includes DB ping | [ ] |
| 4.3 | SignalR hub accessible | Medium | Connect to `/hubs/notifications` | [ ] |
| 4.4 | External service dependencies healthy | Medium | Verify Modbus gateway, other integrations | [ ] |

### 5. Monitoring
| # | Item | Severity | Verification | Status |
|---|------|----------|--------------|--------|
| 5.1 | Structured logging configured (Serilog / built-in) | High | Verify `appsettings` logging section | [ ] |
| 5.2 | Log level appropriate for production (Information/Warning) | Medium | No `Debug` or `Trace` in production | [ ] |
| 5.3 | Error tracking integration (Sentry, Application Insights) | Medium | Verify DSN configured | [ ] |
| 5.4 | Metrics endpoint exposed (`/metrics`) | Low | Check Prometheus/HealthChecks integration | [ ] |

### 6. Rollback Plan
| # | Item | Severity | Verification | Status |
|---|------|----------|--------------|--------|
| 6.1 | Previous Docker image tag documented | High | Note current production image tag before deploy | [ ] |
| 6.2 | Database rollback script prepared | High | Verify `Down()` migration tested | [ ] |
| 6.3 | Rollback procedure written and team-reviewed | Medium | Document steps in deployment runbook | [ ] |
| 6.4 | Communication plan for rollback scenario | Low | Identify who to notify if rollback needed | [ ] |

## Quick Reference
- **Critical items:** 1.1, 2.1, 2.2, 2.3, 3.1, 4.1, 4.2
- **Must-pass before deploy:** All Critical + 1.5, 2.4, 2.5, 3.2, 3.4, 5.1, 6.1, 6.2
- **Nice-to-have:** 1.2, 1.3, 4.3, 5.4, 6.4
