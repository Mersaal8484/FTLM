import React from 'react';
import { render, screen } from '@testing-library/react';
import { describe, test, expect } from 'vitest';
import PageHeader from './PageHeader';

describe('UI PageHeader Component', () => {
  test('renders title and subtitle properly', () => {
    render(
      <PageHeader
        title="Dashboard Overview"
        subtitle="Real-time KPI and monitoring"
      />
    );
    expect(screen.getByRole('heading', { level: 1, name: /Dashboard Overview/i })).toBeInTheDocument();
    expect(screen.getByText(/Real-time KPI and monitoring/i)).toBeInTheDocument();
  });

  test('renders badge when provided', () => {
    render(
      <PageHeader
        title="System Settings"
        badge={{ label: 'Active', color: '#10b981', bg: 'rgba(16,185,129,0.12)' }}
      />
    );
    expect(screen.getByText('Active')).toBeInTheDocument();
  });

  test('renders actions slot at the end side', () => {
    render(
      <PageHeader
        title="Fuel Inventory"
        actions={<button data-testid="action-btn">Export</button>}
      />
    );
    expect(screen.getByTestId('action-btn')).toBeInTheDocument();
  });
});
