import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import { describe, test, expect, vi, afterEach } from 'vitest';
import OperationalDailyLog from './OperationalDailyLog';
import i18n from '../i18n';

vi.mock('../api/client', () => ({
  default: {
    get: vi.fn(),
  },
}));

import client from '../api/client';

afterEach(() => vi.resetAllMocks());

describe('OperationalDailyLog', () => {
  test('renders header and generator selector', async () => {
    client.get.mockImplementation(async (url) => {
      if (url === '/api/generators') {
        return { data: [{ id: 5, name: 'G1', subStationId: 10 }] };
      }
      if (url.includes('/daily-log/')) {
        return {
          data: {
            generatorId: 5,
            businessDate: '2026-11-08',
            rows: [
              {
                capturedAtUtc: '2026-11-08T12:00:00Z',
                activeLoadKw: 420.5,
                apparentLoadKva: 450.0,
                powerFactor: 0.93,
                frequencyHz: 50.0,
                markers: []
              }
            ]
          }
        };
      }
      return { data: [] };
    });

    render(<OperationalDailyLog />);

    await waitFor(() => {
      expect(screen.getByText(i18n.t('operational.log.dailyTitle'))).toBeInTheDocument();
    });

    await waitFor(() => {
      expect(screen.getByText('420.5')).toBeInTheDocument();
    });
  });
});
