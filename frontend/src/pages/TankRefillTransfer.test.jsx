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
      expect(screen.getByText(i18n.t('operational.tanks.commercialRefillTitle'))).toBeInTheDocument();
    });

    // Check tabs exist — getAllByText since tab label text also appears inside the form body
    expect(screen.getAllByText(/تسجيل التوريد \/ التحويل/).length).toBeGreaterThanOrEqual(1);
    expect(screen.getByText(/سجل الحركات والتوريدات العام/)).toBeInTheDocument();

    // Commercial mode chooses the item first, then renders the oil-supply form.
    const oilRadio = screen.getByLabelText('زيت');
    expect(oilRadio).toBeInTheDocument();

    // Switch to oil supply
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

  test('submits an oil supply through the oil transaction endpoint with attachment ids only on the oil payload', async () => {
    client.get.mockImplementation(async (url) => {
      if (url === '/api/fuel-tanks') return { data: [] };
      if (url === '/api/oils') return { data: [{ id: 1, oilLookupId: 10, oilLookupName: '15W-40' }] };
      return { data: [] };
    });
    client.post.mockResolvedValue({ data: { id: 1 } });

    render(<TankRefillTransfer />, { wrapper: ({ children }) => <MemoryRouter>{children}</MemoryRouter> });
    await screen.findByText(i18n.t('operational.tanks.commercialRefillTitle'));
    fireEvent.click(screen.getByLabelText('زيت'));
    fireEvent.change(screen.getByRole('combobox'), { target: { value: '10' } });
    fireEvent.change(screen.getByRole('spinbutton'), { target: { value: '42' } });
    fireEvent.click(screen.getByRole('button', { name: /تسجيل وارد الزيت/ }));

    await waitFor(() => expect(client.post).toHaveBeenCalledWith('/api/oil-transactions', expect.objectContaining({
      oilLookupId: 10,
      quantityLiters: 42,
      attachmentIds: [],
    })));
    const [, payload] = client.post.mock.calls.find(([url]) => url === '/api/oil-transactions');
    expect(payload).not.toHaveProperty('subStationId');
  });
});
