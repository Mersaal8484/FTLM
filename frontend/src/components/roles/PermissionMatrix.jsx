import React, { useState, useMemo } from 'react';
import { LayoutDashboard, Shield, Settings, BarChart3, Server, ChevronDown, ChevronRight } from 'lucide-react';

// ─── Tab Groups ─────────────────────────────────────────────────────────────
const TAB_GROUPS = {
  dashboard: {
    key: 'dashboard',
    labelEn: 'Dashboard',
    labelAr: 'لوحة التحكم',
    icon: LayoutDashboard,
    modulePatterns: ['dashboard', 'widget_'],
    subGroups: [],
  },
  admin: {
    key: 'admin',
    labelEn: 'Admin',
    labelAr: 'الإدارة',
    icon: Shield,
    modulePatterns: [
      'users', 'roles', 'branches', 'stations', 'sub_stations',
      'generators', 'fuel_tanks', 'oil_tanks', 'meters', 'controllers', 'lookups',
    ],
    subGroups: [
      {
        key: 'user_management',
        labelEn: 'User Management',
        labelAr: 'إدارة المستخدمين',
        modulePatterns: ['users', 'roles'],
      },
      {
        key: 'locations',
        labelEn: 'Locations',
        labelAr: 'المواقع',
        modulePatterns: ['branches', 'stations', 'sub_stations'],
      },
      {
        key: 'equipment',
        labelEn: 'Equipment',
        labelAr: 'المعدات',
        modulePatterns: ['generators', 'fuel_tanks', 'oil_tanks', 'meters', 'controllers'],
      },
      {
        key: 'lookups',
        labelEn: 'Lookups',
        labelAr: 'القوائم المرجعية',
        modulePatterns: ['lookups'],
      },
    ],
  },
  operations: {
    key: 'operations',
    labelEn: 'Operations',
    labelAr: 'العمليات',
    icon: Settings,
    modulePatterns: [
      'daily_readings', 'station_readings', 'fuel_movements',
      'oil_transactions', 'periodic_inventory', 'notifications',
      'rollback_approval', 'review_changes',
    ],
    subGroups: [],
  },
  reports: {
    key: 'reports',
    labelEn: 'Reports',
    labelAr: 'التقارير',
    icon: BarChart3,
    modulePatterns: ['reports', 'fuel_report', 'oil_report', 'efficiency_report', 'consumption_report', 'inventory_report'],
    subGroups: [],
  },
  system: {
    key: 'system',
    labelEn: 'System',
    labelAr: 'النظام',
    icon: Server,
    modulePatterns: ['settings', 'logs', 'audit'],
    subGroups: [],
  },
};

const TAB_ORDER = ['dashboard', 'admin', 'operations', 'reports', 'system'];

// ─── Helper: match module to pattern ────────────────────────────────────────
function matchesPattern(modKey, patterns) {
  return patterns.some(p => modKey === p || modKey.startsWith(p + '_') || modKey.startsWith(p + '-'));
}

// ─── PermissionMatrix ───────────────────────────────────────────────────────
export default function PermissionMatrix({
  modules = [],
  permissions = {},
  onChange,
  readOnly = false,
  lang = 'en',
}) {
  const ar = lang === 'ar';
  const [activeTab, setActiveTab] = useState('dashboard');
  const [collapsedGroups, setCollapsedGroups] = useState({});

  // Map module key → module object
  const moduleMap = useMemo(() => {
    const m = {};
    modules.forEach(mod => { m[mod.key] = mod; });
    return m;
  }, [modules]);

  // Organize modules into tab groups (with sub-groups)
  const tabModules = useMemo(() => {
    const result = {};
    const assigned = new Set();

    TAB_ORDER.forEach(tabKey => {
      const group = TAB_GROUPS[tabKey];
      const matched = [];

      modules.forEach(mod => {
        if (assigned.has(mod.key)) return;
        if (matchesPattern(mod.key, group.modulePatterns)) {
          matched.push(mod);
          assigned.add(mod.key);
        }
      });

      // If sub-groups defined, organize into sub-groups
      if (group.subGroups.length > 0) {
        const subGrouped = [];
        const subAssigned = new Set();

        group.subGroups.forEach(sg => {
          const sgModules = [];
          matched.forEach(mod => {
            if (subAssigned.has(mod.key)) return;
            if (matchesPattern(mod.key, sg.modulePatterns)) {
              sgModules.push(mod);
              subAssigned.add(mod.key);
            }
          });
          if (sgModules.length > 0) {
            subGrouped.push({
              ...sg,
              modules: sgModules,
            });
          }
        });

        // Any remaining modules not in a sub-group go to a default group
        const remaining = matched.filter(m => !subAssigned.has(m.key));
        if (remaining.length > 0) {
          subGrouped.push({
            key: '_other',
            labelEn: 'Other',
            labelAr: 'أخرى',
            modulePatterns: [],
            modules: remaining,
          });
        }

        result[tabKey] = subGrouped;
      } else {
        // Flat - no sub-groups
        result[tabKey] = [{ key: '_flat', labelEn: '', labelAr: '', modules: matched }];
      }
    });

    // Put any unassigned modules into 'system'
    modules.forEach(mod => {
      if (!assigned.has(mod.key)) {
        const sysResult = result.system || [];
        if (sysResult.length === 0 || sysResult[0].key !== '_flat') {
          sysResult.push({ key: '_flat', labelEn: '', labelAr: '', modules: [] });
        }
        sysResult[sysResult.length - 1].modules.push(mod);
        result.system = sysResult;
      }
    });

    return result;
  }, [modules]);

  const currentGroups = tabModules[activeTab] || [];

  // Count granted permissions in each tab
  const tabGrantedCounts = useMemo(() => {
    const counts = {};
    TAB_ORDER.forEach(tabKey => {
      let count = 0;
      (tabModules[tabKey] || []).forEach(group => {
        (group.modules || []).forEach(mod => {
          (mod.pagePermissions || []).forEach(pp => {
            if (permissions[mod.key]?.[pp.actionKey]) count++;
          });
        });
      });
      counts[tabKey] = count;
    });
    return counts;
  }, [tabModules, permissions]);

  // Count total modules in a tab
  const tabModuleCounts = useMemo(() => {
    const counts = {};
    TAB_ORDER.forEach(tabKey => {
      let count = 0;
      (tabModules[tabKey] || []).forEach(group => {
        count += (group.modules || []).length;
      });
      counts[tabKey] = count;
    });
    return counts;
  }, [tabModules]);

  const handleSelectAll = () => {
    if (readOnly) return;
    currentGroups.forEach(group => {
      (group.modules || []).forEach(mod => {
        (mod.pagePermissions || []).forEach(pp => {
          if (!permissions[mod.key]?.[pp.actionKey]) {
            onChange(mod.key, pp.actionKey, true);
          }
        });
      });
    });
  };

  const handleDeselectAll = () => {
    if (readOnly) return;
    currentGroups.forEach(group => {
      (group.modules || []).forEach(mod => {
        (mod.pagePermissions || []).forEach(pp => {
          if (permissions[mod.key]?.[pp.actionKey]) {
            onChange(mod.key, pp.actionKey, false);
          }
        });
      });
    });
  };

  const handleSelectSubGroup = (group) => {
    if (readOnly) return;
    (group.modules || []).forEach(mod => {
      (mod.pagePermissions || []).forEach(pp => {
        if (!permissions[mod.key]?.[pp.actionKey]) {
          onChange(mod.key, pp.actionKey, true);
        }
      });
    });
  };

  const handleDeselectSubGroup = (group) => {
    if (readOnly) return;
    (group.modules || []).forEach(mod => {
      (mod.pagePermissions || []).forEach(pp => {
        if (permissions[mod.key]?.[pp.actionKey]) {
          onChange(mod.key, pp.actionKey, false);
        }
      });
    });
  };

  const toggleGroupCollapse = (groupKey) => {
    setCollapsedGroups(prev => ({ ...prev, [groupKey]: !prev[groupKey] }));
  };

  const getModuleLabel = (mod) => ar ? (mod.nameAr || mod.nameEn || mod.key) : (mod.nameEn || mod.key);

  // Count granted in a sub-group
  const getGroupGrantedCount = (group) => {
    let count = 0;
    (group.modules || []).forEach(mod => {
      (mod.pagePermissions || []).forEach(pp => {
        if (permissions[mod.key]?.[pp.actionKey]) count++;
      });
    });
    return count;
  };

  const hasSubGroups = currentGroups.length > 1 || (currentGroups.length === 1 && currentGroups[0].key !== '_flat');

  return (
    <div className="pm-container" dir={ar ? 'rtl' : 'ltr'}>
      {/* ── Tab Bar ─────────────────────────────────────────────── */}
      <div className="pm-tab-bar">
        {TAB_ORDER.map(tabKey => {
          const tab = TAB_GROUPS[tabKey];
          const Icon = tab.icon;
          const granted = tabGrantedCounts[tabKey] || 0;
          const count = tabModuleCounts[tabKey] || 0;
          const isActive = activeTab === tabKey;

          return (
            <button
              key={tabKey}
              className={`pm-tab ${isActive ? 'pm-tab--active' : ''}`}
              onClick={() => setActiveTab(tabKey)}
            >
              <Icon size={15} />
              <span>{ar ? tab.labelAr : tab.labelEn}</span>
              {count > 0 && (
                <span className="pm-tab-count">{count}</span>
              )}
              {granted > 0 && (
                <span className="pm-tab-badge">{granted}</span>
              )}
            </button>
          );
        })}
      </div>

      {/* ── Tab Content ─────────────────────────────────────────── */}
      <div className="pm-tab-content">
        {/* Select All / Deselect All */}
        <div className="pm-tab-actions">
          <button className="pm-btn-select" onClick={handleSelectAll} disabled={readOnly}>
            {ar ? 'تحديد الكل' : 'Select All'}
          </button>
          <button className="pm-btn-select pm-btn-deselect" onClick={handleDeselectAll} disabled={readOnly}>
            {ar ? 'إلغاء تحديد الكل' : 'Deselect All'}
          </button>
        </div>

        {/* Module rows */}
        {currentGroups.length === 0 && (
          <div className="pm-empty">
            {ar ? 'لا توجد وحدات في هذا التبويب' : 'No modules in this tab'}
          </div>
        )}

        {currentGroups.map((group) => {
          const isFlat = group.key === '_flat';
          const isCollapsed = collapsedGroups[group.key] || false;
          const groupGranted = getGroupGrantedCount(group);
          const groupTotal = (group.modules || []).reduce((sum, m) => sum + (m.pagePermissions || []).length, 0);

          if (isFlat) {
            // Flat rendering - no sub-group wrapper
            return (
              <div key="_flat">
                {group.modules.map((mod, idx) => (
                  <ModuleRow
                    key={mod.key}
                    mod={mod}
                    idx={idx}
                    permissions={permissions}
                    onChange={onChange}
                    readOnly={readOnly}
                    getModuleLabel={getModuleLabel}
                  />
                ))}
              </div>
            );
          }

          // Sub-group with header
          return (
            <div key={group.key} className="pm-subgroup">
              <div
                className="pm-subgroup-header"
                onClick={() => toggleGroupCollapse(group.key)}
              >
                <div className="pm-subgroup-left">
                  {isCollapsed ? <ChevronRight size={14} /> : <ChevronDown size={14} />}
                  <span className="pm-subgroup-label">{ar ? group.labelAr : group.labelEn}</span>
                  <span className="pm-subgroup-count">{(group.modules || []).length}</span>
                  {groupGranted > 0 && (
                    <span className="pm-subgroup-granted">{groupGranted}/{groupTotal}</span>
                  )}
                </div>
                {!readOnly && (
                  <div className="pm-subgroup-actions" onClick={(e) => e.stopPropagation()}>
                    <button
                      className="pm-btn-subgroup"
                      onClick={() => handleSelectSubGroup(group)}
                    >
                      {ar ? 'تحديد' : 'Select'}
                    </button>
                    <button
                      className="pm-btn-subgroup pm-btn-subgroup-deselect"
                      onClick={() => handleDeselectSubGroup(group)}
                    >
                      {ar ? 'إلغاء' : 'Deselect'}
                    </button>
                  </div>
                )}
              </div>
              {!isCollapsed && (
                <div className="pm-subgroup-body">
                  {group.modules.map((mod, idx) => (
                    <ModuleRow
                      key={mod.key}
                      mod={mod}
                      idx={idx}
                      permissions={permissions}
                      onChange={onChange}
                      readOnly={readOnly}
                      getModuleLabel={getModuleLabel}
                    />
                  ))}
                </div>
              )}
            </div>
          );
        })}
      </div>
    </div>
  );
}

// ─── Module Row ─────────────────────────────────────────────────────────────
function ModuleRow({ mod, idx, permissions, onChange, readOnly, getModuleLabel }) {
  const grantedCount = (mod.pagePermissions || []).filter(
    pp => permissions[mod.key]?.[pp.actionKey]
  ).length;
  const totalCount = (mod.pagePermissions || []).length;
  const allGranted = grantedCount === totalCount && totalCount > 0;

  return (
    <div className={`pm-module-row ${idx % 2 === 1 ? 'pm-module-row--alt' : ''}`}>
      <div className="pm-module-label" title={mod.key}>
        {getModuleLabel(mod)}
        {allGranted && <span className="pm-module-all-badge">✓</span>}
      </div>
      <div className="pm-module-actions">
        {(mod.pagePermissions || []).map(pp => {
          const granted = permissions[mod.key]?.[pp.actionKey] || false;
          return (
            <label
              key={pp.id}
              className={`pm-action-chip ${granted ? 'pm-action-chip--active' : ''} ${readOnly ? 'pm-action-chip--readonly' : ''}`}
            >
              <input
                type="checkbox"
                checked={granted}
                onChange={(e) => onChange(mod.key, pp.actionKey, e.target.checked)}
                disabled={readOnly}
                hidden
              />
              {pp.actionKey}
            </label>
          );
        })}
      </div>
    </div>
  );
}
