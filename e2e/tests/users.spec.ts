import { test, expect } from '@playwright/test';
import { rand, gotoPage, asArray, apiGet, selectByLabel, inputByLabel, realOptions, ensureHierarchy } from './helpers';

/**
 * 2) User Management (ادارة المستخدمين) — /users
 * Covers: page load, add user, edit user, and the Data-Scope cascade
 * (Branch → Station → SubStation) being populated from the database and
 * interrelated.
 */
test.describe('User Management', () => {
  test('loads without errors', async ({ page }) => {
    await gotoPage(page, '/users');
    await expect(page.getByRole('button', { name: /New User|مستخدم جديد/i })).toBeVisible();
  });

  test('add a new user', async ({ page }) => {
    const name = rand('User');
    const email = `${rand('u')}@test.com`;
    await gotoPage(page, '/users');

    await page.getByRole('button', { name: /New User|مستخدم جديد/i }).click();
    const modal = page.locator('.modal-box');
    await expect(modal).toBeVisible();

    // Role dropdown populated from /api/roles
    const roleSel = selectByLabel(page, /Role|الدور/i);
    const roleOpts = await realOptions(roleSel);
    expect(roleOpts.length, 'role dropdown should be populated').toBeGreaterThan(0);
    await roleSel.selectOption({ label: roleOpts[0] });

    await inputByLabel(page, /Name|الاسم/i).fill(name);
    await inputByLabel(page, /Email|البريد/i).fill(email);
    await inputByLabel(page, /Password|كلمة المرور/i).fill('Passw0rd!');

    // Phone has a fixed +967 prefix; only 9 local digits are entered.
    await modal.locator('input.phone-local-input').fill('712345678');

    await modal.getByRole('button', { name: /Add User|إضافة مستخدم/i }).click();

    await expect(page.getByText(name, { exact: false })).toBeVisible();
    await expect(page.getByText(email, { exact: false })).toBeVisible();
  });

  test('edit an existing user', async ({ page }) => {
    const name = rand('User');
    const email = `${rand('u')}@test.com`;
    await gotoPage(page, '/users');

    await page.getByRole('button', { name: /New User|مستخدم جديد/i }).click();
    const modal = page.locator('.modal-box');
    await selectByLabel(page, /Role|الدور/i).selectOption({ index: 1 }).catch(() => {});
    await inputByLabel(page, /Name|الاسم/i).fill(name);
    await inputByLabel(page, /Email|البريد/i).fill(email);
    await inputByLabel(page, /Password|كلمة المرور/i).fill('Passw0rd!');
    await modal.locator('input.phone-local-input').fill('712345678');
    await modal.getByRole('button', { name: /Add User|إضافة مستخدم/i }).click();
    await expect(page.getByText(name, { exact: false })).toBeVisible();

    // Reload the page so React shows the newly created card.
    await gotoPage(page, '/users');
    await page.locator('.glass-card', { hasText: name }).waitFor({ state: 'visible' });

    // open edit from the card
    await page.locator('.glass-card', { hasText: name }).getByRole('button', { name: /Edit|تعديل/i }).click();
    await expect(modal).toBeVisible();
    const updated = `${name}-ED`;
    await inputByLabel(page, /Name|الاسم/i).fill(updated);
    await modal.getByRole('button', { name: /Update User|تحديث المستخدم/i }).click();

    await expect(page.getByText(updated, { exact: false })).toBeVisible();
  });

  test('data-scope cascade (Branch → Station → SubStation) is DB-driven', async ({ page }) => {
    const h = await ensureHierarchy(page);
    await gotoPage(page, '/users');

    await page.getByRole('button', { name: /New User|مستخدم جديد/i }).click();
    const modal = page.locator('.modal-box');

    // Add a scope row.
    await modal.getByRole('button', { name: /Add Scope|إضافة نطاق/i }).click();

    // The scope cascade selects are rendered directly in the modal, not inside .form-group.
    const selects = modal.locator('select');
    const branchSel = selects.nth(0);
    const branchOpts = await realOptions(branchSel);
    expect(branchOpts, 'scope branch options should include the DB branch').toContain(h.branchName);
    await branchSel.selectOption({ label: h.branchName });

    // Station select appears only after a branch is chosen.
    const stationSel = selects.nth(1);
    await expect(stationSel).toBeVisible();
    const stationOpts = await realOptions(stationSel);
    expect(stationOpts, 'scope station options should include the DB station').toContain(h.stationName);
    await stationSel.selectOption({ label: h.stationName });

    // SubStation select appears only after a station is chosen.
    const subSel = selects.nth(2);
    await expect(subSel).toBeVisible();
    const subOpts = await realOptions(subSel);
    expect(subOpts, 'scope sub-station options should include the DB sub-station').toContain(h.subStationName);
  });
});
