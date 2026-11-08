import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import { describe, test, expect, vi, afterEach } from 'vitest';
import TankRefillTransfer from './TankRefillTransfer';
import i18n from '../i18n';

vi.mock('../api/client', () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
  },
}));

import client from '../api/client';

afterEach(() => vi.resetAllMocks());

describe('TankRefillTransfer', () => {
  test('renders tank selection and form elements', async () => {
    client.get.mockImplementation(async (url) => {
      if (url === '/api/fuel-tanks') {
        return { data: [{ id: 1, name: 'Main Tank A', subStationId: 10 }] };
      }
      return { data: [] };
    });

    render(<TankRefillTransfer />);

    await waitFor(() => {
      expect(screen.getByText(i18n.t('operational.tanks.refillTitle'))).toBeInTheDocument();
    });

    await waitFor(() => {
      expect(screen.getByText('Main Tank A (Site 10)')).toBeInTheDocument();
    });
  });
});
