import React from 'react';
import { fireEvent, render, screen } from '@testing-library/react';
import { MemoryRouter, useLocation } from 'react-router-dom';
import { afterEach, expect, test, vi } from 'vitest';

vi.mock('../api/client', () => ({ default: { get: vi.fn() } }));
vi.mock('../hooks/useCommercialMode', () => ({ default: vi.fn() }));
vi.mock('../permissions/usePermissions', () => ({ default: () => ({ can: () => true }) }));

import client from '../api/client';
import useCommercialMode from '../hooks/useCommercialMode';
import CommercialCaptureHub from './CommercialCaptureHub';

const commercialSite = {
  generators: [{ id: 11, name: 'Generator 1', model: 'G-01', meterId: null }],
  feeders: [{ id: 21, name: 'Feeder 1' }],
  tanks: [{ id: 31, name: 'Daily Tank 1' }],
  meters: [{ id: 41, meterTypeCode: 'SolarMeter' }],
};

function mockRequests() {
  client.get.mockImplementation(async (url) => {
    if (url === '/api/operational-captures/references') return { data: [commercialSite] };
    if (url === '/api/generators/my') return { data: [{ id: 11, lastPowerRecord: 70 }] };
    if (url === '/api/fuel-tanks') return { data: [{ id: 31, name: 'Daily Tank 1', tankRole: 2, generatorId: 11, actualStockLiters: 200 }] };
    return { data: [] };
  });
}

afterEach(() => vi.resetAllMocks());

test('starts with only the hourly generator-card action', async () => {
  useCommercialMode.mockReturnValue({ isCommercial: true, isCommercialMode: true, modeResolved: true });
  mockRequests();
  const { container } = render(<MemoryRouter><CommercialCaptureHub /></MemoryRouter>);

  expect(await screen.findByText('Generator 1')).toBeInTheDocument();
  const hourlyLink = Array.from(container.querySelectorAll('a')).find((link) => link.getAttribute('href')?.includes('kind=hourly-generator'));
  expect(hourlyLink).toHaveAttribute('href', '/operations/capture?kind=hourly-generator&generatorId=11');
  expect(container.querySelector('a[href*="daily-feeders"]')).toBeNull();
  expect(container.querySelector('a[href*="kind=tank"]')).toBeNull();
});

test('end-of-day tab pairs a generator with its daily tank and exposes feeder closing', async () => {
  useCommercialMode.mockReturnValue({ isCommercial: true, isCommercialMode: true, modeResolved: true });
  mockRequests();
  const { container } = render(<MemoryRouter><CommercialCaptureHub /></MemoryRouter>);

  await screen.findByText('Generator 1');
  const tabs = screen.getAllByRole('button');
  fireEvent.click(tabs[1]);

  const dailyGeneratorLink = Array.from(container.querySelectorAll('a')).find((link) => link.getAttribute('href')?.includes('kind=daily-generator'));
  expect(dailyGeneratorLink).toHaveAttribute('href', '/operations/capture?kind=daily-generator&generatorId=11&tankId=31');
  expect(container.querySelector('a[href="/operations/capture?kind=daily-feeders"]')).toBeInTheDocument();
  expect(container.querySelector('a[href="/generators/11/add-oil"]')).toBeInTheDocument();
});

test('end-of-day tab does not pair a main or unlinked tank with a generator', async () => {
  useCommercialMode.mockReturnValue({ isCommercial: true, isCommercialMode: true, modeResolved: true });
  client.get.mockImplementation(async (url) => {
    if (url === '/api/operational-captures/references') return { data: [commercialSite] };
    if (url === '/api/generators/my') return { data: [{ id: 11, lastPowerRecord: 70 }] };
    if (url === '/api/fuel-tanks') return { data: [{ id: 31, name: 'Main Tank', tankRole: 1, actualStockLiters: 200 }] };
    return { data: [] };
  });
  const { container } = render(<MemoryRouter><CommercialCaptureHub /></MemoryRouter>);

  await screen.findByText('Generator 1');
  fireEvent.click(screen.getAllByRole('button')[1]);

  expect(container.querySelector('a[href*="kind=daily-generator"]')).toBeNull();
  expect(container.querySelector('a[href="/commercial/daily-tanks"]')).toBeInTheDocument();
  expect(container.querySelector('button[disabled]')).toBeInTheDocument();
});

function SearchProbe() {
  const location = useLocation();
  return <div data-testid="hub-search">{location.search}</div>;
}

test('reads ?tab=daily from URL, falls back on unknown values, and updates URL query on tab click', async () => {
  useCommercialMode.mockReturnValue({ isCommercial: true, isCommercialMode: true, modeResolved: true });
  mockRequests();

  const { container } = render(
    <MemoryRouter initialEntries={['/operations/capture?tab=daily']}>
      <CommercialCaptureHub />
      <SearchProbe />
    </MemoryRouter>
  );

  expect(await screen.findByText('Generator 1')).toBeInTheDocument();
  expect(container.querySelector('a[href="/generators/11/add-oil"]')).toBeInTheDocument();
  expect(screen.getByTestId('hub-search')).toHaveTextContent('?tab=daily');

  const tabs = screen.getAllByRole('button');
  fireEvent.click(tabs[0]);
  expect(screen.getByTestId('hub-search')).toHaveTextContent('?tab=hourly');
  expect(container.querySelector('a[href*="kind=hourly-generator"]')).toBeInTheDocument();

  fireEvent.click(tabs[1]);
  expect(screen.getByTestId('hub-search')).toHaveTextContent('?tab=daily');
});

test('falls back to hourly tab when query param tab has an unknown value', async () => {
  useCommercialMode.mockReturnValue({ isCommercial: true, isCommercialMode: true, modeResolved: true });
  mockRequests();

  const { container } = render(
    <MemoryRouter initialEntries={['/operations/capture?tab=unknown']}>
      <CommercialCaptureHub />
    </MemoryRouter>
  );

  expect(await screen.findByText('Generator 1')).toBeInTheDocument();
  expect(container.querySelector('a[href*="kind=hourly-generator"]')).toBeInTheDocument();
  expect(container.querySelector('a[href*="daily-feeders"]')).toBeNull();
});
