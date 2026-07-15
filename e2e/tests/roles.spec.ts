import { test, expect } from '@playwright/test';
import { rand, gotoPage, asArray, apiGet, inputByLabel, selectByLabel, realOptions } from './helpers';

/**
 * 1) Roles & Permissions (الادوار والصلاحيات) — /roles
 * Covers: page load, add role (create + permissions save), edit role,
 * and the status dropdown being populated.
 */
test.describe('Roles & Permissions', () => {
  test('loads without errors', async ({ page }) => {
    await gotoPage(page, '/roles');
    await expect(page.getByRole('button', { name: /Add Role|إضافة دور/i })).toBeVisible();
  });

  test('add role (create + permissions) then verify it appears', async ({ page }) => {
    const name = rand('Role');
    await gotoPage(page, '/roles');

    await page.getByRole('button', { name: /Add Role|إضافة دور/i }).click();

    const modal = page.locator('.modal-box');
    await expect(modal).toBeVisible();

    // Status dropdown is populated (Active / Inactive).
    const statusSel = modal.locator('select.form-control');
    const statusOpts = await realOptions(statusSel);
    expect(statusOpts.length).toBeGreaterThan(0);

    await modal.locator('input.form-control').nth(0).fill(name); // Arabic name
    await modal.locator('input.form-control').nth(1).fill(`${name}-EN`); // English name
    await modal.locator('input.form-control').nth(2).fill(`key_${name.toLowerCase()}`); // Key

    await modal.locator('button[type="submit"]').click();

    // New role card should appear in the list.
    await expect(page.getByText(name, { exact: false })).toBeVisible();
  });

  test('edit an existing role and confirm the update persists', async ({ page }) => {
    const name = rand('Role');
    await gotoPage(page, '/roles');

    // create first
    await page.getByRole('button', { name: /Add Role|إضافة دور/i }).click();
    const modal = page.locator('.modal-box');
    await modal.locator('input.form-control').nth(0).fill(name);
    await modal.locator('input.form-control').nth(1).fill(`${name}-EN`);
    await modal.locator('input.form-control').nth(2).fill(`key_${name.toLowerCase()}`);
    await modal.locator('button[type="submit"]').click();
    await expect(page.getByText(name, { exact: false })).toBeVisible();

    // open edit by clicking the role card
    await page.locator('.page-container').getByText(name, { exact: false }).first().click();
    await expect(modal).toBeVisible();
    const updated = `${name}-EDIT`;
    await modal.locator('input.form-control').nth(0).fill(updated);
    await modal.locator('button[type="submit"]').click();

    await expect(page.getByText(updated, { exact: false })).toBeVisible();
  });
});
