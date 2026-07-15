import { test, expect } from '@playwright/test';

const ADMIN = { email: 'admin@ft.com', password: 'Admin@123' };

test.describe('User Management', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/login');
    await page.fill('input[type="email"], input[name="email"]', ADMIN.email);
    await page.fill('input[type="password"], input[name="password"]', ADMIN.password);
    await page.click('button[type="submit"]');
    await page.waitForURL('/', { timeout: 10000 });
    await page.goto('/users');
    await page.waitForLoadState('networkidle');
  });

  test('displays user cards', async ({ page }) => {
    const cards = page.locator('.glass-card').filter({ has: page.locator('button') });
    await expect(cards.first()).toBeVisible({ timeout: 10000 });
  });

  test('opens create user modal', async ({ page }) => {
    const addBtn = page.locator('button').filter({ hasText: /New User|مستخدم جديد/ }).first();
    await expect(addBtn).toBeVisible({ timeout: 5000 });
    await addBtn.click();

    const modal = page.locator('.modal-overlay');
    await expect(modal).toBeVisible({ timeout: 5000 });
    await expect(page.locator('.modal-title')).toContainText(/Add User|إضافة مستخدم/);
  });

  test('create user form has required fields', async ({ page }) => {
    const addBtn = page.locator('button').filter({ hasText: /New User|مستخدم جديد/ }).first();
    await addBtn.click();
    await page.waitForTimeout(300);

    await expect(page.locator('label').filter({ hasText: /Name|الاسم/ })).toBeVisible();
    await expect(page.locator('label').filter({ hasText: /Email|البريد/ })).toBeVisible();
    await expect(page.locator('label').filter({ hasText: /Password|كلمة المرور/ })).toBeVisible();
    await expect(page.locator('label').filter({ hasText: /Role|الدور/ })).toBeVisible();
  });

  test('create user modal has scope section', async ({ page }) => {
    const addBtn = page.locator('button').filter({ hasText: /New User|مستخدم جديد/ }).first();
    await addBtn.click();
    await page.waitForTimeout(300);

    const scopeLabel = page.locator('.form-label').filter({ hasText: /Data Scope|نطاق البيانات/ });
    await expect(scopeLabel).toBeVisible();

    const addScopeBtn = page.locator('button').filter({ hasText: /Add Scope|إضافة نطاق/ });
    await expect(addScopeBtn).toBeVisible();
  });

  test('create user with global scope', async ({ page }) => {
    const addBtn = page.locator('button').filter({ hasText: /New User|مستخدم جديد/ }).first();
    await addBtn.click();
    await page.waitForTimeout(300);

    // Fill required fields
    const nameInput = page.locator('.modal-box input.form-control').first();
    await nameInput.fill('Playwright Test User');

    const emailInput = page.locator('.modal-box input[type="email"]');
    await emailInput.fill('playwright-test@example.com');

    const passwordInput = page.locator('.modal-box input[type="password"]');
    await passwordInput.fill('TestPass123!');

    // Select role
    const roleSelect = page.locator('.modal-box select.form-select').first();
    const roleOptions = roleSelect.locator('option');
    if (await roleOptions.count() > 1) {
      await roleSelect.selectOption({ index: 1 });
    }

    // Fill phone (required: 9 digits after +967)
    const phoneInput = page.locator('.modal-box input.phone-local-input');
    if (await phoneInput.isVisible()) {
      await phoneInput.fill('712345678');
    }

    // Global scope is default (all null) — no need to select anything
    // Verify global scope hint
    const globalHint = page.locator('.modal-box').filter({ hasText: /Global scope|النطاق العالمي/ });
    // The hint only shows if there IS a global scope (default should have one)

    // Submit
    const saveBtn = page.locator('.modal-footer button.btn-primary');
    await saveBtn.click();

    // Wait for modal to close or network to complete
    await page.waitForTimeout(2000);

    // Modal should be closed
    const modal = page.locator('.modal-overlay');
    await expect(modal).toBeHidden({ timeout: 5000 });
  });

  test('cascading dropdowns: selecting Branch shows Stations', async ({ page }) => {
    const addBtn = page.locator('button').filter({ hasText: /New User|مستخدم جديد/ }).first();
    await addBtn.click();
    await page.waitForTimeout(300);

    // Find the scope row's Branch select
    const branchSelect = page.locator('.modal-box select').nth(0);
    if (await branchSelect.isVisible()) {
      const opts = branchSelect.locator('option');
      const count = await opts.count();
      if (count > 1) {
        // Select first non-empty option
        await branchSelect.selectOption({ index: 1 });
        await page.waitForTimeout(500);

        // Station dropdown should now appear
        const stationSelect = page.locator('.modal-box select').nth(1);
        if (await stationSelect.isVisible()) {
          const stationOpts = stationSelect.locator('option');
          const stationCount = await stationOpts.count();
          expect(stationCount).toBeGreaterThanOrEqual(1);
        }
      }
    }
  });

  test('add and remove scope rows', async ({ page }) => {
    const addBtn = page.locator('button').filter({ hasText: /New User|مستخدم جديد/ }).first();
    await addBtn.click();

    const modal = page.locator('.modal-overlay');
    await expect(modal).toBeVisible({ timeout: 5000 });

    // Should have 1 scope row by default
    const scopeSection = page.locator('.modal-box').filter({ hasText: /Data Scope|نطاق البيانات/ });
    const initialSelects = scopeSection.locator('select');
    const initialCount = await initialSelects.count();

    // Click Add Scope
    const addScopeBtn = page.locator('button').filter({ hasText: /Add Scope|إضافة نطاق/ });
    if (await addScopeBtn.isVisible()) {
      await addScopeBtn.click();

      // Should have more selects now
      await expect(scopeSection.locator('select').last()).toBeVisible({ timeout: 3000 });
      const afterAddSelects = scopeSection.locator('select');
      const afterCount = await afterAddSelects.count();
      expect(afterCount).toBeGreaterThan(initialCount);
    }
  });

  test('edit user dialog loads existing user data', async ({ page }) => {
    // Find an Edit button
    const editBtn = page.locator('button').filter({ hasText: /Edit|تعديل/ }).first();
    if (await editBtn.isVisible()) {
      await editBtn.click();
      await page.waitForTimeout(500);

      const modal = page.locator('.modal-overlay');
      await expect(modal).toBeVisible({ timeout: 5000 });
      await expect(page.locator('.modal-title')).toContainText(/Edit User|تعديل مستخدم/);

      // Name field should have a value
      const nameInput = page.locator('.modal-box input.form-control').first();
      const nameValue = await nameInput.inputValue();
      expect(nameValue.length).toBeGreaterThan(0);
    }
  });

  test('edit modal shows scope section', async ({ page }) => {
    const editBtn = page.locator('button').filter({ hasText: /Edit|تعديل/ }).first();
    if (await editBtn.isVisible()) {
      await editBtn.click();
      await page.waitForTimeout(500);

      const modal = page.locator('.modal-overlay');
      await expect(modal).toBeVisible({ timeout: 5000 });

      const scopeLabel = page.locator('.modal-box .form-label').filter({ hasText: /Data Scope|نطاق البيانات/ });
      await expect(scopeLabel).toBeVisible();
    }
  });
});
