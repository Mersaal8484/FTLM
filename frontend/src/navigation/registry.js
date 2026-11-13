import {
  BarChart2,
  Bell,
  Boxes,
  ClipboardCheck,
  Clock,
  Cpu,
  Database,
  Droplet,
  FileText,
  Gauge,
  LayoutDashboard,
  MapPin,
  PackageCheck,
  PackageSearch,
  ScrollText,
  Settings,
  ShieldCheck,
  Truck,
  Undo2,
  Users,
  Wrench,
  Zap,
} from 'lucide-react';

export const navigationGroups = {
  maintenance: {
    id: 'maintenance',
    route: '/maintenance',
    labelKey: 'sidebar.maintenance',
    icon: Wrench,
    order: 30,
  },
  settings: {
    id: 'settings',
    route: null,
    labelKey: 'sidebar.systemSettings',
    icon: Settings,
    order: 90,
  },
};

export const preferredMobileNavigationIds = [
  'dashboard',
  'operations',
  'service-intervals',
  'warehouse-stock',
];

const moduleRule = (module, action = 'view', explicit = false) => ({ type: 'module', module, action, explicit });
const roleRule = (...roles) => ({ type: 'role', roles });
const allRule = (...rules) => ({ type: 'all', rules });
const anyRule = (...rules) => ({ type: 'any', rules });

export const navigationRegistry = [
  { id: 'notifications', route: '/notifications', labelKey: 'sidebar.notifications', icon: Bell, permissionRule: moduleRule('alerts'), group: 'utility', order: 0, legacyDuplicate: false, modeVisibility: 'all' },
  { id: 'dashboard', route: '/', labelKey: 'sidebar.dashboard', icon: LayoutDashboard, permissionRule: moduleRule('dashboard'), group: 'main', order: 10, legacyDuplicate: false, modeVisibility: 'all' },
  { id: 'operations', route: '/operations', labelKey: 'sidebar.operations', icon: Zap, permissionRule: moduleRule('operational_data'), group: 'main', order: 20, legacyDuplicate: false, modeVisibility: 'all' },
  { id: 'my-generators', route: '/my-generators', labelKey: 'sidebar.myGenerators', icon: Zap, permissionRule: roleRule('operator'), group: 'main', order: 25, legacyDuplicate: true, modeVisibility: 'all' },

  { id: 'service-intervals', route: '/maintenance/service', labelKey: 'sidebar.serviceIntervals', icon: Clock, permissionRule: allRule(moduleRule('maintenance'), moduleRule('operational_data')), group: 'maintenance', order: 31, legacyDuplicate: false, modeVisibility: 'all' },
  { id: 'work-permits', route: '/maintenance/permits', labelKey: 'sidebar.workPermits', icon: FileText, permissionRule: moduleRule('maintenance'), group: 'maintenance', order: 32, legacyDuplicate: false, modeVisibility: 'all' },
  { id: 'parts-catalog', route: '/warehouse/parts', labelKey: 'sidebar.partsCatalog', icon: PackageSearch, permissionRule: moduleRule('warehouse'), group: 'maintenance', order: 33, legacyDuplicate: false, modeVisibility: 'all' },
  { id: 'warehouse-stock', route: '/warehouse/stock', labelKey: 'sidebar.warehouseStock', icon: PackageSearch, permissionRule: moduleRule('warehouse'), group: 'maintenance', order: 34, legacyDuplicate: false, modeVisibility: 'all' },
  { id: 'installed-parts', route: '/maintenance/installed-parts', labelKey: 'sidebar.installedParts', icon: Wrench, permissionRule: moduleRule('maintenance'), group: 'maintenance', order: 35, legacyDuplicate: false, modeVisibility: 'all' },
  { id: 'dispatch-requests', route: '/warehouse/dispatch', labelKey: 'sidebar.dispatchRequests', icon: PackageCheck, permissionRule: moduleRule('warehouse'), group: 'maintenance', order: 36, legacyDuplicate: false, modeVisibility: 'all' },
  { id: 'warehouse-supply', route: '/warehouse/supply', labelKey: 'sidebar.warehouseSupply', icon: Boxes, permissionRule: moduleRule('warehouse'), group: 'maintenance', order: 37, legacyDuplicate: false, modeVisibility: 'all' },

  { id: 'fuel-movements', route: '/fuel-movements', labelKey: 'sidebar.fuelMovements', icon: Truck, permissionRule: moduleRule('fuel_movements'), group: 'main', order: 40, legacyDuplicate: false, modeVisibility: 'all' },
  { id: 'generators-overview', route: '/generators/overview', labelKey: 'sidebar.generatorsOverview', icon: Zap, permissionRule: moduleRule('generators', 'view', true), group: 'main', order: 41, legacyDuplicate: false, modeVisibility: 'all' },
  { id: 'fuel-stock', route: '/stock/fuel', labelKey: 'sidebar.fuelStock', icon: Database, permissionRule: moduleRule('inventory'), group: 'main', order: 42, legacyDuplicate: false, modeVisibility: 'all' },
  { id: 'oil-stock', route: '/stock/oil', labelKey: 'sidebar.oilStock', icon: Droplet, permissionRule: moduleRule('oil', 'view', true), group: 'main', order: 43, legacyDuplicate: false, modeVisibility: 'all' },
  { id: 'review-readings', route: '/review-changes', labelKey: 'sidebar.reviewReadings', icon: ClipboardCheck, permissionRule: anyRule(moduleRule('fuel_movements', 'approve'), moduleRule('readings', 'approve'), moduleRule('inventory', 'approve')), group: 'main', order: 44, legacyDuplicate: true, modeVisibility: 'all' },
  { id: 'periodic-inventory', route: '/inventory', labelKey: 'sidebar.periodicInventory', icon: PackageSearch, permissionRule: moduleRule('inventory'), group: 'main', order: 45, legacyDuplicate: false, modeVisibility: 'all' },
  { id: 'reports', route: '/reports', labelKey: 'sidebar.reports', icon: BarChart2, permissionRule: anyRule(moduleRule('reports'), moduleRule('warehouse')), group: 'main', order: 50, legacyDuplicate: false, modeVisibility: 'all' },

  { id: 'locations', route: '/locations', labelKey: 'sidebar.locations', icon: MapPin, permissionRule: moduleRule('settings', 'manage'), group: 'settings', order: 91, legacyDuplicate: false, modeVisibility: 'all' },
  { id: 'setup-generators', route: '/generators', labelKey: 'sidebar.setupGenerators', icon: Zap, permissionRule: moduleRule('settings', 'manage'), group: 'settings', order: 92, legacyDuplicate: false, modeVisibility: 'all' },
  { id: 'meters', route: '/meters', labelKey: 'sidebar.meters', icon: Gauge, permissionRule: moduleRule('settings', 'manage'), group: 'settings', order: 93, legacyDuplicate: false, modeVisibility: 'all' },
  { id: 'setup-oils', route: '/setup-oils', labelKey: 'sidebar.setupOils', icon: Droplet, permissionRule: moduleRule('settings', 'manage'), group: 'settings', order: 94, legacyDuplicate: false, modeVisibility: 'all' },
  { id: 'tanks', route: '/tanks', labelKey: 'sidebar.setupTanks', icon: Database, permissionRule: moduleRule('settings', 'manage'), group: 'settings', order: 95, legacyDuplicate: false, modeVisibility: 'all' },
  { id: 'feeders', route: '/feeders', labelKey: 'sidebar.feeders', icon: Zap, permissionRule: moduleRule('settings', 'manage'), group: 'settings', order: 95.5, legacyDuplicate: false, modeVisibility: 'all' },
  { id: 'controllers', route: '/controllers', labelKey: 'sidebar.controllers', icon: Cpu, permissionRule: moduleRule('settings', 'manage'), group: 'settings', order: 96, legacyDuplicate: false, modeVisibility: 'all' },
  { id: 'users', route: '/users', labelKey: 'sidebar.users', icon: Users, permissionRule: moduleRule('users'), group: 'settings', order: 97, legacyDuplicate: false, modeVisibility: 'all' },
  { id: 'roles', route: '/roles', labelKey: 'sidebar.roles', icon: ShieldCheck, permissionRule: moduleRule('settings', 'manage'), group: 'settings', order: 98, legacyDuplicate: false, modeVisibility: 'all' },
  { id: 'audits', route: '/audits', labelKey: 'sidebar.audits', icon: ScrollText, permissionRule: moduleRule('audits'), group: 'settings', order: 99, legacyDuplicate: false, modeVisibility: 'all' },
  { id: 'rollback', route: '/rollback', labelKey: 'sidebar.rollback', icon: Undo2, permissionRule: roleRule('admin'), group: 'settings', order: 100, legacyDuplicate: true, modeVisibility: 'all' },
];

function allows(rule, context) {
  if (!rule) return true;
  if (rule.type === 'module') {
    return rule.explicit
      ? context.canExplicit(rule.module, rule.action)
      : context.can(rule.module, rule.action);
  }
  if (rule.type === 'role') return rule.roles.some((role) => context.hasRole(role));
  if (rule.type === 'all') return rule.rules.every((child) => allows(child, context));
  if (rule.type === 'any') return rule.rules.some((child) => allows(child, context));
  return false;
}

export function isNavigationItemVisible(item, context) {
  if (item.modeVisibility === 'new' && !context.isNewMode) return false;
  if (item.modeVisibility === 'legacy' && context.isNewMode) return false;
  if (item.legacyDuplicate) {
    if (!context.modeResolved) return false;
    if (context.isNewMode && !context.hasAssignedPermission('legacy_screens', 'view')) return false;
  }
  return allows(item.permissionRule, context);
}

export function visibleNavigationItems(context) {
  return navigationRegistry
    .filter((item) => isNavigationItemVisible(item, context))
    .sort((left, right) => left.order - right.order);
}
