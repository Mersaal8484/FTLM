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

  test('requests /api/sub-stations and renders site select in enterprise mode', async () => {
    useCommercialMode.mockReturnValue({ isCommercialMode: false, modeResolved: true });

    client.get.mockImplementation(async (url) => {
      if (url.includes('/api/sub-stations')) {
        return { data: [{ id: 1, name: 'الموقع الرئيسي' }] };
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

    expect(await screen.findByText('لا توجد بيانات')).toBeInTheDocument();
    await waitFor(() => expect(client.get).toHaveBeenCalledWith('/api/sub-stations', expect.anything()));

    // Open add modal
    const addBtn = screen.getByRole('button', { name: /تصريح جديد/ });
    fireEvent.click(addBtn);

    // Site select should be in the document
    expect(screen.getByText('الموقع')).toBeInTheDocument();
    expect(screen.getByText('الموقع الرئيسي')).toBeInTheDocument();
  });
});
