import { test, expect } from '@playwright/test';

test.describe('Navigation', () => {
  test('redirects to login when not authenticated', async ({ page }) => {
    await page.goto('/');
    await expect(page).toHaveURL(/.*login/);
  });
});
