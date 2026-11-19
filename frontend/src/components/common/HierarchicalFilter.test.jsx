import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, test, vi } from 'vitest';

vi.mock('../../api/client', () => ({ default: { get: vi.fn() } }));
vi.mock('../../hooks/useCommercialMode', () => ({ default: () => ({ isCommercialMode: false, modeResolved: true }) }));
import client from '../../api/client';
import HierarchicalFilter from './HierarchicalFilter';

describe('station report filters', () => {
  beforeEach(() => {
    client.get.mockReset();
    client.get.mockImplementation(url => Promise.resolve({ data: url === '/api/branches' ? [{ id: 1, name: 'صنعاء' }] : [{ id: 2, name: 'محطة صنعاء' }] }));
  });
  test('filters by station without asking for a substation', async () => {
    const onChange = vi.fn();
    render(<HierarchicalFilter lang="ar" onFilterChange={onChange} showDateRange={false} />);
    const branch = await screen.findByRole('option', { name: 'صنعاء' });
    fireEvent.change(branch.parentElement, { target: { value: '1' } });
    const station = await screen.findByRole('option', { name: 'محطة صنعاء' });
    fireEvent.change(station.parentElement, { target: { value: '2' } });
    await waitFor(() => expect(onChange).toHaveBeenLastCalledWith(expect.objectContaining({ stationId: 2, subStationId: null })));
    expect(screen.queryByText('المحطة الفرعية')).not.toBeInTheDocument();
    expect(client.get.mock.calls.some(([url]) => url.includes('/api/sub-stations'))).toBe(false);
  });
});
