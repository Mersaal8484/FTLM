import { Page, Locator, expect } from '@playwright/test';

/* ─────────────────────────────────────────────────────────────────────────────
 * Shared helpers for the FuelTrack E2E suite.
 *
 * Goals covered:
 *  - Authenticated API access (the page context already carries the admin
 *    cookies from the saved storage state, so page.request works against /api).
 *  - Robust field locators that survive AR/EN text via the `.form-group`
 *    + `<label>` markup used consistently across the management pages.
 *  - Guaranteeing a Branch → Station → SubStation hierarchy exists in the DB
 *    (seeded on demand through the API) so dropdown / cascade assertions
 *    can prove the selects are populated from the database.
 * ───────────────────────────────────────────────────────────────────────────── */

export function rand(prefix = 'T'): string {
  return `${prefix}-${Date.now().toString(36)}-${Math.floor(Math.random() * 1e4).toString(36)}`;
}

/** Tolerate the various shapes the API returns (array | {data} | {items}). */
export function asArray(d: any): any[] {
  if (Array.isArray(d)) return d;
  if (d && Array.isArray(d.data)) return d.data;
  if (d && Array.isArray(d.items)) return d.items;
  if (d && Array.isArray(d.value)) return d.value;
  return [];
}

export async function apiGet(page: Page, path: string): Promise<any> {
  const res = await page.request.get(path, { failOnStatusCode: false });
  return res.json().catch(() => null);
}

export async function apiPost(page: Page, path: string, body: any): Promise<any> {
  const res = await page.request.post(path, { data: body, failOnStatusCode: false });
  return res.json().catch(() => null);
}

/** Locate the form-group that contains a label matching the regex. */
export function fieldGroup(page: Page, labelRegex: RegExp): Locator {
  return page.locator('div.form-group', { has: page.locator('label', { hasText: labelRegex }) }).first();
}

export function inputByLabel(page: Page, labelRegex: RegExp): Locator {
  return fieldGroup(page, labelRegex).locator('input');
}

export function selectByLabel(page: Page, labelRegex: RegExp): Locator {
  return fieldGroup(page, labelRegex).locator('select');
}

/** All <option> texts of a select, including the placeholder. */
export async function optionTexts(sel: Locator): Promise<string[]> {
  return sel.locator('option').allTextContents();
}

/** Real options only (drops empty / placeholder entries). */
export async function realOptions(sel: Locator): Promise<string[]> {
  const all = await optionTexts(sel);
  return all.map((s) => s.trim()).filter((s) => s.length > 0);
}

export interface Hierarchy {
  branchId: number;
  branchName: string;
  stationId: number;
  stationName: string;
  subStationId: number;
  subStationName: string;
  generatorId?: number;
  generatorName?: string;
}

/** Ensure a Branch → Station → SubStation chain exists; create on demand via API. */
export async function ensureHierarchy(page: Page): Promise<Hierarchy> {
  // ── Branch ──
  let branches = asArray(await apiGet(page, '/api/branches'));
  let branchName: string;
  let branchId: number;
  if (branches.length) {
    branchName = branches[0].name;
    branchId = branches[0].id;
  } else {
    branchName = rand('BR');
    await apiPost(page, '/api/branches', { name: branchName, code: rand('GOV'), isGeneration: false });
    const refreshed = asArray(await apiGet(page, '/api/branches'));
    const created = refreshed.find((b) => b.name === branchName);
    if (!created) throw new Error('Failed to create/seeded branch');
    branchName = created.name;
    branchId = created.id;
  }

  // ── Station (belongs to branch) ──
  let stations = asArray(await apiGet(page, '/api/stations')).filter((s) => s.branchId === branchId);
  let stationName: string;
  let stationId: number;
  if (stations.length) {
    stationName = stations[0].name;
    stationId = stations[0].id;
  } else {
    stationName = rand('ST');
    await apiPost(page, '/api/stations', {
      branchId: Number(branchId),
      name: stationName,
      code: rand('STA'),
      isActive: true,
    });
    const refreshed = asArray(await apiGet(page, '/api/stations')).filter((s) => s.branchId === branchId);
    const created = refreshed.find((s) => s.name === stationName);
    if (!created) throw new Error('Failed to create/seeded station');
    stationName = created.name;
    stationId = created.id;
  }

  // ── SubStation (belongs to station) ──
  const sid = (x: any) => x.stationId ?? x.station_id;
  let subs = asArray(await apiGet(page, '/api/sub-stations')).filter((x) => sid(x) === stationId);
  let subName: string;
  let subId: number;
  if (subs.length) {
    subName = subs[0].name;
    subId = subs[0].id;
  } else {
    subName = rand('SUB');
    await apiPost(page, '/api/sub-stations', {
      stationId: Number(stationId),
      name: subName,
      code: rand('SUBX'),
      isActive: true,
    });
    const refreshed = asArray(await apiGet(page, '/api/sub-stations'));
    const created = refreshed.filter((x) => sid(x) === stationId).find((x) => x.name === subName);
    if (!created) throw new Error('Failed to create/seeded sub-station');
    subName = created.name;
    subId = created.id;
  }

  return {
    branchId: Number(branchId),
    branchName,
    stationId: Number(stationId),
    stationName,
    subStationId: Number(subId),
    subStationName: subName,
  };
}

/** Best-effort creation of a generator linked to the hierarchy (for linking tests). */
export async function ensureGenerator(page: Page, h: Hierarchy): Promise<Hierarchy> {
  const gens = asArray(await apiGet(page, '/api/generators'));
  if (gens.length) {
    return { ...h, generatorId: gens[0].id, generatorName: gens[0].name };
  }
  const name = rand('GEN');
  await apiPost(page, '/api/generators', {
    name,
    branchId: h.branchId,
    stationId: h.stationId,
    subStationId: h.subStationId,
    nominalPower: 0,
    actualPowerKw: 0,
    isActive: true,
    status: 'active',
  }).catch(() => null);
  const refreshed = asArray(await apiGet(page, '/api/generators'));
  const created = refreshed.find((g) => g.name === name);
  if (created) return { ...h, generatorId: created.id, generatorName: created.name };
  return h;
}

/** Navigate to a page and wait for the SPA shell + lazy route to settle. */
export async function gotoPage(page: Page, path: string): Promise<void> {
  await page.goto(path);
  await page.waitForLoadState('networkidle').catch(() => {});
}

/**
 * Assert a <select> is populated directly from a lookup API.
 * `nameKey` picks the display field from each API row (default `Name`).
 */
export async function assertSelectFromApi(
  page: Page,
  sel: Locator,
  apiPath: string,
  nameKey = 'Name',
): Promise<void> {
  await expect(sel).toBeEnabled();
  const opts = await realOptions(sel);
  const api = asArray(await apiGet(page, apiPath));
  expect(opts.length, `${apiPath} select should have options`).toBeGreaterThan(0);
  for (const row of api) {
    const label = String(row[nameKey] ?? '').trim();
    if (label) expect(opts, `option "${label}" from ${apiPath}`).toContain(label);
  }
}

/**
 * Walk the Branch → Station → SubStation cascade, asserting each child select
 * is enabled only after its parent is chosen and is populated from the DB.
 */
export async function assertCascade(
  page: Page,
  opts: {
    branchLabel: RegExp;
    stationLabel: RegExp;
    subLabel: RegExp;
    branchName: string;
    stationName: string;
    subName: string;
  },
): Promise<void> {
  const branchSel = selectByLabel(page, opts.branchLabel);
  await expect(branchSel).toBeEnabled();
  await branchSel.selectOption({ label: opts.branchName });

  const stationSel = selectByLabel(page, opts.stationLabel);
  await expect(stationSel).toBeEnabled();
  const stationOpts = await realOptions(stationSel);
  expect(stationOpts, 'station options should include the DB station').toContain(opts.stationName);
  await stationSel.selectOption({ label: opts.stationName });

  const subSel = selectByLabel(page, opts.subLabel);
  await expect(subSel).toBeEnabled();
  const subOpts = await realOptions(subSel);
  expect(subOpts, 'sub-station options should include the DB sub-station').toContain(opts.subName);
  await subSel.selectOption({ label: opts.subName });
}
