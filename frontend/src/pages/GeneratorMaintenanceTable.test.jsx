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
      {
        serviceItemId: 2,
        serviceItemCode: 'CoolantFilter',
        serviceItemNameAr: 'فلتر مياه',
        serviceItemNameEn: 'Coolant filter',
        intervalHours: null,
        importance: null,
        lastServiceHours: null,
        dueAtHours: null,
        remainingHours: null,
        overdueByHours: null,
        status: 5, // NotConfigured
        progressFraction: 0,
        warningLeadHours: null,
        isMeterResetAfterLastService: false,
        labelAr: 'غير مهيأ',
        labelEn: 'Not configured',
        definitionLink: '/maintenance/service/definitions',
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
      if (url === '/api/sub-stations') {
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

  test('renders NotConfigured item with active record button and definition link (C4, C5)', async () => {
    client.get.mockImplementation(async (url) => {
      if (url.includes('/api/service-due/station')) {
        return { data: mockTables };
      }
      if (url === '/api/sub-stations') {
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
      expect(screen.getByText('فلتر مياه')).toBeInTheDocument();
    });

    // Check NotConfigured label and definition link
    expect(screen.getByText('غير مهيأ')).toBeInTheDocument();
    expect(screen.getByText('تعريف الفترة')).toBeInTheDocument();

    // Verify all record buttons are enabled (including NotConfigured)
    const recordButtons = screen.getAllByRole('button', { name: /تسجيل خدمة/i });
    expect(recordButtons.length).toBe(2);
    expect(recordButtons[1]).not.toBeDisabled();
  });

  test('reuses clientRequestId on retry / double click (C2)', async () => {
    client.get.mockImplementation(async (url) => {
      if (url.includes('/api/service-due/station')) {
        return { data: mockTables };
      }
      if (url === '/api/sub-stations') {
        return { data: [{ id: 10, name: 'Station 1' }] };
      }
      return { data: [] };
    });

    let callCount = 0;
    const sentRequestIds = [];
    client.post.mockImplementation(async (url, body) => {
      callCount++;
      sentRequestIds.push(body.clientRequestId);
      if (callCount === 1) {
        // First attempt fails with network/server error to trigger retry
        const error = new Error('Network error');
        error.response = { data: { error: 'Temporary error' } };
        throw error;
      }
      return { data: { id: 1 } };
    });

    render(
      <BrowserRouter>
        <GeneratorMaintenanceTable lang="ar" />
      </BrowserRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Generator Alpha')).toBeInTheDocument();
    });

    // Open record modal for first item
    const recordButtons = screen.getAllByRole('button', { name: /تسجيل خدمة/i });
    fireEvent.click(recordButtons[0]);

    // Click submit first time (fails)
    const saveButton = screen.getByRole('button', { name: /حفظ السجل/i });
    fireEvent.click(saveButton);

    await waitFor(() => {
      expect(screen.getByText('Temporary error')).toBeInTheDocument();
    });

    // Click submit second time (retry without editing)
    fireEvent.click(saveButton);

    await waitFor(() => {
      expect(client.post).toHaveBeenCalledTimes(2);
    });

    // Both calls must share the EXACT SAME clientRequestId
    expect(sentRequestIds[0]).toBeDefined();
    expect(sentRequestIds[0]).toBe(sentRequestIds[1]);
  });

  test('opens void confirmation dialog from service history list', async () => {
    client.get.mockImplementation(async (url) => {
      if (url.includes('/api/service-due/station')) {
        return { data: mockTables };
      }
      if (url === '/api/sub-stations') {
        return { data: [{ id: 10, name: 'Station 1' }] };
      }
      if (url.includes('/api/service-replacements/history')) {
        return {
          data: [
            {
              id: 99,
              generatorId: 1,
              serviceItemId: 1,
              kind: 1,
              hoursAtService: 3250,
              occurredAtUtc: '2026-11-01T00:00:00Z',
              isEarly: false,
              voidedAtUtc: null,
            },
          ],
        };
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

    // Open history modal
    const historyButtons = screen.getAllByTitle(/سجل الصيانات/i);
    fireEvent.click(historyButtons[0]);

    await waitFor(() => {
      expect(screen.getByText('3250 h')).toBeInTheDocument();
    });

    // Click void button inside history table
    const voidBtn = screen.getByRole('button', { name: /إلغاء السجل/i });
    fireEvent.click(voidBtn);

    // Verify void confirmation dialog opened
    await waitFor(() => {
      expect(screen.getByText('إلغاء سجل خدمة')).toBeInTheDocument();
      expect(screen.getByText(/إلغاء هذا السجل سيعيد حساب ساعات الاستحقاق للمولد/i)).toBeInTheDocument();
    });
  });
});
