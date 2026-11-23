import React from 'react';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, expect, test, vi } from 'vitest';

vi.mock('../api/client', () => ({ default: { get: vi.fn(), post: vi.fn() } }));
vi.mock('../hooks/useCommercialMode', () => ({ default: vi.fn() }));

import client from '../api/client';
import useCommercialMode from '../hooks/useCommercialMode';
import OperationalCapture from './OperationalCapture';

afterEach(() => vi.resetAllMocks());

test('commercial generator capture initializes only the selected generator and hides the add strip', async () => {
  useCommercialMode.mockReturnValue({ isCommercial: true, isCommercialMode: true, modeResolved: true });
  client.get.mockImplementation(async (url) => {
    if (url === '/api/operational-captures/references') return { data: [{ subStationId: 1, timeZone: 'Asia/Aden', generators: [{ id: 11, name: 'Selected generator', code: 'G-01' }, { id: 12, name: 'Other generator' }], feeders: [], tanks: [], meters: [] }] };
    if (url === '/api/operational-captures/previous-readings') return { data: { readings: [] } };
    return { data: [] };
  });

  render(<MemoryRouter initialEntries={['/operations/capture?kind=generator&generatorId=11']}><OperationalCapture /></MemoryRouter>);

  expect(await screen.findByText('Selected generator')).toBeInTheDocument();
  expect(screen.queryByText('Other generator')).not.toBeInTheDocument();
  expect(document.querySelector('.capture-adds')).toBeNull();
});

test('shows clear message and back link when generatorId is unknown or non-numeric', async () => {
  useCommercialMode.mockReturnValue({ isCommercial: true, isCommercialMode: true, modeResolved: true });
  client.get.mockImplementation(async (url) => {
    if (url === '/api/operational-captures/references') return { data: [{ subStationId: 1, timeZone: 'Asia/Aden', generators: [{ id: 11, name: 'Selected generator' }], feeders: [], tanks: [], meters: [] }] };
    return { data: [] };
  });

  const { rerender } = render(<MemoryRouter initialEntries={['/operations/capture?kind=generator&generatorId=invalid']}><OperationalCapture /></MemoryRouter>);
  expect(screen.getByText(/Unknown generator|مولد غير معروف/i)).toBeInTheDocument();
  expect(screen.getByRole('link', { name: /Back to operations|العودة إلى التشغيل/i })).toHaveAttribute('href', '/operations/capture');

  rerender(<MemoryRouter initialEntries={['/operations/capture?kind=generator&generatorId=999']}><OperationalCapture /></MemoryRouter>);
  expect(await screen.findByText(/Unknown generator|مولد غير معروف/i)).toBeInTheDocument();
  expect(screen.getByRole('link', { name: /Back to operations|العودة إلى التشغيل/i })).toHaveAttribute('href', '/operations/capture');
});
