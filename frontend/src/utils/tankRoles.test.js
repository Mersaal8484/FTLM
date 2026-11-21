import { describe, expect, test } from 'vitest';
import { isDailyTank } from './tankRoles';

describe('isDailyTank', () => {
  test('recognizes enum, name, and generator-linked daily tanks', () => {
    expect(isDailyTank({ tankRole: 2 })).toBe(true);
    expect(isDailyTank({ tankRole: 'Daily' })).toBe(true);
    expect(isDailyTank({ generatorId: 9 })).toBe(true);
  });

  test('does not classify a main tank as daily', () => {
    expect(isDailyTank({ tankRole: 1, generatorId: null })).toBe(false);
  });
});
