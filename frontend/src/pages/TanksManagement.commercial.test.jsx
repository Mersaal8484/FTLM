import React from 'react';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, expect, test, vi } from 'vitest';

vi.mock('../api/client', () => ({ default: { get: vi.fn(), post: vi.fn(), put: vi.fn() } }));
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

test('commercial mode hides daily tanks, retains null-role tanks, and excludes hidden tanks from KPIs', async () => {
  useCommercialMode.mockReturnValue({ isCommercialMode: true, modeResolved: true });
  client.get.mockImplementation(async url => {
    if (url === '/api/fuel-tanks') return { data: [
      { id: 1, name: 'Main Tank', code: 'MT-01', tankRole: 1, capacityLiters: 100, actualStockLiters: 40 },
      { id: 2, name: 'Daily Tank', code: 'DT-01', tankRole: 2, generatorId: 10, capacityLiters: 1000 },
      { id: 3, name: 'Generator-linked legacy tank', code: 'DT-02', generatorId: 11, capacityLiters: 3000 },
      { id: 4, name: 'Legacy Main Tank', code: 'MT-LEGACY', capacityLiters: 200, actualStockLiters: 60 },
    ] };
    if (url.includes('FuelType')) return { data: [{ id: 1, name: 'Diesel' }] };
    return { data: [] };
  });

  render(<MemoryRouter><TanksManagement lang="en" currentUser={{ role: 'admin' }} /></MemoryRouter>);

  expect(await screen.findByText('Main Tank')).toBeInTheDocument();
  expect(screen.getByText('Legacy Main Tank')).toBeInTheDocument();
  expect(screen.queryByText('Daily Tank')).not.toBeInTheDocument();
  expect(screen.queryByText('Generator-linked legacy tank')).not.toBeInTheDocument();
  expect(screen.getByText('300')).toBeInTheDocument();
});

test('commercial create sends main role and shape without subStationId and surfaces the server error', async () => {
  useCommercialMode.mockReturnValue({ isCommercialMode: true, modeResolved: true });
  client.get.mockImplementation(async url => {
    if (url === '/api/fuel-tanks') return { data: [] };
    if (url.includes('FuelType')) return { data: [{ id: 1, name: 'Diesel' }] };
    return { data: [] };
  });
  client.post.mockRejectedValue({ response: { data: { message: 'A main tank for this fuel type already exists.' } } });

  render(<MemoryRouter><TanksManagement lang="en" currentUser={{ role: 'admin' }} /></MemoryRouter>);
  fireEvent.click(await screen.findByRole('button', { name: /New Tank/i }));
  fireEvent.change(screen.getByPlaceholderText('TK-01'), { target: { value: 'MT-01' } });
  fireEvent.change(screen.getByPlaceholderText('e.g. Tank A1'), { target: { value: 'Main Tank' } });
  fireEvent.change(screen.getByLabelText('Fuel Type'), { target: { value: '1' } });
  fireEvent.change(screen.getAllByPlaceholderText('0')[0], { target: { value: '5000' } });
  fireEvent.click(screen.getByRole('button', { name: /Save Tank/i }));

  await waitFor(() => expect(client.post).toHaveBeenCalledTimes(1));
  const [, payload] = client.post.mock.calls[0];
  expect(payload).toMatchObject({ tankRole: 1, shape: 0, fuelLookupId: 1 });
  expect(payload).not.toHaveProperty('subStationId');
  expect(await screen.findByText('A main tank for this fuel type already exists.')).toBeInTheDocument();
});

test('enterprise mode still renders daily and main tanks', async () => {
  useCommercialMode.mockReturnValue({ isCommercialMode: false, modeResolved: true });
  client.get.mockImplementation(async url => {
    if (url === '/api/fuel-tanks') return { data: [
      { id: 1, name: 'Enterprise Main', code: 'MT-01', subStationId: 1, capacityLiters: 100 },
      { id: 2, name: 'Enterprise Daily', code: 'DT-01', subStationId: 1, tankRole: 2, generatorId: 10, capacityLiters: 100 },
    ] };
    if (url === '/api/sub-stations') return { data: [{ id: 1, stationId: 1, name: 'Site' }] };
    if (url === '/api/stations') return { data: [{ id: 1, branchId: 1, name: 'Station' }] };
    if (url === '/api/branches') return { data: [{ id: 1, name: 'Branch' }] };
    if (url.includes('FuelType')) return { data: [] };
    return { data: [] };
  });

  render(<MemoryRouter><TanksManagement lang="en" currentUser={{ role: 'admin' }} /></MemoryRouter>);

  expect(await screen.findByText('Enterprise Main')).toBeInTheDocument();
  expect(screen.getByText('Enterprise Daily')).toBeInTheDocument();
});
