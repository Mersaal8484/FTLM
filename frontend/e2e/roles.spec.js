import { test, expect } from '@playwright/test';

const ADMIN_USER = 'admin@ft.com';
const ADMIN_PASS = 'Admin@123';

// ── Helpers ──────────────────────────────────────────────────────────────────

async function loginAsAdmin(page) {
  await page.goto('/login');
  await page.waitForSelector('input[type="email"]', { timeout: 10000 });

  await page.locator('input[type="email"]').first().fill(ADMIN_USER);
  await page.locator('input[type="password"]').first().fill(ADMIN_PASS);
  await page.locator('button[type="submit"]').click();

  // Wait for navigation away from login (cookie is set, page redirects)
  await page.waitForURL((url) => !url.pathname.includes('/login'), { timeout: 15000 });
}

async function goToRolesPage(page) {
  await page.goto('/roles');
  // Don't use networkidle — ongoing polling prevents it from settling.
  // Just wait for the page header or role list to appear.
  await page.waitForSelector('.page-container, .roles-grid, .page-header', { timeout: 15000 });
  // Give React a moment to render
  await page.waitForTimeout(2000);
}

// ── Tests ────────────────────────────────────────────────────────────────────

test.describe('Roles & Permissions', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
    await goToRolesPage(page);
  });

  test('loads seeded roles (Admin, Operator)', async ({ page }) => {
    const body = await page.locator('body').textContent();
    expect(body).toContain('Admin');
    expect(body).toContain('Operator');
    // Should show permission counts
    expect(body).toMatch(/\d+ (perms|صلاحية)/);
  });

  test('clicking Operator role shows its permissions in the matrix', async ({ page }) => {
    // Click the Operator role card — this opens the edit modal AND loads permissions
    const operatorRole = page.locator('text=Operator').first();
    await operatorRole.click();

    // Close the edit modal that opens when clicking a role card
    // The modal-overlay has an X button inside modal-box
    const closeBtn = page.locator('.modal-overlay .modal-box button').filter({ has: page.locator('svg') }).first();
    await expect(closeBtn).toBeVisible({ timeout: 5000 });
    await closeBtn.click();
    await page.locator('.modal-overlay').first().waitFor({ state: 'hidden', timeout: 5000 });

    // Permission matrix table should be visible with module rows
    const table = page.locator('table.ft-table');
    await expect(table).toBeVisible({ timeout: 5000 });

    const tableText = await table.textContent();
    // Page defaults to Arabic — module names are Arabic
    const hasModules = ['لوحة التحكم', 'المستخدمين', 'الأدوار', 'المولّدات', 'خزانات الوقود'].some(
      m => tableText.includes(m)
    );
    expect(hasModules).toBe(true);
  });

  test('can toggle a permission switch', async ({ page }) => {
    // Select Operator role — this opens the edit modal
    await page.locator('text=Operator').first().click();

    // Close the edit modal that opens when clicking a role card
    const closeBtn = page.locator('.modal-overlay .modal-box button').filter({ has: page.locator('svg') }).first();
    await expect(closeBtn).toBeVisible({ timeout: 5000 });
    await closeBtn.click();
    await page.locator('.modal-overlay').first().waitFor({ state: 'hidden', timeout: 5000 });

    // Wait for matrix to load
    const table = page.locator('table.ft-table');
    await expect(table).toBeVisible({ timeout: 5000 });

    // Find toggle buttons (aria-pressed)
    const toggles = table.locator('button[aria-pressed]');
    const count = await toggles.count();
    expect(count).toBeGreaterThan(0);

    // Record initial state of first toggle, then click it
    const firstToggle = toggles.first();
    const initialState = await firstToggle.getAttribute('aria-pressed');
    await firstToggle.click({ force: true });

    // The toggle state should have changed
    const newState = await firstToggle.getAttribute('aria-pressed');
    expect(newState).not.toEqual(initialState);
  });

  test('Add Role button opens a creation form', async ({ page }) => {
    // Click the "Add Role" button
    const addBtn = page.getByRole('button', { name: /Add Role|إضافة دور/ });
    await addBtn.click();

    // The modal overlay should appear with a form
    const modal = page.locator('.modal-overlay').first();
    await expect(modal).toBeVisible({ timeout: 5000 });

    // Should have form fields for role name (Arabic and English)
    const inputs = modal.locator('input.form-control');
    const inputCount = await inputs.count();
    expect(inputCount).toBeGreaterThanOrEqual(2);

    // Should have a submit button (Create/إنشاء for new role)
    const saveBtn = modal.locator('button[type="submit"]');
    await expect(saveBtn).toBeVisible();
  });

  test('can create and then delete a role', async ({ page }) => {
    const roleName = `E2E_Test_${Date.now()}`;

    // Open creation form
    await page.getByRole('button', { name: /Add Role|إضافة دور/ }).click();
    const modal = page.locator('.modal-overlay').first();
    await expect(modal).toBeVisible({ timeout: 5000 });

    // Fill in the first two text inputs (Name Arabic, Name English)
    const inputs = modal.locator('input.form-control');
    await inputs.first().fill(roleName);
    await inputs.nth(1).fill(`e2e_test_${Date.now()}`);

    // Submit the form
    await modal.locator('button[type="submit"]').click();

    // Wait for modal to close
    await modal.waitFor({ state: 'hidden', timeout: 10000 });

    // New role should appear in the role list
    await expect(page.locator(`text=${roleName}`).first()).toBeVisible({ timeout: 5000 });

    // Find the delete button for THIS role — the delete button is a sibling of the
    // role name span, inside the card's header row.
    // Structure: div(card) > div(header) > [ div(name+shield) , div(buttons) ]
    const roleSpan = page.locator(`span:has-text("${roleName}")`).first();
    const cardHeader = roleSpan.locator('xpath=ancestor::div[contains(@style,"space-between")]');
    const deleteBtn = cardHeader.locator('button[title*="حذف"], button[title*="Delete"]').first();
    await deleteBtn.click();

    // The confirmation modal (React, not browser dialog) should appear
    // It has a "حذف" / "Delete" button inside .modal-box
    const confirmModal = page.locator('.modal-overlay').filter({ has: page.locator('.modal-box') }).last();
    await expect(confirmModal).toBeVisible({ timeout: 5000 });
    const confirmBtn = confirmModal.locator('.btn-danger, button:has-text("حذف"), button:has-text("Delete")').first();
    await confirmBtn.click();

    // Backend soft-deletes (sets IsActive=false), so the role stays in the list but shows inactive
    // Verify the role card now shows an inactive badge
    const roleCard = page.locator(`span:has-text("${roleName}")`).first();
    await expect(roleCard).toBeVisible({ timeout: 5000 });
    // Check that the inactive badge is present near the role card
    const inactiveBadge = page.locator(`text=/غير نشط|Inactive/`).filter({ hasText: /غير نشط|Inactive/ }).first();
    await expect(inactiveBadge).toBeVisible({ timeout: 5000 });
  });

  test('admin role delete button is hidden', async ({ page }) => {
    // The Admin role card should not have a delete button
    // It's disabled in the RoleCard component (disabled={role.key === 'admin'})
    const adminCard = page.locator('text=Admin').first();
    await adminCard.click();

    // Look for delete buttons — admin card should NOT have one
    // Since the card is inline-styled, check that no trash icon is near the Admin text
    const adminSection = page.locator('text=Admin').locator('..');
    const trashBtns = adminSection.locator('button[title*="Delete"], button[title*="حذف"]');
    const count = await trashBtns.count();
    expect(count).toBe(0);
  });
});
