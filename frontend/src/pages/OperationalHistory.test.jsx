import React from 'react';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, expect, test, vi } from 'vitest';

vi.mock('../api/client', () => ({ default: { get: vi.fn() } }));

import client from '../api/client';
import OperationalHistory from './OperationalHistory';

afterEach(() => vi.resetAllMocks());

test('displays daily tank cm level and stored computed litres on submission history', async () => {
  client.get.mockResolvedValueOnce({
    data: [
      {
        id: 5,
        subStationId: 1,
        subStationName: 'Site 1',
        receivedAtUtc: '2026-11-08T18:00:00Z',
        status: 'Pending',
        generatorReadingCount: 1,
        tankLevelReadingCount: 1,
        feederLoadReadingCount: 0,
        meterReadingCount: 0,
        tankReadings: [
          {
            id: 91,
            fuelTankId: 31,
            fuelTankName: 'Daily Tank 1',
            liquidLevelCm: 42,
            computedLitres: 420,
            status: 'Pending',
            eventType: 'Level',
            capturedAtUtc: '2026-11-08T18:00:00Z',
          },
        ],
      },
    ],
  });

  render(
    <MemoryRouter>
      <OperationalHistory />
    </MemoryRouter>,
  );

  expect(await screen.findByText(/Daily Tank 1/)).toBeInTheDocument();
  expect(screen.getByText(/42 cm/)).toBeInTheDocument();
  expect(screen.getByText(/420 L/)).toBeInTheDocument();
});
