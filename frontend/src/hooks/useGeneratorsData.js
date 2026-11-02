import { useCallback } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import client from '../api/client';

const QUERY_KEY = ['generators'];
const LOOKUPS_KEY = ['generators-lookups'];

/**
 * useGeneratorsData — TanStack Query hook for Generators page
 *
 * Features:
 *  - Fetches generators list + all required lookups (stations, sub-stations, types, etc.)
 *  - Exposes add / update / delete mutations with automatic cache invalidation
 *  - Mutations return the saved generator so callers can update local state if needed
 *
 * @returns {{
 *   generators, loading, error,
 *   lookups: { stations, subStations, branches, genTypes, voltages, frequencies, ownershipTypes, statuses, controllers },
 *   lookupsLoading,
 *   addGenerator, updateGenerator, deleteGenerator,
 *   isSaving, isDeleting,
 *   refetch
 * }}
 */
export function useGeneratorsData({ canAddOrEdit = false } = {}) {
  const queryClient = useQueryClient();

  // ── Generators list ──────────────────────────────────────────────────────
  const {
    data: generators,
    isLoading: loading,
    error: rawError,
    refetch,
  } = useQuery({
    queryKey: QUERY_KEY,
    queryFn: async () => {
      const res = await client.get('/api/generators');
      const d = res.data;
      return Array.isArray(d?.data) ? d.data : Array.isArray(d) ? d : [];
    },
    staleTime: 30_000,
    retry: 2,
  });

  // ── Lookups — fetched once, long cache ───────────────────────────────────
  const { data: lookups, isLoading: lookupsLoading } = useQuery({
    queryKey: [...LOOKUPS_KEY, canAddOrEdit],
    queryFn: async () => {
      const requests = [
        client.get('/api/stations'),
        client.get('/api/sub-stations'),
        client.get('/api/lookups/by-category/GeneratorType'),
        client.get('/api/lookups/by-category/Voltage'),
        client.get('/api/lookups/by-category/Frequency'),
        client.get('/api/lookups/by-category/OwnershipType'),
        client.get('/api/lookups/by-category/GeneratorStatus'),
        client.get('/api/branches'),
      ];
      if (canAddOrEdit) {
        requests.push(client.get('/api/controllers'));
      }

      const results = await Promise.allSettled(requests);
      const ok = (r) => r.status === 'fulfilled' && Array.isArray(r.value.data);

      const [stRes, subRes, genTypeRes, voltRes, freqRes, ownRes, statRes, brRes, ctrlRes] = results;

      return {
        stations:       ok(stRes)      ? stRes.value.data.filter(s => s.isActive !== false) : [],
        subStations:    ok(subRes)     ? subRes.value.data.filter(s => s.isActive !== false) : [],
        genTypes:       ok(genTypeRes) ? genTypeRes.value.data : [],
        voltages:       ok(voltRes)    ? voltRes.value.data : [],
        frequencies:    ok(freqRes)    ? freqRes.value.data : [],
        ownershipTypes: ok(ownRes)     ? ownRes.value.data : [],
        statuses:       ok(statRes)    ? statRes.value.data : [],
        branches:       ok(brRes)      ? brRes.value.data : [],
        controllers:    ctrlRes?.status === 'fulfilled'
                          ? (Array.isArray(ctrlRes.value.data) ? ctrlRes.value.data : (ctrlRes.value.data?.controllers ?? []))
                          : [],
      };
    },
    staleTime: 5 * 60_000,   // lookups rarely change — 5 min cache
    retry: 1,
  });

  // ── Mutations ────────────────────────────────────────────────────────────
  const invalidate = useCallback(() => {
    queryClient.invalidateQueries({ queryKey: QUERY_KEY });
  }, [queryClient]);

  const addGenerator = useMutation({
    mutationFn: (payload) => client.post('/api/generators', payload).then(r => r.data),
    onSuccess: invalidate,
  });

  const updateGenerator = useMutation({
    mutationFn: ({ id, payload }) => client.put(`/api/generators/${id}`, payload).then(r => r.data),
    onSuccess: invalidate,
  });

  const deleteGenerator = useMutation({
    mutationFn: (id) => client.delete(`/api/generators/${id}`),
    onSuccess: invalidate,
  });

  return {
    generators: generators ?? [],
    loading,
    error: rawError ? (rawError?.response?.data?.title || rawError.message || 'Error') : null,
    lookups: lookups ?? {
      stations: [], subStations: [], branches: [], genTypes: [],
      voltages: [], frequencies: [], ownershipTypes: [], statuses: [], controllers: [],
    },
    lookupsLoading,
    addGenerator,
    updateGenerator,
    deleteGenerator,
    isSaving: addGenerator.isPending || updateGenerator.isPending,
    isDeleting: deleteGenerator.isPending,
    refetch,
  };
}

export default useGeneratorsData;
