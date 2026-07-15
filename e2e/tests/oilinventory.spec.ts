import { test, expect } from '@playwright/test';
import { gotoPage, asArray, apiGet } from './helpers';

/**
 * Oil Inventory (مخزون الزيت) — /oils  (read view of SetupOils).
 * Light spec: loads, reflects the DB (/api/oils), and opens a detail modal.
 */
test.describe('Oil Inventory (read view)', () => {
  test('loads and reflects DB oil data', async ({ page }) => {
    await gotoPage(page, '/oils');
    // Either an empty state or a grouped table should render.
    await expect(
      page.locator('.ft-table, .station-group, .empty-state').first(),
    ).toBeVisible();

    const oils = asArray(await apiGet(page, '/api/oils'));
    if (oils.length) {
      await expect(page.locator('.station-group').first()).toBeVisible();
    }
  });

  test('clicking a row opens the oil detail modal', async ({ page }) => {
    await gotoPage(page, '/oils');
    const oils = asArray(await apiGet(page, '/api/oils'));
    test.skip(oils.length === 0, 'no oils in DB to open detail');

    const row = page.locator('.ft-table tbody tr').first();
    await row.click();
    await expect(page.locator('.modal-overlay').last()).toBeVisible();
  });
});
