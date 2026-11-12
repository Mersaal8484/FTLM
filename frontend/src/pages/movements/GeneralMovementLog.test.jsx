import React from 'react';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import i18n from '../../i18n';
import client from '../../api/client';
import GeneralMovementLog from './GeneralMovementLog';

vi.mock('../../api/client', () => ({ default: { get: vi.fn(), post: vi.fn() } }));
vi.mock('../reports/utils/reportExport', () => ({ exportToExcel: vi.fn() }));

const empty = { items: [], totalCount: 0, page: 1, pageSize: 20 };
const rows = Array.from({ length: 21 }, (_, index) => ({
  id: index + 1, kind: 'fuel', date: '2026-11-12T10:00:00Z', stationId: 1,
  station: 'Station A', subStationId: 10, subStation: 'Site A', movementType: 'Supply',
  quantity: 100, unit: 'L', reference: 'REF', status: 'pending', createdBy: 'Operator', approvedBy: null,
}));

function mockLookupsAndLedger(ledger = empty) {
  client.get.mockImplementation((url) => {
    if (url === '/api/movement-ledger') return Promise.resolve({ data: ledger });
    return Promise.resolve({ data: [] });
  });
}

describe('GeneralMovementLog', () => {
  beforeEach(async () => { client.get.mockReset(); client.post.mockReset(); await i18n.changeLanguage('en'); });

  it('shows a loading skeleton instead of a blank body', () => {
    client.get.mockImplementation((url) => url === '/api/movement-ledger' ? new Promise(() => {}) : Promise.resolve({ data: [] }));
    render(<GeneralMovementLog lang="en" />);
    expect(screen.getByRole('status', { name: 'Loading movements' })).toBeInTheDocument();
  });

  it('shows an empty state', async () => {
    mockLookupsAndLedger();
    render(<GeneralMovementLog lang="en" />);
    expect(await screen.findByText('No movements found')).toBeInTheDocument();
  });

  it('shows an error state with retry', async () => {
    client.get.mockImplementation((url) => url === '/api/movement-ledger' ? Promise.reject(new Error('offline')) : Promise.resolve({ data: [] }));
    render(<GeneralMovementLog lang="en" />);
    expect(await screen.findByRole('alert')).toHaveTextContent('Movement log could not be loaded.');
    expect(screen.getAllByRole('button', { name: 'Retry' }).length).toBeGreaterThan(0);
  });

  it('sends the selected filters to the server', async () => {
    mockLookupsAndLedger();
    render(<GeneralMovementLog lang="en" />);
    await screen.findByText('No movements found');
    fireEvent.change(screen.getByLabelText('Search'), { target: { value: 'invoice-7' } });
    fireEvent.change(screen.getByLabelText('Item kind'), { target: { value: 'oil' } });
    fireEvent.click(screen.getByRole('button', { name: 'Apply filters' }));
    await waitFor(() => expect(client.get).toHaveBeenCalledWith('/api/movement-ledger', expect.objectContaining({ params: expect.objectContaining({ search: 'invoice-7', kind: 'oil', page: 1 }) })));
  });

  it('renders pagination for server-paged results', async () => {
    mockLookupsAndLedger({ items: rows.slice(0, 20), totalCount: 21 });
    render(<GeneralMovementLog lang="en" />);
    expect(await screen.findByText(/Page 1 of 2/)).toBeInTheDocument();
  });

  it('renders Arabic and English labels from i18n', async () => {
    mockLookupsAndLedger();
    const view = render(<GeneralMovementLog lang="en" />);
    expect(await screen.findByText('General movement log')).toBeInTheDocument();
    await i18n.changeLanguage('ar');
    view.rerender(<GeneralMovementLog lang="ar" />);
    expect(await screen.findByText('السجل العام للحركات')).toBeInTheDocument();
  });
});
