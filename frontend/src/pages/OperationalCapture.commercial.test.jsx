import React from 'react';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, expect, test, vi } from 'vitest';

vi.mock('../api/client', () => ({ default: { get: vi.fn() } }));
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
  client.get.mockImplementation(async (url) => {
    if (url === '/api/operational-captures/references') return { data: [site] };
    if (url === '/api/operational-captures/previous-readings') return { data: { readings: [] } };
    if (url === '/api/fuel-tanks') return { data: [{ id: 31, name: 'Daily tank', tankRole: 2, generatorId: 11 }] };
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
