import { test, expect } from '@playwright/test';

async function loginAsAdmin(page) {
  await page.goto('/login');
  await page.fill('input[type="email"], input[name="email"]', 'admin@ft.com');
  await page.fill('input[type="password"]', 'Admin@123');
  await page.click('button[type="submit"]');
  await page.waitForURL('/', { timeout: 15000 });
}

test('Generators page loads', async ({ page }) => {
  await loginAsAdmin(page);
  const errors = [];
  page.on('pageerror', (err) => errors.push(err.message));
  await page.goto('/generators');
  await page.waitForTimeout(3000);
  expect(errors).toHaveLength(0);
  await expect(page.getByRole('heading', { name: /Generators|المولّدات/ }).first()).toBeVisible({ timeout: 10000 });
});
