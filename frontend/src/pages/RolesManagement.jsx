import React, { useState, useEffect, useCallback, useMemo } from 'react';
import {
  Shield, Plus, ChevronDown, ChevronUp, Trash2, Save, X, Copy, Key
} from 'lucide-react';
import { useTranslation } from 'react-i18next';
import client from '../api/client';
import usePermissions from '../permissions/usePermissions';
import useAuthStore from '../store/authStore';
import useToastStore from '../store/toastStore';
import PermissionMatrix from '../components/roles/PermissionMatrix';

const PROTECTED_KEYS = new Set(['admin']);

const INITIAL_FORM = {
  name: '',
  nameEn: '',
  key: '',
  description: '',
  descriptionEn: '',
  isActive: true,
};

export default function RolesManagement({ lang }) {
  const { t } = useTranslation();
  const ar = lang === 'ar';

  const [roles, setRoles] = useState([]);
  const [modules, setModules] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [showModal, setShowModal] = useState(false);
  const [editingRole, setEditingRole] = useState(null);
  const [form, setForm] = useState(INITIAL_FORM);
  const [saveTarget, setSaveTarget] = useState(null);
  const [permissionChanges, setPermissionChanges] = useState({});
  const [confirmDeleteRole, setConfirmDeleteRole] = useState(null);
  const [isSaving, setIsSaving] = useState(false);
  const [showRoleList, setShowRoleList] = useState(true);

  const { can } = usePermissions();
  const showToast = useToastStore(s => s.showToast);

  const fetchRoles = useCallback(async () => {
    try {
      const res = await client.get('/api/roles');
      setRoles(res.data);
    } catch (err) {
      setError('Failed to load roles');
    }
  }, []);

  const fetchModules = useCallback(async () => {
    try {
      const res = await client.get('/api/roles/modules');
      setModules(res.data);
    } catch (err) {
      // silent
    }
  }, []);

  useEffect(() => {
    setLoading(true);
    Promise.all([fetchRoles(), fetchModules()])
      .finally(() => setLoading(false));
  }, [fetchRoles, fetchModules]);

  const handleAddRole = () => {
    setEditingRole(null);
    setForm(INITIAL_FORM);
    setPermissionChanges({});
    setSaveTarget(null);
    setShowModal(true);
  };

  const handleCloneRole = async (role) => {
    setForm({
      name: '',
      nameEn: role.nameEn || `${role.name} Clone`,
      key: '',
      description: role.description || '',
      descriptionEn: role.descriptionEn || role.description || '',
      isActive: true,
    });
    setPermissionChanges({});
    setEditingRole(null);
    setSaveTarget(null);
    setShowModal(true);

    try {
      const res = await client.get(`/api/roles/${role.id}/permissions`);
      const perms = res.data.permissions || [];
      const initialPermissions = {};
      perms.forEach(p => {
        if (p.isGranted) {
          if (!initialPermissions[p.moduleKey]) initialPermissions[p.moduleKey] = {};
          initialPermissions[p.moduleKey][p.actionKey] = true;
        }
      });
      setPermissionChanges(initialPermissions);
    } catch (err) {
      showToast('Failed to load permissions for cloning', 'error');
    }
  };

  const handleEditRole = async (role) => {
    setEditingRole(role);
    setForm({
      name: role.name,
      nameEn: role.nameEn || role.name,
      key: role.key || '',
      description: role.description || '',
      descriptionEn: role.descriptionEn || '',
      isActive: role.isActive,
    });
    setPermissionChanges({});
    setSaveTarget(role);
    setShowModal(true);

    try {
      const res = await client.get(`/api/roles/${role.id}/permissions`);
      const perms = res.data.permissions || [];
      const initialPermissions = {};
      perms.forEach(p => {
        if (!initialPermissions[p.moduleKey]) initialPermissions[p.moduleKey] = {};
        initialPermissions[p.moduleKey][p.actionKey] = p.isGranted;
      });
      setPermissionChanges(initialPermissions);
    } catch (err) {
      showToast('Failed to load role permissions', 'error');
    }
  };

  const handleDeleteRole = (role) => {
    setConfirmDeleteRole(role);
  };

  const confirmDelete = async () => {
    if (!confirmDeleteRole) return;
    try {
      await client.delete(`/api/roles/${confirmDeleteRole.id}`);
      showToast('Role deleted', 'success');
      fetchRoles();
      if (editingRole?.id === confirmDeleteRole.id) {
        setShowModal(false);
        setEditingRole(null);
      }
    } catch (err) {
      showToast('Failed to delete role', 'error');
    }
    setConfirmDeleteRole(null);
  };

  const handlePermissionChange = (moduleKey, actionKey, value) => {
    setPermissionChanges(prev => {
      const next = { ...prev };
      if (!next[moduleKey]) next[moduleKey] = {};
      next[moduleKey] = { ...next[moduleKey], [actionKey]: value };
      return next;
    });
  };

  const buildPermissionEntries = () => {
    const entries = [];
    modules.forEach(mod => {
      (mod.pagePermissions || []).forEach(pp => {
        const granted = permissionChanges[mod.key]?.[pp.actionKey] || false;
        entries.push({ pagePermissionId: pp.id, isGranted: granted });
      });
    });
    return entries;
  };

  const handleSavePermissions = async () => {
    if (isSaving || !saveTarget) return;
    setIsSaving(true);
    try {
      const entries = buildPermissionEntries();
      await client.put(`/api/roles/${saveTarget.id}/permissions`, {
        roleId: saveTarget.id,
        permissions: entries,
      });
      showToast('Permissions updated', 'success');
      fetchRoles();
    } catch (err) {
      const msg = err.response?.data?.message || err.response?.data?.title || 'Failed to update permissions';
      showToast(msg, 'error');
    } finally {
      setIsSaving(false);
    }
  };

  const handleSave = async () => {
    if (isSaving) return;
    if (!form.name.trim()) {
      showToast('Role name is required', 'error');
      return;
    }

    const key = form.key || form.nameEn.toLowerCase().replace(/[^a-z0-9]/g, '_').replace(/_+/g, '_').replace(/^_|_$/g, '');

    setIsSaving(true);
    try {
      if (saveTarget) {
        await client.put(`/api/roles/${saveTarget.id}`, {
          id: saveTarget.id,
          name: form.name,
          nameEn: form.nameEn,
          key,
          description: form.description,
          descriptionEn: form.descriptionEn,
          isActive: form.isActive,
        });
        const entries = buildPermissionEntries();
        await client.put(`/api/roles/${saveTarget.id}/permissions`, {
          roleId: saveTarget.id,
          permissions: entries,
        });
        showToast('Role updated', 'success');
      } else {
        const res = await client.post('/api/roles', {
          name: form.name,
          nameEn: form.nameEn,
          key,
          description: form.description,
          descriptionEn: form.descriptionEn,
          isActive: form.isActive,
        });
        const newRoleId = res.data.id;
        const entries = buildPermissionEntries();
        await client.put(`/api/roles/${newRoleId}/permissions`, {
          roleId: newRoleId,
          permissions: entries,
        });
        showToast('Role created', 'success');
      }
      setShowModal(false);
      setEditingRole(null);
      fetchRoles();
    } catch (err) {
      const msg = err.response?.data?.message || err.response?.data?.title || (saveTarget ? 'Failed to update role' : 'Failed to create role');
      showToast(msg, 'error');
    } finally {
      setIsSaving(false);
    }
  };

  const totalPermissions = useMemo(() => {
    return Object.values(permissionChanges).reduce((sum, actions) => {
      return sum + Object.values(actions).filter(Boolean).length;
    }, 0);
  }, [permissionChanges]);

  const selectedRoleName = useMemo(() => {
    if (saveTarget) return ar ? saveTarget.nameAr || saveTarget.name : saveTarget.nameEn || saveTarget.name;
    return ar ? 'دور جديد' : 'New Role';
  }, [saveTarget, ar]);

  if (loading) {
    return (
      <div className="page-loading">
        <div className="ft-spinner" />
        <p>{ar ? 'جاري تحميل الأدوار...' : 'Loading roles...'}</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="page-error">
        <p>{error}</p>
        <button className="btn btn-primary" onClick={fetchRoles}>
          {ar ? 'إعادة المحاولة' : 'Retry'}
        </button>
      </div>
    );
  }

  return (
    <div className="page-container">
      <header className="page-header" style={{ justifyContent: 'flex-end' }}>
        <div style={{ display: 'flex', gap: 8, alignItems: 'center', marginLeft: 'auto' }}>
          <span className="text-sm text-muted" style={{ marginLeft: 4 }}>
            {ar ? 'إجمالي الصلاحيات المحددة:' : 'Total Permissions:'} <span style={{ fontWeight: 600 }}>{totalPermissions}</span>
          </span>
          <button className="btn btn-primary" onClick={handleAddRole} disabled={!can('roles', 'create')} style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
            <Plus size={16} />
            {ar ? 'إضافة دور' : 'Add Role'}
          </button>
        </div>
      </header>

      <div style={{ display: 'flex', gap: 24, alignItems: 'flex-start' }}>
        <div style={{ flex: '0 0 320px', maxWidth: 320 }}>
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 12 }}>
            <button
              className="btn btn-ghost"
              onClick={() => setShowRoleList(!showRoleList)}
              style={{ display: 'flex', alignItems: 'center', gap: 8, padding: '8px 12px' }}
            >
              <Shield size={16} style={{ color: 'var(--primary)' }} />
              <span style={{ fontWeight: 600 }}>{ar ? 'الأدوار' : 'Roles'}</span>
              <span style={{ fontSize: 12, color: 'var(--text-muted)', fontWeight: 400 }}>
                ({roles.length})
              </span>
              {showRoleList ? <ChevronUp size={14} /> : <ChevronDown size={14} />}
            </button>
          </div>

          {showRoleList && (
            <div style={{
              maxHeight: 'calc(100vh - 280px)',
              overflowY: 'auto',
              paddingRight: 4,
              display: 'flex',
              flexDirection: 'column',
              gap: 6,
            }}>
              {roles.map(role => (
                <RoleCard
                  key={role.id}
                  role={role}
                  selected={saveTarget?.id === role.id}
                  onEdit={handleEditRole}
                  onDelete={handleDeleteRole}
                  onClone={handleCloneRole}
                  disabled={role.key === 'admin'}
                  canEdit={can('roles', 'edit')}
                  canDelete={can('roles', 'delete')}
                  ar={ar}
                />
              ))}
            </div>
          )}
        </div>

        <div style={{ flex: 1 }}>
          <div style={{
            background: 'var(--card)',
            borderRadius: 'var(--radius)',
            border: '1px solid var(--border)',
            overflow: 'hidden'
          }}>
            <div style={{
              padding: '12px 16px',
              borderBottom: '1px solid var(--border)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'space-between',
              background: 'var(--bg-2)',
            }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                <Key size={14} style={{ color: 'var(--primary)' }} />
                <span style={{ fontWeight: 600 }}>
                  {selectedRoleName}
                </span>
              </div>
              <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                <span className="text-xs text-muted">
                  {ar ? 'الصلاحيات المحددة:' : 'Selected:'} <span style={{ fontWeight: 600 }}>{totalPermissions}</span>
                </span>
                {saveTarget && can('roles', 'edit') && (
                  <button
                    className="btn btn-primary btn-sm"
                    onClick={handleSavePermissions}
                    disabled={isSaving}
                    style={{ display: 'flex', alignItems: 'center', gap: 6 }}
                  >
                    {isSaving ? <div className="ft-spinner" style={{ width: 14, height: 14 }} /> : <Save size={14} />}
                    {isSaving ? (ar ? 'جاري الحفظ...' : 'Saving...') : (ar ? 'حفظ الصلاحيات' : 'Save Permissions')}
                  </button>
                )}
              </div>
            </div>
            <div style={{ padding: '16px' }}>
              <PermissionMatrix
                modules={modules}
                permissions={permissionChanges}
                onChange={handlePermissionChange}
                readOnly={!can('roles', 'edit')}
                lang={lang}
              />
            </div>
          </div>
        </div>
      </div>

      {showModal && (
        <div className="modal-overlay" onClick={e => e.target === e.currentTarget && setShowModal(false)}>
          <div className="modal-box">
            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 16 }}>
            <h2 style={{ fontSize: 18, fontWeight: 600 }}>
              {saveTarget
                ? (ar ? 'تعديل الدور' : 'Edit Role')
                : (ar ? 'إضافة دور جديد' : 'Add New Role')}
            </h2>
            <button className="btn btn-ghost btn-sm" onClick={() => setShowModal(false)} style={{ padding: 4 }}>
              <X size={18} />
            </button>
          </div>
          <form onSubmit={(e) => { e.preventDefault(); handleSave(); }}>
            <div style={{ display: 'grid', gap: 12, gridTemplateColumns: '1fr 1fr' }}>
              <div>
                <label className="form-label">{ar ? 'الاسم بالعربية' : 'Name (Arabic)'}</label>
                <input className="form-control" value={form.name} onChange={e => setForm({...form, name: e.target.value})} required />
              </div>
              <div>
                <label className="form-label">{ar ? 'الاسم بالإنجليزية' : 'Name (English)'}</label>
                <input className="form-control" value={form.nameEn} onChange={e => setForm({...form, nameEn: e.target.value})} />
              </div>
              <div>
                <label className="form-label">{ar ? 'المفتاح' : 'Key'}</label>
                <input className="form-control" value={form.key} onChange={e => setForm({...form, key: e.target.value})} placeholder={ar ? 'سيتم إنشاؤه تلقائياً' : 'auto-generated'} />
              </div>
              <div>
                <label className="form-label">{ar ? 'الحالة' : 'Status'}</label>
                <select className="form-control" value={form.isActive ? 'active' : 'inactive'} onChange={e => setForm({...form, isActive: e.target.value === 'active'})}>
                  <option value="active">{ar ? 'نشط' : 'Active'}</option>
                  <option value="inactive">{ar ? 'غير نشط' : 'Inactive'}</option>
                </select>
              </div>
              <div style={{ gridColumn: 'span 2' }}>
                <label className="form-label">{ar ? 'الوصف بالعربية' : 'Description (Arabic)'}</label>
                <input className="form-control" value={form.description} onChange={e => setForm({...form, description: e.target.value})} />
              </div>
              <div style={{ gridColumn: 'span 2' }}>
                <label className="form-label">{ar ? 'الوصف بالإنجليزية' : 'Description (English)'}</label>
                <input className="form-control" value={form.descriptionEn} onChange={e => setForm({...form, descriptionEn: e.target.value})} />
              </div>
            </div>

            <div style={{ marginTop: 16, display: 'flex', gap: 8, justifyContent: 'flex-end' }}>
              <button type="button" className="btn btn-ghost" onClick={() => setShowModal(false)}>
                {ar ? 'إلغاء' : 'Cancel'}
              </button>
              <button type="submit" className="btn btn-primary" disabled={isSaving || !can('roles', 'edit')} style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
                {isSaving ? <div className="ft-spinner" style={{ width: 14, height: 14 }} /> : <Save size={14} />}
                {isSaving
                  ? (ar ? 'جاري الحفظ...' : 'Saving...')
                  : (saveTarget ? (ar ? 'تحديث' : 'Update') : (ar ? 'إنشاء' : 'Create'))}
              </button>
            </div>
          </form>
          </div>
        </div>
      )}

      {confirmDeleteRole && (
        <div className="modal-overlay" onClick={e => e.target === e.currentTarget && setConfirmDeleteRole(null)}>
          <div className="modal-box">
          <div style={{ textAlign: 'center', padding: 20 }}>
            <h3 style={{ fontSize: 18, fontWeight: 600, marginBottom: 12 }}>
              {ar ? 'حذف الدور' : 'Delete Role'}
            </h3>
            <p style={{ fontSize: 14, color: 'var(--text-secondary)', marginBottom: 8 }}>
              {ar ? 'هل أنت متأكد من حذف الدور' : 'Are you sure you want to delete the role'}
            </p>
            <p style={{ fontSize: 16, fontWeight: 600, color: 'var(--text-primary)', marginBottom: 20 }}>
              "{confirmDeleteRole.name}"?
            </p>
            <div style={{ display: 'flex', gap: 8, justifyContent: 'center' }}>
              <button className="btn btn-ghost" onClick={() => setConfirmDeleteRole(null)} style={{ padding: '8px 20px' }}>
                {ar ? 'إلغاء' : 'Cancel'}
              </button>
              <button className="btn btn-danger" onClick={confirmDelete} style={{ display: 'flex', alignItems: 'center', gap: 6, padding: '8px 20px' }}>
                <Trash2 size={14} />
                {ar ? 'حذف' : 'Delete'}
              </button>
            </div>
          </div>
          </div>
        </div>
      )}
    </div>
  );
}

function RoleCard({ role, selected, onEdit, onDelete, onClone, disabled, canEdit, canDelete, ar }) {
  const isSelected = selected;

  return (
    <div
      style={{
        background: isSelected ? 'var(--bg-selected, var(--bg-2))' : 'var(--card)',
        border: `1px solid ${isSelected ? 'var(--primary)' : 'var(--border)'}`,
        borderRadius: 'var(--radius-sm)',
        padding: '12px 14px',
        cursor: 'pointer',
        transition: 'all 0.15s ease',
        opacity: disabled ? 0.6 : 1,
      }}
      onClick={() => !disabled && onEdit(role)}
    >
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 6 }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 8, minWidth: 0 }}>
          <Shield size={14} style={{ color: 'var(--primary)', flexShrink: 0 }} />
          <span style={{ fontWeight: 600, fontSize: 14, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
            {ar ? (role.nameAr || role.name) : (role.nameEn || role.name)}
          </span>
        </div>
        <div style={{ display: 'flex', gap: 4, flexShrink: 0 }}>
          {!disabled && canEdit && (
            <button className="btn btn-ghost btn-sm" onClick={(e) => { e.stopPropagation(); onClone(role); }} title={ar ? 'نسخ' : 'Clone'} style={{ padding: 4 }}>
              <Copy size={14} />
            </button>
          )}
          {!disabled && canDelete && (
            <button className="btn btn-ghost btn-sm" onClick={(e) => { e.stopPropagation(); onDelete(role); }} title={ar ? 'حذف' : 'Delete'} style={{ padding: 4, color: 'var(--danger)' }}>
              <Trash2 size={14} />
            </button>
          )}
        </div>
      </div>
      <div style={{ display: 'flex', gap: 6, alignItems: 'center' }}>
        {role.permissionCount !== undefined && (
          <span style={{
            background: 'var(--primary-light, rgba(79,70,229,0.1))',
            color: 'var(--primary)',
            padding: '2px 8px',
            borderRadius: 'var(--radius-xs)',
            fontSize: 11,
            fontWeight: 500,
          }}>
            {role.permissionCount} {ar ? 'صلاحية' : 'perms'}
          </span>
        )}
        {!role.isActive && (
          <span style={{
            background: 'var(--danger-light, rgba(239,68,68,0.1))',
            color: 'var(--danger, #ef4444)',
            padding: '2px 8px',
            borderRadius: 'var(--radius-xs)',
            fontSize: 11,
            fontWeight: 500,
          }}>
            {ar ? 'غير نشط' : 'Inactive'}
          </span>
        )}
      </div>
    </div>
  );
}
