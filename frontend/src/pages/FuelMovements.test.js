import {
  getApiErrorMessage,
  getMovementId,
} from './FuelMovements';

describe('FuelMovements helpers', () => {
  test('extracts API error message from nested error shapes', () => {
    const errMsg    = { response: { data: { msg: 'Custom error' } } };
    const errErrors = { response: { data: { errors: [{ msg: 'Station is required' }] } } };
    const errTitle  = { response: { data: { title: 'Validation error' } } };

    expect(getApiErrorMessage(errMsg)).toBe('Custom error');
    expect(getApiErrorMessage(errErrors)).toBe('Station is required');
    expect(getApiErrorMessage(errTitle)).toBe('Validation error');
    expect(getApiErrorMessage({})).toBe('حدث خطأ');
  });

  test('extracts movement id from new int-based DTO', () => {
    expect(getMovementId({ id: 42 })).toBe(42);
    expect(getMovementId({ id: 1 })).toBe(1);
    expect(getMovementId(null)).toBeUndefined();
  });
});
