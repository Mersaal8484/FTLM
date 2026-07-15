import { test, expect } from '@playwright/test';
import { rand, gotoPage, asArray, apiGet, realOptions, ensureHierarchy, inputByLabel, selectByLabel } from './helpers';

/**
 * 3) Governorates & Stations (المحافظات والمحطات) — /locations
 * Hierarchy: Branch (محافظة) → Station (محطة) → SubStation (موقع/فرع).
 * Covers: add branch, add station (Branch dropdown from DB), add sub-station
 * (Parent Station dropdown from DB), and the interrelated tree rendering.
 */
test.describe('Governorates & Stations', () => {
  test('loads without errors', async ({ page }) => {
    await gotoPage(page, '/locations');
    await expect(page.getByRole('button', { name: /Add|إضافة/i })).toBeVisible();
  });

  test('add a branch, then add a station linked to it', async ({ page }) => {
    const branchName = rand('BR');
    await gotoPage(page, '/locations');

    // ── Add Branch ──
    await page.locator('.page-header .btn-primary').click();
    await inputByLabel(page, /Branch Code|كود المحافظة/i).fill(rand('GOV'));
    await inputByLabel(page, /Governorate Name|اسم المحافظة/i).fill(branchName);
    await page.getByRole('button', { name: /Save Location|حفظ الموقع/i }).click();
    await expect(page.getByText(branchName, { exact: false })).toBeVisible();

    // ── Switch to Stations tab ──
    await page.getByRole('button', { name: /Stations|المحطات/i }).click();
    await page.locator('.page-header .btn-primary').click();

    // Branch dropdown must be populated from the DB and contain our branch.
    const branchSel = selectByLabel(page, /Governorate \(Branch\)|المحافظة/i);
    const opts = await realOptions(branchSel);
    expect(opts, 'station Branch dropdown should list DB governorates').toContain(branchName);
    await branchSel.selectOption({ label: branchName });

    const stationName = rand('ST');
    await inputByLabel(page, /Station Code|كود المحطة/i).fill(rand('STA'));
    await inputByLabel(page, /Station Name|اسم المحطة/i).fill(stationName);
    await page.getByRole('button', { name: /Save Location|حفظ الموقع/i }).click();
    await expect(page.getByText(stationName, { exact: false })).toBeVisible();
  });

  test('add a sub-station whose Parent Station dropdown is DB-driven', async ({ page }) => {
    const h = await ensureHierarchy(page);
    await gotoPage(page, '/locations');

    // Switch to Sites/Branches tab.
    await page.getByRole('button', { name: /Sites|المواقع|الفروع/i }).click();
    await page.locator('.page-header .btn-primary').click();

    const parentSel = selectByLabel(page, /Parent Station|المحطة التابعة/i);
    const opts = await realOptions(parentSel);
    expect(opts, 'sub-station Parent Station dropdown should list DB stations').toContain(h.stationName);
    await parentSel.selectOption({ label: h.stationName });

    const subName = rand('SUB');
    await inputByLabel(page, /Site\/Branch Code|كود الموقع/i).fill(rand('SB'));
    await inputByLabel(page, /Site\/Branch Name|اسم الموقع/i).fill(subName);
    await page.getByRole('button', { name: /Save Location|حفظ الموقع/i }).click();
    await expect(page.getByText(subName, { exact: false })).toBeVisible();
  });

  test('tree shows the Branch → Station → SubStation relationship from the DB', async ({ page }) => {
    const h = await ensureHierarchy(page);
    await gotoPage(page, '/locations');

    const branchRow = page.locator('div', { hasText: h.branchName }).filter({ has: page.locator('svg.lucide-chevron-down, svg.lucide-chevron-up') }).first();
    await branchRow.locator('button').first().click().catch(() => {});
    await expect(page.getByText(h.stationName, { exact: false })).toBeVisible();

    const stationRow = page.locator('div', { hasText: h.stationName }).filter({ has: page.locator('svg.lucide-chevron-down, svg.lucide-chevron-up') }).first();
    await stationRow.locator('button').first().click().catch(() => {});
    await expect(page.getByText(h.subStationName, { exact: false })).toBeVisible();
  });

  test('edit a branch', async ({ page }) => {
    const h = await ensureHierarchy(page);
    await gotoPage(page, '/locations');

    const branchRow = page.locator('div', { hasText: h.branchName }).filter({ has: page.locator('svg.lucide-pencil') }).first();
    await branchRow.locator('svg.lucide-pencil').click();
    const nameInput = inputByLabel(page, /Governorate Name|اسم المحافظة/i);
    await expect(nameInput).toBeVisible();
    const updated = `${h.branchName}-ED`;
    await nameInput.fill(updated);
    await page.getByRole('button', { name: /Save Location|حفظ الموقع/i }).click();
    await expect(page.getByText(updated, { exact: false })).toBeVisible();
  });
});
