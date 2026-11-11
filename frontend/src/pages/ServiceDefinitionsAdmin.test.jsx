import React from 'react';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { describe, test, expect, vi, afterEach } from 'vitest';
import { BrowserRouter } from 'react-router-dom';
import ServiceDefinitionsAdmin from './ServiceDefinitionsAdmin';

vi.mock('../api/client', () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
    delete: vi.fn(),
  },
}));

import client from '../api/client';

afterEach(() => vi.resetAllMocks());

const mockDefs = [
  {
    id: 1,
    generatorModelKey: 'PERKINS 4012',
    serviceItemId: 1,
    serviceItemNameAr: 'بدلة زيت',
    serviceItemNameEn: 'Oil change set',
    intervalHours: 250,
    effectiveLeadHours: 50,
    importance: 3,
  },
];

const mockItems = [
  {
    id: 1,
    code: 'OilChangeSet',
    nameAr: 'بدلة زيت',
    nameEn: 'Oil change set',
    sortOrder: 1,
    suggestedIntervalHours: 250,
  },
];

describe('ServiceDefinitionsAdmin', () => {
  test('opens add-definition dialog when "تعريف فترة لطراز" button is clicked', async () => {
    client.get.mockImplementation(async (url) => {
      if (url.includes('/api/service-interval-definitions')) {
        return { data: mockDefs };
      }
      if (url.includes('/api/service-items')) {
        return { data: mockItems };
      }
      return { data: [] };
    });

    render(
      <BrowserRouter>
        <ServiceDefinitionsAdmin lang="ar" />
      </BrowserRouter>
    );

    await screen.findByRole('cell', { name: 'PERKINS 4012' });

    const addDefButton = screen.getByRole('button', { name: /تعريف فترة لطراز/i });
    fireEvent.click(addDefButton);

    await waitFor(() => {
      expect(screen.getByText('تعريف فترة صيانة لطراز مولد')).toBeInTheDocument();
      expect(screen.getByText('طراز المولد (مثال: Perkins 4012 أو QSK60)')).toBeInTheDocument();
    });
  });

  test('opens add-item dialog when "إضافة بند صيانة" button is clicked', async () => {
    client.get.mockImplementation(async (url) => {
      if (url.includes('/api/service-interval-definitions')) {
        return { data: mockDefs };
      }
      if (url.includes('/api/service-items')) {
        return { data: mockItems };
      }
      return { data: [] };
    });

    render(
      <BrowserRouter>
        <ServiceDefinitionsAdmin lang="ar" />
      </BrowserRouter>
    );

    await screen.findByRole('cell', { name: 'PERKINS 4012' });

    const addItemButton = screen.getByRole('button', { name: /إضافة بند صيانة/i });
    fireEvent.click(addItemButton);

    await waitFor(() => {
      expect(screen.getByText('إضافة بند صيانة جديد')).toBeInTheDocument();
      expect(screen.getByText('رمز البند (إنجليزي فريد، مثال: CoolantFilter)')).toBeInTheDocument();
    });
  });
});
