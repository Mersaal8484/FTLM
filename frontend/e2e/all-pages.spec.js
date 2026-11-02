import { test, expect } from '@playwright/test';

async function loginAsAdmin(page) {
  await page.goto('/login');
  await page.fill('input[type="email"], input[name="email"]', 'admin@ft.com');
  await page.fill('input[type="password"]', 'Admin@123');
  await page.click('button[type="submit"]');
  await page.waitForTimeout(3000);
}

const PAGES = [
  { path: '/', name: 'Dashboard' },
  { path: '/generators', name: 'Generators' },
  { path: '/generators/active', name: 'Active Generators' },
  { path: '/generators/overview', name: 'Generators Overview' },
  { path: '/tanks', name: 'Tanks Management' },
  { path: '/oils', name: 'Oil Inventory' },
  { path: '/locations', name: 'Locations Management' },
  { path: '/meters', name: 'Meters Management' },
  { path: '/users', name: 'Users' },
  { path: '/roles', name: 'Roles Management' },
  { path: '/controllers', name: 'Controllers' },
  { path: '/audits', name: 'Audit Logs' },
  { path: '/notifications', name: 'Notifications' },
  { path: '/my-generators', name: 'My Generators' },
  { path: '/fuel-movements', name: 'Fuel Movements' },
  { path: '/movement-logs', name: 'Movement Logs' },
  { path: '/inventory', name: 'Periodic Inventory' },
  { path: '/review-changes', name: 'Review Changes' },
  { path: '/stock/fuel', name: 'Fuel Stock' },
  { path: '/stock/oil', name: 'Oil Stock' },
  { path: '/warehouse/parts', name: 'Warehouse Parts' },
  { path: '/warehouse/stock', name: 'Warehouse Stock' },
  { path: '/warehouse/supply', name: 'Warehouse Supply' },
  { path: '/warehouse/dispatch', name: 'Warehouse Dispatch' },
  { path: '/warehouse/reports', name: 'Warehouse Reports' },
  { path: '/maintenance', name: 'Maintenance Dashboard' },
  { path: '/maintenance/permits', name: 'Work Permits' },
  { path: '/maintenance/installed-parts', name: 'Installed Parts' },
  { path: '/reports', name: 'Reports' },
  { path: '/setup-oils', name: 'Setup Oils' },
  { path: '/rollback', name: 'Rollback Approval' },
];

test.describe('All Pages Load Without JS Errors', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  for (const p of PAGES) {
    test(p.name, async ({ page }) => {
      const errors = [];
      page.on('pageerror', (err) => errors.push(err.message));
      await page.goto(p.path);
      await page.waitForTimeout(2000);
      expect(errors).toHaveLength(0);
    });
  }
});
