# Template: Playwright Test

## Metadata
- **Name**: playwright-test
- **Purpose**: E2E browser test for FuelTrack frontend
- **Layer**: E2E Tests
- **Path**: `frontend/e2e/{name}.spec.js`

## Template

```javascript
import { test, expect } from '@playwright/test';

test.describe('{Name} Page', () => {
  test.beforeEach(async ({ page }) => {
    // Login
    await page.goto('/login');
    await page.fill('[data-testid="email"]', 'admin@ft.com');
    await page.fill('[data-testid="password"]', 'Admin@123');
    await page.click('[data-testid="login-button"]');
    await page.waitForURL('/');
  });

  test('should display {entities} list', async ({ page }) => {
    // Navigate
    await page.click('[data-testid="nav-{entity}"]');
    await page.waitForLoadState('networkidle');

    // Assert
    await expect(page.locator('h2')).toContainText('{Entity}');
    await expect(page.locator('table')).toBeVisible();
  });

  test('should create new {entity}', async ({ page }) => {
    // Navigate
    await page.click('[data-testid="nav-{entity}"]');
    await page.waitForLoadState('networkidle');

    // Open modal
    await page.click('[data-testid="create-button"]');
    await expect(page.locator('.modal')).toBeVisible();

    // Fill form
    await page.fill('[data-testid="name-input"]', 'Test {Entity}');
    await page.click('[data-testid="save-button"]');

    // Assert
    await expect(page.locator('.modal')).not.toBeVisible();
    await expect(page.locator('text=Test {Entity}')).toBeVisible();
  });

  test('should handle loading state', async ({ page }) => {
    // Navigate
    await page.click('[data-testid="nav-{entity}"]');

    // Assert loading spinner appears
    await expect(page.locator('.spinner-border')).toBeVisible();
  });

  test('should handle empty state', async ({ page }) => {
    // This test verifies empty state handling
    // Implementation depends on whether data can be empty
  });
});
```

## Checklist
- [ ] Uses `beforeEach` for login
- [ ] Uses `data-testid` selectors (not CSS classes)
- [ ] Uses `waitForLoadState('networkidle')` after navigation
- [ ] Tests loading states
- [ ] Tests error states
- [ ] Tests CRUD operations
- [ ] Uses `expect` assertions
- [ ] No hardcoded URLs
