import { useCallback } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import client from '../api/client';

const INVENTORY_KEY = ['periodic-inventories'];
const INVENTORY_REFS_KEY = ['periodic-inventories-refs'];

/**
 * usePeriodicInventoryData — TanStack Query hook for PeriodicInventoryPage
 *
 * Features:
 *  - Fetches periodic inventories list + reference data (stations, sub-stations, tanks, oils, periodTypes)
 *  - Exposes mutations for creating, updating status (review/submit/approve) with auto cache invalidation
 *
 * @returns {{
 *   inventories, loading, error,
 *   refs: { stations, subStations, tanks, oils, periodTypes },
 *   refsLoading,
 *   createInventory, updateStatus,
 *   isSaving, refetch
 * }}
 */
export function usePeriodicInventoryData() {
  const queryClient = useQueryClient();

  // ── Inventory list ──────────────────────────────────────────────────────
  const {
    data: inventories,
    isLoading: loading,
    error: rawError,
    refetch,
  } = useQuery({
    queryKey: INVENTORY_KEY,
    queryFn: async () => {
      const res = await client.get('/api/inventory');
      return res.data?.data || res.data?.items || (Array.isArray(res.data) ? res.data : []);
    },
    staleTime: 15_000,
    retry: 2,
  });

  // ── Reference data ──────────────────────────────────────────────────────
  const { data: refs, isLoading: refsLoading } = useQuery({
    queryKey: INVENTORY_REFS_KEY,
    queryFn: async () => {
      const [statRes, subRes, tankRes, oilRes, ptRes] = await Promise.allSettled([
        client.get('/api/stations'),
        client.get('/api/sub-stations'),
        client.get('/api/fuel-tanks'),
        client.get('/api/oils'),
        client.get('/api/lookups/by-category/PeriodType'),
      ]);

      const ok = (r, transform = (d) => d) =>
        r.status === 'fulfilled' && r.value ? transform(r.value.data) : [];

      return {
        stations: ok(statRes, (d) => (Array.isArray(d) ? d : d?.items || [])),
        subStations: ok(subRes, (d) => {
          const list = Array.isArray(d) ? d : d?.items || [];
          return list.filter((s) => s.isActive !== false);
        }),
        tanks: ok(tankRes, (d) => (Array.isArray(d) ? d : d?.items || [])),
        oils: ok(oilRes, (d) => (Array.isArray(d) ? d : d?.items || [])),
        periodTypes: ok(ptRes, (d) => (Array.isArray(d) ? d : d?.items || [])),
      };
    },
    staleTime: 5 * 60_000, // 5 min
    retry: 1,
  });

  // ── Mutations ───────────────────────────────────────────────────────────
  const createMutation = useMutation({
    mutationFn: async (payload) => {
      const res = await client.post('/api/inventory', payload);
      return res.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: INVENTORY_KEY });
    },
  });

  const updateStatusMutation = useMutation({
    mutationFn: async ({ id, payload }) => {
      const res = await client.put(`/api/inventory/${id}`, payload);
      return res.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: INVENTORY_KEY });
    },
  });

  const createInventory = useCallback(
    async (payload) => createMutation.mutateAsync(payload),
    [createMutation]
  );

  const updateStatus = useCallback(
    async (id, payload) => updateStatusMutation.mutateAsync({ id, payload }),
    [updateStatusMutation]
  );

  return {
    inventories: inventories ?? [],
    loading,
    error: rawError ? (rawError?.response?.data?.message || rawError.message || 'Error loading inventories') : null,
    refs: refs ?? { stations: [], subStations: [], tanks: [], oils: [], periodTypes: [] },
    refsLoading,
    createInventory,
    updateStatus,
    isSaving: createMutation.isPending || updateStatusMutation.isPending,
    refetch,
  };
}

export default usePeriodicInventoryData;
