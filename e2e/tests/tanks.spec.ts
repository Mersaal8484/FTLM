import { test, expect } from '@playwright/test';
import { rand, gotoPage, assertSelectFromApi, assertCascade, ensureHierarchy, selectByLabel, inputByLabel, realOptions } from './helpers';

/**
 * 4) Tank Setup (اعداد الخزانات) — /tanks
 * Covers: page load, add tank with the Fuel-Type lookup dropdown and the
 * Branch → Station → SubStation cascade (all DB-driven), and edit.
 */
const tankModal = (page: any) =>
  page.locator('div.glass-card', { has: page.getByRole('button', { name: /Save Tank|حفظ الخزان/i }) }).last();

test.describe('Tank Setup', () => {
  test('loads without errors', async ({ page }) => {
    await gotoPage(page, '/tanks');
    await expect(page.getByRole('button', { name: /New Tank|خزان جديد/i })).toBeVisible();
  });

  test('add tank: Fuel-Type lookup + Branch→Station→SubStation cascade from DB', async ({ page }) => {
    const h = await ensureHierarchy(page);
    const name = rand('Tank');
    await gotoPage(page, '/tanks');

    await page.getByRole('button', { name: /New Tank|خزان جديد/i }).click();
    const modal = tankModal(page);
    await expect(modal).toBeVisible();

    // Fuel Type dropdown is linked to the FuelType lookup table.
    const fuelSel = selectByLabel(page, /Fuel Type|نوع الوقود/i);
    await assertSelectFromApi(page, fuelSel, '/api/lookups/by-category/FuelType');

    // Branch dropdown linked to /api/branches.
    const branchSel = selectByLabel(page, /Branch|الفرع/i);
    const branchOpts = await realOptions(branchSel);
    expect(branchOpts, 'branch dropdown should list DB governorates').toContain(h.branchName);

    // Full cascade + creation.
    await inputByLabel(page, /Tank Code|كود الخزان/i).fill(rand('TK'));
    await inputByLabel(page, /Tank Name|اسم الخزان/i).fill(name);
    await inputByLabel(page, /Capacity|السعة/i).first().fill('1000');

    await assertCascade(page, {
      branchLabel: /Branch|الفرع/i,
      stationLabel: /Station|المحطة/i,
      subLabel: /Area|المنطقة|ربط/i,
      branchName: h.branchName,
      stationName: h.stationName,
      subName: h.subStationName,
    });

    await modal.getByRole('button', { name: /Save Tank|حفظ الخزان/i }).click();
    await expect(page.getByText(name, { exact: false })).toBeVisible();
  });

  test('edit a tank', async ({ page }) => {
    const h = await ensureHierarchy(page);
    const name = rand('Tank');
    await gotoPage(page, '/tanks');

    await page.getByRole('button', { name: /New Tank|خزان جديد/i }).click();
    const modal = tankModal(page);
    await inputByLabel(page, /Tank Code|كود الخزان/i).fill(rand('TK'));
    await inputByLabel(page, /Tank Name|اسم الخزان/i).fill(name);
    await selectByLabel(page, /Branch|الفرع/i).selectOption({ label: h.branchName });
    await modal.getByRole('button', { name: /Save Tank|حفظ الخزان/i }).click();
    await expect(page.getByText(name, { exact: false })).toBeVisible();

    await page.locator('.glass-card', { hasText: name }).getByRole('button', { name: /Edit|تعديل/i }).click();
    await expect(modal).toBeVisible();
    const updated = `${name}-ED`;
    await inputByLabel(page, /Tank Name|اسم الخزان/i).fill(updated);
    await modal.getByRole('button', { name: /Save Tank|حفظ الخزان/i }).click();
    await expect(page.getByText(updated, { exact: false })).toBeVisible();
  });
});
