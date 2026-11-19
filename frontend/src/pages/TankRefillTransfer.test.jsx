import React from 'react';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { describe, test, expect, vi, afterEach } from 'vitest';
import { MemoryRouter } from 'react-router-dom';
import TankRefillTransfer from './TankRefillTransfer';
import i18n from '../i18n';

vi.mock('../api/client', () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
  },
}));

vi.mock('../hooks/useCommercialMode', () => ({
  default: () => ({ isCommercial: true, isCommercialMode: true, modeResolved: true }),
}));

import client from '../api/client';

afterEach(() => vi.resetAllMocks());

describe('TankRefillTransfer with Oil Refill and Log', () => {
  test('renders entry and log tabs and allows switching', async () => {
    client.get.mockImplementation(async (url) => {
      if (url === '/api/fuel-tanks') {
        return { data: [{ id: 1, name: 'Main Tank A', subStationId: 10, tankRole: 1 }] };
      }
      if (url === '/api/oils') {
        return { data: [{ id: 1, oilLookupId: 10, oilLookupName: '15W-40', currentStock: 500 }] };
      }
      if (url === '/api/movement-ledger') {
        return { data: { items: [], totalCount: 0 } };
      }
      return { data: [] };
    });

    render(<TankRefillTransfer />, {
      wrapper: ({ children }) => <MemoryRouter>{children}</MemoryRouter>,
    });

    await waitFor(() => {
      expect(screen.getByText(i18n.t('operational.tanks.refillTitle'))).toBeInTheDocument();
    });

    // Check tabs exist
    expect(screen.getByText(/تسجيل التوريد \/ التحويل/)).toBeInTheDocument();
    expect(screen.getByText(/سجل الحركات والتوريدات العام/)).toBeInTheDocument();

    // Check Oil Receipt radio exists
    const oilRadio = screen.getByLabelText(/توريد زيت خارجي/);
    expect(oilRadio).toBeInTheDocument();

    // Switch to Oil Receipt
    fireEvent.click(oilRadio);
    expect(screen.getByText(/نوع الزيت \/ خزان الزيت/)).toBeInTheDocument();

    // Switch to Log tab
    const logTabBtn = screen.getByText(/سجل الحركات والتوريدات العام/);
    fireEvent.click(logTabBtn);

    // Should request movement ledger and show ledger section
    await waitFor(() => {
      expect(client.get).toHaveBeenCalledWith('/api/movement-ledger', expect.anything());
    });
  });
});
