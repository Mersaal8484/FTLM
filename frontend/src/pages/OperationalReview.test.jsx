import React from 'react';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { describe, test, expect, vi, beforeEach, afterEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';

// ── Module mocks ─────────────────────────────────────────────────────────────

vi.mock('../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

// ── Imports (after mocks) ────────────────────────────────────────────────────

import client from '../api/client';
import OperationalReview from './OperationalReview';

// ── Test wrapper ─────────────────────────────────────────────────────────────

function makeWrapper() {
  const qc = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return ({ children }) => (
    <MemoryRouter>
      <QueryClientProvider client={qc}>{children}</QueryClientProvider>
    </MemoryRouter>
  );
}

function renderReview() {
  return render(<OperationalReview />, { wrapper: makeWrapper() });
}

// ── Fixtures ──────────────────────────────────────────────────────────────────

const pendingSubmission = {
  id: 1,
  subStationId: 10,
  siteName: 'المحطة الرئيسية',
  receivedAtUtc: '2026-11-07T20:00:00Z',
  submittedByUserId: 3,
  submitterName: 'أحمد علي',
  generatorLineCount: 2,
  tankLineCount: 1,
  feederLineCount: 0,
  meterLineCount: 1,
  hasSkewWarning: false,
};

afterEach(() => vi.resetAllMocks());

// ── Tests ─────────────────────────────────────────────────────────────────────

describe('OperationalReview — queue view', () => {
  test('renders "no pending submissions" when API returns empty list', async () => {
    client.get.mockResolvedValueOnce({ data: [] });
    renderReview();

    // The AR i18n key operational.review.empty renders "لا توجد إرسالات معلّقة."
    await waitFor(() => {
      expect(screen.getByText(/لا توجد إرسالات/i)).toBeInTheDocument();
    });
  });

  test('renders submission row when API returns one pending submission', async () => {
    client.get.mockResolvedValueOnce({ data: [pendingSubmission] });
    renderReview();

    // Site name is in a <strong> — exact match works
    await waitFor(() => {
      expect(screen.getByText('المحطة الرئيسية')).toBeInTheDocument();
    });
    // Submitter name is split across text nodes ("مرسل بواسطة: أحمد علي"),
    // so check the article's full text content instead
    expect(screen.getByRole('article')).toHaveTextContent('أحمد علي');
  });

  test('calls GET /api/operational-review on mount', async () => {
    client.get.mockResolvedValueOnce({ data: [] });
    renderReview();

    await waitFor(() => {
      expect(client.get).toHaveBeenCalledWith('/api/operational-review');
    });
  });
});

describe('OperationalReview — detail / approve flow', () => {
  const detailResponse = {
    id: 1,
    subStationId: 10,
    siteName: 'المحطة الرئيسية',
    receivedAtUtc: '2026-11-07T20:00:00Z',
    submittedByUserId: 3,
    submitterName: 'أحمد علي',
    status: 'Pending',
    generatorLines: [],
    tankLines: [],
    feederLines: [],
    meterLines: [],
  };

  test('calls approve endpoint and invalidates queue on approve click', async () => {
    // Auto-accept the window.confirm dialog
    vi.stubGlobal('confirm', vi.fn(() => true));

    // First GET → queue list; second GET → submission detail
    client.get
      .mockResolvedValueOnce({ data: [pendingSubmission] })
      .mockResolvedValueOnce({ data: detailResponse });

    client.post.mockResolvedValueOnce({ data: { message: 'Submission approved.' } });

    renderReview();

    // Click the queue row to open detail view
    const siteName = await screen.findByText('المحطة الرئيسية');
    fireEvent.click(siteName);

    // Wait for approve button in detail view
    const approveBtn = await screen.findByRole('button', {
      name: /approve|اعتماد/i,
    });
    fireEvent.click(approveBtn);

    await waitFor(() => {
      expect(client.post).toHaveBeenCalledWith('/api/operational-review/1/approve');
    });

    vi.unstubAllGlobals();
  });
});
