import React from 'react';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { describe, test, expect, vi, afterEach } from 'vitest';
import { BrowserRouter } from 'react-router-dom';
import GeneratorMaintenanceTable from './GeneratorMaintenanceTable';

vi.mock('../api/client', () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
  },
}));

import client from '../api/client';

afterEach(() => vi.resetAllMocks());

const mockTables = [
  {
    generatorId: 1,
    generatorName: 'Generator Alpha',
    model: 'Perkins 4012',
    normalizedModelKey: 'PERKINS 4012',
    subStationId: 10,
    subStationName: 'Station 1',
    currentOfficialHours: 3500,
    lastClosingAtUtc: '2026-11-10T00:00:00Z',
    dueOrOverdueCount: 1,
    items: [
      {
        serviceItemId: 1,
        serviceItemCode: 'OilChangeSet',
        serviceItemNameAr: 'بدلة زيت',
        serviceItemNameEn: 'Oil change set',
        intervalHours: 250,
        importance: 3,
        lastServiceHours: 3250,
        dueAtHours: 3500,
        remainingHours: 0,
        overdueByHours: 0,
        status: 3, // Due
        progressFraction: 1.0,
        warningLeadHours: 50,
        isMeterResetAfterLastService: false,
        labelAr: 'مستحق الآن',
        labelEn: 'Due now',
      },
    ],
  },
];

describe('GeneratorMaintenanceTable', () => {
  test('renders generator table with service items and due status', async () => {
    client.get.mockImplementation(async (url) => {
      if (url.includes('/api/service-due/station')) {
        return { data: mockTables };
      }
      if (url === '/api/substations') {
        return { data: [{ id: 10, name: 'Station 1' }] };
      }
      return { data: [] };
    });

    render(
      <BrowserRouter>
        <GeneratorMaintenanceTable lang="ar" />
      </BrowserRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Generator Alpha')).toBeInTheDocument();
    });

    expect(screen.getByText('بدلة زيت')).toBeInTheDocument();
    expect(screen.getByText('مستحق الآن')).toBeInTheDocument();
  });
});
