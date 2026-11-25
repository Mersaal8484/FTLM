import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, test, expect, vi, beforeEach } from 'vitest';
import Generators from './Generators';

vi.mock('../hooks/useCommercialMode', () => ({
  default: () => ({ isCommercialMode: true, modeResolved: true }),
}));

vi.mock('../api/client', () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    patch: vi.fn(),
    delete: vi.fn(),
  },
}));

import client from '../api/client';

describe('Generators Commercial Daily Tank', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  test('form shows daily tank dropdown and no tank calibration settings in commercial mode', async () => {
    client.get.mockImplementation(async (url) => {
      if (url === '/api/generators') return { data: [] };
      if (url === '/api/fuel-tanks') {
        return {
          data: [
            { id: 201, name: 'Available DT', code: 'DT-201', tankRole: 2, isActive: true, generatorId: null },
            { id: 202, name: 'Taken DT', code: 'DT-202', tankRole: 2, isActive: true, generatorId: 99 },
          ],
        };
      }
      return { data: [] };
    });

    render(
      <MemoryRouter>
        <Generators lang="en" t={{ generators: 'Generators', manageGenerators: 'Manage', noData: 'No data' }} currentUser={{ role: 'admin' }} />
      </MemoryRouter>
    );

    // Click Add Generator
    const addBtn = await screen.findByRole('button', { name: /Add Generator/i });
    fireEvent.click(addBtn);

    // Modal opens
    expect(await screen.findByText('Daily Tank *')).toBeInTheDocument();

    // Verify calibration and shape fields are NOT shown
    expect(screen.queryByText(/Calibration Mode/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/Cylinder Height/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/Cone Height/i)).not.toBeInTheDocument();

    // Verify only the unlinked daily tank is in the dropdown
    const select = screen.getByRole('combobox');
    expect(select).toBeInTheDocument();
    expect(screen.getByText(/Available DT \(DT-201\)/i)).toBeInTheDocument();
    expect(screen.queryByText(/Taken DT \(DT-202\)/i)).not.toBeInTheDocument();
  });

  test('shows empty state warning and blocks submit when no daily tank is available', async () => {
    client.get.mockImplementation(async (url) => {
      if (url === '/api/generators') return { data: [] };
      if (url === '/api/fuel-tanks') return { data: [] }; // No tanks
      return { data: [] };
    });

    render(
      <MemoryRouter>
        <Generators lang="en" t={{ generators: 'Generators', manageGenerators: 'Manage', noData: 'No data' }} currentUser={{ role: 'admin' }} />
      </MemoryRouter>
    );

    const addBtn = await screen.findByRole('button', { name: /Add Generator/i });
    fireEvent.click(addBtn);

    // Warning alert is visible
    expect(await screen.findByText(/No available daily tank. Create one in/i)).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /Daily Tanks settings/i })).toHaveAttribute('href', '/commercial/daily-tanks');

    // Submit button is disabled
    const saveBtn = screen.getByRole('button', { name: /Add Generator/i });
    expect(saveBtn).toBeDisabled();
  });

  test('submitting generator sends DailyTankId in payload', async () => {
    client.get.mockImplementation(async (url) => {
      if (url === '/api/generators') return { data: [] };
      if (url === '/api/fuel-tanks') {
        return {
          data: [
            { id: 201, name: 'DT-01', code: 'DT-01', tankRole: 2, isActive: true, generatorId: null },
          ],
        };
      }
      return { data: [] };
    });
    client.post.mockResolvedValue({ data: { id: 10, name: 'Generator 1' } });

    render(
      <MemoryRouter>
        <Generators lang="en" t={{ generators: 'Generators', manageGenerators: 'Manage', noData: 'No data' }} currentUser={{ role: 'admin' }} />
      </MemoryRouter>
    );

    const addBtn = await screen.findByRole('button', { name: /Add Generator/i });
    fireEvent.click(addBtn);

    // Fill Generator Name
    const nameInput = screen.getAllByRole('textbox')[0];
    fireEvent.change(nameInput, { target: { value: 'Generator 1' } });

    // Select Daily Tank
    const tankSelect = screen.getByRole('combobox');
    fireEvent.change(tankSelect, { target: { value: '201' } });

    // Submit form
    const saveBtn = screen.getByRole('button', { name: /Add Generator/i });
    expect(saveBtn).not.toBeDisabled();
    fireEvent.click(saveBtn);

    await waitFor(() => {
      expect(client.post).toHaveBeenCalledTimes(1);
    });

    const [url, payload] = client.post.mock.calls[0];
    expect(url).toBe('/api/generators');
    expect(payload.Name).toBe('Generator 1');
    expect(payload.DailyTankId).toBe(201);
    expect(payload).not.toHaveProperty('DailyTank');
  });
});
