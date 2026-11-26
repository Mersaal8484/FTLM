import React from 'react';
import { fireEvent, render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom';
import { afterEach, expect, test, vi } from 'vitest';

vi.mock('../api/client', () => ({ default: { get: vi.fn(), post: vi.fn() } }));
vi.mock('../hooks/useCommercialMode', () => ({ default: vi.fn() }));

import client from '../api/client';
import useCommercialMode from '../hooks/useCommercialMode';
import AddOilToGenerator from './AddOilToGenerator';

function LocationProbe({ label }) {
  const location = useLocation();
  return <div>{label}:{location.pathname}{location.search}</div>;
}

function mockRequests() {
  client.get.mockImplementation(async (url) => {
    if (url === '/api/generators/my/11') return { data: { id: 11, name: 'Generator 1', stationId: 1 } };
    if (url === '/api/oil-transactions/generator/11') return { data: [] };
    if (url === '/api/oils') return { data: [] };
    return { data: [] };
  });
}

afterEach(() => vi.resetAllMocks());

test('back button navigates to /operations/capture?tab=daily in commercial mode', async () => {
  useCommercialMode.mockReturnValue({ isCommercial: true, isCommercialMode: true, modeResolved: true });
  mockRequests();

  const { container } = render(
    <MemoryRouter initialEntries={['/generators/11/add-oil']}>
      <Routes>
        <Route path="/generators/:generatorId/add-oil" element={<AddOilToGenerator lang="en" />} />
        <Route path="/operations/capture" element={<LocationProbe label="commercial-capture" />} />
        <Route path="/my-generators" element={<LocationProbe label="enterprise-my-generators" />} />
      </Routes>
    </MemoryRouter>
  );

  await screen.findByText('Add Oil to Generator');
  const backButton = container.querySelector('h1 button');
  fireEvent.click(backButton);

  expect(await screen.findByText('commercial-capture:/operations/capture?tab=daily')).toBeInTheDocument();
});

test('back button navigates to /my-generators in enterprise mode', async () => {
  useCommercialMode.mockReturnValue({ isCommercial: false, isCommercialMode: false, modeResolved: true });
  mockRequests();

  const { container } = render(
    <MemoryRouter initialEntries={['/generators/11/add-oil']}>
      <Routes>
        <Route path="/generators/:generatorId/add-oil" element={<AddOilToGenerator lang="en" />} />
        <Route path="/operations/capture" element={<LocationProbe label="commercial-capture" />} />
        <Route path="/my-generators" element={<LocationProbe label="enterprise-my-generators" />} />
      </Routes>
    </MemoryRouter>
  );

  await screen.findByText('Add Oil to Generator');
  const backButton = container.querySelector('h1 button');
  fireEvent.click(backButton);

  expect(await screen.findByText('enterprise-my-generators:/my-generators')).toBeInTheDocument();
});
