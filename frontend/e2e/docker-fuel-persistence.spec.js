import { test, expect } from '@playwright/test';

test('fuel supply form saves and reads a pending movement on Docker', async ({ page }) => {
  await page.goto('/login');
  await page.locator('input[type="email"]').fill('admin@ft.com');
  await page.locator('input[type="password"]').fill(process.env.TEST_ADMIN_PASSWORD);
  await page.locator('button[type="submit"]').click();
  await page.waitForURL('/');
  await page.goto('/fuel-movements');
  await page.getByRole('button', { name: /الوارد وقود/ }).click();
  await page.getByRole('button', { name: 'حركة جديدة' }).click();
  const form = page.locator('form').last();
  const tankSelect = form.locator('select').first();
  await expect(tankSelect.locator('option')).not.toHaveCount(1);
  await tankSelect.selectOption({ index: 1 });
  await form.getByLabel('الكمية الموردة (لتر) *').fill('0.001');
  let id;
  try {
    const responsePromise = page.waitForResponse(r => r.url().endsWith('/api/fuel-movements') && r.request().method() === 'POST');
    await form.getByRole('button', { name: 'حفظ وإرسال للموافقة' }).click();
    const response = await responsePromise;
    expect(response.status()).toBe(201);
    const created = await response.json();
    id = created.id;
    expect(created.quantityLiters).toBe(0.001);
    expect(created.transactionTypeLookupId).toBeTruthy();
    const loaded = await page.request.get(`/api/fuel-movements/${id}`);
    expect(loaded.ok()).toBeTruthy();
    expect((await loaded.json()).quantityLiters).toBe(0.001);
  } finally {
    if (id) expect((await page.request.delete(`/api/fuel-movements/${id}`)).status()).toBe(204);
  }
});
