import React from 'react';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { afterEach, describe, expect, test, vi } from 'vitest';

vi.mock('../api/client', () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    delete: vi.fn(),
  },
}));

vi.mock('../hooks/useCommercialMode', () => ({ default: vi.fn() }));

import client from '../api/client';
import useCommercialMode from '../hooks/useCommercialMode';
import PeriodicInventoryPage from './PeriodicInventoryPage';

afterEach(() => vi.resetAllMocks());

const mockCurrentUser = {
  role: 'admin',
  permissions: {
    inventory: {
      create: true,
      read: true,
      update: true,
      delete: true,
    },
  },
};

const mockTanks = [
  { id: 1, name: 'Main Tank 1', fuelLookupId: 10, subStationId: 100 },
];

const mockOils = [
  { id: 1, name: 'Oil 15W40', oilLookupId: 20, subStationId: 100 },
];

const mockPeriodTypes = [
  { id: 1, name: 'شهري' },
];

describe('PeriodicInventoryPage Commercial Mode (Task A)', () => {
  test('commercial mode hides site select and loads tanks/oils immediately in draft modal', async () => {
    useCommercialMode.mockReturnValue({ isCommercialMode: true, isCommercial: true, modeResolved: true });

    client.get.mockImplementation(async (url) => {
      if (url === '/api/inventory') {
        return { data: { data: [] } };
      }
      if (url === '/api/fuel-tanks') {
        return { data: mockTanks };
      }
      if (url === '/api/oils') {
        return { data: mockOils };
      }
      if (url.includes('/api/lookups/by-category/PeriodType')) {
        return { data: mockPeriodTypes };
      }
      if (url === '/api/stations' || url === '/api/sub-stations') {
        throw new Error('Endpoints should not be called in commercial mode');
      }
      return { data: [] };
    });

    render(<PeriodicInventoryPage lang="ar" currentUser={mockCurrentUser} />);

    // Table should not have location column
    expect(await screen.findByText('مخزون الوقود الفعلي')).toBeInTheDocument();
    expect(screen.queryByText('اسم الموقع')).not.toBeInTheDocument();

    // Click 'جديد' to open new field inventory modal
    const newBtn = screen.getByRole('button', { name: /جديد/ });
    fireEvent.click(newBtn);

    expect(await screen.findByText('بيانات الجرد الميداني الجديد')).toBeInTheDocument();

    // 'اسم الموقع *' select must NOT exist
    expect(screen.queryByText(/اسم الموقع/)).not.toBeInTheDocument();

    // Tanks and oils must be rendered directly without 'يرجى اختيار الموقع أولاً'
    expect(screen.queryByText(/يرجى اختيار الموقع أولاً/)).not.toBeInTheDocument();
    expect(screen.getByText('Main Tank 1')).toBeInTheDocument();
    expect(screen.getByText('Oil 15W40')).toBeInTheDocument();
  });

  test('enterprise mode groups site assets under station and sends their identifiers', async () => {
    useCommercialMode.mockReturnValue({ isCommercialMode: false, isCommercial: false, modeResolved: true });

    client.get.mockImplementation(async (url) => {
      if (url === '/api/inventory') {
        return { data: { data: [] } };
      }
      if (url === '/api/stations') {
        return { data: [{ id: 10, name: 'المحطة 1' }] };
      }
      if (url === '/api/sub-stations') {
        return { data: [{ id: 100, stationId: 10, name: 'الموقع 1', isActive: true }, { id: 101, stationId: 10, name: 'الموقع 2', isActive: true }] };
      }
      if (url === '/api/fuel-tanks') {
        return { data: [...mockTanks, { id: 2, name: 'Tank 2', fuelLookupId: 10, subStationId: 101 }] };
      }
      if (url === '/api/oils') {
        return { data: mockOils };
      }
      if (url.includes('/api/lookups/by-category/PeriodType')) {
        return { data: mockPeriodTypes };
      }
      return { data: [] };
    });

    client.post.mockResolvedValue({ data: { ids: [1, 2] } });
    render(<PeriodicInventoryPage lang="ar" currentUser={mockCurrentUser} />);

    // Table SHOULD have location column in enterprise mode
    expect(await screen.findByText('مخزون الوقود الفعلي')).toBeInTheDocument();
    expect(screen.queryByText('اسم الموقع')).not.toBeInTheDocument();

    // Click 'جديد' to open new field inventory modal
    const newBtn = screen.getByRole('button', { name: /جديد/ });
    fireEvent.click(newBtn);

    expect(await screen.findByText('بيانات الجرد الميداني الجديد')).toBeInTheDocument();

    // 'اسم الموقع *' select MUST exist in enterprise mode
    const station = screen.getByRole('option', { name: 'المحطة 1' });
    fireEvent.change(station.parentElement, { target: { value: '10' } });
    expect(await screen.findByText('Tank 2')).toBeInTheDocument();
    const fuel = screen.getAllByPlaceholderText('الكمية الفعلية باللتر...');
    fireEvent.change(fuel[0], { target: { value: '10' } });
    fireEvent.change(fuel[1], { target: { value: '20' } });
    fireEvent.change(screen.getByPlaceholderText('L'), { target: { value: '5' } });
    fireEvent.click(screen.getByRole('button', { name: 'إنشاء مسودة الجرد' }));
    await waitFor(() => expect(client.post).toHaveBeenCalledWith('/api/inventory/station', expect.objectContaining({ stationId: 10, fuelReadings: [{ tankId: 1, actualQtyLiters: 10 }, { tankId: 2, actualQtyLiters: 20 }] })));

  });
});
