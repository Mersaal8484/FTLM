import { useCallback, useEffect } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import client from '../api/client';
import connection from '../socket';

/**
 * useDashboardData — TanStack Query hook for Dashboard page
 *
 * Features:
 *  - Fetches /api/reports/dashboard-stats with filter params
 *  - Auto-refetches every 60 seconds (staleTime = 30s)
 *  - Listens to SignalR events and invalidates cache on relevant backend events
 *  - Fetches /api/analytics/insights separately, also cached
 *
 * @param {object} filter  — { branchId, stationId, subStationId, dateFrom, dateTo }
 * @returns {{ data, stats, comparisons, trends, loading, error, refresh, insights, insightsLoading }}
 */
export function useDashboardData(filter = {}) {
  const queryClient = useQueryClient();

  // Build query params string for cache key + request
  const buildParams = useCallback(() => {
    const params = new URLSearchParams();
    if (filter.branchId)     params.set('branchId',     filter.branchId);
    if (filter.stationId)    params.set('stationId',    filter.stationId);
    if (filter.subStationId) params.set('subStationId', filter.subStationId);
    if (filter.dateFrom)     params.set('dateFrom',     filter.dateFrom);
    if (filter.dateTo)       params.set('dateTo',       filter.dateTo);
    return params.toString();
  }, [filter.branchId, filter.stationId, filter.subStationId, filter.dateFrom, filter.dateTo]);

  // ── Main dashboard stats query ──────────────────────────────────────────
  const queryKey = ['dashboard-stats', buildParams()];

  const {
    data: rawData,
    isLoading: loading,
    error,
    refetch,
  } = useQuery({
    queryKey,
    queryFn: async () => {
      const res = await client.get(`/api/reports/dashboard-stats?${buildParams()}`);
      return res.data;
    },
    staleTime: 30_000,          // 30 s — treat as fresh
    refetchInterval: 60_000,    // background refetch every 60 s
    retry: 2,
    refetchOnWindowFocus: true,
  });

  // ── Analytics insights query ────────────────────────────────────────────
  const insightsKey = ['dashboard-insights', filter.subStationId];

  const {
    data: insightsRaw,
    isLoading: insightsLoading,
    refetch: refetchInsights,
  } = useQuery({
    queryKey: insightsKey,
    queryFn: async () => {
      const params = { days: 90 };
      if (filter.subStationId) params.subStationId = filter.subStationId;
      const res = await client.get('/api/analytics/insights', { params });
      return Array.isArray(res.data?.insights) ? res.data.insights : [];
    },
    staleTime: 5 * 60_000,     // 5 min
    retry: 1,
  });

  // ── SignalR — invalidate cache on relevant events ───────────────────────
  const invalidate = useCallback(() => {
    queryClient.invalidateQueries({ queryKey: ['dashboard-stats'] });
  }, [queryClient]);

  useEffect(() => {
    const refreshEvents = [
      'ReadingCreated', 'ReadingApproved', 'ReadingRejected',
      'FuelMovementCreated', 'FuelMovementApproved',
      'InventoryCreated', 'InventoryApproved',
      'GeneratorCreated', 'GeneratorDeleted',
      'StationCreated', 'StationUpdated', 'StationDeleted',
      'SubStationCreated', 'SubStationUpdated', 'SubStationDeleted',
      'FuelTankUpdated',
    ];
    refreshEvents.forEach(evt => connection.on(evt, invalidate));
    return () => { refreshEvents.forEach(evt => connection.off(evt, invalidate)); };
  }, [invalidate]);

  const refresh = useCallback(() => {
    refetch();
    refetchInsights();
  }, [refetch, refetchInsights]);

  return {
    data: rawData ?? null,
    stats:        rawData?.stats ?? {},
    comparisons:  rawData?.comparisons ?? [],
    trends:       rawData?.trends ?? { trendDaily: [], trendStation: [] },
    loading,
    error: error ? (error?.response?.data?.title || error.message || 'Error loading dashboard') : null,
    refresh,
    insights:        insightsRaw ?? [],
    insightsLoading,
  };
}

export default useDashboardData;
