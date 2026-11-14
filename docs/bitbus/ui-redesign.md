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
   - Rebuilt the dashboard's visual hierarchy into a command header, five-card summary strip, supporting-metric band, insight rail, analytical chart workspace and site-capacity panel while preserving every existing metric and calculation.
   - Reskinned operational review/history, maintenance baselines and due table, warehouse stock, fuel/oil views, periodic inventory, rollback approval, authentication recovery screens and reports.
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

## Local font assets

The CSS expects these self-hosted files under `frontend/public/fonts/`:

- `Inter-Latin-Variable.woff2`
- `Cairo-Arabic-Variable.woff2`
- `Cairo-Latin-Variable.woff2`

The three font binaries and their matching `Inter-OFL.txt` and `Cairo-OFL.txt` license files are now tracked in the redesign branch, so no manual font copy is required.

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

## Phase 5 — cleanup-only pass

**Branch:** `redesign/ui-cleanup`
**Base:** `development` at `3ecffbb`

This pass is intentionally visual-only. It changed neither routes, navigation labels,
translation files/keys, permission checks, API/store logic, dependencies, CSP nor test files.
No DOM structure, headings, button names, roles or business calculations were changed.

### Converted to the visual system

- Extended the existing design-token layer with reusable medium/pill radii, accent-counter
  treatment and an overlay token. The light-theme warning token was adjusted from `#a66a13`
  to `#a36812` so warning text on white clears the 4.5:1 contrast threshold.
- Replaced one-off colours, alpha fills, radii and shadows in these interactive screens with
  the established semantic tokens: `AddReading`, `ActiveGenerators`, `FuelMovements`,
  `FuelOilDashboard`, `FuelStockPage`, `GeneratorMaintenanceTable`, `Generators`,
  `GeneratorsOverview`, `InstalledParts`, `LocationsManagement`, `MyGenerators`,
  `Notifications`, `OilInventory`, `OilStockPage`, `RecordReplacement`, `ReviewChanges`,
  `RolesManagement`, `ServiceDefinitionsAdmin`, `StockInventory`, `SubStationReading`,
  `TankRefillTransfer`, `TankSetup`, `TanksManagement` and `Users`.
- Normalised the shared hierarchical filter to the same text, danger, border and radius tokens.

### Contrast review

Static WCAG contrast calculations from the final token values:

- Dark: primary text/background **14.09:1**, muted text/background **7.61:1**, faint
  text/background **5.32:1**.
- Light: primary text/background **11.80:1**, muted text/background **5.51:1**, faint
  text/background **4.60:1**.
- Light semantic foregrounds on white: primary **5.67:1**, secondary **6.23:1**,
  success **4.69:1**, warning **4.62:1**, danger **5.09:1**.

### Intentionally retained hard-coded colours

- `PeriodicInventoryPage.jsx` print markup and its `@media print` rules, plus the print block
  in `pages/reports/reports.css`: fixed black/white/paper colours are required for legible
  print/PDF output and are outside the interactive themes.
- Chart data-series palettes in `Dashboard.jsx`, `OperationalDailyLog.jsx` and the report-tab
  chart configuration remain fixed. They identify data series, rather than application chrome;
  replacing them with one semantic UI colour would reduce chart distinction.

### Deliberately not removed or changed

- No selector was deleted from `frontend/src/styles.css`. A static grep cannot prove every
  legacy selector is unused because older pages use generated class strings and attribute-based
  selectors. Keeping them avoids a cascade regression without a rendered verification pass.
- The legacy standalone report-listing components retain their compatible legacy token aliases
  and fallback values for now. They require a complete rendered report matrix before a broad
  conversion can be proved safe; no functional or theme-breaking change was made to them here.

### Phase 5 files changed

- `frontend/src/styles.css`
- `frontend/src/components/common/HierarchicalFilter.jsx`
- The 24 page components listed in **Converted to the visual system** above.
- This report: `docs/bitbus/ui-redesign.md`.

### Manual verification checklist

Run these commands manually from PowerShell; they were not run by Codex:

```powershell
Set-Location D:\FTM\frontend
npm.cmd run test -- --run
npm.cmd run build
npm.cmd run dev
```

Then, in both Arabic/RTL and English/LTR, and in both dark and light themes, inspect:

- `FuelMovements`, `FuelStockPage`, `OilStockPage`, `OilInventory` and `TankRefillTransfer`:
  group headers, status badges, low-stock alerts and counters.
- `AddReading`, `TankSetup`, `SubStationReading`, `ReviewChanges`, `Notifications` and
  `TankRefillTransfer`: alert/error/success surfaces, modal overlays and destructive actions.
- `FuelOilDashboard`, `ActiveGenerators`, `Generators`, `GeneratorsOverview`,
  `GeneratorMaintenanceTable`, `ServiceDefinitionsAdmin`, `RecordReplacement` and
  `InstalledParts`: table headers, status badges, empty states and pagination.
- The hierarchical filters wherever used: labels, disabled selectors, chevrons and the clear
  button, including keyboard focus.
- Print preview for periodic inventory and report pages, confirming that paper output remains
  black-on-white and that chart series remain individually distinguishable.

Build, unit tests, Playwright and screenshots remain owner-run verification steps.
