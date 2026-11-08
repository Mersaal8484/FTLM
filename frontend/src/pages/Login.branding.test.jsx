import { afterEach, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import Login from './Login';

vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: key => key }) }));
afterEach(() => vi.unstubAllGlobals());

it('shows the current tenant name and logo on the login page', async () => {
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: true, json: async () => ({ name: 'شركة المثال', logoUrl: '/api/tenant-branding/logo' }) }));
  render(<MemoryRouter><Login /></MemoryRouter>);
  expect(await screen.findByRole('heading', { name: 'شركة المثال' })).toBeTruthy();
  expect(screen.getByRole('img', { name: 'شركة المثال' }).getAttribute('src')).toBe('/api/tenant-branding/logo');
});
