import { test, expect } from '@playwright/test';

async function loginAsAdmin(page) {
  await page.goto('/login');
  await page.fill('input[type="email"], input[name="email"]', 'admin@ft.com');
  await page.fill('input[type="password"]', 'Admin@123');
  await page.click('button[type="submit"]');
  await page.waitForURL('/', { timeout: 15000 });
}

test.describe('Maintenance & Warehouse Pages', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test('Work Permits page loads', async ({ page }) => {
    const errors = [];
    page.on('pageerror', (err) => errors.push(err.message));
    await page.goto('/maintenance/permits');
    await page.waitForTimeout(3000);
    expect(errors).toHaveLength(0);
    await expect(page.getByRole('heading', { name: /Work Permits|تصاريح العمل/ })).toBeVisible({ timeout: 10000 });
  });

  test('Maintenance Dashboard page loads', async ({ page }) => {
    const errors = [];
    page.on('pageerror', (err) => errors.push(err.message));
    await page.goto('/maintenance');
    await page.waitForTimeout(3000);
    expect(errors).toHaveLength(0);
    await expect(page.getByRole('heading', { name: /Maintenance Dashboard|لوحة الصيانة/ })).toBeVisible({ timeout: 10000 });
  });

  test('Warehouse Reports page loads', async ({ page }) => {
    const errors = [];
    page.on('pageerror', (err) => errors.push(err.message));
    await page.goto('/warehouse/reports');
    await page.waitForTimeout(3000);
    expect(errors).toHaveLength(0);
    await expect(page.getByRole('heading', { name: /Warehouse Reports|تقارير المخازن/ })).toBeVisible({ timeout: 10000 });
  });
});
