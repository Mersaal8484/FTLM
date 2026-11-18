import {
  canReviewInventory,
  canSubmitInventory,
  buildInventoryStatusPayload,
  formatEnergyReading,
  formatInventoryEfficiency,
  getInventoryPrintDate,
  getMissingFuelReason,
  buildInventoryReadings,
  localInventoryDate,
} from './PeriodicInventoryPage';

describe('PeriodicInventoryPage inventory actions', () => {
  test('combines tanks by fuel and preserves oil identifiers', () => {
    expect(buildInventoryReadings(
      [{ fuelLookupId: 19, actualQty: '15000' }, { fuelLookupId: 19, actualQty: '500' }],
      [{ oil: 1, oilLookupId: 22, actualQty: '400' }],
    )).toEqual({ fuelReadings: [{ fuelLookupId: 19, actualQtyLiters: 15500 }], oilReadings: [{ oilId: 1, oilLookupId: 22, actualQtyLiters: 400 }] });
    expect(() => buildInventoryReadings([{ fuelLookupId: 19, actualQty: '' }], [])).toThrow();
  });
  test('admin can review draft and submitted inventories', () => {
    const admin = { role: 'admin' };

    expect(canReviewInventory(admin, { status: 'draft' })).toBe(true);
    expect(canReviewInventory(admin, { status: 'submitted' })).toBe(true);
    expect(canReviewInventory(admin, { status: 'approved' })).toBe(false);
  });

  test('inventory head submits drafts but does not approve them', () => {
    const inventoryHead = { role: 'inventory_head', permissions: { inventory: { create: true, update: true } } };

    expect(canSubmitInventory(inventoryHead, { status: 'draft' })).toBe(true);
    expect(canSubmitInventory(inventoryHead, { status: 'rejected' })).toBe(true);
    expect(canReviewInventory(inventoryHead, { status: 'draft' })).toBe(false);
  });

  test('normalizes inventory print dates for filtering', () => {
    expect(getInventoryPrintDate({ inventoryDate: '2026-06-03T12:00:00.000Z' })).toBe('2026-06-03');
    expect(getInventoryPrintDate({ inventoryDate: '2026-11-19T00:00:00Z' })).toBe('2026-11-19');
  });

  test('uses the browser local calendar date around midnight', () => {
    expect(localInventoryDate(new Date(2026, 10, 19, 0, 30))).toBe('2026-11-19');
    expect(localInventoryDate(new Date(2026, 11, 31, 23, 30))).toBe('2026-12-31');
  });

  test('uses missing fuel reason and falls back to notes for older records', () => {
    expect(getMissingFuelReason({ missingFuelReason: 'leak', notes: 'old note' })).toBe('leak');
    expect(getMissingFuelReason({ notes: 'old note' })).toBe('old note');
  });

  test('shows entered energy reading even when no-meter flag is set', () => {
    expect(formatEnergyReading(90000, true)).toBe('90,000');
    expect(formatEnergyReading(0, true)).toBe('-');
    expect(formatEnergyReading(0, false)).toBe('0');
  });

  test('formats efficiency value as percentage or dash', () => {
    expect(formatInventoryEfficiency(0.8543)).toBe('0.85%');
    expect(formatInventoryEfficiency(1)).toBe('1.00%');
    expect(formatInventoryEfficiency(0)).toBe('—');
    expect(formatInventoryEfficiency(null)).toBe('—');
    expect(formatInventoryEfficiency('invalid')).toBe('—');
  });

  test('status updates keep the full inventory payload values', () => {
    const payload = buildInventoryStatusPayload({
      id: 'inv-1',
      station: { id: 'station-1', name: 'Station' },
      subStation: { id: 'site-1', name: 'Site' },
      sentEnergyReading: 90000,
      internalConsumptionReading: 250,
      missingFuel: 500,
      missingFuelReason: 'field note',
    }, 'approved');

    expect(payload).toEqual(expect.objectContaining({
      station: 'station-1',
      subStation: 'site-1',
      sentEnergyReading: 90000,
      internalConsumptionReading: 250,
      missingFuel: 500,
      missingFuelReason: 'field note',
      status: 'approved',
    }));
  });
});
