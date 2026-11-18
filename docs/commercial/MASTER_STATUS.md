# Commercial Station Mode — Master Status

**Branch:** `fix/commercial-solar-metering`  
**Authority:** owner-reviewed commercial brief and `01-owner-review-change-map.md`

This is the single implementation-status checklist for Commercial Station Mode.

| Item | Status | Evidence / next boundary |
|---|---|---|
| C1 — feasibility and owner change map | Done | `00-feasibility.md`, `01-owner-review-change-map.md` |
| C2 — hidden 1:1 technical hierarchy and commercial scope | Done | `CommercialStationModeService`, C2 report |
| C2.5 A — automatic/manual activation | In progress | Startup configuration and admin settings card are being added; owner verification pending |
| C2.5 B1 — locations route/navigation | Done in C2 | `EnterpriseOnly` route and commercial controller guards |
| C2.5 B2 — generator commercial residual audit | In progress | Server-side scope resolution and UI hierarchy hiding changed; active-mode verification remains required |
| C2.5 B3 — tank commercial residual audit | In progress | Server-side technical-scope resolution now covers tank create, read, update and delete; tank screens omit hierarchy requests in commercial mode. Owner verification remains required. |
| C2.5 B4 — feeder-meter-only screen | Pending audit | Do not mark complete without active-mode screen and API evidence |
| C2.5 B5 — controller commercial residual audit | Pending audit | Do not mark complete without active-mode screen and API evidence |
| C2.5 B6 — oil setup commercial residual audit | Pending audit | Do not mark complete without active-mode screen and API evidence |
| C2.5 B7 — daily fuel tanks | Done in C3, pending active-mode verification | `DailyTanks` and C3 report |
| C2.5 B8 — users shift/scope hiding | Done in C2, pending active-mode verification | `Users.jsx`, `UserService` |
| C2.5 B10 — feeders hidden/redirected | Pending audit | Hidden mapping must remain owned by `MeterService` |
| C2.5 B11 — hierarchy filters audit | Pending audit | Search all frontend hierarchy selectors under active commercial mode |
| C3 — equipment settings and daily tanks | Done | `03-equipment-settings-report.md` |
| C4 / C4.1 — operational workflow and solar metering | Done | `04-operator-approval-workflows-report.md`, `04b-solar-metering-proposal.md` |
| C5 — fuel/oil ledger and physical stocktake | Out of scope for C2.5 | Not changed by this task |
| C6 — maintenance | Not started | Not changed by this task |
| C7 — roles, navigation, branding hardening | Not started | Not changed by this task |

## Verification status

No build, test, package, or database command has been run by the coding agent. The owner executes all verification commands manually in PowerShell.
