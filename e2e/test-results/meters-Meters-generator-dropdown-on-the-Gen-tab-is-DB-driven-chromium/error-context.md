# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: meters.spec.ts >> Meters >> generator dropdown on the Gen tab is DB-driven
- Location: tests\meters.spec.ts:45:7

# Error details

```
Error: expect(locator).toBeEnabled() failed

Locator: locator('div.form-group').filter({ has: locator('label').filter({ hasText: /Site\/Branch|الموقع|الفرع/i }) }).first().locator('select')
Expected: enabled
Timeout: 8000ms
Error: element(s) not found

Call log:
  - Expect "toBeEnabled" with timeout 8000ms
  - waiting for locator('div.form-group').filter({ has: locator('label').filter({ hasText: /Site\/Branch|الموقع|الفرع/i }) }).first().locator('select')

```

```yaml
- complementary:
  - img "PEC"
  - text: FuelTrack Energy Tracking S System Admin System Admin
  - img
  - navigation:
    - img
    - link "Notifications":
      - /url: /notifications
    - button "Open notification preview":
      - img
    - text: Main Menu
    - link "Dashboard":
      - /url: /
      - img
      - text: Dashboard
    - link "Generators Overview":
      - /url: /generators/overview
      - img
      - text: Generators Overview
    - link "Fuel Stock":
      - /url: /stock/fuel
      - img
      - text: Fuel Stock
    - link "Oil Stock":
      - /url: /stock/oil
      - img
      - text: Oil Stock
    - link "Fuel & Oil Movements":
      - /url: /fuel-movements
      - img
      - text: Fuel & Oil Movements
    - link "Movement Logs":
      - /url: /movement-logs
      - img
      - text: Movement Logs
    - link "Periodic Inventory":
      - /url: /inventory
      - img
      - text: Periodic Inventory
    - link "Review Readings":
      - /url: /review-changes
      - img
      - text: Review Readings
    - link "Dispatch Requests":
      - /url: /warehouse/dispatch
      - img
      - text: Dispatch Requests
    - link "Warehouse Supply":
      - /url: /warehouse/supply
      - img
      - text: Warehouse Supply
    - link "Maintenance":
      - /url: /maintenance
      - img
      - text: Maintenance
    - link "Work Permits":
      - /url: /maintenance/permits
      - img
      - text: Work Permits
    - link "Parts Catalog":
      - /url: /warehouse/parts
      - img
      - text: Parts Catalog
    - link "Warehouse Stock":
      - /url: /warehouse/stock
      - img
      - text: Warehouse Stock
    - link "Warehouse Reports":
      - /url: /warehouse/reports
      - img
      - text: Warehouse Reports
    - link "Installed Parts":
      - /url: /maintenance/installed-parts
      - img
      - text: Installed Parts
    - button "Reports":
      - img
      - text: Reports
      - img
    - button "System Setup":
      - img
      - text: System Setup
      - img
  - button "Light Mode":
    - img
    - text: Light Mode
  - button "🇸🇦 عربي"
- main:
  - heading "Meters" [level=1]
  - paragraph: Manage meters linked to sites and generators
  - button:
    - img
  - button "New":
    - img
    - text: New
  - img
  - text: 2 Total
  - img
  - text: 0 عداد مولدات
  - img
  - text: 0 عداد وقود
  - img
  - text: 0 عداد طاقة
  - img
  - textbox "Search name, serial, or site..."
  - img
  - text: عداد خلية التحرير MTDH02 Active
  - img
  - text: محطة توليد ذهبان - موقع التحررير Energy 75,000 kWh Hours 0 h
  - button "Edit":
    - img
    - text: Edit
  - button "Delete":
    - img
    - text: Delete
  - img
  - text: عداد خلية ذهبان MTDH01 Active
  - img
  - text: محطة توليد ذهبان - موقع ذهبان Energy 300,000 kWh Hours 0 h
  - button "Edit":
    - img
    - text: Edit
  - button "Delete":
    - img
    - text: Delete
  - dialog:
    - img
    - text: New Meter
    - button "Close modal":
      - img
    - button "Fuel Meter":
      - img
      - text: Fuel Meter
    - button "Gen Meter":
      - img
      - text: Gen Meter
    - button "Energy Meter":
      - img
      - text: Energy Meter
    - text: Location Governorate **
    - combobox "Governorate **":
      - option "— Select —"
      - option "BR-mt4pgckv-2t0" [selected]
      - option "امانة العاصمة"
    - text: Station *
    - combobox "Station *":
      - option "— Select —"
      - option "ST-mt4pgf9c-6y9" [selected]
    - text: Site / Branch *
    - combobox "Site / Branch *":
      - option "— Select —" [selected]
      - option "SUB-mt4pgh12-5dd"
      - option "SUB-mt4pgjcb-4d5"
    - text: Meter Details Meter Name **
    - textbox "Meter Name **"
    - paragraph: Meter name is required
    - text: Serial Number **
    - textbox "Serial Number **"
    - paragraph: Serial number is required
    - text: Readings Standard Reading
    - spinbutton "Standard Reading"
    - text: Running Reading
    - spinbutton "Running Reading"
    - checkbox "Active Meter" [checked]
    - text: Active Meter
    - button "Cancel"
    - button "Save Meter":
      - img
      - text: Save Meter
```

# Test source

```ts
  123 |   } else {
  124 |     subName = rand('SUB');
  125 |     await apiPost(page, '/api/sub-stations', {
  126 |       stationId: Number(stationId),
  127 |       name: subName,
  128 |       code: rand('SUBX'),
  129 |       isActive: true,
  130 |     });
  131 |     const refreshed = asArray(await apiGet(page, '/api/sub-stations'));
  132 |     const created = refreshed.filter((x) => sid(x) === stationId).find((x) => x.name === subName);
  133 |     if (!created) throw new Error('Failed to create/seeded sub-station');
  134 |     subName = created.name;
  135 |     subId = created.id;
  136 |   }
  137 | 
  138 |   return {
  139 |     branchId: Number(branchId),
  140 |     branchName,
  141 |     stationId: Number(stationId),
  142 |     stationName,
  143 |     subStationId: Number(subId),
  144 |     subStationName: subName,
  145 |   };
  146 | }
  147 | 
  148 | /** Best-effort creation of a generator linked to the hierarchy (for linking tests). */
  149 | export async function ensureGenerator(page: Page, h: Hierarchy): Promise<Hierarchy> {
  150 |   const gens = asArray(await apiGet(page, '/api/generators'));
  151 |   if (gens.length) {
  152 |     return { ...h, generatorId: gens[0].id, generatorName: gens[0].name };
  153 |   }
  154 |   const name = rand('GEN');
  155 |   await apiPost(page, '/api/generators', {
  156 |     name,
  157 |     branchId: h.branchId,
  158 |     stationId: h.stationId,
  159 |     subStationId: h.subStationId,
  160 |     nominalPower: 0,
  161 |     actualPowerKw: 0,
  162 |     isActive: true,
  163 |     status: 'active',
  164 |   }).catch(() => null);
  165 |   const refreshed = asArray(await apiGet(page, '/api/generators'));
  166 |   const created = refreshed.find((g) => g.name === name);
  167 |   if (created) return { ...h, generatorId: created.id, generatorName: created.name };
  168 |   return h;
  169 | }
  170 | 
  171 | /** Navigate to a page and wait for the SPA shell + lazy route to settle. */
  172 | export async function gotoPage(page: Page, path: string): Promise<void> {
  173 |   await page.goto(path);
  174 |   await page.waitForLoadState('networkidle').catch(() => {});
  175 | }
  176 | 
  177 | /**
  178 |  * Assert a <select> is populated directly from a lookup API.
  179 |  * `nameKey` picks the display field from each API row (default `Name`).
  180 |  */
  181 | export async function assertSelectFromApi(
  182 |   page: Page,
  183 |   sel: Locator,
  184 |   apiPath: string,
  185 |   nameKey = 'Name',
  186 | ): Promise<void> {
  187 |   await expect(sel).toBeEnabled();
  188 |   const opts = await realOptions(sel);
  189 |   const api = asArray(await apiGet(page, apiPath));
  190 |   expect(opts.length, `${apiPath} select should have options`).toBeGreaterThan(0);
  191 |   for (const row of api) {
  192 |     const label = String(row[nameKey] ?? '').trim();
  193 |     if (label) expect(opts, `option "${label}" from ${apiPath}`).toContain(label);
  194 |   }
  195 | }
  196 | 
  197 | /**
  198 |  * Walk the Branch → Station → SubStation cascade, asserting each child select
  199 |  * is enabled only after its parent is chosen and is populated from the DB.
  200 |  */
  201 | export async function assertCascade(
  202 |   page: Page,
  203 |   opts: {
  204 |     branchLabel: RegExp;
  205 |     stationLabel: RegExp;
  206 |     subLabel: RegExp;
  207 |     branchName: string;
  208 |     stationName: string;
  209 |     subName: string;
  210 |   },
  211 | ): Promise<void> {
  212 |   const branchSel = selectByLabel(page, opts.branchLabel);
  213 |   await expect(branchSel).toBeEnabled();
  214 |   await branchSel.selectOption({ label: opts.branchName });
  215 | 
  216 |   const stationSel = selectByLabel(page, opts.stationLabel);
  217 |   await expect(stationSel).toBeEnabled();
  218 |   const stationOpts = await realOptions(stationSel);
  219 |   expect(stationOpts, 'station options should include the DB station').toContain(opts.stationName);
  220 |   await stationSel.selectOption({ label: opts.stationName });
  221 | 
  222 |   const subSel = selectByLabel(page, opts.subLabel);
> 223 |   await expect(subSel).toBeEnabled();
      |                        ^ Error: expect(locator).toBeEnabled() failed
  224 |   const subOpts = await realOptions(subSel);
  225 |   expect(subOpts, 'sub-station options should include the DB sub-station').toContain(opts.subName);
  226 |   await subSel.selectOption({ label: opts.subName });
  227 | }
  228 | 
```