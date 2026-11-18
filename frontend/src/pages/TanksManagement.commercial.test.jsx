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
