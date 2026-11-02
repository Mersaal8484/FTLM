import { useQuery, useQueryClient } from '@tanstack/react-query';
import client from './client';

export const stockSummaryKeys = {
  all: ['stockSummary'],
  fuel: (from, to) => [...stockSummaryKeys.all, 'fuel', from, to],
  oil: (from, to) => [...stockSummaryKeys.all, 'oil', from, to],
};

export const useFuelStockSummary = (from, to) =>
  useQuery({
    queryKey: stockSummaryKeys.fuel(from, to),
    queryFn: () =>
      client
        .get('/api/fuel-movements/fuel-stock-summary', { params: { from, to } })
        .then((r) => r.data),
    enabled: !!from && !!to,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });

export const useOilStockSummary = (from, to) =>
  useQuery({
    queryKey: stockSummaryKeys.oil(from, to),
    queryFn: () =>
      client
        .get('/api/fuel-movements/oil-stock-summary', { params: { from, to } })
        .then((r) => r.data),
    enabled: !!from && !!to,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
