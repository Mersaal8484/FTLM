import React from 'react';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, expect, test, vi } from 'vitest';

vi.mock('../api/client', () => ({ default: { get: vi.fn(), post: vi.fn() } }));
vi.mock('../hooks/useCommercialMode', () => ({ default: vi.fn() }));

import client from '../api/client';
import useCommercialMode from '../hooks/useCommercialMode';
import OperationalCapture from './OperationalCapture';

const site = {
  subStationId: 1,
  timeZone: 'Asia/Aden',
  generators: [{ id: 11, name: 'Selected generator', code: 'G-01' }, { id: 12, name: 'Other generator' }],
  feeders: [{ id: 21, name: 'Feeder 1', meterMultiplier: 1 }],
  tanks: [{ id: 31, name: 'Daily tank' }],
  meters: [],
};

function mockRequests() {
  client.get.mockImplementation(async (url, config) => {
    if (url === '/api/operational-captures/references') return { data: [site] };
    if (url === '/api/operational-captures/previous-readings') {
      return {
        data: {
          readings: [],
          tankReadings: [{ fuelTankId: 31, liquidLevelCm: 50, computedLitres: 500, capturedAtUtc: '2026-01-01T00:00:00Z', openingLitres: 500, transferInLitres: 100 }],
          tankBalances: [{ fuelTankId: 31, openingLitres: 500, transferInLitres: 100 }],
        },
      };
    }
    if (url === '/api/fuel-tanks') return { data: [{ id: 31, name: 'Daily tank', tankRole: 2, generatorId: 11, maxLevelCm: 100, calibrationMode: 1, litresPerCm: 10 }] };
    if (url === '/api/fuel-tanks/31/level-to-litres') {
      const cm = Number(config?.params?.cm);
      return { data: { tankId: 31, liquidLevelCm: cm, litres: cm * 10, maxLevelCm: 100, calibrationMode: 1, litresPerCm: 10 } };
    }
    return { data: [] };
  });
}

afterEach(() => vi.resetAllMocks());

test('hourly generator capture includes only operational inputs and hides the add strip', async () => {
  useCommercialMode.mockReturnValue({ isCommercial: true, isCommercialMode: true, modeResolved: true });
  mockRequests();
  render(<MemoryRouter initialEntries={['/operations/capture?kind=hourly-generator&generatorId=11']}><OperationalCapture /></MemoryRouter>);

  expect(await screen.findByText('Selected generator')).toBeInTheDocument();
  expect(screen.queryByText('Other generator')).not.toBeInTheDocument();
  expect(screen.getAllByRole('spinbutton')).toHaveLength(6);
  expect(document.querySelector('.capture-adds')).toBeNull();
});

test('daily generator capture contains energy, running-hours, and daily-tank inputs', async () => {
  useCommercialMode.mockReturnValue({ isCommercial: true, isCommercialMode: true, modeResolved: true });
  mockRequests();
  render(<MemoryRouter initialEntries={['/operations/capture?kind=daily-generator&generatorId=11&tankId=31']}><OperationalCapture /></MemoryRouter>);

  expect(await screen.findByText('Selected generator')).toBeInTheDocument();
  expect(screen.getByText(/Daily tank/)).toBeInTheDocument();
  expect(screen.getAllByRole('spinbutton')).toHaveLength(3);
  expect(document.querySelector('.capture-adds')).toBeNull();
});

test('shows clear message and back link when hourly generatorId is unknown or non-numeric', async () => {
  useCommercialMode.mockReturnValue({ isCommercial: true, isCommercialMode: true, modeResolved: true });
  mockRequests();
  render(<MemoryRouter initialEntries={['/operations/capture?kind=hourly-generator&generatorId=invalid']}><OperationalCapture /></MemoryRouter>);

  expect(await screen.findByText(/Unknown generator|مولد غير معروف/i)).toBeInTheDocument();
  expect(screen.getByRole('link', { name: /Back to operations|العودة إلى التشغيل/i })).toHaveAttribute('href', '/operations/capture');
});

test('daily tank cm input calls backend conversion endpoint, shows litres + diff + preview consumption, and blocks out-of-range submit', async () => {
  const alertMock = vi.fn();
  vi.stubGlobal('alert', alertMock);
  useCommercialMode.mockReturnValue({ isCommercial: true, isCommercialMode: true, modeResolved: true });
  mockRequests();
  render(<MemoryRouter initialEntries={['/operations/capture?kind=daily-generator&generatorId=11&tankId=31']}><OperationalCapture /></MemoryRouter>);

  const levelInput = await screen.findByLabelText(/Daily tank level \(cm\)|مستوى الخزان اليومي \(سم\)/i);
  expect(levelInput).toHaveAttribute('min', '0');
  expect(levelInput).toHaveAttribute('step', 'any');
  expect(levelInput).toBeRequired();

  fireEvent.change(levelInput, { target: { value: '42' } });

  await waitFor(() => {
    expect(client.get).toHaveBeenCalledWith('/api/fuel-tanks/31/level-to-litres', { params: { cm: 42 } });
  });
  expect(await screen.findByText(/≈ 420 L/)).toBeInTheDocument();
  expect(screen.getByText(/1 cm = 10 L|1 سم = 10 L/)).toBeInTheDocument();
  expect(screen.getByText(/50 cm · 500 L/)).toBeInTheDocument();
  expect(screen.getByText(/-8 cm · -80 L/)).toBeInTheDocument();
  // Daily preview: opening 500 + transferIn 100 - closing 420 = 180 L
  expect(screen.getByText(/Closing tank stock: 420 L|رصيد الإقفال: 420 L/i)).toBeInTheDocument();
  expect(screen.getByText(/180 L/)).toBeInTheDocument();

  // Out-of-range input (> 100 cm) shows clear error and prevents POST submit
  fireEvent.change(levelInput, { target: { value: '120' } });
  expect(await screen.findByText(/between 0 and 100|بين 0 و 100/i)).toBeInTheDocument();

  const submitBtn = screen.getByRole('button', { name: /Submit|إرسال/i });
  fireEvent.click(submitBtn);
  expect(client.post).not.toHaveBeenCalled();
  expect(alertMock).toHaveBeenCalled();
  vi.unstubAllGlobals();
});

test('surfaces readable InvalidOperationException and ArgumentOutOfRangeException messages from level-to-litres in UI', async () => {
  useCommercialMode.mockReturnValue({ isCommercial: true, isCommercialMode: true, modeResolved: true });
  client.get.mockImplementation(async (url, config) => {
    if (url === '/api/operational-captures/references') return { data: [site] };
    if (url === '/api/operational-captures/previous-readings') return { data: { readings: [], tankReadings: [], tankBalances: [] } };
    if (url === '/api/fuel-tanks') return { data: [{ id: 31, name: 'Daily tank', tankRole: 2, generatorId: 11 }] };
    if (url === '/api/fuel-tanks/31/level-to-litres') {
      const cm = Number(config?.params?.cm);
      if (cm === 15) {
        throw { response: { status: 400, data: { title: 'Bad Request', detail: 'Tank must be calibrated before converting levels.' } } };
      }
      throw { response: { status: 400, data: { title: 'Bad Request', detail: "Liquid level must be between 0 and 100 cm. (Parameter 'cm')" } } };
    }
    return { data: [] };
  });

  render(<MemoryRouter initialEntries={['/operations/capture?kind=daily-generator&generatorId=11&tankId=31']}><OperationalCapture /></MemoryRouter>);

  const levelInput = await screen.findByLabelText(/Daily tank level \(cm\)|مستوى الخزان اليومي \(سم\)/i);
  fireEvent.change(levelInput, { target: { value: '15' } });
  expect(await screen.findByText('Tank must be calibrated before converting levels.')).toBeInTheDocument();

  fireEvent.change(levelInput, { target: { value: '120' } });
  expect(await screen.findByText('Liquid level must be between 0 and 100 cm.')).toBeInTheDocument();
});
