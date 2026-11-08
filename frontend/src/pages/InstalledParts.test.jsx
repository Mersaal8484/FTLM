import { afterEach, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import InstalledParts from './InstalledParts';
import client from '../api/client';

vi.mock('../api/client', () => ({ default: { get: vi.fn(), post: vi.fn() } }));
afterEach(() => vi.resetAllMocks());

it('renders the paginated generator response in filters and the installation form', async () => {
  client.get.mockImplementation(async url => ({ data: ({
    '/api/installed-parts': [],
    '/api/generators': { data: [{ id: 12, name: 'Generator A' }], pagination: { total: 1 } },
    '/api/spare-parts': { items: [] },
    '/api/work-permits': { items: [] },
    '/api/warehouse/dispatch': { items: [] },
  })[url] }));
  render(<InstalledParts lang="en" currentUser={{ role: 'admin' }} />);
  await waitFor(() => expect(screen.getByText('Generator A')).toBeTruthy());
  await userEvent.click(screen.getByRole('button', { name: 'Install Part' }));
  expect(screen.getAllByText('Generator A')).toHaveLength(2);
});

it('shows an error when an API fails instead of silently presenting incomplete data', async () => {
  client.get.mockImplementation(async url => {
    if (url === '/api/generators') throw new Error('unavailable');
    return { data: [] };
  });
  render(<InstalledParts lang="en" currentUser={{ role: 'admin' }} />);
  expect(await screen.findByText('Some data could not be loaded. Please try again.')).toBeTruthy();
});
