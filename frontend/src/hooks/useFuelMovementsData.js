import { useCallback } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import client from '../api/client';

const MOVEMENTS_KEY = ['fuel-movements'];
const REFS_KEY      = ['fuel-movements-refs'];   // tanks, oils, stations, sub-stations, transfer-destinations

/**
 * useFuelMovementsData — TanStack Query hook for FuelMovements page
 *
 * Features:
 *  - Fetches movements list + reference data (tanks, oils, stations, sub-stations, transfer dests)
 *  - Exposes create / update / approve / reject mutations with automatic cache invalidation
 *  - All mutations optimistically invalidate the movements cache
 *
 * @returns {{
 *   movements, loading, error,
 *   refs: { tanks, oils, stations, subStations, transferDests },
 *   refsLoading,
 *   createMovement, updateMovement, approveMovement, rejectMovement,
 *   isSaving, refetch
 * }}
 */
export function useFuelMovementsData() {
  const queryClient = useQueryClient();

  // ── Movements list ──────────────────────────────────────────────────────
  const {
    data: movements,
    isLoading: loading,
    error: rawError,
    refetch,
  } = useQuery({
    queryKey: MOVEMENTS_KEY,
    queryFn: async () => {
      const res = await client.get('/api/fuel-movements');
      return res.data?.items ?? res.data ?? [];
    },
    staleTime: 15_000,
    retry: 2,
  });

  // ── Reference data (lookups for form selects) ───────────────────────────
  const { data: refs, isLoading: refsLoading } = useQuery({
    queryKey: REFS_KEY,
    queryFn: async () => {
      const [movRes, tankRes, oilRes, statRes, subStatRes, destRes] = await Promise.allSettled([
        // Movements already fetched above; skip here — we only need refs
        Promise.resolve(null),
        client.get('/api/fuel-tanks'),
        client.get('/api/oils'),
        client.get('/api/stations'),
        client.get('/api/sub-stations'),
        client.get('/api/fuel-movements/transfer-destinations'),
      ]);

      const ok = (r, transform) =>
        r.status === 'fulfilled' && r.value ? transform(r.value.data) : [];

      const destRaw = destRes.status === 'fulfilled' && destRes.value
        ? destRes.value.data
        : [];

      return {
        tanks:         ok(tankRes,    d => Array.isArray(d) ? d : []),
        oils:          ok(oilRes,     d => Array.isArray(d) ? d : []),
        stations:      ok(statRes,    d => Array.isArray(d) ? d : []),
        subStations:   ok(subStatRes, d => (Array.isArray(d) ? d : []).filter(s => s.isActive !== false)),
        transferDests: Array.isArray(destRaw) ? destRaw : (destRaw?.data ?? []),
      };
    },
    staleTime: 5 * 60_000,    // refs rarely change
    retry: 1,
  });

  // ── Invalidate helper ───────────────────────────────────────────────────
  const invalidate = useCallback(() => {
    queryClient.invalidateQueries({ queryKey: MOVEMENTS_KEY });
  }, [queryClient]);

  // ── Mutations ────────────────────────────────────────────────────────────
  const createMovement = useMutation({
    mutationFn: (payload) => client.post('/api/fuel-movements', payload).then(r => r.data),
    onSuccess: invalidate,
  });

  const updateMovement = useMutation({
    mutationFn: ({ id, payload }) => client.put(`/api/fuel-movements/${id}`, payload).then(r => r.data),
    onSuccess: invalidate,
  });

  const approveMovement = useMutation({
    mutationFn: (id) => client.post(`/api/fuel-movements/${id}/approve`),
    onSuccess: invalidate,
  });

  const rejectMovement = useMutation({
    mutationFn: (id) => client.post(`/api/fuel-movements/${id}/reject`),
    onSuccess: invalidate,
  });

  return {
    movements: Array.isArray(movements) ? movements : [],
    loading,
    error: rawError
      ? (rawError?.response?.data?.title || rawError.message || 'Error loading movements')
      : null,

    refs: refs ?? { tanks: [], oils: [], stations: [], subStations: [], transferDests: [] },
    refsLoading,

    createMovement,
    updateMovement,
    approveMovement,
    rejectMovement,
    isSaving: createMovement.isPending || updateMovement.isPending,
    refetch,
  };
}

export default useFuelMovementsData;
