import { useMemo } from 'react';
import useAuthStore from '../store/authStore';

const MODULE_ALIASES = {
  tanks: 'fuel_tanks',
  oil: 'oil_transactions',
  inventory: 'periodic_inventory',
  readings: 'station_readings',
  alerts: 'notifications',
};

const ACTION_ALIASES = {
  add: 'create',
  read: 'view',
};

function normalizeAction(action) {
  return ACTION_ALIASES[action] || action;
}

function blockAllows(block, action) {
  if (!block) return false;
  if (block.manage === true) return true;
  return block[normalizeAction(action)] === true;
}

export default function usePermissions(currentUser) {
  const contextUser = useAuthStore((s) => s.user);
  const user = currentUser || contextUser;

  return useMemo(() => {
    const role = user?.role;
    const permissions = user?.permissions || {};

    const can = (module, action = 'view') => {
      if (role === 'admin') return true;

      const key = MODULE_ALIASES[module] || module;
      if (blockAllows(permissions[key], action)) return true;

      if (module === 'readings') {
        if (blockAllows(permissions.daily_readings, action)) return true;
      }

      if (module === 'stations') {
        if (blockAllows(permissions.branches, action)) return true;
        if (blockAllows(permissions.sub_stations, action)) return true;
      }

      if (module === 'settings') {
        if (blockAllows(permissions.roles, action)) return true;
        if (blockAllows(permissions.lookups, action)) return true;
        if (blockAllows(permissions.meters, action)) return true;
        if (blockAllows(permissions.controllers, action)) return true;
      }

      return false;
    };

    const canAny = (module, actions = []) => actions.some((a) => can(module, a));

    const canExplicit = (module, action = 'view') => {
      if (role === 'admin') return true;
      const key = MODULE_ALIASES[module] || module;
      const block = permissions[key];
      if (!block) return false;
      return block[action] === true;
    };

    const canModule = (module) => {
      if (role === 'admin') return true;

      const key = MODULE_ALIASES[module] || module;
      const block = permissions[key];
      if (block) {
        if (block.manage === true) return true;
        if (Object.values(block).some((v) => v === true)) return true;
      }

      if (module === 'readings') {
        const db = permissions.daily_readings;
        if (db && Object.values(db).some((v) => v === true)) return true;
      }

      if (module === 'stations') {
        for (const k of ['branches', 'sub_stations']) {
          const b = permissions[k];
          if (b && Object.values(b).some((v) => v === true)) return true;
        }
      }

      if (module === 'settings') {
        for (const k of ['roles', 'lookups', 'meters', 'controllers']) {
          const b = permissions[k];
          if (b && Object.values(b).some((v) => v === true)) return true;
        }
      }

      return false;
    };

    const filterModules = (moduleList) => moduleList.filter((m) => canModule(m));

    return { can, canAny, canExplicit, canModule, filterModules, role, permissions };
  }, [user]);
}
