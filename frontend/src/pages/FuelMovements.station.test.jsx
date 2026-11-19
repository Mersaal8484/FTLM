import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { test, expect, vi } from 'vitest';
vi.mock('../hooks/useCommercialMode', () => ({ default: () => ({ isCommercialMode: false, modeResolved: true }) }));
vi.mock('../api/client', () => ({ default: { get: vi.fn(), post: vi.fn(), put: vi.fn() } }));
vi.mock('./movements/GeneralMovementLog', () => ({ default: () => null }));
import client from '../api/client';
import FuelMovements from './FuelMovements';

test('station selection includes both sites and saves the selected tank site', async () => {
  client.get.mockImplementation(async url => {
    if (url === '/api/sub-stations') return { data: [{ id: 1, stationId: 5, stationName: 'A', name: 'Site 1' }, { id: 2, stationId: 5, stationName: 'A', name: 'Site 2' }] };
    if (url === '/api/lookups/by-category/TransactionType') return { data: [{ id: 200, code: 'Purchase' }] };
    if (url === '/api/stations') return { data: [{ id: 5, name: 'A' }] };
    if (url === '/api/fuel-tanks') return { data: [{ id: 11, name: 'Tank 1', subStationId: 1, fuelLookupId: 3 }, { id: 12, name: 'Tank 2', subStationId: 2, fuelLookupId: 3 }] };
    return { data: url === '/api/fuel-movements' ? { items: [] } : [] };
  });
  client.post.mockResolvedValue({ data: {} });
  render(<FuelMovements lang="en" currentUser={{ role: 'admin' }} />);
  fireEvent.click(await screen.findByRole('button', { name: /Supply/i }));
  fireEvent.click(await screen.findByRole('button', { name: /New Movement/i }));
  const station = await screen.findByRole('option', { name: 'A' });
  fireEvent.change(station.parentElement, { target: { value: '5' } });
  expect(screen.getByRole('option', { name: /Tank 1/ })).toBeInTheDocument();
  const tank = screen.getByRole('option', { name: /Tank 2/ });
  fireEvent.change(tank.parentElement, { target: { value: '12' } });
  fireEvent.change(screen.getByLabelText(/Qty \(L\)/i), { target: { value: '10' } });
  fireEvent.click(screen.getByRole('button', { name: /Save & Submit/i }));
  await waitFor(() => expect(client.post).toHaveBeenCalledWith('/api/fuel-movements', expect.objectContaining({ subStationId: 2, transactionTypeLookupId: 200, quantityLiters: 10 })));
});
