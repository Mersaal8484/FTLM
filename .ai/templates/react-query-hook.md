# Template: React Query Hook

## Metadata
- **Name**: react-query-hook
- **Purpose**: Custom React Query hook for API operations
- **Layer**: Frontend
- **Path**: `frontend/src/api/hooks/use{Entity}.js`

## Template

```javascript
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import client from '../client';

// Query Keys
export const {entity}Keys = {
  all: ['{entities}'],
  lists: () => [...{entity}Keys.all, 'list'],
  list: (params) => [...{entity}Keys.lists(), params],
  details: () => [...{entity}Keys.all, 'detail'],
  detail: (id) => [...{entity}Keys.details(), id],
};

// List Query
export const use{Entities} = (params = {}) => {
  return useQuery({
    queryKey: {entity}Keys.list(params),
    queryFn: async () => {
      const response = await client.get('/api/{entities}', { params });
      return response.data;
    },
  });
};

// Detail Query
export const use{Entity} = (id) => {
  return useQuery({
    queryKey: {entity}Keys.detail(id),
    queryFn: async () => {
      const response = await client.get(`/api/{entities}/${id}`);
      return response.data;
    },
    enabled: !!id,
  });
};

// Create Mutation
export const useCreate{Entity} = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (data) => {
      const response = await client.post('/api/{entities}', data);
      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: {entity}Keys.lists() });
    },
  });
};

// Update Mutation
export const useUpdate{Entity} = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ id, data }) => {
      const response = await client.put(`/api/{entities}/${id}`, data);
      return response.data;
    },
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: {entity}Keys.detail(id) });
      queryClient.invalidateQueries({ queryKey: {entity}Keys.lists() });
    },
  });
};

// Delete Mutation
export const useDelete{Entity} = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (id) => {
      await client.delete(`/api/{entities}/${id}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: {entity}Keys.lists() });
    },
  });
};
```

## Checklist
- [ ] Query keys follow hierarchical pattern
- [ ] All mutations invalidate related queries
- [ ] `enabled` option on detail queries (skip when no id)
- [ ] Uses `client` from `../client`
- [ ] Error handling in mutation callbacks
- [ ] Optimistic updates considered for frequently accessed data
