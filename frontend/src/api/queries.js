import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import client from './client';

// ── Query Key Factories ──

export const stationKeys = {
  all: ['stations'],
  lists: () => [...stationKeys.all, 'list'],
  detail: (id) => [...stationKeys.all, 'detail', id],
};

export const subStationKeys = {
  all: ['substations'],
  lists: () => [...subStationKeys.all, 'list'],
  detail: (id) => [...subStationKeys.all, 'detail', id],
};

export const generatorKeys = {
  all: ['generators'],
  lists: () => [...generatorKeys.all, 'list'],
  detail: (id) => [...generatorKeys.all, 'detail', id],
};

export const userKeys = {
  all: ['users'],
  lists: () => [...userKeys.all, 'list'],
  detail: (id) => [...userKeys.all, 'detail', id],
};

export const roleKeys = {
  all: ['roles'],
  lists: () => [...roleKeys.all, 'list'],
};

export const fuelTankKeys = {
  all: ['fuelTanks'],
  lists: () => [...fuelTankKeys.all, 'list'],
};

export const controllerKeys = {
  all: ['controllers'],
  lists: () => [...controllerKeys.all, 'list'],
};

export const meterKeys = {
  all: ['meters'],
  lists: () => [...meterKeys.all, 'list'],
};

export const branchKeys = {
  all: ['branches'],
  lists: () => [...branchKeys.all, 'list'],
};

export const fuelMovementKeys = {
  all: ['fuelMovements'],
  lists: () => [...fuelMovementKeys.all, 'list'],
};

export const oilKeys = {
  all: ['oils'],
  lists: () => [...oilKeys.all, 'list'],
};

export const auditKeys = {
  all: ['audits'],
  lists: () => [...auditKeys.all, 'list'],
};

export const lookupKeys = {
  all: ['lookups'],
  byCategory: (cat) => [...lookupKeys.all, cat],
};

// ── Utility: normalize array from various response shapes ──
const toArray = (data) => {
  if (Array.isArray(data)) return data;
  if (Array.isArray(data?.data)) return data.data;
  if (Array.isArray(data?.items)) return data.items;
  return [];
};

// ── Entity Queries ──

export const useStations = () =>
  useQuery({
    queryKey: stationKeys.lists(),
    queryFn: () => client.get('/api/stations').then((r) => r.data),
    select: toArray,
  });

export const useSubStations = () =>
  useQuery({
    queryKey: subStationKeys.lists(),
    queryFn: () => client.get('/api/sub-stations').then((r) => r.data),
    select: toArray,
  });

export const useGenerators = () =>
  useQuery({
    queryKey: generatorKeys.lists(),
    queryFn: () => client.get('/api/generators').then((r) => r.data),
    select: toArray,
  });

export const useUsers = () =>
  useQuery({
    queryKey: userKeys.lists(),
    queryFn: () => client.get('/api/users').then((r) => r.data),
    select: toArray,
  });

export const useRoles = () =>
  useQuery({
    queryKey: roleKeys.lists(),
    queryFn: () => client.get('/api/roles').then((r) => r.data),
    select: toArray,
  });

export const useFuelTanks = () =>
  useQuery({
    queryKey: fuelTankKeys.lists(),
    queryFn: () => client.get('/api/fuel-tanks').then((r) => r.data),
    select: toArray,
  });

export const useControllers = () =>
  useQuery({
    queryKey: controllerKeys.lists(),
    queryFn: () => client.get('/api/controllers').then((r) => r.data),
    select: toArray,
  });

export const useMeters = () =>
  useQuery({
    queryKey: meterKeys.lists(),
    queryFn: () => client.get('/api/meters').then((r) => r.data),
    select: toArray,
  });

export const useBranches = () =>
  useQuery({
    queryKey: branchKeys.lists(),
    queryFn: () => client.get('/api/branches').then((r) => r.data),
    select: toArray,
  });

export const useGovernorates = () =>
  useQuery({
    queryKey: [...branchKeys.all, 'governorates'],
    queryFn: () => client.get('/api/branches?type=governorate').then((r) => r.data),
    select: toArray,
  });

export const useFuelMovements = () =>
  useQuery({
    queryKey: fuelMovementKeys.lists(),
    queryFn: () => client.get('/api/fuel-movements').then((r) => r.data),
    select: toArray,
  });

export const useOils = () =>
  useQuery({
    queryKey: oilKeys.lists(),
    queryFn: () => client.get('/api/oils').then((r) => r.data),
    select: toArray,
  });

// ── Lookup Queries (with category param) ──

export const useLookups = (category) =>
  useQuery({
    queryKey: lookupKeys.byCategory(category),
    queryFn: () => client.get(`/api/lookups/by-category/${category}`).then((r) => r.data),
    select: toArray,
    enabled: !!category,
  });

// ── Dashboard Stats ──

export const useDashboardStats = (dateRange) =>
  useQuery({
    queryKey: ['dashboard', 'stats', dateRange],
    queryFn: () =>
      client
        .get('/analytics/insights', {
          params: {
            from: dateRange?.from,
            to: dateRange?.to,
          },
        })
        .then((r) => r.data),
  });

// ── Audit Logs ──

export const useAudits = (params) =>
  useQuery({
    queryKey: [...auditKeys.lists(), params],
    queryFn: () => client.get('/api/audits', { params }).then((r) => r.data),
  });

export const useExportAudits = (params) =>
  useMutation({
    mutationFn: () =>
      client.get('/api/audits/export', { params, responseType: 'blob' }),
  });

// ── Inventory Summary ──

export const useInventorySummary = () =>
  useQuery({
    queryKey: ['inventorySummary'],
    queryFn: () => client.get('/api/reports/inventory-summary').then((r) => r.data),
  });

export const useTransferDestinations = () =>
  useQuery({
    queryKey: ['transferDestinations'],
    queryFn: () =>
      client.get('/api/fuel-movements/transfer-destinations').then((r) => r.data),
  });

// ── Mutations: Stations ──

export const useCreateStation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data) => client.post('/api/stations', data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: stationKeys.all }),
  });
};

export const useUpdateStation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, ...data }) =>
      client.put(`/api/stations/${id}`, data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: stationKeys.all }),
  });
};

export const useDeleteStation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id) => client.delete(`/api/stations/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: stationKeys.all }),
  });
};

// ── Mutations: Sub-Stations ──

export const useCreateSubStation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data) => client.post('/api/sub-stations', data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: subStationKeys.all }),
  });
};

export const useUpdateSubStation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, ...data }) =>
      client.put(`/api/sub-stations/${id}`, data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: subStationKeys.all }),
  });
};

export const useDeleteSubStation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id) => client.delete(`/api/sub-stations/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: subStationKeys.all }),
  });
};

// ── Mutations: Generators ──

export const useCreateGenerator = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data) => client.post('/api/generators', data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: generatorKeys.all }),
  });
};

export const useUpdateGenerator = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, ...data }) =>
      client.put(`/api/generators/${id}`, data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: generatorKeys.all }),
  });
};

export const useDeleteGenerator = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id) => client.delete(`/api/generators/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: generatorKeys.all }),
  });
};

// ── Mutations: Users ──

export const useCreateUser = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data) => client.post('/api/users', data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: userKeys.all }),
  });
};

export const useUpdateUser = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, ...data }) =>
      client.put(`/api/users/${id}`, data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: userKeys.all }),
  });
};

export const useDeleteUser = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id) => client.delete(`/api/users/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: userKeys.all }),
  });
};

// ── Mutations: Roles ──

export const useCreateRole = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data) => client.post('/api/roles', data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: roleKeys.all }),
  });
};

export const useInitRoles = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: () => client.post('/api/roles/init').then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: roleKeys.all }),
  });
};

// ── Mutations: Fuel Tanks ──

export const useCreateFuelTank = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data) => client.post('/api/fuel-tanks', data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: fuelTankKeys.all }),
  });
};

export const useUpdateFuelTank = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, ...data }) =>
      client.put(`/api/fuel-tanks/${id}`, data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: fuelTankKeys.all }),
  });
};

export const useDeleteFuelTank = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id) => client.delete(`/api/fuel-tanks/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: fuelTankKeys.all }),
  });
};

// ── Mutations: Controllers ──

export const useCreateController = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data) => client.post('/api/controllers', data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: controllerKeys.all }),
  });
};

export const useUpdateController = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, ...data }) =>
      client.put(`/api/controllers/${id}`, data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: controllerKeys.all }),
  });
};

export const useDeleteController = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id) => client.delete(`/api/controllers/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: controllerKeys.all }),
  });
};

// ── Mutations: Meters ──

export const useCreateMeter = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data) => client.post('/api/meters', data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: meterKeys.all }),
  });
};

export const useUpdateMeter = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, ...data }) =>
      client.put(`/api/meters/${id}`, data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: meterKeys.all }),
  });
};

export const useDeleteMeter = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id) => client.delete(`/api/meters/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: meterKeys.all }),
  });
};

// ── Mutations: Fuel Movements ──

export const useCreateFuelMovement = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data) =>
      client.post('/api/fuel-movements', data).then((r) => r.data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: fuelMovementKeys.all });
      qc.invalidateQueries({ queryKey: fuelTankKeys.all });
      qc.invalidateQueries({ queryKey: ['inventorySummary'] });
    },
  });
};

// ── Mutations: Branches ──

export const useCreateBranch = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data) => client.post('/api/branches', data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: branchKeys.all }),
  });
};

// ── Mutations: Oils ──

export const useCreateOil = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data) => client.post('/api/oils', data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: oilKeys.all }),
  });
};

// ── Auth Mutations ──

export const useLogin = () =>
  useMutation({
    mutationFn: (payload) =>
      client.post('/api/auth/login', payload).then((r) => r.data),
  });

export const useAuthMe = () =>
  useQuery({
    queryKey: ['auth', 'me'],
    queryFn: () => client.get('/api/auth/me').then((r) => r.data),
    staleTime: 5 * 60 * 1000,
    retry: false,
  });

export const useForgotPassword = () =>
  useMutation({
    mutationFn: (data) =>
      client.post('/api/auth/forgot-password', data).then((r) => r.data),
  });

export const useResetPassword = () =>
  useMutation({
    mutationFn: (data) =>
      client.post('/api/auth/reset-password', data).then((r) => r.data),
  });

export const useChangePassword = () =>
  useMutation({
    mutationFn: (data) =>
      client.post('/api/auth/change-password', data).then((r) => r.data),
  });

// ── Query Keys: Warehouse & Maintenance ──

export const sparePartKeys = {
  all: ['spareParts'],
  lists: () => [...sparePartKeys.all, 'list'],
  detail: (id) => [...sparePartKeys.all, 'detail', id],
  byCategory: (cat) => [...sparePartKeys.all, 'category', cat],
};

export const warehouseStockKeys = {
  all: ['warehouseStock'],
  lists: () => [...warehouseStockKeys.all, 'list'],
  byStation: (stationId) => [...warehouseStockKeys.all, 'station', stationId],
};

export const supplyKeys = {
  all: ['supplies'],
  lists: () => [...supplyKeys.all, 'list'],
  detail: (id) => [...supplyKeys.all, 'detail', id],
};

export const dispatchKeys = {
  all: ['dispatches'],
  lists: () => [...dispatchKeys.all, 'list'],
  detail: (id) => [...dispatchKeys.all, 'detail', id],
};

export const workPermitKeys = {
  all: ['workPermits'],
  lists: () => [...workPermitKeys.all, 'list'],
  detail: (id) => [...workPermitKeys.all, 'detail', id],
};

export const installedPartKeys = {
  all: ['installedParts'],
  lists: () => [...installedPartKeys.all, 'list'],
  byGenerator: (genId) => [...installedPartKeys.all, 'generator', genId],
};

// ── Queries: Warehouse & Maintenance ──

export const useSpareParts = (params) =>
  useQuery({
    queryKey: [...sparePartKeys.lists(), params],
    queryFn: () => client.get('/api/spare-parts', { params }).then((r) => r.data),
  });

export const useWarehouseStock = (params) =>
  useQuery({
    queryKey: [...warehouseStockKeys.lists(), params],
    queryFn: () => client.get('/api/warehouse/stock', { params }).then((r) => r.data),
  });

export const useSupplies = (params) =>
  useQuery({
    queryKey: [...supplyKeys.lists(), params],
    queryFn: () => client.get('/api/warehouse/supply', { params }).then((r) => r.data),
  });

export const useDispatches = (params) =>
  useQuery({
    queryKey: [...dispatchKeys.lists(), params],
    queryFn: () => client.get('/api/warehouse/dispatch', { params }).then((r) => r.data),
  });

export const useWorkPermits = (params) =>
  useQuery({
    queryKey: [...workPermitKeys.lists(), params],
    queryFn: () => client.get('/api/work-permits', { params }).then((r) => r.data),
  });

export const useInstalledParts = (params) =>
  useQuery({
    queryKey: [...installedPartKeys.lists(), params],
    queryFn: () => client.get('/api/installed-parts', { params }).then((r) => r.data),
  });

export const useMaintenanceDashboard = () =>
  useQuery({
    queryKey: ['maintenance', 'dashboard'],
    queryFn: () => client.get('/api/maintenance/dashboard').then((r) => r.data),
  });

// ── Mutations: Warehouse & Maintenance ──

export const useCreateSparePart = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data) => client.post('/api/spare-parts', data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: sparePartKeys.all }),
  });
};

export const useUpdateSparePart = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, ...data }) => client.put(`/api/spare-parts/${id}`, data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: sparePartKeys.all }),
  });
};

export const useDeleteSparePart = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id) => client.delete(`/api/spare-parts/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: sparePartKeys.all }),
  });
};

export const useCreateSupply = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data) => client.post('/api/warehouse/supply', data).then((r) => r.data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: supplyKeys.all });
      qc.invalidateQueries({ queryKey: warehouseStockKeys.all });
    },
  });
};

export const useConfirmSupply = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id) => client.post(`/api/warehouse/supply/${id}/confirm`).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: supplyKeys.all }),
  });
};

export const useCreateDispatch = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data) => client.post('/api/warehouse/dispatch', data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: dispatchKeys.all }),
  });
};

export const useApproveDispatch = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }) => client.post(`/api/warehouse/dispatch/${id}/approve`, data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: dispatchKeys.all }),
  });
};

export const useExecuteDispatch = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id) => client.post(`/api/warehouse/dispatch/${id}/execute`).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: dispatchKeys.all }),
  });
};

export const useCreateWorkPermit = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data) => client.post('/api/work-permits', data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: workPermitKeys.all }),
  });
};

export const useUpdateWorkPermit = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, ...data }) => client.put(`/api/work-permits/${id}`, data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: workPermitKeys.all }),
  });
};

export const useDeleteWorkPermit = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id) => client.delete(`/api/work-permits/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: workPermitKeys.all }),
  });
};

export const useCreateInstalledPart = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data) => client.post('/api/installed-parts', data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: installedPartKeys.all }),
  });
};

export const useRemoveInstalledPart = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }) => client.post(`/api/installed-parts/${id}/remove`, data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: installedPartKeys.all }),
  });
};
