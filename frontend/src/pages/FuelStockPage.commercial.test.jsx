import React from 'react';
import { render, screen } from '@testing-library/react';
import { afterEach, expect, test, vi } from 'vitest';

vi.mock('@tanstack/react-query', () => ({
  useQueryClient: () => ({ invalidateQueries: vi.fn() }),
}));
vi.mock('../hooks/useCommercialMode', () => ({ default: () => ({ isCommercialMode: true }) }));
vi.mock('../socket', () => ({ default: { on: vi.fn(), off: vi.fn() } }));
vi.mock('../api/stockSummaryApi', () => ({
  stockSummaryKeys: { all: ['stockSummary'], fuel: () => ['stockSummary', 'fuel'] },
  useFuelStockSummary: () => ({ data: [], isLoading: false }),
  useCommercialFuelTankStock: () => ({
    isLoading: false,
    data: {
      mainTanks: [{ tankId: 1, tankName: 'الخزان الرئيسي', tankCode: 'MAIN-1', fuelLookupName: 'ديزل', capacityLitres: 5000, currentLitres: 4500, currentLevelCm: 90, usableLitres: 4300, externalReceiptLitres: 0, transferredOutLitres: 600, alertLevel: 'normal' }],
      dailyTanks: [{ tankId: 2, tankName: 'خزان مولد 1', tankCode: 'DAY-1', fuelLookupName: 'ديزل', generatorName: 'مولد 1', capacityLitres: 500, currentLitres: 220, currentLevelCm: 22, usableLitres: 200, transferredInLitres: 250, consumedLitres: 30, alertLevel: 'normal' }],
    },
  }),
}));

import FuelStockPage from './FuelStockPage';

afterEach(() => vi.clearAllMocks());

test('commercial fuel stock separates main-tank transfers from daily-tank generator consumption', () => {
  render(<FuelStockPage lang="ar" />);

  expect(screen.getByText('الخزانات الرئيسية')).toBeInTheDocument();
  expect(screen.getByText('الخزانات اليومية والمولدات')).toBeInTheDocument();
  expect(screen.getByText('لا يُحسب من الرئيسي')).toBeInTheDocument();
  expect(screen.getByText('مولد 1')).toBeInTheDocument();
  expect(screen.getByText('30 L')).toBeInTheDocument();
  expect(screen.getByText('22.00 cm')).toBeInTheDocument();
  expect(screen.getByText('90.00 cm')).toBeInTheDocument();
});
