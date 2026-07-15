import { test, expect } from '@playwright/test';

// Login helper
async function login(page) {
  await page.goto('/login');
  await page.fill('input[type="email"], input[name="email"]', 'admin@ft.com');
  await page.fill('input[type="password"]', 'Admin@123');
  await page.click('button[type="submit"]');
  await page.waitForURL((url) => !url.pathname.includes('/login'), { timeout: 10000 });
}

const REPORT_ROUTES = [
  { path: '/reports/station-readings', name: 'Station Readings' },
  { path: '/reports/fuel-transactions', name: 'Fuel Transactions' },
  { path: '/reports/oil-transactions', name: 'Oil Transactions' },
  { path: '/reports/periodic-inventory', name: 'Periodic Inventory' },
  { path: '/reports/efficiency', name: 'Efficiency' },
  { path: '/reports/generators', name: 'Generators' },
];

for (const route of REPORT_ROUTES) {
  test.describe(`Report: ${route.name}`, () => {
    test.beforeEach(async ({ page }) => {
      await login(page);
    });

    test(`${route.name} loads without infinite re-render`, async ({ page }) => {
      let fetchCount = 0;
      let branchCount = 0;

      // Count API calls
      page.on('request', (req) => {
        const url = req.url();
        if (url.includes('/api/reports/')) fetchCount++;
        if (url.includes('/api/branches')) branchCount++;
      });

      await page.goto(route.path, { waitUntil: 'networkidle', timeout: 30000 });

      // Wait a bit for any additional requests
      await page.waitForTimeout(3000);

      // These should NOT be > 5 after networkidle settles
      console.log(`${route.name}: /api/reports calls=${fetchCount}, /api/branches calls=${branchCount}`);

      expect(fetchCount).toBeLessThanOrEqual(3);
      expect(branchCount).toBeLessThanOrEqual(3);
    });

    test(`${route.name} shows filter component`, async ({ page }) => {
      await page.goto(route.path, { waitUntil: 'networkidle', timeout: 30000 });

      // HierarchicalFilter should be visible (has filter selects)
      const filterSection = page.locator('select').first();
      await expect(filterSection).toBeVisible({ timeout: 10000 });
    });

    test(`${route.name} shows table or empty state`, async ({ page }) => {
      await page.goto(route.path, { waitUntil: 'networkidle', timeout: 30000 });

      // Should have either a table or a "no data" message
      const hasContent = await page.locator('table, [class*="no-data"], [class*="empty"]').first().isVisible({ timeout: 10000 });
      expect(hasContent).toBeTruthy();
    });
  });
}
