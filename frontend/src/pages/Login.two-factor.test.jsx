import { beforeEach, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import Login from './Login';
import client from '../api/client';

vi.mock('../api/client', () => ({ default: { post: vi.fn(), get: vi.fn() } }));
vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: key => key }) }));

beforeEach(() => vi.clearAllMocks());

it('waits for the second factor before requesting the authenticated profile', async () => {
  const user = userEvent.setup();
  const onLogin = vi.fn();
  client.post.mockResolvedValueOnce({ data: { requiresTwoFactor: true, tempToken: 'challenge' } });
  client.post.mockResolvedValueOnce({ data: {} });
  client.get.mockResolvedValue({ data: { name: 'Admin' } });
  const { container } = render(<MemoryRouter><Login onLogin={onLogin} /></MemoryRouter>);
  await user.type(container.querySelector('input[type="email"]'), 'admin@example.test');
  await user.type(container.querySelector('input[type="password"]'), 'Password-123!');
  await user.click(screen.getByRole('button', { name: 'common.login' }));
  const code = await screen.findByLabelText('auth.twoFactorCode');
  expect(client.get).not.toHaveBeenCalled();
  await user.type(code, '123456');
  await user.click(screen.getByRole('button', { name: 'common.login' }));
  await waitFor(() => expect(onLogin).toHaveBeenCalledWith({ name: 'Admin' }));
  expect(client.post).toHaveBeenLastCalledWith('/api/auth/login/2fa', { tempToken: 'challenge', code: '123456' });
});
