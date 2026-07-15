import { test, expect } from '@playwright/test';
import { rand, apiGet, apiPost, ensureHierarchy } from './helpers';

/**
 * Daily Reading form (AddReading) — Active Control Panel integration.
 *
 * Verifies the change from this session:
 *   - The form shows the control panel (controller) currently linked to the
 *     generator, or a clear warning when none is linked.
 *   - A submitted reading is recorded against that controller (ControllerId).
 */
test.describe('Daily Reading — Active Control Panel', () => {
  async function makeGenerator(page: any, subStationId: number, name: string) {
    const res = await apiPost(page, '/api/generators', {
      subStationId: Number(subStationId),
      name,
      serialNumber: rand('SN'),
      nominalPower: 0,
      actualPowerKw: 0,
      fuelConsumptionRatePerHour: 0,
    });
    const gens = asArray(await apiGet(page, '/api/generators'));
    const created = gens.find((g: any) => g.name === name) || res;
    return created;
  }

  async function makeController(page: any, generatorId: number, name: string) {
    const res = await apiPost(page, '/api/controllers', {
      generatorId: Number(generatorId),
      name,
      code: rand('C'),
      controllerType: 'Test',
      energyReading: 0,
      operatingHours: 0,
      autoConnect: false,
    });
    const list = asArray(await apiGet(page, '/api/controllers'));
    return list.find((c: any) => c.name === name) || res;
  }

  test('shows the linked control panel and records it on the reading', async ({ page }) => {
    const h = await ensureHierarchy(page);

    // Generator A — linked to a controller.
    const genA = await makeGenerator(page, h.subStationId, rand('GEN-A'));
    const ctrlA = await makeController(page, genA.id, rand('CTRL-A'));
    expect(ctrlA?.id, 'controller should be created and linked').toBeTruthy();

    // Open the daily reading form for generator A.
    await page.goto(`/generators/${genA.id}/add-reading`);
    await page.waitForLoadState('networkidle').catch(() => {});

    await expect(page.getByRole('heading', { name: /New Reading Entry/i })).toBeVisible();
    await expect(page.getByText(/Active Control Panel/i)).toBeVisible();
    // The linked controller name must be shown (not the warning).
    await expect(page.getByText(ctrlA.name, { exact: false })).toBeVisible();
    await expect(page.getByText(/No control panel linked/i)).toHaveCount(0);

    // Backend: a reading for this generator is recorded against the controller.
    const readingDate = new Date().toISOString();
    const post = await apiPost(page, '/api/readings', {
      generatorId: Number(genA.id),
      readingDate,
      prevEnergyKWh: 0,
      currentEnergyKWh: 120,
      prevRunningHours: 0,
      currentRunningHours: 12,
      fuelLookupId: 19, // Diesel (seeded)
      standardFuelRate: 1,
      oilLookupId: 21, // LockOil (seeded)
      oilUsedLiters: 0,
      notes: 'e2e-controller-check',
    });
    expect(post?.id, 'reading should be created').toBeTruthy();

    const list = asArray(await apiGet(page, `/api/readings?generatorId=${genA.id}`));
    const created = list.find((r: any) => r.id === post.id);
    expect(created, 'created reading should be returned by the API').toBeTruthy();
    expect(created.controllerId, 'reading must record the linked controller').toBe(Number(ctrlA.id));
    expect(created.controllerName, 'reading should include the controller name').toBe(ctrlA.name);
  });

  test('warns when the generator has no linked control panel', async ({ page }) => {
    const h = await ensureHierarchy(page);

    // Generator B — intentionally NOT linked to any controller.
    const genB = await makeGenerator(page, h.subStationId, rand('GEN-B'));

    await page.goto(`/generators/${genB.id}/add-reading`);
    await page.waitForLoadState('networkidle').catch(() => {});

    await expect(page.getByRole('heading', { name: /New Reading Entry/i })).toBeVisible();
    await expect(page.getByText(/Active Control Panel/i)).toBeVisible();
    await expect(page.getByText(/No control panel linked/i)).toBeVisible();
  });
});

function asArray(d: any): any[] {
  if (Array.isArray(d)) return d;
  if (d && Array.isArray(d.data)) return d.data;
  if (d && Array.isArray(d.items)) return d.items;
  return [];
}
