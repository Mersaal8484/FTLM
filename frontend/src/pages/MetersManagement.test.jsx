import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, test, vi } from 'vitest';

vi.mock('../api/client', () => ({ default: { get: vi.fn() } }));
vi.mock('../hooks/useCommercialMode', () => ({ default: vi.fn() }));

import client from '../api/client';
import useCommercialMode from '../hooks/useCommercialMode';
import MetersManagement from './MetersManagement';

afterEach(() => vi.resetAllMocks());

describe('MetersManagement loading', () => {
  test('loads commercial meters without requesting disabled hierarchy endpoints', async () => {
    useCommercialMode.mockReturnValue({ isCommercialMode: true, modeResolved: true });
    client.get.mockImplementation(async url => {
      if (['/api/branches', '/api/stations', '/api/sub-stations'].includes(url)) throw new Error('Disabled endpoint');
      return { data: url === '/api/generators' ? { data: [] } : [] };
    });
    render(<MetersManagement lang="ar" />);
    expect(await screen.findByText('لا توجد عدادات')).toBeInTheDocument();
    expect(client.get).not.toHaveBeenCalledWith('/api/branches');
    expect(client.get).not.toHaveBeenCalledWith('/api/stations');
    expect(client.get).not.toHaveBeenCalledWith('/api/sub-stations');
    expect(screen.queryByText(/فشل تحميل البيانات/)).not.toBeInTheDocument();
  });

  test('loads hierarchy in enterprise mode', async () => {
    useCommercialMode.mockReturnValue({ isCommercialMode: false, modeResolved: true });
    client.get.mockResolvedValue({ data: [] });
    render(<MetersManagement lang="ar" />);
    await waitFor(() => expect(client.get).toHaveBeenCalledWith('/api/sub-stations'));
    expect(client.get).toHaveBeenCalledWith('/api/branches');
    expect(client.get).toHaveBeenCalledWith('/api/stations');
    expect(await screen.findByText('لا توجد عدادات')).toBeInTheDocument();
  });

  test('waits until mode has resolved before fetching', () => {
    useCommercialMode.mockReturnValue({ isCommercialMode: false, modeResolved: false });
    render(<MetersManagement lang="ar" />);
    expect(client.get).not.toHaveBeenCalled();
  });
});
