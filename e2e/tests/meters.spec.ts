import { test, expect } from '@playwright/test';
import { rand, gotoPage, asArray, apiGet, assertSelectFromApi, assertCascade, ensureHierarchy, ensureGenerator, selectByLabel, inputByLabel, realOptions } from './helpers';

/**
 * 7) Meters (العدادات) — /meters
 * Covers: page load, add meter with the Branch → Station → SubStation
 * cascade (DB-driven), the Generator dropdown (DB-driven, Gen tab), and edit.
 */
test.describe('Meters', () => {
  test('loads without errors', async ({ page }) => {
    await gotoPage(page, '/meters');
    await expect(page.getByRole('button', { name: /New|جديد/i })).toBeVisible();
  });

  test('add meter with Branch→Station→SubStation cascade from DB', async ({ page }) => {
    const h = await ensureHierarchy(page);
    const name = rand('Meter');
    await gotoPage(page, '/meters');

    await page.getByRole('button', { name: /New|جديد/i }).click();
    const modal = page.locator('.modal-overlay').last();
    await expect(modal).toBeVisible();

    // Branch dropdown linked to /api/branches.
    const branchSel = selectByLabel(page, /Governorate|المحافظة/i);
    const branchOpts = await realOptions(branchSel);
    expect(branchOpts, 'governorate dropdown should list DB governorates').toContain(h.branchName);

    await assertCascade(page, {
      branchLabel: /Governorate|المحافظة/i,
      stationLabel: /Station|المحطة/i,
      subLabel: /Site\/Branch|الموقع|الفرع/i,
      branchName: h.branchName,
      stationName: h.stationName,
      subName: h.subStationName,
    });

    await inputByLabel(page, /Meter Name|اسم العداد/i).fill(name);
    await inputByLabel(page, /Serial Number|الرقم التسلسلي/i).fill(rand('SN'));

    await modal.getByRole('button', { name: /Save Meter|حفظ العداد/i }).click();
    await expect(page.getByText(name, { exact: false })).toBeVisible();
  });

  test('generator dropdown on the Gen tab is DB-driven', async ({ page }) => {
    const h = await ensureGenerator(page, await ensureHierarchy(page));
    if (!h.generatorName) test.skip(true, 'no generator available to test the generator dropdown');
    await gotoPage(page, '/meters');

    await page.getByRole('button', { name: /New|جديد/i }).click();
    const modal = page.locator('.modal-overlay').last();
    await expect(modal).toBeVisible();

    // choose the cascade so the generator select can render
    await assertCascade(page, {
      branchLabel: /Governorate|المحافظة/i,
      stationLabel: /Station|المحطة/i,
      subLabel: /Site\/Branch|الموقع|الفرع/i,
      branchName: h.branchName,
      stationName: h.stationName,
      subName: h.subStationName,
    });

    // Switch to the Generator tab.
    await page.getByRole('button', { name: /Gen Meter|Gen|المولد/i }).click();
    const genSel = selectByLabel(page, /Generator|المولد/i);
    await expect(genSel).toBeEnabled();
    const genOpts = await realOptions(genSel);
    expect(genOpts, 'generator dropdown should list DB generators').toContain(h.generatorName!);
  });

  test('edit a meter', async ({ page }) => {
    const h = await ensureHierarchy(page);
    const name = rand('Meter');
    await gotoPage(page, '/meters');

    await page.getByRole('button', { name: /New|جديد/i }).click();
    const modal = page.locator('.modal-overlay').last();
    await selectByLabel(page, /Governorate|المحافظة/i).selectOption({ label: h.branchName });
    await selectByLabel(page, /Station|المحطة/i).selectOption({ label: h.stationName });
    await selectByLabel(page, /Site\/Branch|الموقع|الفرع/i).selectOption({ label: h.subStationName });
    await inputByLabel(page, /Meter Name|اسم العداد/i).fill(name);
    await inputByLabel(page, /Serial Number|الرقم التسلسلي/i).fill(rand('SN'));
    await modal.getByRole('button', { name: /Save Meter|حفظ العداد/i }).click();
    await expect(page.getByText(name, { exact: false })).toBeVisible();

    await page.locator('.ctrl-card', { hasText: name }).getByRole('button', { name: /Edit|تعديل/i }).click();
    await expect(modal).toBeVisible();
    const updated = `${name}-ED`;
    await inputByLabel(page, /Meter Name|اسم العداد/i).fill(updated);
    await modal.getByRole('button', { name: /Save Meter|حفظ العداد/i }).click();
    await expect(page.getByText(updated, { exact: false })).toBeVisible();
  });
});
