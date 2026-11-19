# Commercial Mode UI Hierarchy Exposure Sweep (H3)

Inventory of frontend components and hooks with enterprise hierarchy exposure (Branch / Station / SubStation) evaluated for Commercial Station Mode. No code changes applied in this audit.

| Component / Hook | Hierarchy Control | Current Commercial Behavior | Owning Phase | One-line Fix |
|---|---|---|---|---|
| `HierarchicalFilter.jsx` | Branch / Station / Site selects | Renders 3-level dropdowns; disabled/empty endpoints cause empty controls | C7 | Wrap with `useCommercialMode()`: render flat station indicator or hide entirely |
| `useFuelMovementsData.js` | Site filter query param | Queries all sites or requires manual site selection | C5 | Default site query param to technical commercial scope |
| `useGeneratorsData.js` | SubStation filter query | Fetches generators across sites unless scoped | C6 | Bind generator list queries directly to commercial station |
| `usePeriodicInventoryData.js` | Site / Tank hierarchical filter | Calls `/api/sub-stations` for tank grouping | C5 | Use commercial single-station scope; bypass sub-station fetch |
| `DailyTanks.jsx` | SubStation picker for day tanks | Renders site selector when configuring daily tanks | C5 | Automatically attach technical commercial site ID and hide site selector |
| `EnergyBalanceBoard.jsx` | Station / SubStation filter | Displays site filter dropdowns in toolbar | C7 | Auto-select commercial station scope and hide site picker |
| `FeedersManagement.jsx` | SubStation select on feeder modal | Empty options in commercial mode | C6 | Auto-bind to technical commercial substation; hide site dropdown |
| `FuelMovements.jsx` | Site / Branch hierarchical filter | Shows full hierarchy filter toolbar | C5 | Replace hierarchy filter with single-station summary in commercial mode |
| `GeneratorMaintenanceTable.jsx` | SubStation column and filter | Lists site column and filters by substation | C6 | Hide site column and scope to commercial station |
| `Generators.jsx` | Site selection on Add/Edit Generator | Requires choosing site / substation | C6 | Default `subStationId` to commercial scope; hide field in commercial mode |
| `LocationsManagement.jsx` | Full Branch/Station/Site tree | Allows creating and editing stations/branches | Hidden | Hide navigation link in commercial mode; route guards to 403/redirect |
| `PeriodicInventoryPage.jsx` | Site selector for stock count | Prompts for site selection | C5 | Omit site picker; load inventory directly for commercial station |
| `SetupOils.jsx` | SubStation assignment for oil tanks | Shows site selector | C5 | Bind oil setup to technical commercial station scope |
| `SubStationReading.jsx` | Site selector for meter reading entry | Requires choosing site before entry | C6 | Bypass site choice and load active commercial station directly |
| `Users.jsx` | User scope assignment (Branch/Station/Site) | Displays tree of sites for user permissions | C7 | In commercial mode, simplify user assignment to role-only without hierarchy |

## Summary
- **C5 (Fuel & Oils):** `useFuelMovementsData`, `usePeriodicInventoryData`, `DailyTanks`, `FuelMovements`, `PeriodicInventoryPage`, `SetupOils`.
- **C6 (Equipment & Maintenance):** `useGeneratorsData`, `FeedersManagement`, `GeneratorMaintenanceTable`, `Generators`, `SubStationReading`.
- **C7 (System & Security / Polish):** `HierarchicalFilter`, `EnergyBalanceBoard`, `Users`.
- **Hidden / Restricted:** `LocationsManagement` (hierarchy management is disabled in commercial station mode).
