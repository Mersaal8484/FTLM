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
  default: () => ({ can: () => true, canExplicit: () => true, hasAssignedPermission: () => false }),
}));
vi.mock('../hooks/useOperationalMode', () => ({ default: () => ({ isNewMode: false, modeResolved: true }) }));

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
    const labels = within(group.querySelector('.nav-group-children')).getAllByRole('link').map(link => link.textContent);
    expect(labels).toEqual(['Service Intervals', 'Work Permits', 'Parts Catalog', 'Warehouse Stock', 'Installed Parts', 'Dispatch Requests', 'Warehouse Supply']);
    expect(within(group).queryByText('Warehouse Reports')).not.toBeInTheDocument();
    expect(within(group).getByRole('button', { name: 'Maintenance' })).toHaveAttribute('aria-expanded', 'true');
  });

  it('renders reports as one top-level destination without nested report links', () => {
    renderSidebar('/reports/warehouse');
    const reportsLink = screen.getByRole('link', { name: 'Reports' });
    expect(reportsLink).toHaveAttribute('href', '/reports');
    expect(reportsLink).toHaveClass('active');
    expect(document.querySelector('[data-nav-group="Reports"]')).not.toBeInTheDocument();
    expect(screen.queryByText('Warehouse Reports')).not.toBeInTheDocument();
  });
});
