import React from 'react';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, expect, test, vi } from 'vitest';

vi.mock('../api/client', () => ({ default: { get: vi.fn() } }));
vi.mock('../hooks/useCommercialMode', () => ({ default: vi.fn() }));
vi.mock('./TankSetup', () => ({ default: () => null }));

import client from '../api/client';
import useCommercialMode from '../hooks/useCommercialMode';
import TanksManagement from './TanksManagement';

afterEach(() => vi.resetAllMocks());

test('waits for mode and loads commercial tanks without hierarchy requests', async () => {
  useCommercialMode.mockReturnValue({ isCommercialMode: false, modeResolved: false });
  client.get.mockResolvedValue({ data: [] });
  const view = render(<MemoryRouter><TanksManagement lang="ar" currentUser={{ role: 'admin' }} /></MemoryRouter>);
  expect(client.get).not.toHaveBeenCalled();
  useCommercialMode.mockReturnValue({ isCommercialMode: true, modeResolved: true });
  view.rerender(<MemoryRouter><TanksManagement lang="ar" currentUser={{ role: 'admin' }} /></MemoryRouter>);
  await screen.findByText(/لا توجد خزانات/);
  expect(client.get).not.toHaveBeenCalledWith('/api/stations', expect.anything());
  expect(client.get).not.toHaveBeenCalledWith('/api/sub-stations', expect.anything());
  expect(screen.queryByText('فشل تحميل البيانات.')).not.toBeInTheDocument();
});

test('shows only main tanks in commercial mode', async () => {
  useCommercialMode.mockReturnValue({ isCommercialMode: true, modeResolved: true });
  client.get.mockImplementation(async url => {
    if (url === '/api/fuel-tanks') return { data: [
      { id: 1, name: 'Main Tank', code: 'MT-01', tankRole: 1, capacityLiters: 5000 },
      { id: 2, name: 'Daily Tank', code: 'DT-01', tankRole: 2, generatorId: 10, capacityLiters: 1000 },
    ] };
    if (url.includes('FuelType')) return { data: [{ id: 1, name: 'Diesel' }] };
    return { data: [] };
  });

  render(<MemoryRouter><TanksManagement lang="en" currentUser={{ role: 'admin' }} /></MemoryRouter>);

  expect(await screen.findByText('Main Tank')).toBeInTheDocument();
  expect(screen.queryByText('Daily Tank')).not.toBeInTheDocument();
});
