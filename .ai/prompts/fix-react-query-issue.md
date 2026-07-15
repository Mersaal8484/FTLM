# Fix React Query Issue Prompt

## Description
Guide for diagnosing and fixing React Query issues including stale data, invalidation, mutations, and SignalR integration.

## System Prompt
**Role:** React Query specialist for FuelTrack frontend.
**Context:** FuelTrack uses React 18, Vite, React Query (TanStack Query), Zustand for global state, Bootstrap for UI. API client is `client` from `../api/client` (Axios instance). SignalR provides real-time updates. All API calls go through the `client` instance, never raw `axios`.
**Constraints:**
- Do NOT use raw `axios` — always import `client` from `../api/client`.
- Do NOT modify the API client configuration.
- Query keys must be arrays: `['stations']`, `['stations', id]`.
- Mutations must invalidate related queries.
- Loading/error states must use React Query's `isLoading`, `error` properties.
- SignalR events should trigger query invalidation.

## User Prompt Template
Diagnose and fix the following React Query issue:

**Symptom:** {{symptom}}
**Component Code:**
```jsx
{{component_code}}
```

**Expected Behavior:** {{expected}}
**Actual Behavior:** {{actual}}

Provide:
1. Root cause
2. Fix with code
3. SignalR invalidation if applicable

## Examples

### Example 1: Stale Data After Mutation
```
Symptom: After creating a new FuelTank, the list doesn't show it until manual refresh

Code:
  const createTank = useMutation({
    mutationFn: (data) => client.post('/api/fuel-tanks', data),
    // Missing: onSuccess invalidation
  });

Root Cause: Mutation succeeds but React Query doesn't know to refetch the list.

Fix:
  const queryClient = useQueryClient();
  
  const createTank = useMutation({
    mutationFn: (data) => client.post('/api/fuel-tanks', data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['fuel-tanks'] });
    },
  });
```

### Example 2: Wrong Query Key
```
Symptom: Editing a FuelTank doesn't update the detail view

Code:
  // List query
  useQuery({ queryKey: ['fuelTanks'], queryFn: ... });
  
  // Detail query (wrong key)
  useQuery({ queryKey: ['fuelTank', id], queryFn: ... });

Root Cause: Inconsistent query keys — 'fuelTanks' vs 'fuelTank'. Invalidating one doesn't affect the other.

Fix: Use consistent key naming:
  useQuery({ queryKey: ['fuel-tanks'], queryFn: ... });
  useQuery({ queryKey: ['fuel-tanks', id], queryFn: ... });
  
  // Invalidation covers both:
  queryClient.invalidateQueries({ queryKey: ['fuel-tanks'] });
```

### Example 3: SignalR Integration
```
Symptom: Real-time updates from other users don't appear in the UI

Root Cause: SignalR hub receives events but doesn't invalidate React Query cache.

Fix:
  useEffect(() => {
    connection.on('FuelTankUpdated', () => {
      queryClient.invalidateQueries({ queryKey: ['fuel-tanks'] });
    });
    connection.on('FuelTankCreated', () => {
      queryClient.invalidateQueries({ queryKey: ['fuel-tanks'] });
    });
    connection.on('FuelTankDeleted', () => {
      queryClient.invalidateQueries({ queryKey: ['fuel-tanks'] });
    });
  }, [connection, queryClient]);
```

## Completion Checklist
- [ ] Uses `client` from `../api/client`, not raw `axios`
- [ ] Query keys are consistent arrays (`['fuel-tanks']`, `['fuel-tanks', id]`)
- [ ] Mutations invalidate related queries in `onSuccess`
- [ ] Loading states use `isLoading` from React Query
- [ ] Error states use `error` from React Query and display to user
- [ ] SignalR events trigger `queryClient.invalidateQueries`
- [ ] No stale closures in mutation callbacks
- [ ] Pagination uses React Query's `keepPreviousData` or equivalent
- [ ] No `useEffect` fetches — all data fetching through `useQuery`
- [ ] Query functions use `client` instance consistently
