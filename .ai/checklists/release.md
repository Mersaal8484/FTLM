# Release Checklist

## Description
Ensure code freeze, testing, documentation, deployment, monitoring, and communication are complete before shipping to production.

## Sections

### 1. Code Freeze
| # | Item | Severity | Verification | Status |
|---|------|----------|--------------|--------|
| 1.1 | All planned features merged to release branch | Critical | Review release branch vs sprint backlog | [ ] |
| 1.2 | No open critical/high bugs on release branch | Critical | Check issue tracker for unresolved critical bugs | [ ] |
| 1.3 | Feature flags configured for incomplete work | Medium | Verify `FeatureFlags` settings if applicable | [ ] |
| 1.4 | No `TODO`/`FIXME` in critical path code | Medium | Search for `TODO` in service/controller files | [ ] |
| 1.5 | Release branch created from `main` | Medium | Verify branch naming convention | [ ] |

### 2. Testing
| # | Item | Severity | Verification | Status |
|---|------|----------|--------------|--------|
| 2.1 | All unit tests pass (0 failures) | Critical | `dotnet test` — full suite green | [ ] |
| 2.2 | All integration tests pass (0 failures) | Critical | Integration test suite green | [ ] |
| 2.3 | E2E tests pass on staging environment | High | Playwright tests against staging URL | [ ] |
| 2.4 | Manual smoke test of critical paths | High | Login → Dashboard → Create → Edit → Delete | [ ] |
| 2.5 | Performance test within acceptable thresholds | Medium | API response times < 500ms for common endpoints | [ ] |
| 2.6 | Security scan completed (no new vulnerabilities) | High | Run dependency audit (`npm audit`, `dotnet list vuln`) | [ ] |

### 3. Documentation
| # | Item | Severity | Verification | Status |
|---|------|----------|--------------|--------|
| 3.1 | CHANGELOG updated with all changes | High | Review `CHANGELOG.md` or release notes | [ ] |
| 3.2 | API documentation reflects new/changed endpoints | Medium | Verify Swagger UI matches actual endpoints | [ ] |
| 3.3 | README updated for new dependencies or setup steps | Low | Check `README.md` for accuracy | [ ] |
| 3.4 | Migration guide documented if breaking changes | Medium | Verify upgrade instructions exist | [ ] |
| 3.5 | AGENTS.md session summary reflects release changes | Low | Review session entries | [ ] |

### 4. Deployment
| # | Item | Severity | Verification | Status |
|---|------|----------|--------------|--------|
| 4.1 | Staging environment deployed and verified | Critical | App accessible on staging URL | [ ] |
| 4.2 | Database migrations tested on staging | Critical | Staging DB matches expected schema | [ ] |
| 4.3 | Docker image tagged with release version | High | Verify `docker tag` output | [ ] |
| 4.4 | Environment variables updated for production | High | Verify `appsettings.Production.json` or K8s secrets | [ ] |
| 4.5 | Production deployment window communicated | Medium | Team notified of deployment schedule | [ ] |
| 4.6 | Previous version tagged for rollback | High | Verify previous Docker image tag preserved | [ ] |

### 5. Monitoring
| # | Item | Severity | Verification | Status |
|---|------|----------|--------------|--------|
| 5.1 | Application logs streaming correctly | High | Check log output in production | [ ] |
| 5.2 | Error rate baseline established | Medium | Note pre-deploy error rate | [ ] |
| 5.3 | Alert rules configured for new endpoints/features | Medium | Verify monitoring dashboards updated | [ ] |
| 5.4 | Health check endpoint responding in production | Critical | `GET /health` returns 200 OK | [ ] |
| 5.5 | Database connection pool healthy | Medium | Monitor connection count post-deploy | [ ] |

### 6. Communication
| # | Item | Severity | Verification | Status |
|---|------|----------|--------------|--------|
| 6.1 | Team notified of release schedule | High | Slack/email message sent | [ ] |
| 6.2 | Stakeholders informed of new features | Medium | Release notes shared | [ ] |
| 6.3 | Rollback contact person identified | High | Confirm on-call engineer for deployment | [ ] |
| 6.4 | Post-deployment verification scheduled | Medium | Calendar event for 1-hour post-deploy check | [ ] |
| 6.5 | Retrospective scheduled (if significant release) | Low | Calendar event created | [ ] |

## Quick Reference
- **Critical items:** 1.1, 1.2, 2.1, 2.2, 4.1, 4.2, 5.4
- **Must-pass before production deploy:** All Critical + 2.3, 2.4, 2.6, 3.1, 3.2, 4.3, 4.4, 4.6, 5.1, 6.1, 6.3
- **Nice-to-have:** 1.3, 1.4, 2.5, 3.3, 3.4, 3.5, 5.3, 5.5, 6.5
