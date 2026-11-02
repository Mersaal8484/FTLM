import { test, expect } from '@playwright/test';

async function loginAsAdmin(page) {
  await page.goto('/login');
  await page.fill('input[type="email"], input[name="email"]', 'admin@ft.com');
  await page.fill('input[type="password"]', 'Admin@123');
  await page.click('button[type="submit"]');
  await page.waitForURL('/', { timeout: 15000 });
}

test('Dashboard page loads', async ({ page }) => {
  await loginAsAdmin(page);
  const errors = [];
  page.on('pageerror', (err) => errors.push(err.message));
  await page.goto('/');
  await page.waitForTimeout(3000);
  console.log('URL:', page.url());
  console.log('Errors:', errors);
  expect(errors).toHaveLength(0);
  await expect(page).toHaveURL(new RegExp('^/(/|$|\\?)'));
});
