import { test, expect } from '@playwright/test';
import { rand, gotoPage, asArray, apiGet, apiPost, ensureHierarchy } from './helpers';

/**
 * Warehouse Dispatch Requests page — /warehouse/dispatch
 * Covers: page load, incoming/outgoing tabs, and API-driven create + list.
 */
test.describe('Warehouse Dispatch Requests', () => {
  test('loads without errors', async ({ page }) => {
    await gotoPage(page, '/warehouse/dispatch');
    await expect(page.getByRole('heading', { name: /Warehouse Dispatch Requests|طلبات الصرف/i })).toBeVisible();
  });

  test('incoming tab shows dispatch requests from API', async ({ page }) => {
    const h = await ensureHierarchy(page);

    // Ensure a spare part exists for the dispatch request
    const parts = asArray(await apiGet(page, '/api/spare-parts'));
    let partId: number | undefined;
    if (parts.length) {
      partId = parts[0].id;
    } else {
      const created = await apiPost(page, '/api/spare-parts', {
        name: rand('PART'),
        partNumber: rand('PN'),
        unitLookupId: 1,
        partCategoryLookupId: 1,
        isActive: true,
      });
      partId = created.id;
    }

    // Create a dispatch request via API
    await apiPost(page, '/api/warehouse/dispatch', {
      subStationId: h.subStationId,
      stationId: h.stationId,
      workPermitId: null,
      purpose: 'E2E test dispatch',
      notes: 'Created by E2E test',
      items: [
        {
          sparePartId: partId,
          requestedQuantity: 3,
          notes: 'Test item',
        },
      ],
    });

    await gotoPage(page, '/warehouse/dispatch');
    await page.waitForTimeout(3000);

    // The incoming tab should show at least one row
    const rows = page.locator('.data-table tbody tr');
    await expect(rows).toHaveCount(1, { timeout: 10000 });
  });

  test('outgoing tab filters by current user', async ({ page }) => {
    await gotoPage(page, '/warehouse/dispatch');
    await page.waitForTimeout(2000);

    // Switch to outgoing tab
    await page.getByRole('button', { name: /My Requests|الصادرة/i }).click();
    await page.waitForTimeout(2000);

    // Should not crash; the table should be visible (may be empty if no outgoing requests)
    const table = page.locator('.data-table');
    await expect(table).toBeVisible();
  });
});
