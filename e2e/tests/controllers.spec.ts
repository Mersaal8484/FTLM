import { test, expect } from '@playwright/test';
import { rand, gotoPage, asArray, apiGet, inputByLabel, selectByLabel, realOptions, ensureGenerator, ensureHierarchy } from './helpers';

/**
 * 6) Generator Screens Setup (اعداد شاشات المولدات) — /controllers
 * Covers: page load, add screen (Model static list + Generator dropdown from
 * DB), and edit.
 */
test.describe('Generator Screens Setup', () => {
  test('loads without errors', async ({ page }) => {
    await gotoPage(page, '/controllers');
    await expect(page.getByRole('button', { name: /Add Screen|إضافة شاشة/i })).toBeVisible();
  });

  test('add a screen: Model list + Generator dropdown from DB', async ({ page }) => {
    const h = await ensureGenerator(page, await ensureHierarchy(page));
    const before = asArray(await apiGet(page, '/api/controllers')).length;
    await gotoPage(page, '/controllers');

    await page.getByRole('button', { name: /Add Screen|إضافة شاشة/i }).click();
    const modal = page.locator('.modal-overlay').last();
    await expect(modal).toBeVisible();

    // Model is a fixed lookup list (DSE8610 / DSE8920).
    const modelSel = selectByLabel(page, /Model|الموديل/i);
    const modelOpts = await realOptions(modelSel);
    expect(modelOpts.length, 'Model list should have options').toBeGreaterThan(0);

    await inputByLabel(page, /Serial|الرقم التسلسلي/i).fill(rand('CTRL'));

    // Generator dropdown is DB-driven (only when a generator exists / is selected).
    const gens = asArray(await apiGet(page, '/api/generators'));
    if (gens.length && h.generatorName) {
      const genSel = selectByLabel(page, /Generator|المولد/i);
      const genOpts = await realOptions(genSel);
      expect(genOpts, 'generator dropdown should list DB generators').toContain(h.generatorName);
    }

    await modal.getByRole('button', { name: /Add Screen|إضافة شاشة|Update Screen|تحديث الشاشة/i }).click();

    const after = asArray(await apiGet(page, '/api/controllers')).length;
    expect(after, 'a controller should be persisted via API').toBeGreaterThan(before);

    // Reload the page so React shows the newly created card.
    await gotoPage(page, '/controllers');
    await page.locator('.ctrl-card').first().waitFor({ state: 'visible' });
  });

  test('edit a screen', async ({ page }) => {
    await ensureGenerator(page, await ensureHierarchy(page));
    await gotoPage(page, '/controllers');

    // ensure at least one controller exists
    let list = asArray(await apiGet(page, '/api/controllers'));
    if (!list.length) {
      await page.getByRole('button', { name: /Add Screen|إضافة شاشة/i }).click();
      const modal = page.locator('.modal-overlay').last();
      await selectByLabel(page, /Model|الموديل/i).selectOption({ index: 1 }).catch(() => {});
      await inputByLabel(page, /Serial|الرقم التسلسلي/i).fill(rand('CTRL'));
      await modal.getByRole('button', { name: /Add Screen|إضافة شاشة|Update Screen|تحديث الشاشة/i }).click();
      list = asArray(await apiGet(page, '/api/controllers'));
    }
    expect(list.length, 'a controller is required to test edit').toBeGreaterThan(0);

    await page.locator('.ctrl-card').first().getByRole('button', { name: /Edit|تعديل/i }).click();
    const modal = page.locator('.modal-overlay').last();
    await expect(modal).toBeVisible();
    const serial = rand('CTRL-ED');
    await inputByLabel(page, /Serial|الرقم التسلسلي/i).fill(serial);
    await modal.getByRole('button', { name: /Add Screen|إضافة شاشة|Update Screen|تحديث الشاشة/i }).click();
    await expect(modal).toBeHidden();
  });
});
