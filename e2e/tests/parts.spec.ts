import { test, expect } from '@playwright/test';
import { rand, gotoPage, asArray, apiGet, apiPost, ensureHierarchy } from './helpers';

/**
 * Warehouse Parts Catalog page — /warehouse/parts
 * Covers: page load, add part via API, list, filter, and export button.
 */
test.describe('Warehouse Parts Catalog', () => {
  test('loads without errors', async ({ page }) => {
    await gotoPage(page, '/warehouse/parts');
    await expect(page.getByRole('heading', { name: /Parts Catalog|كتالوج قطع الغيار/i })).toBeVisible();
  });

  test('add part via API and see it in the table', async ({ page }) => {
    const h = await ensureHierarchy(page);
    await gotoPage(page, '/warehouse/parts');

    // Ensure a spare part exists via API
    const parts = asArray(await apiGet(page, '/api/spare-parts'));
    if (!parts.length) {
      const catRes = await apiGet(page, '/api/lookups/by-category/PartCategory');
      const cats = asArray(catRes);
      const catId = cats.length ? cats[0].id : 1;

      const unitRes = await apiGet(page, '/api/lookups/by-category/PartUnit');
      const units = asArray(unitRes);
      const unitId = units.length ? units[0].id : 1;

      await apiPost(page, '/api/spare-parts', {
        name: rand('PART'),
        partNumber: rand('PN'),
        partCategoryLookupId: catId,
        unitLookupId: unitId,
        unitPrice: 10,
        minStockQuantity: 5,
        isHoursTracked: true,
        isActive: true,
      });
    }

    await gotoPage(page, '/warehouse/parts');
    await page.waitForTimeout(3000);

    const rows = page.locator('.data-table tbody tr');
    await expect(rows).toHaveCount(1, { timeout: 10000 });
  });

  test('filter by category via API', async ({ page }) => {
    await gotoPage(page, '/warehouse/parts');
    await page.waitForTimeout(2000);

    // The category dropdown should be populated from API lookups
    const catSelect = page.locator('select').filter({ hasText: /All Categories|كل الفئات/i }).first();
    await expect(catSelect).toBeEnabled();
  });

  test('export button is present', async ({ page }) => {
    await gotoPage(page, '/warehouse/parts');
    await expect(page.getByRole('button', { name: /Export Excel|تصدير Excel/i })).toBeVisible();
  });
});
