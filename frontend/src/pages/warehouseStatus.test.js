import { describe, expect, it } from 'vitest';
import { dispatchStatus, supplyStatus } from './warehouseStatus';

describe('warehouse status labels', () => {
  it('recognizes localized supply states without treating unknown states as draft', () => {
    expect(supplyStatus('مسودة')).toBe('draft');
    expect(supplyStatus('مكتمل')).toBe('completed');
    expect(supplyStatus('غير معروف')).not.toBe('draft');
  });
  it('recognizes localized dispatch states', () => {
    expect(dispatchStatus('قيد الانتظار')).toBe('pending');
    expect(dispatchStatus('معتمد')).toBe('approved');
    expect(dispatchStatus('مرفوض')).toBe('rejected');
  });
});
