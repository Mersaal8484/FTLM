import React from 'react';
import { render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, test, vi } from 'vitest';
import i18n from '../i18n';

vi.mock('../api/client', () => ({ default: { get: vi.fn(), post: vi.fn() } }));
import client from '../api/client';
import StocktakePage from './StocktakePage';

describe('StocktakePage', () => {
  beforeEach(async () => {
    await i18n.changeLanguage('ar');
    client.get.mockReset();
    client.get.mockImplementation((url) => Promise.resolve({ data: url === '/api/stocktakes/draft'
      ? { fuelLines: [], oilLines: [] }
      : { items: [] } }));
  });

  test('renders the commercial physical stocktake form with its required photo control', async () => {
    render(<StocktakePage lang="ar" />);

    expect(await screen.findByRole('heading', { name: 'الجرد الفعلي' })).toBeInTheDocument();
    expect(screen.getByLabelText(/صورة مطلوبة/i)).toHaveAttribute('accept', 'image/jpeg,image/webp');
  });
});
