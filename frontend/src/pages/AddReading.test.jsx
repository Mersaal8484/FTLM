import React from 'react';
import { fireEvent, render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom';
import { afterEach, expect, test, vi } from 'vitest';

vi.mock('../api/client', () => ({ default: { get: vi.fn(), post: vi.fn() } }));
vi.mock('../hooks/useCommercialMode', () => ({ default: vi.fn() }));

import client from '../api/client';
import useCommercialMode from '../hooks/useCommercialMode';
import AddReading from './AddReading';

function LocationProbe({ label }) {
  const location = useLocation();
  return <div>{`${label}:${location.pathname}${location.search}`}</div>;
}

function mockRequests() {
  client.get.mockImplementation(async (url) => {
    if (url === '/api/generators/my/11') return { data: { id: 11, name: 'PERKINS-01', stationId: 1, stationName: 'Station 1', assignedOilTypes: [] } };
    if (url === '/api/generators/my/11/last-reading') return { data: {} };
    if (url === '/api/oils') return { data: [] };
    if (url === '/api/controllers') return { data: [] };
    return { data: [] };
  });
}

afterEach(() => vi.resetAllMocks());

test('back button navigates to /operations/capture in commercial mode', async () => {
  useCommercialMode.mockReturnValue({ isCommercial: true, isCommercialMode: true, modeResolved: true });
  mockRequests();

  const { container } = render(
    <MemoryRouter initialEntries={['/generators/11/add-reading']}>
      <Routes>
        <Route path="/generators/:generatorId/add-reading" element={<AddReading lang="en" currentUser={{}} />} />
        <Route path="/operations/capture" element={<LocationProbe label="commercial-hub" />} />
        <Route path="/my-generators" element={<LocationProbe label="enterprise-my-generators" />} />
      </Routes>
    </MemoryRouter>,
  );

  expect(await screen.findByText('PERKINS-01')).toBeInTheDocument();
  const buttons = container.querySelectorAll('button');
  fireEvent.click(buttons[0]);
  expect(await screen.findByText('commercial-hub:/operations/capture')).toBeInTheDocument();
});

test('back button navigates to /my-generators in enterprise mode', async () => {
  useCommercialMode.mockReturnValue({ isCommercial: false, isCommercialMode: false, modeResolved: true });
  mockRequests();

  const { container } = render(
    <MemoryRouter initialEntries={['/generators/11/add-reading']}>
      <Routes>
        <Route path="/generators/:generatorId/add-reading" element={<AddReading lang="en" currentUser={{}} />} />
        <Route path="/operations/capture" element={<LocationProbe label="commercial-hub" />} />
        <Route path="/my-generators" element={<LocationProbe label="enterprise-my-generators" />} />
      </Routes>
    </MemoryRouter>,
  );

  expect(await screen.findByText('PERKINS-01')).toBeInTheDocument();
  const buttons = container.querySelectorAll('button');
  fireEvent.click(buttons[0]);
  expect(await screen.findByText('enterprise-my-generators:/my-generators')).toBeInTheDocument();
});
