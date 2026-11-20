import React from 'react';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, test, vi } from 'vitest';
import i18n from '../i18n';

vi.mock('../api/client', () => ({ default: { get: vi.fn() } }));
import client from '../api/client';
import OilLedger from './OilLedger';

const days = Array.from({ length: 31 }, (_, index) => ({
  date: `2026-01-${String(index + 1).padStart(2, '0')}`, oilLookupId: 1, oilLookupName: 'SAE 40',
  opening: index, inbound: 0, outbound: 0, transferIn: 0, transferOut: 0, loss: 0, closing: index,
}));

describe('OilLedger', () => {
  beforeEach(async () => {
    await i18n.changeLanguage('en');
    client.get.mockReset();
    client.get.mockImplementation((url) => Promise.resolve({ data: url === '/api/oil-ledger' ? { rows: days } : [{ id: 7, typeName: 'Purchase', quantityLiters: 20 }] }));
  });

  test('renders one row per month day without hierarchy controls', async () => {
    const view = render(<OilLedger lang="en" />);
    await waitFor(() => expect(view.container.querySelectorAll('tbody > tr')).toHaveLength(31));
    expect(screen.queryByText(/station|sub-station|branch/i)).not.toBeInTheDocument();
  });

  test('expands a day to show its transactions', async () => {
    render(<OilLedger lang="en" />);
    const expand = await screen.findAllByRole('button', { name: 'Show day transactions' });
    fireEvent.click(expand[0]);
    await screen.findByText(/Purchase/);
    expect(client.get).toHaveBeenCalledWith('/api/oil-ledger/day', expect.objectContaining({ params: expect.objectContaining({ date: '2026-01-01' }) }));
  });
});
