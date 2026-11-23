import React from 'react';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, expect, test, vi } from 'vitest';

vi.mock('../api/client', () => ({ default: { get: vi.fn() } }));
vi.mock('../hooks/useCommercialMode', () => ({ default: vi.fn() }));

import client from '../api/client';
import useCommercialMode from '../hooks/useCommercialMode';
import CommercialCaptureHub from './CommercialCaptureHub';

afterEach(() => vi.resetAllMocks());

test('renders commercial generator, feeder, and tank capture actions from the technical reference site', async () => {
  useCommercialMode.mockReturnValue({ isCommercial: true, isCommercialMode: true, modeResolved: true });
  client.get.mockImplementation(async (url) => {
    if (url === '/api/operational-captures/references') return { data: [{
      generators: [{ id: 11, name: 'Generator 1', model: 'G-01', meterId: null }],
      feeders: [{ id: 21, name: 'Feeder 1' }],
      tanks: [{ id: 31, name: 'Tank 1' }],
      meters: [{ id: 41, meterTypeCode: 'SolarMeter' }],
    }] };
    if (url === '/api/generators/my') return { data: [{ id: 11, name: 'Generator 1', lastPowerRecord: 70 }] };
    return { data: [] };
  });

  render(<MemoryRouter><CommercialCaptureHub /></MemoryRouter>);

  expect(await screen.findByText('Generator 1')).toBeInTheDocument();
  expect(screen.getByRole('link', { name: /Record reading|تسجيل قراءة/i })).toHaveAttribute('href', '/operations/capture?kind=generator&generatorId=11');
  expect(screen.getByRole('link', { name: /Record feeders|تسجيل الفيدرات/i })).toHaveAttribute('href', '/operations/capture?kind=feeders');
  expect(screen.getByRole('link', { name: /Record tank level|تسجيل مستوى الخزان/i })).toHaveAttribute('href', '/operations/capture?kind=tank');
});

test('generator card displays the generator name from the reference DTO even when details has no name', async () => {
  useCommercialMode.mockReturnValue({ isCommercial: true, isCommercialMode: true, modeResolved: true });
  client.get.mockImplementation(async (url) => {
    if (url === '/api/operational-captures/references') return { data: [{ generators: [{ id: 7, name: 'Alpha Gen', code: 'AG-01' }], feeders: [], tanks: [], meters: [] }] };
    if (url === '/api/generators/my') return { data: [{ id: 7, lastPowerRecord: 120, lastHoursRecord: 500 }] };
    return { data: [] };
  });
  render(<MemoryRouter><CommercialCaptureHub /></MemoryRouter>);
  expect(await screen.findByText('Alpha Gen')).toBeInTheDocument();
});
