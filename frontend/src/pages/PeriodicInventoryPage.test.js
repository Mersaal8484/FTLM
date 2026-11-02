import {
  canReviewInventory,
  canSubmitInventory,
  buildInventoryStatusPayload,
  formatEnergyReading,
  formatInventoryEfficiency,
  getInventoryPrintDate,
  getMissingFuelReason,
} from './PeriodicInventoryPage';

describe('PeriodicInventoryPage inventory actions', () => {
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
