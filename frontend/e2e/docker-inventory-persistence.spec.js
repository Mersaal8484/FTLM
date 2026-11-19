import { test, expect } from '@playwright/test';

test('inventory persists on Docker and reports remain readable', async ({ page }) => {
  await page.goto('/login');
  await page.locator('input[type="email"]').fill('admin@ft.com');
  await page.locator('input[type="password"]').fill(process.env.TEST_ADMIN_PASSWORD);
  await page.locator('button[type="submit"]').click();
  await page.waitForURL('/');

  const periodsResponse = await page.request.get('/api/lookups/by-category/PeriodType');
  expect(periodsResponse.ok()).toBeTruthy();
  const periods = await periodsResponse.json();
  expect(periods.length).toBeGreaterThan(0);
  const tanksResponse = await page.request.get('/api/fuel-tanks');
  expect(tanksResponse.ok()).toBeTruthy();
  const tanks = await tanksResponse.json();
  const tank = tanks.find(item => item.fuelLookupId && item.subStationId);
  expect(tank).toBeTruthy();

  let inventoryId;
  let stationInventoryIds = [];
  try {
    const now = new Date();
    const createdResponse = await page.request.post('/api/inventory', { data: {
      subStationId: tank.subStationId, year: now.getFullYear(), month: now.getMonth() + 1,
      periodTypeLookupId: periods[0].id, inventoryDate: now.toISOString(),
      notes: 'Docker automated persistence test', fuelReadings: [{ fuelLookupId: tank.fuelLookupId, actualQtyLiters: 0 }], oilReadings: [],
    } });
    expect(createdResponse.status()).toBe(201);
    const created = await createdResponse.json();
    inventoryId = created.id;
    const loadedResponse = await page.request.get(`/api/inventory/${inventoryId}`);
    expect(loadedResponse.ok()).toBeTruthy();
    const loaded = await loadedResponse.json();
    expect(loaded.id).toBe(inventoryId);
    expect(loaded.fuelReadings[0].actualStockLiters).toBe(0);
    const batchResponse = await page.request.post('/api/inventory/station', { data: {
      stationId: 0, year: now.getFullYear(), month: now.getMonth() + 1,
      periodTypeLookupId: periods[0].id, inventoryDate: now.toISOString(), notes: 'Docker station batch test',
      fuelReadings: [{ tankId: tank.id, actualQtyLiters: 0 }], oilReadings: [],
    } });
    expect(batchResponse.ok()).toBeTruthy();
    stationInventoryIds = (await batchResponse.json()).ids;
    expect(stationInventoryIds).toHaveLength(1);
    const savedBatch = await page.request.get(`/api/inventory/${stationInventoryIds[0]}`);
    expect(savedBatch.ok()).toBeTruthy();
    expect((await savedBatch.json()).fuelReadings[0].actualStockLiters).toBe(0);
    const report = await page.request.get('/api/reports/efficiency?groupByStation=true&page=1&pageSize=20');
    expect(report.ok()).toBeTruthy();
    expect(Array.isArray((await report.json()).items)).toBeTruthy();
  } finally {
    for (const id of stationInventoryIds) expect((await page.request.delete(`/api/inventory/${id}`)).status()).toBe(204);
    if (inventoryId) {
      const deleted = await page.request.delete(`/api/inventory/${inventoryId}`);
      expect(deleted.status()).toBe(204);
    }
  }
});
