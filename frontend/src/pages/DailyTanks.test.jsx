import React from 'react';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { describe, test, expect, vi, beforeEach, afterEach } from 'vitest';
import DailyTanks from './DailyTanks';

vi.mock('../api/client', () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    delete: vi.fn(),
  },
}));

vi.mock('../hooks/useCommercialMode', () => ({
  default: vi.fn(),
}));

import client from '../api/client';
import useCommercialMode from '../hooks/useCommercialMode';

describe('DailyTanks', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    useCommercialMode.mockReturnValue({ isCommercialMode: true, modeResolved: true });
  });

  afterEach(() => {
    vi.resetAllMocks();
  });

  test('generator is optional when creating daily tank in commercial mode', async () => {
    client.get.mockImplementation(async (url) => {
      if (url === '/api/fuel-tanks') return { data: [] };
      if (url === '/api/generators') return { data: [] };
      if (url.startsWith('/api/lookups')) return { data: [{ id: 1, name: 'ديزل' }] };
      return { data: [] };
    });
    client.post.mockResolvedValue({ data: { id: 101, name: 'Unlinked Daily Tank' } });

    render(<DailyTanks lang="en" currentUser={{ role: 'admin' }} />);

    // Click Add Daily Tank button
    const addBtn = await screen.findByRole('button', { name: /Add Daily Tank/i });
    fireEvent.click(addBtn);

    // Fill form without selecting a generator
    const nameInput = screen.getByPlaceholderText('e.g. Gen 1 Daily Tank');
    fireEvent.change(nameInput, { target: { value: 'Unlinked Daily Tank' } });

    const capInput = screen.getByPlaceholderText('1000');
    fireEvent.change(capInput, { target: { value: '1500' } });

    // Submit form
    const saveBtn = screen.getByRole('button', { name: /Save Daily Tank/i });
    fireEvent.click(saveBtn);

    await waitFor(() => {
      expect(client.post).toHaveBeenCalledTimes(1);
    });

    const [postUrl, payload] = client.post.mock.calls[0];
    expect(postUrl).toBe('/api/fuel-tanks');
    expect(payload.name).toBe('Unlinked Daily Tank');
    expect(payload.generatorId).toBeNull();
    expect(payload.tankRole).toBe(2);
  });

  test('lists only daily tanks in commercial mode and shows Unlinked for unassigned tanks', async () => {
    client.get.mockImplementation(async (url) => {
      if (url === '/api/fuel-tanks') return { data: [
        { id: 1, name: 'Main Tank', tankRole: 1, capacityLiters: 5000 },
        { id: 2, name: 'Daily Tank', tankRole: 2, generatorId: 10, capacityLiters: 1000 },
        { id: 3, name: 'Unlinked Daily Tank', tankRole: 2, generatorId: null, capacityLiters: 800 },
      ] };
      if (url === '/api/generators') return { data: [{ id: 10, name: 'Gen-01' }] };
      if (url.startsWith('/api/lookups')) return { data: [] };
      return { data: [] };
    });

    render(<DailyTanks lang="en" currentUser={{ role: 'admin' }} />);

    expect(await screen.findByText('Daily Tank')).toBeInTheDocument();
    expect(await screen.findByText('Unlinked Daily Tank')).toBeInTheDocument();
    expect(screen.queryByText('Main Tank')).not.toBeInTheDocument();
    expect(screen.getByText('Unlinked')).toBeInTheDocument();
  });

  test('submitting new daily tank in commercial mode omits subStationId in POST payload', async () => {
    client.get.mockImplementation(async (url) => {
      if (url === '/api/fuel-tanks') return { data: [] };
      if (url === '/api/generators') return { data: [{ id: 10, name: 'Gen-01' }] };
      if (url.startsWith('/api/lookups')) return { data: [{ id: 1, name: 'ديزل' }] };
      return { data: [] };
    });
    client.post.mockResolvedValue({ data: { id: 101, name: 'Daily Tank 1' } });

    render(<DailyTanks lang="en" currentUser={{ role: 'admin' }} />);

    const addBtn = await screen.findByRole('button', { name: /Add Daily Tank/i });
    fireEvent.click(addBtn);

    // Fill form
    const nameInput = screen.getByPlaceholderText('e.g. Gen 1 Daily Tank');
    fireEvent.change(nameInput, { target: { value: 'Daily Tank 1' } });

    const genSelect = screen.getAllByRole('combobox')[0];
    fireEvent.change(genSelect, { target: { value: '10' } });

    const capInput = screen.getByPlaceholderText('1000');
    fireEvent.change(capInput, { target: { value: '1500' } });

    // Submit form
    const saveBtn = screen.getByRole('button', { name: /Save Daily Tank/i });
    fireEvent.click(saveBtn);

    await waitFor(() => {
      expect(client.post).toHaveBeenCalledTimes(1);
    });

    const [postUrl, payload] = client.post.mock.calls[0];
    expect(postUrl).toBe('/api/fuel-tanks');
    expect(payload.name).toBe('Daily Tank 1');
    expect(payload.generatorId).toBe(10);
    expect(payload.capacityLiters).toBe(1500);
    expect(payload.tankRole).toBe(2);
    // Crucial requirement A7: subStationId must NOT be in the commercial payload
    expect(payload).not.toHaveProperty('subStationId');
  });

  test('displays server error message from detail field when creation fails', async () => {
    client.get.mockImplementation(async (url) => {
      if (url === '/api/fuel-tanks') return { data: [] };
      if (url === '/api/generators') return { data: [{ id: 10, name: 'Gen-01' }] };
      if (url.startsWith('/api/lookups')) return { data: [{ id: 1, name: 'ديزل' }] };
      return { data: [] };
    });

    client.post.mockRejectedValue({
      response: {
        status: 409,
        data: {
          title: 'Conflict',
          detail: 'This generator already has a daily tank.',
        },
      },
    });

    render(<DailyTanks lang="en" currentUser={{ role: 'admin' }} />);

    const addBtn = await screen.findByRole('button', { name: /Add Daily Tank/i });
    fireEvent.click(addBtn);

    const nameInput = screen.getByPlaceholderText('e.g. Gen 1 Daily Tank');
    fireEvent.change(nameInput, { target: { value: 'Daily Tank 2' } });

    const genSelect = screen.getAllByRole('combobox')[0];
    fireEvent.change(genSelect, { target: { value: '10' } });

    const capInput = screen.getByPlaceholderText('1000');
    fireEvent.change(capInput, { target: { value: '1500' } });

    const saveBtn = screen.getByRole('button', { name: /Save Daily Tank/i });
    fireEvent.click(saveBtn);

    await waitFor(() => {
      expect(screen.getByText(/This generator already has a daily tank/i)).toBeInTheDocument();
    });
  });

  test('shows non-blocking warning when calibration fails but keeps the tank', async () => {
    client.get.mockImplementation(async (url) => {
      if (url === '/api/fuel-tanks') return { data: [] };
      if (url === '/api/generators') return { data: [{ id: 10, name: 'Gen-01' }] };
      if (url.startsWith('/api/lookups')) return { data: [{ id: 1, name: 'ديزل' }] };
      return { data: [] };
    });

    client.post.mockResolvedValue({ data: { id: 101, name: 'Daily Tank 1' } });
    client.put.mockRejectedValue({
      response: {
        status: 400,
        data: { detail: 'Calibration geometry error' },
      },
    });

    render(<DailyTanks lang="en" currentUser={{ role: 'admin' }} />);

    const addBtn = await screen.findByRole('button', { name: /Add Daily Tank/i });
    fireEvent.click(addBtn);

    const nameInput = screen.getByPlaceholderText('e.g. Gen 1 Daily Tank');
    fireEvent.change(nameInput, { target: { value: 'Daily Tank 1' } });

    const genSelect = screen.getAllByRole('combobox')[0];
    fireEvent.change(genSelect, { target: { value: '10' } });

    const capInput = screen.getByPlaceholderText('1000');
    fireEvent.change(capInput, { target: { value: '1500' } });

    const diameterInput = screen.getByPlaceholderText('200');
    fireEvent.change(diameterInput, { target: { value: '100' } });

    const coneInput = screen.getByPlaceholderText('50');
    fireEvent.change(coneInput, { target: { value: '25' } });

    const saveBtn = screen.getByRole('button', { name: /Save Daily Tank/i });
    fireEvent.click(saveBtn);

    await waitFor(() => {
      expect(screen.getByText(/Tank saved, calibration failed: Calibration geometry error/i)).toBeInTheDocument();
    });
  });
});
