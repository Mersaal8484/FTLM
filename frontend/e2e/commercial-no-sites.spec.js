import { test, expect } from '@playwright/test';

test('commercial pages hide technical sites and preserve tank selection', async ({ page }) => {
  const errors = [];
  page.on('pageerror', error => errors.push(error.message));
  await page.goto('/login');
  await page.locator('input[type="email"]').fill('admin@ft.com');
  await page.locator('input[type="password"]').fill(process.env.TEST_ADMIN_PASSWORD);
  await page.locator('button[type="submit"]').click();
  await page.waitForURL('/');

  await page.goto('/stock/fuel');
  await expect(page.getByText('نوع الوقود', { exact: true })).toBeVisible();
  await expect(page.getByText('المواقع', { exact: true })).toHaveCount(0);
  await page.goto('/warehouse/dispatch');
  await expect(page.getByRole('columnheader', { name: 'رقم الطلب' })).toBeVisible();
  await expect(page.getByRole('columnheader', { name: 'الموقع', exact: true })).toHaveCount(0);
  await page.goto('/warehouse/supply');
  await expect(page.getByRole('columnheader', { name: 'الموقع', exact: true })).toHaveCount(0);
  await page.goto('/reports/warehouse');
  await expect(page.getByPlaceholder('رقم الموقع')).toHaveCount(0);
  await expect(page.getByText('فلتر زيت مولد تجريبي')).toBeVisible();
  await expect(page.getByRole('columnheader', { name: 'الموقع', exact: true })).toHaveCount(0);
  await page.getByRole('button', { name: 'تقرير الصرف' }).click();
  await expect(page.getByPlaceholder('رقم الموقع الفرعي')).toHaveCount(0);
  await page.goto('/fuel-movements');
  await page.getByRole('button', { name: /الوارد وقود/ }).click();
  await page.getByRole('button', { name: 'حركة جديدة' }).click();
  await expect(page.getByText('الموقع / الفرع *')).toHaveCount(0);
  await expect(page.locator('select option', { hasText: 'خزان ديزل تجريبي' })).toHaveCount(1);
  await page.getByRole('button', { name: /نقل وقود/ }).click();
  await expect(page.getByText('إنشاء حركة نقل غير متاح في وضع المحطة الواحدة.', { exact: false })).toBeVisible();
  await expect(page.getByText('إلى موقع *')).toHaveCount(0);
  expect(errors).toEqual([]);
});
