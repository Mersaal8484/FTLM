Phase 5 complete. All 3 priorities delivered.

**Priority 24 (HealthChecks):**
- `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` package installed
- `Program.cs`: `AddHealthChecks().AddDbContextCheck<FuelTrackDbContext>()` + `/healthz/live` and `/healthz/ready` endpoints mapped. .NET build succeeds.

**Priority 25 (Docker):**
- `src/FuelTrack.API/Dockerfile`: multi-stage build for net10.0 (restore with layer caching, publish, aspnet runtime)
- `docker-compose.yml` at root: API + frontend + SQL Server with healthcheck dependency chain, env vars for secrets, persistent volume for SQL Server data

**Priority 26 (Frontend cleanup):**
- `src/pages/Login.js`: removed unused `setCsrfToken` import
- `src/pages/OilInventory.js`: removed unused `Plus, Edit2` imports
- `src/i18n.js`: merged App.js flat translations (`dashboard`, `overview`, etc.) into a single `appTranslations` object, exported as both named and init arg
- `src/App.js`: replaced inline `translations` object with `useTranslation()` hook; `i18n` obtained from hook instead of direct import
- `.gitignore`: appended `*.db` pattern
- ESLint: 0 errors (remaining warnings are pre-existing)

**Next session:**
- Verify frontend integration: test that all child components receive correct `t` translations via `useTranslation()` (Login.js already uses it; App.js children get it via prop `t` which now comes from react-i18next)
- Continue to Phase 6 priorities if following `docs/implementation_plan.md`