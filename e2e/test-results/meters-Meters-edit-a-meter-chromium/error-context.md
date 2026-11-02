# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: meters.spec.ts >> Meters >> edit a meter
- Location: tests\meters.spec.ts:72:7

# Error details

```
Test timeout of 60000ms exceeded.
```

```
Error: locator.selectOption: Test timeout of 60000ms exceeded.
Call log:
  - waiting for locator('div.form-group').filter({ has: locator('label').filter({ hasText: /Site\/Branch|الموقع|الفرع/i }) }).first().locator('select')

```

# Page snapshot

```yaml
- generic [ref=e3]:
  - complementary [ref=e4]:
    - generic [ref=e5] [cursor=pointer]:
      - img "PEC" [ref=e7]
      - generic [ref=e8]:
        - generic [ref=e9]: FuelTrack
        - generic [ref=e10]: Energy Tracking
    - generic [ref=e12] [cursor=pointer]:
      - generic [ref=e13]: S
      - generic [ref=e14]:
        - generic [ref=e15]: System Admin
        - generic [ref=e16]: System Admin
      - img [ref=e17]
    - navigation [ref=e19]:
      - generic [ref=e21] [cursor=pointer]:
        - img [ref=e22]
        - link "Notifications" [ref=e25]:
          - /url: /notifications
        - button "Open notification preview" [ref=e26]:
          - img [ref=e27]
      - generic [ref=e29]: Main Menu
      - link "Dashboard" [ref=e30] [cursor=pointer]:
        - /url: /
        - img [ref=e31]
        - generic [ref=e36]: Dashboard
      - link "Generators Overview" [ref=e37] [cursor=pointer]:
        - /url: /generators/overview
        - img [ref=e38]
        - generic [ref=e40]: Generators Overview
      - link "Fuel Stock" [ref=e41] [cursor=pointer]:
        - /url: /stock/fuel
        - img [ref=e42]
        - generic [ref=e46]: Fuel Stock
      - link "Oil Stock" [ref=e47] [cursor=pointer]:
        - /url: /stock/oil
        - img [ref=e48]
        - generic [ref=e50]: Oil Stock
      - link "Fuel & Oil Movements" [ref=e51] [cursor=pointer]:
        - /url: /fuel-movements
        - img [ref=e52]
        - generic [ref=e57]: Fuel & Oil Movements
      - link "Movement Logs" [ref=e58] [cursor=pointer]:
        - /url: /movement-logs
        - img [ref=e59]
        - generic [ref=e62]: Movement Logs
      - link "Periodic Inventory" [ref=e63] [cursor=pointer]:
        - /url: /inventory
        - img [ref=e64]
        - generic [ref=e70]: Periodic Inventory
      - link "Review Readings" [ref=e71] [cursor=pointer]:
        - /url: /review-changes
        - img [ref=e72]
        - generic [ref=e76]: Review Readings
      - link "Dispatch Requests" [ref=e77] [cursor=pointer]:
        - /url: /warehouse/dispatch
        - img [ref=e78]
        - generic [ref=e83]: Dispatch Requests
      - link "Warehouse Supply" [ref=e84] [cursor=pointer]:
        - /url: /warehouse/supply
        - img [ref=e85]
        - generic [ref=e95]: Warehouse Supply
      - link "Maintenance" [ref=e96] [cursor=pointer]:
        - /url: /maintenance
        - img [ref=e97]
        - generic [ref=e99]: Maintenance
      - link "Work Permits" [ref=e100] [cursor=pointer]:
        - /url: /maintenance/permits
        - img [ref=e101]
        - generic [ref=e104]: Work Permits
      - link "Parts Catalog" [ref=e105] [cursor=pointer]:
        - /url: /warehouse/parts
        - img [ref=e106]
        - generic [ref=e112]: Parts Catalog
      - link "Warehouse Stock" [ref=e113] [cursor=pointer]:
        - /url: /warehouse/stock
        - img [ref=e114]
        - generic [ref=e120]: Warehouse Stock
      - link "Warehouse Reports" [ref=e121] [cursor=pointer]:
        - /url: /warehouse/reports
        - img [ref=e122]
        - generic [ref=e123]: Warehouse Reports
      - link "Installed Parts" [ref=e124] [cursor=pointer]:
        - /url: /maintenance/installed-parts
        - img [ref=e125]
        - generic [ref=e127]: Installed Parts
      - button "Reports" [ref=e128] [cursor=pointer]:
        - img [ref=e129]
        - generic [ref=e130]: Reports
        - img [ref=e131]
      - button "System Setup" [ref=e133] [cursor=pointer]:
        - img [ref=e134]
        - generic [ref=e137]: System Setup
        - img [ref=e138]
    - generic [ref=e140]:
      - button "Light Mode" [ref=e141] [cursor=pointer]:
        - img [ref=e142]
        - generic [ref=e148]: Light Mode
      - button "🇸🇦 عربي" [ref=e149] [cursor=pointer]:
        - generic [ref=e150]: 🇸🇦
        - generic [ref=e151]: عربي
  - main [ref=e152]:
    - generic [ref=e154]:
      - generic [ref=e156]:
        - generic [ref=e157]:
          - heading "Meters" [level=1] [ref=e159]
          - paragraph [ref=e160]: Manage meters linked to sites and generators
        - generic [ref=e162]:
          - button [ref=e163] [cursor=pointer]:
            - img [ref=e164]
          - button "New" [ref=e169] [cursor=pointer]:
            - img [ref=e170]
            - generic [ref=e171]: New
      - generic [ref=e172]:
        - generic [ref=e173]:
          - img [ref=e175]
          - generic [ref=e178]:
            - generic [ref=e179]: "2"
            - generic [ref=e180]: Total
        - generic [ref=e181]:
          - img [ref=e183]
          - generic [ref=e185]:
            - generic [ref=e186]: "0"
            - generic [ref=e187]: عداد مولدات
        - generic [ref=e188]:
          - img [ref=e190]
          - generic [ref=e193]:
            - generic [ref=e194]: "0"
            - generic [ref=e195]: عداد وقود
        - generic [ref=e196]:
          - img [ref=e198]
          - generic [ref=e204]:
            - generic [ref=e205]: "0"
            - generic [ref=e206]: عداد طاقة
      - generic [ref=e208]:
        - img
        - textbox "Search name, serial, or site..." [ref=e209]
      - generic [ref=e210]:
        - generic [ref=e211]:
          - generic [ref=e212]:
            - generic [ref=e213]:
              - img [ref=e215]
              - generic [ref=e218]:
                - generic [ref=e219]: عداد خلية التحرير
                - generic [ref=e220]: MTDH02
            - generic [ref=e221]: Active
          - generic [ref=e222]:
            - generic [ref=e223]:
              - img [ref=e224]
              - generic [ref=e227]: محطة توليد ذهبان - موقع التحررير
            - generic [ref=e228]:
              - generic [ref=e229]:
                - generic [ref=e230]: Energy
                - generic [ref=e231]: 75,000 kWh
              - generic [ref=e232]:
                - generic [ref=e233]: Hours
                - generic [ref=e234]: 0 h
          - generic [ref=e235]:
            - button "Edit" [ref=e236] [cursor=pointer]:
              - generic [ref=e237]:
                - img [ref=e238]
                - text: Edit
            - button "Delete" [ref=e241] [cursor=pointer]:
              - generic [ref=e242]:
                - img [ref=e243]
                - text: Delete
        - generic [ref=e246]:
          - generic [ref=e247]:
            - generic [ref=e248]:
              - img [ref=e250]
              - generic [ref=e253]:
                - generic [ref=e254]: عداد خلية ذهبان
                - generic [ref=e255]: MTDH01
            - generic [ref=e256]: Active
          - generic [ref=e257]:
            - generic [ref=e258]:
              - img [ref=e259]
              - generic [ref=e262]: محطة توليد ذهبان - موقع ذهبان
            - generic [ref=e263]:
              - generic [ref=e264]:
                - generic [ref=e265]: Energy
                - generic [ref=e266]: 300,000 kWh
              - generic [ref=e267]:
                - generic [ref=e268]: Hours
                - generic [ref=e269]: 0 h
          - generic [ref=e270]:
            - button "Edit" [ref=e271] [cursor=pointer]:
              - generic [ref=e272]:
                - img [ref=e273]
                - text: Edit
            - button "Delete" [ref=e276] [cursor=pointer]:
              - generic [ref=e277]:
                - img [ref=e278]
                - text: Delete
      - dialog [ref=e281]:
        - generic [ref=e282]:
          - generic [ref=e283]:
            - generic [ref=e285]:
              - img [ref=e286]
              - text: New Meter
            - button "Close modal" [active] [ref=e287] [cursor=pointer]:
              - img [ref=e288]
          - generic [ref=e291]:
            - generic [ref=e292]:
              - button "Fuel Meter" [ref=e293] [cursor=pointer]:
                - img [ref=e294]
                - text: Fuel Meter
              - button "Gen Meter" [ref=e297] [cursor=pointer]:
                - img [ref=e298]
                - text: Gen Meter
              - button "Energy Meter" [ref=e300] [cursor=pointer]:
                - img [ref=e301]
                - text: Energy Meter
            - generic [ref=e307]: Location
            - generic [ref=e308]:
              - generic [ref=e309]: Governorate **
              - combobox "Governorate **" [ref=e310]:
                - option "— Select —"
                - option "BR-mt4pgckv-2t0" [selected]
                - option "امانة العاصمة"
            - generic [ref=e311]:
              - generic [ref=e312]: Station *
              - combobox "Station *" [ref=e313]:
                - option "— Select —"
                - option "ST-mt4pgf9c-6y9" [selected]
            - generic [ref=e314]:
              - generic [ref=e315]: Site / Branch *
              - combobox "Site / Branch *" [ref=e316]:
                - option "— Select —" [selected]
                - option "SUB-mt4pgh12-5dd"
                - option "SUB-mt4pgjcb-4d5"
            - generic [ref=e317]: Meter Details
            - generic [ref=e318]:
              - generic [ref=e319]: Meter Name **
              - textbox "Meter Name **" [ref=e320]
              - paragraph [ref=e321]: Meter name is required
            - generic [ref=e322]:
              - generic [ref=e323]: Serial Number **
              - textbox "Serial Number **" [ref=e324]
              - paragraph [ref=e325]: Serial number is required
            - generic [ref=e326]: Readings
            - generic [ref=e327]:
              - generic [ref=e328]:
                - generic [ref=e329]: Standard Reading
                - spinbutton "Standard Reading" [ref=e330]
              - generic [ref=e331]:
                - generic [ref=e332]: Running Reading
                - spinbutton "Running Reading" [ref=e333]
            - generic [ref=e335]:
              - checkbox "Active Meter" [checked] [ref=e336]
              - text: Active Meter
          - generic [ref=e337]:
            - button "Cancel" [ref=e338] [cursor=pointer]:
              - generic [ref=e339]: Cancel
            - button "Save Meter" [ref=e340] [cursor=pointer]:
              - img [ref=e341]
              - generic [ref=e345]: Save Meter
```

# Test source

```ts
  1  | import { test, expect } from '@playwright/test';
  2  | import { rand, gotoPage, asArray, apiGet, assertSelectFromApi, assertCascade, ensureHierarchy, ensureGenerator, selectByLabel, inputByLabel, realOptions } from './helpers';
  3  | 
  4  | /**
  5  |  * 7) Meters (العدادات) — /meters
  6  |  * Covers: page load, add meter with the Branch → Station → SubStation
  7  |  * cascade (DB-driven), the Generator dropdown (DB-driven, Gen tab), and edit.
  8  |  */
  9  | test.describe('Meters', () => {
  10 |   test('loads without errors', async ({ page }) => {
  11 |     await gotoPage(page, '/meters');
  12 |     await expect(page.getByRole('button', { name: /New|جديد/i })).toBeVisible();
  13 |   });
  14 | 
  15 |   test('add meter with Branch→Station→SubStation cascade from DB', async ({ page }) => {
  16 |     const h = await ensureHierarchy(page);
  17 |     const name = rand('Meter');
  18 |     await gotoPage(page, '/meters');
  19 | 
  20 |     await page.getByRole('button', { name: /New|جديد/i }).click();
  21 |     const modal = page.locator('.modal-overlay').last();
  22 |     await expect(modal).toBeVisible();
  23 | 
  24 |     // Branch dropdown linked to /api/branches.
  25 |     const branchSel = selectByLabel(page, /Governorate|المحافظة/i);
  26 |     const branchOpts = await realOptions(branchSel);
  27 |     expect(branchOpts, 'governorate dropdown should list DB governorates').toContain(h.branchName);
  28 | 
  29 |     await assertCascade(page, {
  30 |       branchLabel: /Governorate|المحافظة/i,
  31 |       stationLabel: /Station|المحطة/i,
  32 |       subLabel: /Site\/Branch|الموقع|الفرع/i,
  33 |       branchName: h.branchName,
  34 |       stationName: h.stationName,
  35 |       subName: h.subStationName,
  36 |     });
  37 | 
  38 |     await inputByLabel(page, /Meter Name|اسم العداد/i).fill(name);
  39 |     await inputByLabel(page, /Serial Number|الرقم التسلسلي/i).fill(rand('SN'));
  40 | 
  41 |     await modal.getByRole('button', { name: /Save Meter|حفظ العداد/i }).click();
  42 |     await expect(page.getByText(name, { exact: false })).toBeVisible();
  43 |   });
  44 | 
  45 |   test('generator dropdown on the Gen tab is DB-driven', async ({ page }) => {
  46 |     const h = await ensureGenerator(page, await ensureHierarchy(page));
  47 |     if (!h.generatorName) test.skip(true, 'no generator available to test the generator dropdown');
  48 |     await gotoPage(page, '/meters');
  49 | 
  50 |     await page.getByRole('button', { name: /New|جديد/i }).click();
  51 |     const modal = page.locator('.modal-overlay').last();
  52 |     await expect(modal).toBeVisible();
  53 | 
  54 |     // choose the cascade so the generator select can render
  55 |     await assertCascade(page, {
  56 |       branchLabel: /Governorate|المحافظة/i,
  57 |       stationLabel: /Station|المحطة/i,
  58 |       subLabel: /Site\/Branch|الموقع|الفرع/i,
  59 |       branchName: h.branchName,
  60 |       stationName: h.stationName,
  61 |       subName: h.subStationName,
  62 |     });
  63 | 
  64 |     // Switch to the Generator tab.
  65 |     await page.getByRole('button', { name: /Gen Meter|Gen|المولد/i }).click();
  66 |     const genSel = selectByLabel(page, /Generator|المولد/i);
  67 |     await expect(genSel).toBeEnabled();
  68 |     const genOpts = await realOptions(genSel);
  69 |     expect(genOpts, 'generator dropdown should list DB generators').toContain(h.generatorName!);
  70 |   });
  71 | 
  72 |   test('edit a meter', async ({ page }) => {
  73 |     const h = await ensureHierarchy(page);
  74 |     const name = rand('Meter');
  75 |     await gotoPage(page, '/meters');
  76 | 
  77 |     await page.getByRole('button', { name: /New|جديد/i }).click();
  78 |     const modal = page.locator('.modal-overlay').last();
  79 |     await selectByLabel(page, /Governorate|المحافظة/i).selectOption({ label: h.branchName });
  80 |     await selectByLabel(page, /Station|المحطة/i).selectOption({ label: h.stationName });
> 81 |     await selectByLabel(page, /Site\/Branch|الموقع|الفرع/i).selectOption({ label: h.subStationName });
     |                                                             ^ Error: locator.selectOption: Test timeout of 60000ms exceeded.
  82 |     await inputByLabel(page, /Meter Name|اسم العداد/i).fill(name);
  83 |     await inputByLabel(page, /Serial Number|الرقم التسلسلي/i).fill(rand('SN'));
  84 |     await modal.getByRole('button', { name: /Save Meter|حفظ العداد/i }).click();
  85 |     await expect(page.getByText(name, { exact: false })).toBeVisible();
  86 | 
  87 |     await page.locator('.ctrl-card', { hasText: name }).getByRole('button', { name: /Edit|تعديل/i }).click();
  88 |     await expect(modal).toBeVisible();
  89 |     const updated = `${name}-ED`;
  90 |     await inputByLabel(page, /Meter Name|اسم العداد/i).fill(updated);
  91 |     await modal.getByRole('button', { name: /Save Meter|حفظ العداد/i }).click();
  92 |     await expect(page.getByText(updated, { exact: false })).toBeVisible();
  93 |   });
  94 | });
  95 | 
```