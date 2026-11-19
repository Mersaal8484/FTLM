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
import WorkPermits from './WorkPermits';

afterEach(() => vi.resetAllMocks());

const mockCurrentUser = {
  role: 'admin',
  permissions: {
    maintenance: {
      read: true,
      create: true,
      add: true,
      edit: true,
      delete: true,
    },
  },
};

describe('WorkPermits Commercial Mode', () => {
  test('does not request /api/sub-stations and hides site select in commercial mode', async () => {
    useCommercialMode.mockReturnValue({ isCommercialMode: true, modeResolved: true });

    client.get.mockImplementation(async (url) => {
      if (url.includes('/api/sub-stations')) {
        throw new Error('Disabled endpoint in commercial mode');
      }
      if (url === '/api/work-permits') {
        return { data: { items: [], totalCount: 0 } };
      }
      if (url.includes('/api/generators')) {
        return { data: [{ id: 101, name: 'مولد 1' }] };
      }
      if (url.includes('/api/lookups/by-category/')) {
        return { data: [{ id: 1, name: 'اختبار' }] };
      }
      return { data: [] };
    });

    render(<WorkPermits lang="ar" currentUser={mockCurrentUser} />);

    // Wait for initial load
    expect(await screen.findByText('لا توجد بيانات')).toBeInTheDocument();
    expect(client.get).not.toHaveBeenCalledWith(expect.stringContaining('/api/sub-stations'));

    // Open add modal
    const addBtn = screen.getByRole('button', { name: /تصريح جديد/ });
    fireEvent.click(addBtn);

    // Site select should not be in the document
    expect(screen.queryByText('الموقع')).not.toBeInTheDocument();
    expect(screen.getAllByText('المولد').length).toBeGreaterThanOrEqual(1);
  });

  test('groups legacy sites under one station in enterprise mode', async () => {
    useCommercialMode.mockReturnValue({ isCommercialMode: false, modeResolved: true });

    client.get.mockImplementation(async (url) => {
      if (url.includes('/api/sub-stations')) {
        return { data: [{ id: 1, stationId: 3, stationName: 'محطة صنعاء', name: 'موقع أ' }, { id: 2, stationId: 3, stationName: 'محطة صنعاء', name: 'موقع ب' }] };
      }
      if (url === '/api/work-permits') {
        return { data: { items: [], totalCount: 0 } };
      }
      if (url.includes('/api/generators')) {
        return { data: [{ id: 101, name: 'مولد 1', subStationId: 2 }] };
      }
      if (url.includes('/api/lookups/by-category/')) {
        return { data: [{ id: 1, name: 'اختبار' }] };
      }
      return { data: [] };
    });

    client.post.mockResolvedValue({ data: {} });
    render(<WorkPermits lang="ar" currentUser={mockCurrentUser} />);

    expect(await screen.findByText('لا توجد بيانات')).toBeInTheDocument();
    await waitFor(() => expect(client.get).toHaveBeenCalledWith('/api/sub-stations', expect.anything()));

    // Open add modal
    const addBtn = screen.getByRole('button', { name: /تصريح جديد/ });
    fireEvent.click(addBtn);

    expect(screen.queryByText('الموقع')).not.toBeInTheDocument();
    expect(screen.getAllByText('محطة صنعاء')).toHaveLength(1);
    expect(screen.queryByText('موقع أ')).not.toBeInTheDocument();
    expect(screen.queryByText('موقع ب')).not.toBeInTheDocument();
    const stationSelect = screen.getByRole('option', { name: 'محطة صنعاء' }).parentElement;
    fireEvent.change(stationSelect, { target: { value: '3' } });
    const generatorOption = await screen.findByRole('option', { name: 'مولد 1' });
    fireEvent.change(generatorOption.parentElement, { target: { value: '101' } });
    fireEvent.click(screen.getByRole('button', { name: 'حفظ' }));
    await waitFor(() => expect(client.post).toHaveBeenCalledWith('/api/work-permits', expect.objectContaining({ generatorId: 101, subStationId: 2 })));

  });
});
