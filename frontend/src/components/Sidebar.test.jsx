import React from 'react';
import { render, screen, within } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import i18n from '../i18n';
import Sidebar from './Sidebar';

vi.mock('../api/client', () => ({ default: { get: vi.fn(), post: vi.fn(), patch: vi.fn() } }));
vi.mock('../hooks/useTenantBranding', () => ({ default: () => ({ name: 'FuelTrack', logoUrl: '' }) }));
vi.mock('../store/uiStore', () => ({ default: () => ({ setLang: vi.fn() }) }));
vi.mock('../permissions/usePermissions', () => ({
  default: () => ({ can: () => true, canExplicit: () => true }),
}));

const user = { name: 'Admin', role: 'admin', roles: ['admin'], permissions: {} };
const props = { lang: 'en', theme: 'dark', onToggleTheme: vi.fn(), currentUser: user, unreadCount: 0, onLogout: vi.fn() };

function renderSidebar(path = '/') {
  return render(<MemoryRouter initialEntries={[path]}><Sidebar {...props} /></MemoryRouter>);
}

describe('Sidebar navigation', () => {
  beforeEach(async () => { await i18n.changeLanguage('en'); });

  it('renders the exact top-level order for a full-permission user', () => {
    const view = renderSidebar();
    const labels = [...view.container.querySelectorAll('[data-nav-top]')].map(item => item.getAttribute('data-nav-top'));
    expect(labels).toEqual([
      'Notifications', 'Dashboard', 'Operations', 'Maintenance', 'Fuel & Oil Movements',
      'Generators Overview', 'Fuel Stock', 'Oil Stock', 'Review Readings', 'Periodic Inventory',
      'Reports', 'System Settings',
    ]);
    expect(screen.queryByText('Movement Logs')).not.toBeInTheDocument();
  });

  it('contains exactly seven maintenance children and no warehouse reports', () => {
    renderSidebar('/maintenance/service');
    const group = document.querySelector('[data-nav-group="Maintenance"]');
    const labels = [...group.querySelectorAll('.nav-group-children a span')].map(item => item.textContent);
    expect(labels).toEqual(['Service Intervals', 'Work Permits', 'Parts Catalog', 'Warehouse Stock', 'Installed Parts', 'Dispatch Requests', 'Warehouse Supply']);
    expect(within(group).queryByText('Warehouse Reports')).not.toBeInTheDocument();
    expect(within(group).getByRole('button', { name: 'Maintenance' })).toHaveAttribute('aria-expanded', 'true');
  });

  it('places warehouse reports inside Reports and auto-expands on its route', () => {
    renderSidebar('/warehouse/reports');
    const group = document.querySelector('[data-nav-group="Reports"]');
    expect(within(group).getByText('Warehouse Reports')).toBeInTheDocument();
    expect(within(group).getByRole('button', { name: 'Reports' })).toHaveAttribute('aria-expanded', 'true');
  });
});
