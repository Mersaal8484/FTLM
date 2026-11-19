import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { expect, test, vi } from 'vitest';
vi.mock('../hooks/useCommercialMode', () => ({ default: () => ({ isCommercialMode: false, modeResolved: true }) }));
vi.mock('../api/client', () => ({ default: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() } }));
import client from '../api/client';
import Generators from './Generators';

test('shows generators from both legacy sites directly under their station', async () => {
  client.get.mockImplementation(async url => {
    if (url === '/api/generators') return { data: [{ id: 1, name: 'Generator A', subStationId: 10 }, { id: 2, name: 'Generator B', subStationId: 20 }] };
    if (url === '/api/sub-stations') return { data: [{ id: 10, stationId: 5, stationName: 'Station A', name: 'Old site 1' }, { id: 20, stationId: 5, stationName: 'Station A', name: 'Old site 2' }] };
    if (url === '/api/stations') return { data: [{ id: 5, branchId: 1, name: 'Station A' }] };
    return { data: [] };
  });
  render(<Generators lang="en" t={{ generators: 'Generators', manageGenerators: 'Manage', noData: 'No data' }} currentUser={{ role: 'admin' }} />);
  fireEvent.click(await screen.findByText('Station A'));
  await waitFor(() => expect(screen.getByText('Generator A')).toBeInTheDocument());
  expect(screen.getByText('Generator B')).toBeInTheDocument();
  expect(screen.queryByText('Old site 1')).not.toBeInTheDocument();
  expect(screen.queryByText('Old site 2')).not.toBeInTheDocument();
});
