import { test, expect } from '@playwright/test';

async function login(page) {
  await page.goto('/login');
  await page.fill('input[type="email"], input[name="email"]', 'admin@ft.com');
  await page.fill('input[type="password"]', 'Admin@123');
  await page.click('button[type="submit"]');
  await page.waitForURL((url) => !url.pathname.includes('/login'), { timeout: 10000 });
}

test.describe('Reports Page', () => {
  test.beforeEach(async ({ page }) => {
    await login(page);
  });

  test('reports page loads and shows main tabs', async ({ page }) => {
    await page.goto('/reports', { waitUntil: 'networkidle', timeout: 30000 });

    await expect(page.locator('text=Reports Center')).toBeVisible({ timeout: 10000 });
    await expect(page.locator('text=Daily Reports')).toBeVisible();
    await expect(page.locator('text=Periodic Reports')).toBeVisible();
    await expect(page.locator('text=Efficiency Report')).toBeVisible();
  });

  test('daily reports tab shows sub-tabs', async ({ page }) => {
    await page.goto('/reports', { waitUntil: 'networkidle', timeout: 30000 });

    const subTabs = page.locator('.subtab-pill');
    await expect(subTabs.first()).toBeVisible({ timeout: 10000 });
    await expect(subTabs).toHaveCount(7);
  });

  test('switching to periodic tab shows data', async ({ page }) => {
    await page.goto('/reports', { waitUntil: 'networkidle', timeout: 30000 });

    await page.click('text=Periodic Reports');
    await page.waitForTimeout(1000);

    const hasContent = await page.locator('table, [class*="no-data"], [class*="empty"]').first().isVisible({ timeout: 10000 });
    expect(hasContent).toBeTruthy();
  });

  test('switching to efficiency tab shows data', async ({ page }) => {
    await page.goto('/reports', { waitUntil: 'networkidle', timeout: 30000 });

    await page.click('text=Efficiency Report');
    await page.waitForTimeout(1000);

    const hasContent = await page.locator('table, [class*="no-data"], [class*="empty"]').first().isVisible({ timeout: 10000 });
    expect(hasContent).toBeTruthy();
  });

  test('filter component is visible', async ({ page }) => {
    await page.goto('/reports', { waitUntil: 'networkidle', timeout: 30000 });

    const filterSection = page.locator('select').first();
    await expect(filterSection).toBeVisible({ timeout: 10000 });
  });

  test('no infinite re-render on report load', async ({ page }) => {
    let fetchCount = 0;

    page.on('request', (req) => {
      if (req.url().includes('/api/reports/')) fetchCount++;
    });

    await page.goto('/reports', { waitUntil: 'networkidle', timeout: 30000 });
    await page.waitForTimeout(2000);

    expect(fetchCount).toBeLessThanOrEqual(5);
  });
});