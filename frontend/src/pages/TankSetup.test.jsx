import React from 'react';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { describe, test, expect, vi, afterEach } from 'vitest';
import TankSetup from './TankSetup';
import i18n from '../i18n';

vi.mock('../api/client', () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
  },
}));

import client from '../api/client';

afterEach(() => vi.resetAllMocks());

describe('TankSetup', () => {
  test('renders tank setup fields and preview button', async () => {
    client.get.mockImplementation(async (url) => {
      if (url === '/api/fuel-tanks') {
        return { data: [{ id: 1, name: 'Daily Tank G1', subStationId: 10, diameterCm: 200, cylinderHeightCm: 300, coneHeightCm: 50, multiplicationFactor: 1.0, deadStockLiters: 100 }] };
      }
      return { data: [] };
    });

    render(<TankSetup />);

    await waitFor(() => {
      expect(screen.getByText(i18n.t('operational.tanks.setupTitle'))).toBeInTheDocument();
    });

    await waitFor(() => {
      expect(screen.getByText(i18n.t('operational.tanks.preview'))).toBeInTheDocument();
    });
  });

  test('toggles dead stock mode between litres and cm and posts expected payload', async () => {
    client.get.mockImplementation(async (url) => {
      if (url === '/api/fuel-tanks') {
        return { data: [{ id: 1, name: 'Daily Tank G1', subStationId: 10, diameterCm: 200, cylinderHeightCm: 300, coneHeightCm: 50, multiplicationFactor: 1.0 }] };
      }
      return { data: [] };
    });
    client.post.mockResolvedValue({
      data: {
        computedCapacityLitres: 10000,
        totalHeightCm: 350,
        deadStockLitres: 500,
        deadStockLevelCm: 25.4,
        points: []
      }
    });

    render(<TankSetup />);

    await waitFor(() => {
      expect(screen.getByText(i18n.t('operational.tanks.deadStockModeLitres'))).toBeInTheDocument();
    });

    // Toggle mode to cm
    const modeSelect = screen.getByDisplayValue(i18n.t('operational.tanks.deadStockModeLitres'));
    fireEvent.change(modeSelect, { target: { value: 'cm' } });

    expect(screen.getByPlaceholderText('e.g. 25')).toBeInTheDocument();
  });
});
