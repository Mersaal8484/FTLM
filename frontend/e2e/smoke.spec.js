import { test, expect } from '@playwright/test';

test('page loads without errors', async ({ page }) => {
  const errors = [];
  page.on('pageerror', (err) => errors.push(err.message));
  await page.goto('/login');
  expect(errors).toHaveLength(0);
});
