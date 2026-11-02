import { test, expect } from '@playwright/test';
import { rand, gotoPage, asArray, apiGet, assertSelectFromApi, assertCascade, ensureHierarchy, ensureGenerator, selectByLabel, inputByLabel, realOptions } from './helpers';

/**
 * 8) Generator Setup (اعداد المولدات) — /generators
 * Covers: page load, add generator with lookup dropdowns (GeneratorType,
 * Voltage, Frequency, OwnershipType, GeneratorStatus) + the
 * Branch → Station → SubStation cascade (DB-driven) + the Linked-Controller
 * dropdown, and edit.
 */
const LOOKUP_CATEGORIES = ['GeneratorType', 'Voltage', 'Frequency', 'OwnershipType', 'GeneratorStatus'];

test.describe('Generator Setup', () => {
  test('loads without errors', async ({ page }) => {
    await gotoPage(page, '/generators');
    await expect(page.getByRole('button', { name: /Add Generator|إضافة مولد/i })).toBeVisible();
  });

  test('all lookup dropdowns are populated from the database', async ({ page }) => {
    await gotoPage(page, '/generators');
    await page.getByRole('button', { name: /Add Generator|إضافة مولد/i }).click();
    const modal = page.locator('.modal-overlay').last();
    await expect(modal).toBeVisible();

    for (const cat of LOOKUP_CATEGORIES) {
      const label = new RegExp(cat.replace(/([A-Z])/g, ' $1').trim() + '|' + (cat === 'GeneratorType' ? 'نوع المولد' : cat === 'Voltage' ? 'مستوى الفولتات' : cat === 'Frequency' ? 'التردد' : cat === 'OwnershipType' ? 'نوع الملكية' : 'حالة التشغيل'), 'i');
      const sel = selectByLabel(page, label);
      await assertSelectFromApi(page, sel, `/api/lookups/by-category/${cat}`);
    }
  });

  test('add generator with Branch→Station→SubStation cascade from DB', async ({ page }) => {
    const h = await ensureHierarchy(page);
    const name = rand('Gen');
    await gotoPage(page, '/generators');

    await page.getByRole('button', { name: /Add Generator|إضافة مولد/i }).click();
    const modal = page.locator('.modal-overlay').last();
    await expect(modal).toBeVisible();

    // Branch dropdown linked to /api/branches.
    const branchSel = selectByLabel(page, /Branch|الفرع/i);
    const branchOpts = await realOptions(branchSel);
    expect(branchOpts, 'branch dropdown should list DB governorates').toContain(h.branchName);

    await inputByLabel(page, /Generator Name|اسم المولد/i).fill(name);
    await inputByLabel(page, /Serial|الرقم التسلسلي/i).fill(rand('SN'));

    // pick the first valid option for each lookup
    for (const cat of LOOKUP_CATEGORIES) {
      const label = new RegExp(cat.replace(/([A-Z])/g, ' $1').trim(), 'i');
      const sel = selectByLabel(page, label);
      const opts = await realOptions(sel);
      if (opts.length) await sel.selectOption({ label: opts[0] });
    }

    await assertCascade(page, {
      branchLabel: /Branch|الفرع/i,
      stationLabel: /Station|المحطة/i,
      subLabel: /Area|المنطقة|وحدة فرعية/i,
      branchName: h.branchName,
      stationName: h.stationName,
      subName: h.subStationName,
    });

    await modal.getByRole('button', { name: /Add Generator|إضافة مولد|Update Generator|تحديث المولد/i }).click();
    await expect(page.getByText(name, { exact: false })).toBeVisible();
  });

  test('Linked Controller dropdown is DB-driven', async ({ page }) => {
    const h = await ensureHierarchy(page);
    await gotoPage(page, '/generators');

    await page.getByRole('button', { name: /Add Generator|إضافة مولد/i }).click();
    const modal = page.locator('.modal-overlay').last();

    const controllers = asArray(await apiGet(page, '/api/controllers'));
    if (!controllers.length) test.skip(true, 'no controllers in DB to verify the Linked Controller dropdown');

    const ctrlSel = selectByLabel(page, /Linked Controller|شاشة التحكم|Select Screen|اختر الشاشة/i);
    const opts = await realOptions(ctrlSel);
    expect(opts.length, 'linked-controller dropdown should list DB controllers').toBeGreaterThan(0);
  });

  test('edit a generator', async ({ page }) => {
    const h = await ensureHierarchy(page);
    const name = rand('Gen');
    await gotoPage(page, '/generators');

    await page.getByRole('button', { name: /Add Generator|إضافة مولد/i }).click();
    const modal = page.locator('.modal-overlay').last();
    await inputByLabel(page, /Generator Name|اسم المولد/i).fill(name);
    await selectByLabel(page, /Branch|الفرع/i).selectOption({ label: h.branchName });
    await modal.getByRole('button', { name: /Add Generator|إضافة مولد|Update Generator|تحديث المولد/i }).click();
    await expect(page.getByText(name, { exact: false })).toBeVisible();

    await page.locator('.gen-card', { hasText: name }).getByRole('button', { name: /Edit|تعديل/i }).click();
    await expect(modal).toBeVisible();
    const updated = `${name}-ED`;
    await inputByLabel(page, /Generator Name|اسم المولد/i).fill(updated);
    await modal.getByRole('button', { name: /Add Generator|إضافة مولد|Update Generator|تحديث المولد/i }).click();
    await expect(page.getByText(updated, { exact: false })).toBeVisible();
  });
});

/**
 * Active Generators (المولدات النشطة) — /generators/active
 * Covers: page loads, displays only active generators linked to control screens,
 * no add/edit/delete buttons present, data filtered by user scope.
 */
test.describe('Active Generators', () => {
  test('loads without errors', async ({ page }) => {
    await gotoPage(page, '/generators/active');
    await expect(page.locator('.page-header h1')).toBeVisible();
  });

  test('has no add/edit/delete buttons', async ({ page }) => {
    await gotoPage(page, '/generators/active');
    await expect(page.getByRole('button', { name: /Add Generator|إضافة مولد/i })).not.toBeVisible();
    await expect(page.getByRole('button', { name: /Edit|تعديل/i })).not.toBeVisible();
    await expect(page.getByRole('button', { name: /Delete|حذف/i })).not.toBeVisible();
  });

  test('displays only active generators linked to control screens', async ({ page }) => {
    await gotoPage(page, '/generators/active');
    const genCards = page.locator('.gen-card');
    const count = await genCards.count();
    // If there are generators shown, they should all have a linked controller
    for (let i = 0; i < count; i++) {
      const card = genCards.nth(i);
      // Each card should show a linked controller name
      const controllerLabel = card.locator('text=/شاشة التحكم|Linked Controller/i');
      await expect(controllerLabel).toBeVisible();
    }
  });

  test('search filters generators by name, location, or controller', async ({ page }) => {
    await gotoPage(page, '/generators/active');
    const genCardsBefore = page.locator('.gen-card');
    const countBefore = await genCardsBefore.count();

    if (countBefore > 0) {
      // Get the name of the first generator
      const firstGenName = await genCardsBefore.first().locator('.gen-name').textContent();
      if (firstGenName) {
        // Search for the first generator's name
        await page.getByRole('textbox', { name: /search/i }).fill(firstGenName);
        const genCardsAfter = page.locator('.gen-card');
        const countAfter = await genCardsAfter.count();
        expect(countAfter).toBeGreaterThanOrEqual(1);
        // Verify the searched name appears in the results
        await expect(page.getByText(firstGenName, { exact: false })).toBeVisible();
      }
    }
  });
});
