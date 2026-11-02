import { test as setup, expect } from '@playwright/test';

const authFile = '.auth/admin.json';

/**
 * Global setup: authenticate once as admin and persist the session (cookies + localStorage)
 * to `.auth/admin.json`. The chromium project reuses this state.
 *
 * We also force the UI language to English so that visible-text selectors are stable
 * across runs (the app defaults to Arabic).
 */
setup('authenticate as admin', async ({ page }) => {
  const email = process.env.E2E_EMAIL || 'admin@ft.com';
  const password = process.env.E2E_PASSWORD || 'Admin@123';

  await page.goto('/login');

  await page.locator('input[type="email"]').fill(email);
  await page.locator('input[type="password"]').fill(password);
  await page.locator('button[type="submit"]').click();

  // Wait until we leave the login page (dashboard or force-password-change).
  await page
    .waitForURL((url) => !url.pathname.includes('/login'), { timeout: 20000 })
    .catch(() => {});

  // Confirm we are actually authenticated.
  const onLogin = page.url().includes('/login');
  expect(onLogin, 'Login failed — check E2E_EMAIL / E2E_PASSWORD and that the backend + DB are running').toBe(false);

  // Pin the UI to English for deterministic text selectors.
  await page.evaluate(() => localStorage.setItem('lang', 'en'));
  await page.reload();
  await page.waitForLoadState('networkidle').catch(() => {});

  await page.context().storageState({ path: authFile });
});
