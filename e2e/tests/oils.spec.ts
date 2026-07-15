import { test, expect } from '@playwright/test';
import { rand, gotoPage, asArray, apiGet, assertSelectFromApi, assertCascade, ensureHierarchy, selectByLabel, inputByLabel, realOptions } from './helpers';

/**
 * 5) Oil Stock Setup (اعداد مخزون الزيت) — /setup-oils
 * Covers: page load, add oil type with the OilType lookup dropdown and the
 * Branch → Station → SubStation cascade (all DB-driven), and edit.
 */
test.describe('Oil Stock Setup', () => {
  test('loads without errors', async ({ page }) => {
    await gotoPage(page, '/setup-oils');
    await expect(page.getByRole('button', { name: /Add Oil Type|إضافة نوع زيت/i })).toBeVisible();
  });

  test('add oil type: OilType lookup + Branch→Station→SubStation cascade from DB', async ({ page }) => {
    const h = await ensureHierarchy(page);
    await gotoPage(page, '/setup-oils');

    await page.getByRole('button', { name: /Add Oil Type|إضافة نوع زيت/i }).click();

    // Oil Type dropdown linked to the OilType lookup table.
    const oilSel = selectByLabel(page, /Oil Type|نوع الزيت/i);
    await assertSelectFromApi(page, oilSel, '/api/lookups/by-category/OilType');

    // Branch dropdown linked to /api/branches.
    const branchSel = selectByLabel(page, /Branch|الفرع/i);
    const branchOpts = await realOptions(branchSel);
    expect(branchOpts, 'branch dropdown should list DB governorates').toContain(h.branchName);

    await inputByLabel(page, /Current Stock|الكمية الحالية/i).fill('500');
    await inputByLabel(page, /Min Stock|الحد الأدنى/i).fill('100');

    await assertCascade(page, {
      branchLabel: /Branch|الفرع/i,
      stationLabel: /Station|المحطة الأساسية|المحطة/i,
      subLabel: /Area|المنطقة/i,
      branchName: h.branchName,
      stationName: h.stationName,
      subName: h.subStationName,
    });

    await page.getByRole('button', { name: /Add Oil|إضافة زيت/i }).click();

    // The new oil should appear in the table.
    const oils = asArray(await apiGet(page, '/api/oils'));
    expect(oils.length, 'oil should be persisted via API').toBeGreaterThan(0);
    await expect(page.locator('.ft-table tbody tr').first()).toBeVisible();
  });

  test('edit an oil type', async ({ page }) => {
    const h = await ensureHierarchy(page);
    await gotoPage(page, '/setup-oils');

    await page.getByRole('button', { name: /Add Oil Type|إضافة نوع زيت/i }).click();
    await selectByLabel(page, /Oil Type|نوع الزيت/i).selectOption({ index: 1 }).catch(() => {});
    await selectByLabel(page, /Branch|الفرع/i).selectOption({ label: h.branchName });
    await inputByLabel(page, /Current Stock|الكمية الحالية/i).fill('500');
    await inputByLabel(page, /Min Stock|الحد الأدنى/i).fill('100');
    await page.getByRole('button', { name: /Add Oil|إضافة زيت/i }).click();
    await expect(page.locator('.ft-table tbody tr').first()).toBeVisible();

    // Edit the first row.
    await page.locator('.ft-table tbody tr').first().getByRole('button', { name: /Edit|تعديل/i }).click();
    await expect(page.getByRole('button', { name: /Update|تحديث/i })).toBeVisible();
    await inputByLabel(page, /Current Stock|الكمية الحالية/i).fill('750');
    await page.getByRole('button', { name: /Update|تحديث/i }).click();
    await expect(page.locator('.ft-table tbody tr').first()).toBeVisible();
  });
});
