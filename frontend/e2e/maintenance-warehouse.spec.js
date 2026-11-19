import { test, expect } from '@playwright/test';

async function loginAsAdmin(page) {
  await page.goto('/login');
  await page.fill('input[type="email"], input[name="email"]', 'admin@ft.com');
  await page.fill('input[type="password"]', process.env.TEST_ADMIN_PASSWORD || 'Admin@123');
  await page.click('button[type="submit"]');
  await page.waitForURL('/', { timeout: 15000 });
}

test.describe('Maintenance & Warehouse Pages', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test('Work Permits page loads', async ({ page }) => {
    const errors = [];
    page.on('pageerror', (err) => errors.push(err.message));
    await page.goto('/maintenance/permits');
    await page.waitForTimeout(3000);
    expect(errors).toHaveLength(0);
    await expect(page.getByRole('heading', { name: /Work Permits|تصاريح العمل/ })).toBeVisible({ timeout: 10000 });
  });

  test('Work Permits accepts wrapped lists when opening the form', async ({ page }) => {
    await page.route('**/api/commercial-station-mode', route => route.fulfill({ json: { isCommercialMode: false } }));
    await page.route('**/api/sub-stations?*', route => route.fulfill({ json: { items: [{ id: 1, name: 'محطة صنعاء' }] } }));
    await page.route('**/api/lookups/by-category/*', route => route.fulfill({ json: { data: [{ id: 1, name: 'صيانة' }] } }));
    await page.goto('/maintenance/permits');
    await page.getByRole('button', { name: /تصريح جديد|New Permit/ }).click();
    await expect(page.locator('option', { hasText: 'محطة صنعاء' })).toHaveCount(1);
    await expect(page.getByText(/حدث خطأ غير متوقع|Unexpected error/)).toHaveCount(0);
  });

  test('Work Permits reports invalid list responses without crashing', async ({ page }) => {
    await page.route('**/api/work-permits?*', route => route.fulfill({ json: { unexpected: true } }));
    await page.goto('/maintenance/permits');
    await expect(page.getByText(/فشل تحميل البيانات|Failed to load data/)).toBeVisible();
    await expect(page.getByRole('heading', { name: /Work Permits|تصاريح العمل/ })).toBeVisible();
  });

  test('Work Permits uses server pages, lookup IDs and full details', async ({ page }) => {
    const requests = [];
    await page.route('**/api/lookups/by-category/MaintenanceStatus', route => route.fulfill({ json: [{ id: 89, code: 'Submitted', name: 'مقدم' }, { id: 92, code: 'Approved', name: 'معتمد' }] }));
    await page.route('**/api/work-permits?*', route => {
      const url = new URL(route.request().url());
      requests.push(url.searchParams.toString());
      const number = url.searchParams.get('page') === '2' ? 'WP-SECOND' : 'WP-FIRST';
      return route.fulfill({ json: { items: [{ id: 42, permitNumber: number, status: 'Submitted', priority: 'Normal' }], totalCount: 21 } });
    });
    await page.route('**/api/work-permits/42', route => route.fulfill({ json: { id: 42, permitNumber: 'WP-SECOND', problemDescription: 'تفاصيل اختبار', tasks: [] } }));
    await page.goto('/maintenance/permits');
    await expect(page.getByText('WP-FIRST')).toBeVisible();
    await page.getByRole('button', { name: /الصفحة التالية|Next page/ }).click();
    await expect(page.getByText('WP-SECOND')).toBeVisible();
    await page.getByTitle(/تفاصيل|Details/).click();
    await expect(page.getByText('تفاصيل اختبار')).toBeVisible();
    expect(requests.some(query => new URLSearchParams(query).get('page') === '2')).toBe(true);
  });

  test('Maintenance Dashboard page loads', async ({ page }) => {
    const errors = [];
    page.on('pageerror', (err) => errors.push(err.message));
    await page.goto('/maintenance');
    await page.waitForTimeout(3000);
    expect(errors).toHaveLength(0);
    await expect(page.getByRole('heading', { name: /Maintenance Dashboard|لوحة الصيانة/ })).toBeVisible({ timeout: 10000 });
  });

  test('Warehouse Reports page loads', async ({ page }) => {
    const errors = [];
    page.on('pageerror', (err) => errors.push(err.message));
    await page.goto('/warehouse/reports');
    await page.waitForTimeout(3000);
    expect(errors).toHaveLength(0);
    await expect(page.getByRole('heading', { name: /Warehouse Reports|تقارير المخازن/ })).toBeVisible({ timeout: 10000 });
  });
});
