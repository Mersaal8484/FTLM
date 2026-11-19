import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { expect, test, vi } from 'vitest';
vi.mock('../api/client', () => ({ default: { get: vi.fn(), put: vi.fn(), post: vi.fn() } }));
vi.mock('../hooks/useCommercialMode', () => ({ default: () => ({ isCommercialMode: false, modeResolved: true }) }));
vi.mock('./TankSetup', () => ({ default: () => null }));
import client from '../api/client';
import TanksManagement from './TanksManagement';

test('groups legacy tanks by station and retains the original link and zero stock on edit', async () => {
  client.get.mockImplementation(async url => {
    if (url === '/api/fuel-tanks') return { data: [{ id: 1, name: 'Tank A', code: 'A', subStationId: 1, capacityLiters: 100, fuelLookupId: 3, actualStockLiters: 0 }, { id: 2, name: 'Tank B', code: 'B', subStationId: 2, capacityLiters: 100, fuelLookupId: 3 }] };
    if (url === '/api/sub-stations') return { data: [{ id: 1, stationId: 5, name: 'Old site 1' }, { id: 2, stationId: 5, name: 'Old site 2' }] };
    if (url === '/api/stations') return { data: [{ id: 5, branchId: 7, name: 'Station A' }] };
    if (url === '/api/branches') return { data: [{ id: 7, name: 'Branch A' }] };
    if (url.includes('FuelType')) return { data: [{ id: 3, name: 'Diesel' }] };
    return { data: [] };
  });
  client.put.mockResolvedValue({ data: {} });
  render(<MemoryRouter><TanksManagement lang="en" currentUser={{ role: 'admin' }} /></MemoryRouter>);
  expect(await screen.findByText('Tank A')).toBeInTheDocument();
  expect(screen.getByText('Tank B')).toBeInTheDocument();
  expect(screen.getAllByText('Station A')).toHaveLength(1);
  expect(screen.queryByText('Old site 1')).not.toBeInTheDocument();
  const card = screen.getByText('Tank A').closest('.glass-card');
  fireEvent.click(card.querySelector('button'));
  fireEvent.click(screen.getByRole('button', { name: /Save Tank/i }));
  await waitFor(() => expect(client.put).toHaveBeenCalledWith('/api/fuel-tanks/1', expect.objectContaining({ subStationId: 1, actualStockLiters: 0 })));
});
