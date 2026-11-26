import { expect, test } from 'vitest';
import { validateTankStockInputs } from './tankCalibration';

const baseFields = ({ cap = 1000, minAlert = 100, deadStock = 250, actualStock = 500 } = {}) => [
  { key: 'capacity', label: 'Capacity', unit: 'litres', resolvedLitres: cap },
  { key: 'minAlert', label: 'Min Alert', unit: 'litres', resolvedLitres: minAlert },
  { key: 'deadStock', label: 'Dead Stock', unit: 'litres', resolvedLitres: deadStock },
  { key: 'actualStock', label: 'Actual Stock', unit: 'litres', resolvedLitres: actualStock },
];

test('allows dead stock above minimum alert when within tank capacity', () => {
  expect(validateTankStockInputs({ ar: false, config: {}, fields: baseFields({ deadStock: 250, minAlert: 100, cap: 1000 }) })).toBe('');
});

test('rejects dead stock below zero or above tank capacity', () => {
  expect(validateTankStockInputs({ ar: false, config: {}, fields: baseFields({ deadStock: 1200, minAlert: 100, cap: 1000 }) }))
    .toBe('Dead stock must be between 0 and tank capacity.');
  expect(validateTankStockInputs({ ar: true, config: {}, fields: baseFields({ deadStock: -5, minAlert: 100, cap: 1000 }) }))
    .toBe('النقطة الميتة يجب أن تكون بين 0 وسعة الخزان.');
});
