# UI-1 — Navigation and polish report

**Branch:** `feature/bitbus-ui-navigation`

**Base:** `personal/development` at `038750a`

**Verification:** tests and builds intentionally not run by the agent; owner verification required

## A — Sidebar navigation

Commit: `5998ca5 feat(navigation): reorganize sidebar groups`

Changed:

- `frontend/src/components/Sidebar.jsx`
- `frontend/src/components/NavGroup.jsx`
- `frontend/src/components/Sidebar.test.jsx`
- `frontend/src/App.jsx`
- `frontend/src/i18n.js`
- `frontend/src/styles.css`
- Deleted `frontend/src/pages/MovementLogs.jsx`

The top-level order now follows the approved list while retaining the operator-only **My Generators** contextual entry. Maintenance, Reports, and System Settings share `NavGroup` and auto-expand for active child routes. Warehouse Reports remains at `/warehouse/reports`, retains the `warehouse` permission, and is an internal Reports-group item after existing report items; it is not a standalone top-level item or a Maintenance child. `/movement-logs` redirects to `/fuel-movements`.

Tests added: 3 Sidebar tests covering exact order, Maintenance children, Warehouse Reports placement, active-route expansion, and removal of Movement Logs.

## B — General movement log

Commit: `5a61e71 feat(movements): add unified general movement log`

Changed:

- `frontend/src/pages/FuelMovements.jsx`
- `frontend/src/pages/FuelMovements.test.jsx`
- `frontend/src/pages/movements/GeneralMovementLog.jsx`
- `frontend/src/pages/movements/GeneralMovementLog.test.jsx`
- `frontend/src/i18n.js`
- `src/FuelTrack.API/Controllers/FuelMovementsController.cs`
- `src/FuelTrack.API/Controllers/MovementLedgerController.cs`
- `src/FuelTrack.API/Middleware/PermissionEnforcementMiddleware.cs`
- `src/FuelTrack.Application/DTOs/Movements/MovementLedgerDtos.cs`
- `src/FuelTrack.Application/Interfaces/IFuelMovementService.cs`
- `src/FuelTrack.Application/Interfaces/IMovementLedgerService.cs`
- `src/FuelTrack.Infrastructure/DependencyInjection.cs`
- `src/FuelTrack.Infrastructure/Services/FuelMovementService.cs`
- `src/FuelTrack.Infrastructure/Services/MovementLedgerService.cs`
- `tests/FuelTrack.Tests/Services/MovementLedgerServiceTests.cs`

The default tab is General Log. The unified ledger supplies scoped fuel/oil rows with server paging, filtering, and sorting. Per-type fuel tabs request a server `type` filter and also apply a defensive client classification. The log includes loading, retry/error and empty states, locale-aware dates/numbers, details, approve/reject actions, and filtered Excel export through the existing dependency.

Tests added: 10 (3 backend service cases, 6 General Log component cases, and 1 per-type classification case). Existing FuelMovements expectations affected by the intentionally replaced Summary tab were updated.

## C — Operations translations

Commit: `cdeee74 fix(i18n): complete operations hub translations`

Changed:

- `frontend/src/pages/OperationsHub.jsx`
- `frontend/src/pages/OperationsHub.test.jsx`
- `frontend/src/i18n.js`
- `frontend/src/i18n.test.js`

Added Arabic and English title/description keys for refill, tank setup, daily log, and energy balance. Removed ineffective key fallbacks. The static audit scans literal `t('...')` usage and requires parity in both languages; template keys must match an explicit dynamic-prefix allow-list.

Tests added: 3 effective cases (hub actions in Arabic and English, plus the static literal-key audit). Because tests were not run, no legacy missing-key output was produced by the agent; any output from the owner's run must be treated as a follow-up defect list.

## D — Light theme and empty states

Commit: `fix(theme): complete light operational surfaces` (the commit containing this report).

Changed:

- `frontend/src/styles.css`
- `frontend/src/pages/EnergyBalanceBoard.jsx`
- `frontend/src/pages/MaintenanceDashboard.jsx`
- `frontend/src/themeTokens.test.js`
- This report

Added light-theme operational amber, teal, grid, chart-grid and tooltip tokens. Energy Balance no longer embeds dark-only colors. Maintenance chart bars use chart tokens, and both bottom panels now render stable, themed empty states when arrays are missing or empty.

The code-level contrast audit covered Dashboard, Operations Hub, Maintenance Dashboard, Service Intervals, General Movement Log, Fuel Stock, Oil Stock, Review Readings, Periodic Inventory, Warehouse Reports, shared modals and shared form controls. These surfaces inherit the shared `--text`/`--text-muted` against `--surface`/`--surface-2`; the light palette uses dark body text and the dark palette uses light body text. Print-only black/white rules in Periodic Inventory were intentionally retained because they target paper output, not either screen theme.

Tests added: 2 static theme-token/hard-coded-color guards.

## Conflicts and boundaries

- The requested branch did not exist at the start. It was created in a dedicated worktree from `personal/development` at `038750a`; no prior work was overwritten.
- Section A required Movement Logs to be folded into B before deletion, so B was implemented and committed before A while retaining one commit per section.
- The task stated `.theme-light` had only two rules. The current base already had a full light palette; only the missing operational/chart tokens were added.
- No `recharts` imports or components exist under `frontend/src`. Existing CSS-based chart bars were themed; no dependency or parallel chart system was introduced.
- Warehouse Reports was interpreted exactly as the final task: an item embedded inside the existing Reports navigation group, not a top-level entry. Its route, page, and permission module are unchanged.
- No migrations, dependencies, lockfiles, pushes, builds, or tests were performed.

## Owner verification

Tests added in this patch: **18**, not run. Run the normal backend and frontend verification manually after the final commit, then inspect both themes and both languages at the listed pages.
