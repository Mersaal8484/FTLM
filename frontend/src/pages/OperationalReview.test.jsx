import React from 'react';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { describe, test, expect, vi, beforeEach, afterEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';

// ── Module mocks ─────────────────────────────────────────────────────────────

vi.mock('../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));
vi.mock('../hooks/useCommercialMode', () => ({ default: vi.fn() }));

// ── Imports (after mocks) ────────────────────────────────────────────────────

import client from '../api/client';
import useCommercialMode from '../hooks/useCommercialMode';
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
beforeEach(() => {
  useCommercialMode.mockReturnValue({ isCommercialMode: true, isCommercial: true, modeResolved: true });
});

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

  test('approves one generator bundle without approving the whole submission', async () => {
    // Auto-accept the window.confirm dialog
    vi.stubGlobal('confirm', vi.fn(() => true));

    const generatorBundleDetail = {
      ...detailResponse,
      generatorLines: [{
        id: 81, status: 'Pending', capturedAtUtc: '2026-11-07T20:00:00Z', receivedAtUtc: '2026-11-07T20:01:00Z',
        hasSkewWarning: false, photoAttachmentIds: [], warningsJson: null, rejectionReason: null, hasReuseWarning: false,
        values: { generatorId: 44, generatorName: 'Generator bundle', isRunning: true },
      }],
    };

    // First GET → queue list; second GET → submission detail
    client.get
      .mockResolvedValueOnce({ data: [pendingSubmission] })
      .mockResolvedValueOnce({ data: generatorBundleDetail });

    client.post.mockResolvedValueOnce({ data: { message: 'Review bundle approved.' } });

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
      expect(client.post).toHaveBeenCalledWith('/api/operational-review/1/bundles/generator/44/approve');
    });

    vi.unstubAllGlobals();
  });

  test('renders daily tank reading level in cm alongside stored computed litres', async () => {
    const tankDetail = {
      ...detailResponse,
      tankLines: [
        {
          id: 77,
          lineType: 'tank',
          status: 'Pending',
          capturedAtUtc: '2026-11-07T20:00:00Z',
          receivedAtUtc: '2026-11-07T20:01:00Z',
          hasSkewWarning: false,
          photoAttachmentIds: [],
          warningsJson: null,
          rejectionReason: null,
          hasReuseWarning: false,
          values: {
            fuelTankId: 31,
            fuelTankName: 'Daily Tank 1',
            liquidLevelCm: 42,
            computedLitres: 420,
            eventType: 0,
          },
        },
      ],
    };

    client.get
      .mockResolvedValueOnce({ data: [pendingSubmission] })
      .mockResolvedValueOnce({ data: tankDetail });

    renderReview();
    fireEvent.click(await screen.findByText('المحطة الرئيسية'));

    expect((await screen.findAllByText(/Daily Tank 1/)).length).toBeGreaterThan(0);
    expect(screen.getByText(/42 cm/)).toBeInTheDocument();
    expect(screen.getByText(/420 L/)).toBeInTheDocument();
  });

  test('shows submitted current meter values even when no previous official value exists', async () => {
    const reviewDetail = {
      ...detailResponse,
      generatorLines: [{
        id: 41, status: 'Pending', capturedAtUtc: '2026-11-07T20:00:00Z', receivedAtUtc: '2026-11-07T20:01:00Z',
        hasSkewWarning: false, photoAttachmentIds: [], warningsJson: null, rejectionReason: null, hasReuseWarning: false,
        values: { generatorName: 'Generator A', isRunning: true, lineVoltageVolts: 400, currentAmps: 120, oilPressure: 5, oilTemperatureCelsius: 82, waterTemperatureCelsius: 76, activeLoadKw: 90 },
      }],
      meterLines: [{
        id: 42, status: 'Pending', subjectType: 'GeneratorEnergyProduced', subjectId: 1, kind: 'DailyClosing',
        capturedAtUtc: '2026-11-07T20:00:00Z', receivedAtUtc: '2026-11-07T20:01:00Z', businessDate: '2026-11-07',
        hasSkewWarning: false, photoAttachmentIds: [], rawValue: 1250, multiplierSnapshot: 1,
        isMeterReset: false, resetNote: null, warningsJson: null, rejectionReason: null,
        previousApprovedValue: null, deltaPreview: null, deltaSpanDays: null, subjectName: 'Generator A',
      }],
    };

    client.get
      .mockResolvedValueOnce({ data: [pendingSubmission] })
      .mockResolvedValueOnce({ data: reviewDetail });

    renderReview();
    fireEvent.click(await screen.findByText('المحطة الرئيسية'));

    expect(await screen.findByText('Generator A')).toBeInTheDocument();
    expect(screen.getByText('1250')).toBeInTheDocument();
    expect(screen.getByText('400')).toBeInTheDocument();
    expect(screen.getByText('120')).toBeInTheDocument();
  });

  test('groups a feeder load, its counter, and their shared photo into one review card', async () => {
    const feederDetail = {
      ...detailResponse,
      feederLines: [{
        id: 61, status: 'Recorded', capturedAtUtc: '2026-11-07T20:00:00Z', receivedAtUtc: '2026-11-07T20:01:00Z',
        hasSkewWarning: false, photoAttachmentIds: [301], warningsJson: null, rejectionReason: null, hasReuseWarning: false,
        values: { feederId: 91, feederName: 'Feeder A', activeLoadKw: null, apparentLoadKva: null },
      }],
      meterLines: [{
        id: 62, status: 'Pending', subjectType: 2, subjectId: 91, kind: 1,
        capturedAtUtc: '2026-11-07T20:00:00Z', receivedAtUtc: '2026-11-07T20:01:00Z', businessDate: '2026-11-07',
        hasSkewWarning: false, photoAttachmentIds: [301], rawValue: 10600, multiplierSnapshot: 1,
        isMeterReset: false, resetNote: null, warningsJson: null, rejectionReason: null,
        previousApprovedValue: 8000, deltaPreview: 2600, deltaSpanDays: 1, subjectName: 'Feeder A',
      }],
    };

    client.get
      .mockResolvedValueOnce({ data: [pendingSubmission] })
      .mockResolvedValueOnce({ data: feederDetail });

    const view = renderReview();
    fireEvent.click(await screen.findByText('المحطة الرئيسية'));

    expect(await screen.findByText('Feeder A')).toBeInTheDocument();
    expect(screen.getByText('10600')).toBeInTheDocument();
    expect(screen.getByText('8000')).toBeInTheDocument();
    expect(view.container.querySelectorAll('img')).toHaveLength(1);
  });
});
