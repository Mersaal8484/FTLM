import { defineConfig, devices } from '@playwright/test';

/**
 * FuelTrack E2E suite (Playwright).
 *
 * Prerequisites to actually RUN these tests against a real environment:
 *   1) Backend API listening on http://localhost:5000 (docker compose `sqlserver` + API).
 *   2) Frontend dev server on http://localhost:3000 (started automatically below).
 *   3) A seeded admin (admin@ft.com / Admin@123) — overridable via
 *      E2E_EMAIL / E2E_PASSWORD env vars.
 *
 * Run:  cd e2e && npx playwright test
 * List:  cd e2e && npx playwright test --list
 */
export default defineConfig({
  name: 'FuelTrack E2E Tests',
  testDir: './tests',
  timeout: 60000,
  expect: { timeout: 8000 },
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: 0,
  workers: 1,
  reporter: [['html', { open: 'never' }], ['list']],
  use: {
    baseURL: process.env.BASE_URL || 'http://localhost:3000',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    headless: true,
  },
  projects: [
    { name: 'setup', testMatch: /auth\.setup\.ts/ },
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'], storageState: '.auth/admin.json' },
      dependencies: ['setup'],
    },
  ],
  webServer: {
    command: 'cd ../frontend && npm run dev',
    url: 'http://localhost:3000',
    reuseExistingServer: true,
    timeout: 120000,
  },
});
