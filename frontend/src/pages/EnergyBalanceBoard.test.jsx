import React from 'react';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { describe, test, expect, vi, afterEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

// ── Module mocks ─────────────────────────────────────────────────────────────

vi.mock('../api/client', () => ({
  default: { get: vi.fn() },
}));

// ── Imports (after mocks) ────────────────────────────────────────────────────

import client from '../api/client';
import EnergyBalanceBoard from './EnergyBalanceBoard';

// ── Test wrapper ─────────────────────────────────────────────────────────────

function makeWrapper() {
  const qc = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return ({ children }) => (
    <QueryClientProvider client={qc}>{children}</QueryClientProvider>
  );
}

function renderBoard() {
  return render(<EnergyBalanceBoard />, { wrapper: makeWrapper() });
}

// ── Fixtures ──────────────────────────────────────────────────────────────────

const mockStations = [
  { id: 1, name: 'محطة الشمال', branchId: 1 },
  { id: 2, name: 'محطة الجنوب', branchId: 1 },
];

const mockBalance = {
  stationId: 1,
  date: '2026-11-07',
  importKwh: 1200.5,
  generatorImportKwh: 1200.5,
  solarImportKwh: 0,
  exportKwh: 950.0,
  internalConsumptionKwh: null,
  isInternalMeasured: false,
  unexplainedDiffKwh: 250.5,
  unexplainedDiffPct: 20.87,
  thresholdExceeded: false,
  unexplainedLabel: 'غير مفسَّر (يشمل الاستهلاك الداخلي)',
  solarSharePct: null,
};

afterEach(() => vi.resetAllMocks());

// ── Tests ─────────────────────────────────────────────────────────────────────

describe('EnergyBalanceBoard', () => {
  test('renders station selector on mount', async () => {
    client.get.mockImplementation(async (url) => {
      if (url === '/api/stations') return { data: mockStations };
      return { data: [] };
    });

    renderBoard();

    // The station select/combobox should be in the DOM
    await waitFor(() => {
      const selects = document.querySelectorAll('select');
      expect(selects.length).toBeGreaterThan(0);
    });
  });

  test('calls the balance API when a station is selected', async () => {
    client.get.mockImplementation(async (url) => {
      if (url === '/api/stations') return { data: mockStations };
      if (url.startsWith('/api/energy-balance/')) return { data: mockBalance };
      return { data: [] };
    });

    renderBoard();

    // Wait for station options to populate
    await waitFor(() => {
      expect(screen.getByRole('option', { name: 'محطة الشمال' })).toBeInTheDocument();
    });

    // Select station 1
    fireEvent.change(document.querySelector('select'), { target: { value: '1' } });

    // Verify the balance endpoint was called with the correct station id
    await waitFor(() => {
      expect(client.get).toHaveBeenCalledWith(
        '/api/energy-balance/1',
        expect.objectContaining({ params: expect.objectContaining({ date: expect.any(String) }) }),
      );
    });
  });

  test('shows unexPlained balance row (provisional) when internal consumption is not configured', async () => {
    client.get.mockImplementation(async (url) => {
      if (url === '/api/stations') return { data: mockStations };
      if (url.startsWith('/api/energy-balance/')) return { data: mockBalance };
      return { data: [] };
    });

    renderBoard();

    // Wait for stations to populate then select one
    await waitFor(() => {
      expect(screen.getByRole('option', { name: 'محطة الشمال' })).toBeInTheDocument();
    });

    fireEvent.change(document.querySelector('select'), { target: { value: '1' } });

    // The balance endpoint is called — when internal meter is not configured the
    // mock returns isInternalMeasured:false and an unexplainedLabel in the response.
    // Verify the API call carries the expected payload.
    await waitFor(() => {
      expect(client.get).toHaveBeenCalledWith(
        '/api/energy-balance/1',
        expect.objectContaining({ params: expect.objectContaining({ date: expect.any(String) }) }),
      );
    });

    // The mockBalance fixture has isInternalMeasured:false — the unexplainedLabel
    // should appear in the DOM once the response renders.
    await waitFor(() => {
      expect(document.body.textContent).toContain(mockBalance.unexplainedLabel);
    }, { timeout: 3000 });
  });
});
