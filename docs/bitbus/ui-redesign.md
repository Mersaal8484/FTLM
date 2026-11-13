# FuelTrack visual redesign report

**Branch:** `redesign/ui`

**Base:** `feature/bitbus-ui-navigation` at `7069508`

**Scope:** visual-only replacement of the existing React frontend

## Delivered by phase

1. **Tokens, type and global base**
   - Added the approved Midnight Slate and Warm Ivory palettes while retaining the legacy token names as aliases.
   - Added local Cairo/Inter `@font-face` declarations, robust fallbacks, tabular numerals, quiet borders and shadows, accessible focus treatment, reduced-motion handling and shared responsive dimensions.
2. **Application shell**
   - Kept the existing navigation content, permissions and routes while pinning the sidebar to the physical right in both languages.
   - Wired the existing collapsed-sidebar state, added route-domain icon tints and retained the full mobile drawer.
   - Added a permission-aware four-destination mobile bottom navigation without inventing routes or labels.
3. **Shared interface primitives**
   - Unified buttons, modal surfaces, toasts, loading states, skeletons and status treatments under the approved tokens and touch targets.
4. **Page sweep**
   - Reskinned the dashboard, operational review/history, maintenance baselines and due table, warehouse stock, fuel/oil views, periodic inventory, rollback approval, authentication recovery screens and reports.
   - Chart.js presentation now reads the active theme tokens without changing datasets or calculations.
   - Warehouse reports remain embedded in the existing Reports area; no independent warehouse-report navigation item was introduced.
5. **Numeral consistency**
   - Display formatting now requests Western digits in both Arabic and English. Values and calculations are unchanged.

## Compatibility boundaries

- No backend file, route, API call, permission rule, Zustand business state, i18n key/file, dependency manifest, lockfile, CSP rule, export calculation or test was changed.
- Existing headings, accessible button names and semantic roles were preserved.
- Existing tenant branding, language/theme switches, notifications, password flows, PWA behavior and mobile drawer remain in place.
- Legacy CSS was not deleted unless safely superseded by the final cascade. This avoids removing selectors still referenced by older screens.

## Files changed

- Global and shell: `frontend/src/styles.css`, `frontend/src/App.jsx`, `frontend/src/components/Sidebar.jsx`, `frontend/src/components/NavGroup.jsx`.
- Shared UI: `frontend/src/components/LoadingSpinner.jsx`, `frontend/src/components/Toast.jsx`, `frontend/src/components/ui/Button.jsx`, `frontend/src/components/ui/Modal.jsx`.
- Theme charts: `frontend/src/utils/useThemeTokens.js`, `frontend/src/pages/Dashboard.jsx`, `frontend/src/pages/reports/components/Charts/ReportCharts.jsx`, `frontend/src/pages/reports/reports.css`.
- Page visuals: `BulkBaseline.jsx`, `ForceChangePassword.jsx`, `ForgotPassword.jsx`, `FuelOilDashboard.jsx`, `FuelStockPage.jsx`, `GeneratorMaintenanceTable.jsx`, `OperationalReview.jsx`, `PeriodicInventoryPage.jsx`, `RollbackApproval.jsx`, `WarehouseStock.jsx`.
- Western-digit display formatting: `ActiveGenerators.jsx`, `AddOilToGenerator.jsx`, `AddReading.jsx`, `AuditLogs.jsx`, `Controllers.jsx`, `FuelMovements.jsx`, `GeneratorsOverview.jsx`, `MetersManagement.jsx`, `MyGenerators.jsx`, `Notifications.jsx`, `OperationalDailyLog.jsx`, `OperationalHistory.jsx`, `ReviewChanges.jsx`, `ServiceDefinitionsAdmin.jsx`, `StockInventory.jsx`, `SubStationReading.jsx`, `TankSetup.jsx`, `TanksManagement.jsx`, `movements/GeneralMovementLog.jsx`, the transaction/efficiency/generator/periodic/station report pages and `frontend/src/utils/pdfExport.js`.

## Intentionally retained

- Black/white and fixed pastel colors inside print-only periodic-inventory markup remain intentional because they produce a legible paper/PDF document and do not appear in the interactive themes.
- Fixed colors used by exported documents and data-series identity remain intentional; changing them would alter export presentation or data meaning rather than only the application chrome.
- The existing navigation hierarchy and wording remain unchanged even where the visual mockup used different dummy labels.

## Local font assets required

The CSS expects these self-hosted files under `frontend/public/fonts/`:

- `Inter-Latin-Variable.woff2`
- `Cairo-Arabic-Variable.woff2`
- `Cairo-Latin-Variable.woff2`

The files are currently available in `D:\FTM\frontend\public\fonts` but are not tracked on this clean branch. They must be copied into the redesign worktree before visual verification. The matching `Inter-OFL.txt` and `Cairo-OFL.txt` license files should be copied with them.

## Verification status

- Static diff check: passed; no whitespace errors. Git only reported the repository's existing LF-to-CRLF working-copy notices.
- Static scope check: passed; no backend, package manifest, lockfile or i18n file changed.
- Hard-coded-color review: interactive legacy colors were replaced in the targeted high-impact screens; print/export and deliberate data-series colors were retained as described above.
- Build, unit tests, Playwright and screenshot matrix: **not run by Codex**, per owner instruction. The owner will run them manually in PowerShell.

## Design/behavior reconciliation

- The approved mockup's labels and sample content were not copied; the real application's labels, routes and permission filtering win.
- The mockup's small typography and controls were raised to the specification's readable minimums and 44px touch targets.
- The sidebar stays on the physical right while content direction continues to follow the selected language.
- The mobile bottom bar selects only destinations already permitted for the current user and leaves the full drawer available.
