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
  businessDate: '2026-11-07',
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

  test('groups same-station submissions for one business date into one review page', async () => {
    client.get.mockResolvedValueOnce({
      data: [
        pendingSubmission,
        { ...pendingSubmission, id: 2, meterLineCount: 2, receivedAtUtc: '2026-11-07T20:10:00Z' },
        { ...pendingSubmission, id: 3, subStationId: 11, siteName: 'Other Station' },
      ],
    });

    renderReview();

    await waitFor(() => {
      expect(screen.getAllByRole('article')).toHaveLength(2);
    });
    expect(screen.getByText('Other Station')).toBeInTheDocument();
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
            generatorId: 1,
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
    expect(screen.getByText('420')).toBeInTheDocument();
  });

  test('shows submitted current meter values even when no previous official value exists', async () => {
    const reviewDetail = {
      ...detailResponse,
      generatorLines: [{
        id: 41, status: 'Pending', capturedAtUtc: '2026-11-07T20:00:00Z', receivedAtUtc: '2026-11-07T20:01:00Z',
        hasSkewWarning: false, photoAttachmentIds: [], warningsJson: null, rejectionReason: null, hasReuseWarning: false,
        values: { generatorId: 1, generatorName: 'Generator A', isRunning: true, lineVoltageVolts: 400, currentAmps: 120, oilPressure: 5, oilTemperatureCelsius: 82, waterTemperatureCelsius: 76, activeLoadKw: 90 },
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
    expect(screen.getByText(/1[,\u066C]?250/)).toBeInTheDocument();
    expect(screen.getByText(/End-of-day generator review|مراجعة قراءات نهاية اليوم للمولدات/i)).toBeInTheDocument();
    expect(screen.getByText(/Energy current|قراءة الطاقة الحالية/i)).toBeInTheDocument();
  });

  test('renders one generator row with fuel, oil, meter differences, photo, and a per-generator decision', async () => {
    const generatorDetail = {
      ...detailResponse,
      generatorLines: [{
        id: 71, status: 'Pending', capturedAtUtc: '2026-11-07T20:00:00Z', receivedAtUtc: '2026-11-07T20:01:00Z',
        hasSkewWarning: false, photoAttachmentIds: [401], warningsJson: null, rejectionReason: null, hasReuseWarning: false,
        values: { generatorId: 7, generatorName: 'Generator 7', oilId: 9, oilName: 'Engine Oil', oilAddedLitres: 20 },
      }],
      tankLines: [{
        id: 72, status: 'Pending', capturedAtUtc: '2026-11-07T20:00:00Z', receivedAtUtc: '2026-11-07T20:01:00Z',
        hasSkewWarning: false, photoAttachmentIds: [401], warningsJson: null, rejectionReason: null, hasReuseWarning: false,
        values: { generatorId: 7, fuelTankName: 'Tank 7', liquidLevelCm: 30, computedLitres: 300 },
      }],
      meterLines: [
        { id: 73, status: 'Pending', subjectType: 0, subjectId: 7, kind: 'DailyClosing', capturedAtUtc: '2026-11-07T20:00:00Z', receivedAtUtc: '2026-11-07T20:01:00Z', businessDate: '2026-11-07', hasSkewWarning: false, photoAttachmentIds: [401], rawValue: 1250, multiplierSnapshot: 1, isMeterReset: false, resetNote: null, warningsJson: null, rejectionReason: null, previousApprovedValue: 1000, deltaPreview: 250, deltaSpanDays: 1, subjectName: 'Generator 7' },
        { id: 74, status: 'Pending', subjectType: 1, subjectId: 7, kind: 'DailyClosing', capturedAtUtc: '2026-11-07T20:00:00Z', receivedAtUtc: '2026-11-07T20:01:00Z', businessDate: '2026-11-07', hasSkewWarning: false, photoAttachmentIds: [401], rawValue: 520, multiplierSnapshot: 1, isMeterReset: false, resetNote: null, warningsJson: null, rejectionReason: null, previousApprovedValue: 500, deltaPreview: 20, deltaSpanDays: 1, subjectName: 'Generator 7' },
      ],
      dailyFuelConsumptions: [{ generatorId: 7, generatorName: 'Generator 7', dailyTankId: 17, dailyTankName: 'Tank 7', businessDate: '2026-11-07', openingBalanceLitres: 600, inboundRefillsLitres: 0, closingBalanceLitres: 300, consumptionLitres: 300, producedKwh: 250, kwhPerLiter: 0.83, warnings: [] }],
    };

    client.get
      .mockResolvedValueOnce({ data: [pendingSubmission] })
      .mockResolvedValueOnce({ data: generatorDetail });

    const view = renderReview();
    fireEvent.click(await screen.findByText('المحطة الرئيسية'));

    expect(await screen.findByText('Generator 7')).toBeInTheDocument();
    expect(screen.getByText('Engine Oil')).toBeInTheDocument();
    expect(screen.getByText(/1[,\u066C]?250/)).toBeInTheDocument();
    expect(screen.getByText('250')).toBeInTheDocument();
    expect(screen.getByText('520')).toBeInTheDocument();
    expect(screen.getByText(/الطاقة المنتجة \(kWh\)|Produced Energy \(kWh\)/i)).toBeInTheDocument();
    expect(screen.getByText(/^ساعات التشغيل$|^Running hours$/i)).toBeInTheDocument();
    expect(screen.queryByText(/فرق الطاقة \(kWh\)|Energy difference \(kWh\)/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/فرق ساعات التشغيل|Hours difference/i)).not.toBeInTheDocument();
    expect(view.container.querySelectorAll('img')).toHaveLength(1);
    expect(screen.getByRole('button', { name: /approve generator data|اعتماد بيانات المولد/i })).toBeInTheDocument();
  });

  test('combines generator oil, meter, and daily-tank submissions into one complete generator row', async () => {
    const oilAndMeterDetail = {
      ...detailResponse,
      id: 1,
      generatorLines: [{
        id: 91, status: 'Pending', capturedAtUtc: '2026-11-07T20:00:00Z', receivedAtUtc: '2026-11-07T20:01:00Z',
        hasSkewWarning: false, photoAttachmentIds: [501], warningsJson: null, rejectionReason: null, hasReuseWarning: false,
        values: { generatorId: 12, generatorName: 'Generator 12', oilId: 4, oilName: 'Engine Oil', oilAddedLitres: 20 },
      }],
      meterLines: [{
        id: 92, status: 'Pending', subjectType: 0, subjectId: 12, kind: 'DailyClosing', capturedAtUtc: '2026-11-07T20:00:00Z', receivedAtUtc: '2026-11-07T20:01:00Z', businessDate: '2026-11-07',
        hasSkewWarning: false, photoAttachmentIds: [501], rawValue: 1250, multiplierSnapshot: 1, isMeterReset: false, resetNote: null, warningsJson: null, rejectionReason: null,
        previousApprovedValue: 1000, deltaPreview: 250, deltaSpanDays: 1, subjectName: 'Generator 12',
      }],
      dailyFuelConsumptions: [{ generatorId: 12, generatorName: 'Generator 12', dailyTankId: 25, dailyTankName: 'Tank 12', businessDate: '2026-11-07', openingBalanceLitres: 600, inboundRefillsLitres: 0, closingBalanceLitres: 0, consumptionLitres: 0, producedKwh: 250, kwhPerLiter: 0.42, warnings: [] }],
    };
    const tankDetail = {
      ...detailResponse,
      id: 2,
      tankLines: [{
        id: 93, status: 'Pending', capturedAtUtc: '2026-11-07T20:05:00Z', receivedAtUtc: '2026-11-07T20:06:00Z',
        hasSkewWarning: false, photoAttachmentIds: [502], warningsJson: null, rejectionReason: null, hasReuseWarning: false,
        values: { fuelTankId: 25, fuelTankName: 'Tank 12', generatorId: 12, liquidLevelCm: 30, computedLitres: 300, beforeComputedLitres: 600, eventType: 0 },
      }],
    };

    client.get
      .mockResolvedValueOnce({ data: [pendingSubmission, { ...pendingSubmission, id: 2, receivedAtUtc: '2026-11-07T20:05:00Z' }] })
      .mockResolvedValueOnce({ data: oilAndMeterDetail })
      .mockResolvedValueOnce({ data: tankDetail });

    const view = renderReview();
    fireEvent.click(await screen.findByText('المحطة الرئيسية'));

    expect(await screen.findByText('Generator 12')).toBeInTheDocument();
    expect(screen.getByText('Engine Oil')).toBeInTheDocument();
    expect(screen.getByText('30 cm')).toBeInTheDocument();
    expect(screen.getByText('20 L')).toBeInTheDocument();
    expect(screen.getAllByText('300').length).toBeGreaterThan(0);
    expect(view.container.querySelectorAll('img')).toHaveLength(2);
    expect(screen.getByText(/^شاشة المولد$|^Generator screen$/i)).toBeInTheDocument();
    expect(screen.getByText(/^مستوى الخزان$|^Tank level$/i)).toBeInTheDocument();
  });

  test('uses the feeder counter difference as the active kW load and hides separate load columns', async () => {
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
    expect(screen.getByText(/10[,\u066C]?600/)).toBeInTheDocument();
    expect(screen.getByText(/8[,\u066C]?000/)).toBeInTheDocument();
    expect(screen.getByText(/الحمل الفعلي \(kW\)|Active load \(kW\)/i)).toBeInTheDocument();
    expect(screen.queryByText(/فرق الطاقة \(kWh\)|Energy difference \(kWh\)/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/الحمل الظاهري \(kVA\)|Apparent load \(kVA\)/i)).not.toBeInTheDocument();
    expect(view.container.querySelectorAll('img')).toHaveLength(1);
    expect(screen.getByRole('button', { name: /approve feeder data|اعتماد بيانات الفيدر/i })).toBeInTheDocument();
  });
});
