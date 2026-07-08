import React, { useEffect, useMemo, useState } from 'react';
import { Users as UsersIcon, ShieldCheck, Shield, Trash2, Edit2, LogIn, Plus, X, KeyRound, ChevronUp, ChevronDown } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import client from '../api/client';

/* ── Map backend API ↔ frontend form field names ── */
function normalizeUser(u, rolesList) {
  const rawRole = Array.isArray(u.roles) ? (u.roles[0] || '') : (u.role || '');
  const matchedRole = rolesList.find(r => r.key === rawRole || r.name === rawRole);
  return {
    ...u,
    name: u.fullName || u.userName || '',
    role: matchedRole ? matchedRole.key : rawRole,
    phone: String(u.phoneNumber || '').replace(/\D/g, '').startsWith('967')
      ? String(u.phoneNumber).replace(/\D/g, '').slice(3)
      : String(u.phoneNumber || '').replace(/\D/g, ''),
    scopes: u.scopes || [],
  };
}
function normalizeForApi(form) {
  return {
    userName: form.name,
    fullName: form.name,
    email: form.email,
    phoneNumber: form.phone ? `967${form.phone}` : '',
    roles: form.role ? [form.role] : [],
    scopes: form.scopes || [],
  };
}

const initialForm = {
  name: '',
  email: '',
  password: '',
  role: 'operator',
  phone: '',
  scopes: [],
};

const ROLE_HIERARCHY = {
  'admin': 100,
  'supervisor': 50,
  'inventory_head': 45,
  'fuel_manager': 40,
  'operator': 10
};

const SHIFT_OPTIONS = [
  { type: 'day', duration: 12, ar: 'صباحية - 12 ساعة', en: 'Day - 12 hours' },
  { type: 'night', duration: 12, ar: 'مسائية - 12 ساعة', en: 'Night - 12 hours' },
  { type: 'dual', duration: 12, ar: 'صباحية ومسائية - كل 12 ساعة', en: 'Day + Night - every 12 hours' },
  { type: 'full', duration: 24, ar: 'كاملة - 24 ساعة', en: 'Full - 24 hours' },
];

const SHIFT_ASSIGNABLE_ROLES = new Set(['operator', 'Operation', 'operation']);
const isShiftAssignableRole = (role) => SHIFT_ASSIGNABLE_ROLES.has(role);

function Users({ t, lang, currentUser }) {
  const ar = lang === 'ar';
  
  const perms = currentUser?.permissions?.users || {};
  const canManage = currentUser?.role === 'admin' || perms.manage;
  const [users, setUsers] = useState([]);
  const [branches, setBranches] = useState([]);
  const [stations, setStations] = useState([]);
  const [subStations, setSubStations] = useState([]);
  const [roles, setRoles] = useState([]);
  
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [authError, setAuthError] = useState(false);
  
  const [showModal, setShowModal] = useState(false);
  const [resetUser, setResetUser] = useState(null);
  const [resetPassword, setResetPassword] = useState('');
  const [editId, setEditId] = useState(null);
  const [form, setForm] = useState(initialForm);
  const [saving, setSaving] = useState(false);

  const navigate = useNavigate();

  const loadData = async () => {
    setLoading(true); setError(null);
    try {
      const [uRes, bRes, sRes, subRes, rRes] = await Promise.allSettled([
        client.get('/api/users'),
        client.get('/api/branches'),
        client.get('/api/stations'),
        client.get('/api/sub-stations'),
        client.get('/api/roles'),
      ]);

      const getValue = (res, fallback) => {
        if (res.status === 'fulfilled') {
          const d = res.value.data;
          return Array.isArray(d) ? d : (d?.items || fallback);
        }
        return fallback;
      };

      const rawUsers = getValue(uRes, []);
      setUsers(rawUsers.filter(u => u.isActive !== false).map(u => normalizeUser(u, getValue(rRes, []))));
      setBranches(getValue(bRes, []));
      setStations(getValue(sRes, []));
      setSubStations(getValue(subRes, []).filter(s => s.isActive !== false));
      setRoles(getValue(rRes, []));

      const authFailed = [uRes, bRes, sRes, subRes, rRes].some(
        r => r.status === 'rejected' && (r.reason?.response?.status === 401 || r.reason?.response?.status === 403)
      );
      if (authFailed) { setAuthError(true); return; }

      if (uRes.status === 'rejected') setError(t.noData);
    } catch (err) {
      if (err.response?.status === 401 || err.response?.status === 403) setAuthError(true);
      else setError(t.noData);
    } finally { setLoading(false); }
  };

  useEffect(() => { loadData(); }, [t]);

  const openAdd = () => { setForm(initialForm); setEditId(null); setShowModal(true); };

  const openEdit = async (u) => {
    try {
      const res = await client.get(`/api/users/${u.id}`);
      const detail = res.data;
      const detailRole = Array.isArray(detail.roles) ? (detail.roles[0] || '') : (detail.role || '');
      const matchedRole = roles.find(r => r.key === detailRole || r.name === detailRole);
      setForm({
        name: detail.fullName || detail.userName || u.name || '',
        email: detail.email || u.email || '',
        password: '',
        role: matchedRole ? matchedRole.key : (detailRole || u.role || ''),
        phone: normalizePhoneForInput(detail.phoneNumber || u.phone),
        scopes: detail.scopes || u.scopes || [],
      });
    } catch {
      setForm({
        name: u.name || '',
        email: u.email || '',
        password: '',
        role: u.role || '',
        phone: normalizePhoneForInput(u.phone || u.phoneNumber),
        scopes: u.scopes || [],
      });
    }
    setEditId(u.id);
    setShowModal(true);
  };

  const closeModal = () => setShowModal(false);
  const fld = (k, v) => setForm(f => ({ ...f, [k]: v }));
  const fldPhone = (v) => fld('phone', String(v || '').replace(/\D/g, '').slice(0, 9));
  const fldRole = (value) => {
    setForm(f => ({
      ...f,
      role: value,
      assignedShiftType: isShiftAssignableRole(value) ? (f.assignedShiftType || 'day') : '',
      assignedShiftDuration: isShiftAssignableRole(value) ? (f.assignedShiftDuration || 12) : null,
    }));
  };

  function normalizePhoneForInput(phone) {
    const digits = String(phone || '').replace(/\D/g, '');
    return digits.startsWith('967') ? digits.slice(3, 12) : digits.slice(0, 9);
  }

  /* ── Scope row helpers ── */
  const addScope = () => {
    setForm(f => ({ ...f, scopes: [...f.scopes, { branchId: null, stationId: null, subStationId: null }] }));
  };
  const removeScope = (idx) => {
    setForm(f => ({ ...f, scopes: f.scopes.filter((_, i) => i !== idx) }));
  };
  const updateScope = (idx, field, value) => {
    setForm(f => {
      const next = [...f.scopes];
      const updated = { ...next[idx], [field]: value || null };
      if (field === 'branchId') { updated.stationId = null; updated.subStationId = null; }
      if (field === 'stationId') { updated.subStationId = null; }
      next[idx] = updated;
      return { ...f, scopes: next };
    });
  };

  const stationsForBranch = (branchId) =>
    branchId ? stations.filter(s => s.branchId === branchId) : [];
  const subStationsForStation = (stationId) =>
    stationId ? subStations.filter(ss => ss.stationId === stationId) : [];

  /* ── Scope validation ── */
  const hasGlobalScope = form.scopes.some(s => !s.branchId && !s.stationId && !s.subStationId);
  const scopeCount = form.scopes.length;
  const isGlobalOnly = hasGlobalScope && scopeCount === 1;

  /* ── Save ── */
  const handleSave = async () => {
    if (!form.name || !form.email) return alert(ar ? 'الاسم والبريد مطلوبان' : 'Name and email are required');
    if (!editId && !form.password) return alert(ar ? 'كلمة المرور مطلوبة' : 'Password is required');
    if (!editId && (form.password.length < 8 || !/[A-Z]/.test(form.password) || !/[a-z]/.test(form.password) || !/\d/.test(form.password)))
      return alert(ar ? 'كلمة المرور يجب أن تكون 8 أحرف على الأقل وتحتوي حرفاً كبيراً وصغيراً ورقماً' : 'Password must be at least 8 characters with uppercase, lowercase, and a digit');
    if (!/^\d{9}$/.test(form.phone)) return alert(ar ? 'رقم الهاتف مطلوب: أدخل 9 أرقام بعد مفتاح اليمن 967' : 'Phone is required: enter 9 digits after Yemen code 967');
    
    setSaving(true);
    try {
      if (editId) {
        const updatePayload = {
          fullName: form.name,
          email: form.email,
          phoneNumber: form.phone ? `967${form.phone}` : '',
        };
        await client.put(`/api/users/${editId}`, updatePayload);
        if (form.role) {
          await client.post(`/api/users/${editId}/assign-roles`, [form.role]);
        }
        await client.post(`/api/users/${editId}/scopes`, { scopes: form.scopes });
      } else {
        const payload = normalizeForApi(form);
        payload.password = form.password;
        await client.post('/api/users', payload);
      }
      closeModal();
      loadData();
    } catch (err) {
      const errData = err.response?.data;
      alert(typeof errData === 'string' ? errData : errData?.msg || errData?.detail || 'Error');
    } finally {
      setSaving(false);
    }
  };

  const openResetPassword = (user) => { setResetUser(user); setResetPassword(''); };
  const closeResetPassword = () => { setResetUser(null); setResetPassword(''); };

  const handleResetPassword = async () => {
    if (!resetUser) return;
    if (resetPassword.length < 7 || !/[A-Za-z]/.test(resetPassword) || !/\d/.test(resetPassword) || !/[^A-Za-z0-9]/.test(resetPassword)) {
      return alert(ar ? 'كلمة المرور يجب أن تكون 7 أحرف على الأقل وتحتوي حرفاً ورقماً ورمزاً' : 'Password must be at least 7 characters and include a letter, number, and symbol');
    }
    setSaving(true);
    try {
      await client.patch(`/api/users/${resetUser.id}/reset-password`, { password: resetPassword });
      alert(ar ? 'تمت إعادة تعيين كلمة المرور، وسيُطلب من المستخدم تغييرها عند الدخول' : 'Password reset. The user must change it on next login.');
      closeResetPassword();
      loadData();
    } catch (err) {
      alert(err.response?.data?.msg || 'Error');
    } finally { setSaving(false); }
  };

  const removeUser = async (id) => {
    if (!window.confirm(t.confirmDeleteUser)) return;
    try {
      await client.delete(`/api/users/${id}`);
      setUsers(users.filter(u => u.id !== id));
    } catch (e) { alert(e.response?.data?.msg || (ar ? 'فشل الحذف' : 'Failed to delete')); }
  };

  const getRoleName = (key) => {
    const r = roles.find(x => x.key === key);
    return r ? r.name : key;
  };

  const getScopeLabel = (scope) => {
    if (!scope.branchId && !scope.stationId && !scope.subStationId) return ar ? 'الكل' : 'All';
    const parts = [];
    if (scope.branchId) {
      const b = branches.find(x => x.id === scope.branchId);
      parts.push(b?.name || `Branch #${scope.branchId}`);
    }
    if (scope.stationId) {
      const s = stations.find(x => x.id === scope.stationId);
      parts.push(s?.name || `Station #${scope.stationId}`);
    }
    if (scope.subStationId) {
      const ss = subStations.find(x => x.id === scope.subStationId);
      parts.push(ss?.name || `Sub #${scope.subStationId}`);
    }
    return parts.join(' → ');
  };

  if (authError) return (
    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', minHeight: '60vh' }}>
      <div className="glass-card" style={{ maxWidth: 420, width: '100%', textAlign: 'center', padding: 48 }}>
        <ShieldCheck size={52} style={{ color: 'var(--primary)', marginBottom: 16 }} />
        <h2 style={{ color: 'var(--text)', marginBottom: 8 }}>{t.userManagement}</h2>
        <p style={{ color: 'var(--text-muted)', marginBottom: 24 }}>{t.loginFirst}</p>
        <button className="btn btn-primary" onClick={() => navigate('/login')}>
          <LogIn size={15} /> {t.login}
        </button>
      </div>
    </div>
  );

  return (
    <div>
      <div className="page-header">
        <div className="page-header-row">
          <div>
            <h1>{t.userManagement}</h1>
            <p>{t.manageUsers}</p>
          </div>
          {canManage && (
            <button className="btn btn-primary" onClick={openAdd}>
              <Plus size={16} /> {ar ? 'مستخدم جديد' : 'New User'}
            </button>
          )}
        </div>
      </div>

      {error && <div className="alert alert-danger">{error}</div>}

      {loading ? (
        <div className="spinner-wrap"><div className="ft-spinner" /></div>
      ) : (
        <div className="grid-auto">
          {users.length === 0 ? (
            <div className="empty-state">
              <UsersIcon size={48} />
              <h3>{t.noData}</h3>
            </div>
          ) : users.map(user => (
            <div key={user.id} className="glass-card" style={{ padding: 20 }}>
              <div style={{ display: 'flex', alignItems: 'flex-start', gap: 14, marginBottom: 16 }}>
                <div style={{
                  width: 44, height: 44, borderRadius: '50%', flexShrink: 0,
                  background: user.role === 'admin'
                    ? 'linear-gradient(135deg, var(--primary), hsl(270,70%,65%))'
                    : 'linear-gradient(135deg, var(--secondary), var(--primary))',
                  display: 'flex', alignItems: 'center', justifyContent: 'center',
                  fontSize: 16, fontWeight: 700, color: '#fff',
                }}>
                  {(user.name || user.email || '?')[0].toUpperCase()}
                </div>
                <div style={{ flex: 1, minWidth: 0 }}>
                  <div style={{ fontWeight: 700, color: 'var(--text)', fontSize: 15, whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
                    {user.name || (ar ? 'بدون اسم' : 'No name')}
                  </div>
                  <div style={{ fontSize: 13, color: 'var(--text-muted)', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
                    {user.email}
                  </div>
                  <div style={{ marginTop: 6, display: 'flex', gap: 6, flexWrap: 'wrap' }}>
                    <span className={`badge ${user.role === 'admin' ? 'badge-primary' : user.role === 'supervisor' ? 'badge-warning' : 'badge-muted'}`}>
                      {user.role === 'admin' ? <ShieldCheck size={11} /> : <Shield size={11} />}
                      {getRoleName(user.role)}
                    </span>
                    {user.mustChangePassword && (
                      <span className="badge badge-warning">
                        {ar ? 'تغيير كلمة المرور مطلوب' : 'Password change required'}
                      </span>
                    )}
                  </div>
                </div>
              </div>
              {canManage && (
                <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
                  <button className="btn btn-sm btn-ghost" style={{ flex: 1 }} onClick={() => openEdit(user)}>
                    <Edit2 size={13} /> {t.edit}
                  </button>
                  <button className="btn btn-sm btn-ghost" style={{ flex: 1 }} onClick={() => openResetPassword(user)}>
                    <KeyRound size={13} /> {ar ? 'إعادة المرور' : 'Reset'}
                  </button>
                  <button className="btn btn-danger btn-sm btn-icon" onClick={() => removeUser(user.id)}>
                    <Trash2 size={14} />
                  </button>
                </div>
              )}
            </div>
          ))}
        </div>
      )}

      {/* ─── Modal ─── */}
      {showModal && (
        <div className="modal-overlay" onClick={e => e.target === e.currentTarget && closeModal()}>
          <div className="modal-box" style={{ maxWidth: 600 }}>
            <div className="modal-header">
              <div className="modal-title">{editId ? (ar ? 'تعديل مستخدم' : 'Edit User') : (ar ? 'إضافة مستخدم' : 'Add User')}</div>
              <button className="modal-close" onClick={closeModal}><X size={18} /></button>
            </div>
            <div className="modal-body">
              <div className="form-group">
                <label className="form-label">{ar ? 'الاسم *' : 'Name *'}</label>
                <input className="form-control" value={form.name} onChange={e => fld('name', e.target.value)} dir="auto" />
              </div>
              <div className="form-group">
                <label className="form-label">{ar ? 'البريد الإلكتروني *' : 'Email *'}</label>
                <input className="form-control" type="email" value={form.email} onChange={e => fld('email', e.target.value)} />
              </div>
              {!editId && (
                <div className="form-group">
                  <label className="form-label">{ar ? 'كلمة المرور *' : 'Password *'}</label>
                  <input
                    className="form-control"
                    type="password"
                    autoComplete="new-password"
                    value={form.password}
                    onChange={e => fld('password', e.target.value)}
                  />
                </div>
              )}
              
              <div className="form-row">
                <div className="form-group">
                  <label className="form-label">{ar ? 'الدور الوظيفي' : 'Role'}</label>
                  <select className="form-select" value={form.role} onChange={e => fldRole(e.target.value)}>
                    {roles
                      .filter(r => {
                        if (currentUser?.role === 'admin') return true;
                        const myWeight = ROLE_HIERARCHY[currentUser?.role] || 0;
                        const targetWeight = ROLE_HIERARCHY[r.key] || 0;
                        return targetWeight < myWeight;
                      })
                      .map(r => (
                        <option key={r.key} value={r.key}>{r.name}</option>
                      ))}
                  </select>
                </div>
                <div className="form-group">
                  <label className="form-label">{ar ? 'رقم الهاتف *' : 'Phone *'}</label>
                  <div className="phone-input-wrap" dir="ltr">
                    <span className="phone-prefix">+967</span>
                    <input
                      className="form-control phone-local-input"
                      value={form.phone}
                      onChange={e => fldPhone(e.target.value)}
                      inputMode="numeric"
                      maxLength={9}
                      placeholder="7XXXXXXXX"
                    />
                  </div>
                </div>
              </div>

              {isShiftAssignableRole(form.role) && (
                <div className="form-group">
                  <label className="form-label">{ar ? 'وردية المشغل' : 'Operator Shift'}</label>
                  <select
                    className="form-select"
                    value={form.assignedShiftType || 'day'}
                    onChange={e => {
                      const opt = SHIFT_OPTIONS.find(x => x.type === e.target.value) || SHIFT_OPTIONS[0];
                      setForm(f => ({ ...f, assignedShiftType: opt.type, assignedShiftDuration: opt.duration }));
                    }}
                  >
                    {SHIFT_OPTIONS.map(opt => (
                      <option key={opt.type} value={opt.type}>{ar ? opt.ar : opt.en}</option>
                    ))}
                  </select>
                </div>
              )}

              {/* ── Scope Selector ── */}
              <div className="form-group">
                <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 8 }}>
                  <label className="form-label" style={{ marginBottom: 0, color: 'var(--primary)' }}>
                    {ar ? 'نطاق البيانات' : 'Data Scope'}
                  </label>
                  {!isGlobalOnly && (
                    <button className="btn btn-sm btn-ghost" onClick={addScope} type="button">
                      <Plus size={13} /> {ar ? 'إضافة نطاق' : 'Add Scope'}
                    </button>
                  )}
                </div>
                <div style={{ fontSize: 12, color: 'var(--text-muted)', marginBottom: 8 }}>
                  {ar
                    ? 'حدد نطاق البيانات الذي يمكن للمستخدم الوصول إليه. "الكل" تعني الوصول الكامل.'
                    : 'Define which data the user can access. "All" means full access.'}
                </div>
                {hasGlobalScope && (
                  <div style={{ fontSize: 12, color: 'var(--warning)', marginBottom: 8 }}>
                    {ar ? 'النطاق العالمي يمنح وصولاً كاملاً.' : 'Global scope grants full access to all data.'}
                  </div>
                )}
                <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
                  {form.scopes.map((scope, idx) => (
                    <ScopeRow
                      key={idx}
                      idx={idx}
                      scope={scope}
                      ar={ar}
                      branches={branches}
                      stations={stations}
                      subStations={subStations}
                      stationsForBranch={stationsForBranch}
                      subStationsForStation={subStationsForStation}
                      updateScope={updateScope}
                      removeScope={removeScope}
                      canRemove={form.scopes.length > 1}
                    />
                  ))}
                  {form.scopes.length === 0 && (
                    <div style={{ fontSize: 12, color: 'var(--text-muted)', padding: 12, textAlign: 'center', background: 'var(--bg-secondary)', borderRadius: 8 }}>
                      {ar ? 'لم يتم تعيين أي نطاق' : 'No scopes assigned'}
                    </div>
                  )}
                </div>
              </div>
            </div>
            <div className="modal-footer">
              <button className="btn btn-ghost" onClick={closeModal}>{ar ? 'إلغاء' : 'Cancel'}</button>
              <button className="btn btn-primary" onClick={handleSave} disabled={saving}>
                {saving
                  ? <><span className="ft-spinner" style={{ width: 14, height: 14 }} /> {ar ? 'جاري الحفظ...' : 'Saving...'}</>
                  : (editId ? (ar ? 'تحديث المستخدم' : 'Update User') : (ar ? 'إضافة مستخدم' : 'Add User'))}
              </button>
            </div>
          </div>
        </div>
      )}

      {resetUser && (
        <div className="modal-overlay" onClick={e => e.target === e.currentTarget && closeResetPassword()}>
          <div className="modal-box" style={{ maxWidth: 420 }}>
            <div className="modal-header">
              <div className="modal-title">{ar ? 'إعادة تعيين كلمة المرور' : 'Reset Password'}</div>
              <button className="modal-close" onClick={closeResetPassword}><X size={18} /></button>
            </div>
            <div className="modal-body">
              <div style={{ marginBottom: 14, color: 'var(--text-muted)', fontSize: 13 }}>
                {resetUser.name || resetUser.email}
              </div>
              <div className="form-group">
                <label className="form-label">{ar ? 'كلمة المرور الجديدة *' : 'New Password *'}</label>
                <input
                  className="form-control"
                  type="password"
                  value={resetPassword}
                  onChange={e => setResetPassword(e.target.value)}
                  placeholder={ar ? '7 أحرف مع رقم ورمز' : '7 chars with number and symbol'}
                />
              </div>
              <div style={{ color: 'var(--text-faint)', fontSize: 12 }}>
                {ar
                  ? 'بعد الحفظ سيطلب النظام من المستخدم تغيير كلمة المرور عند أول دخول.'
                  : 'After saving, the user will be forced to change it on next login.'}
              </div>
            </div>
            <div className="modal-footer">
              <button className="btn btn-ghost" onClick={closeResetPassword}>{ar ? 'إلغاء' : 'Cancel'}</button>
              <button className="btn btn-primary" onClick={handleResetPassword} disabled={saving}>
                {saving ? <><span className="ft-spinner" style={{ width: 14, height: 14 }} /> {ar ? 'جاري الحفظ...' : 'Saving...'}</> : (ar ? 'إعادة التعيين' : 'Reset')}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

/* ── ScopeRow: cascading Branch → Station → SubStation dropdowns ── */
function ScopeRow({ idx, scope, ar, branches, stations, subStations, stationsForBranch, subStationsForStation, updateScope, removeScope, canRemove }) {
  const stationOpts = stationsForBranch(scope.branchId);
  const subStationOpts = subStationsForStation(scope.stationId);
  const isGlobal = !scope.branchId && !scope.stationId && !scope.subStationId;

  return (
    <div style={{
      display: 'flex', gap: 6, alignItems: 'center', flexWrap: 'wrap',
      padding: '8px 10px', borderRadius: 8,
      background: isGlobal ? 'var(--primary-bg, rgba(59,130,246,0.08))' : 'var(--bg-secondary)',
      border: '1px solid var(--border)',
    }}>
      <div style={{ flex: '1 1 140px', minWidth: 0 }}>
        <select
          className="form-select"
          value={scope.branchId || ''}
          onChange={e => updateScope(idx, 'branchId', e.target.value ? Number(e.target.value) : null)}
          style={{ fontSize: 13 }}
        >
          <option value="">{ar ? 'الكل (عالمي)' : 'All (Global)'}</option>
          {branches.map(b => <option key={b.id} value={b.id}>{b.name}</option>)}
        </select>
      </div>

      {scope.branchId && (
        <div style={{ flex: '1 1 140px', minWidth: 0 }}>
          <select
            className="form-select"
            value={scope.stationId || ''}
            onChange={e => updateScope(idx, 'stationId', e.target.value ? Number(e.target.value) : null)}
            style={{ fontSize: 13 }}
          >
            <option value="">{ar ? 'الكل في الفرع' : 'All in Branch'}</option>
            {stationOpts.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
          </select>
        </div>
      )}

      {scope.stationId && (
        <div style={{ flex: '1 1 140px', minWidth: 0 }}>
          <select
            className="form-select"
            value={scope.subStationId || ''}
            onChange={e => updateScope(idx, 'subStationId', e.target.value ? Number(e.target.value) : null)}
            style={{ fontSize: 13 }}
          >
            <option value="">{ar ? 'الكل في المحطة' : 'All in Station'}</option>
            {subStationOpts.map(ss => <option key={ss.id} value={ss.id}>{ss.name}</option>)}
          </select>
        </div>
      )}

      {canRemove && (
        <button
          className="btn btn-sm btn-icon btn-danger"
          onClick={() => removeScope(idx)}
          type="button"
          style={{ flexShrink: 0 }}
        >
          <X size={14} />
        </button>
      )}
    </div>
  );
}

export default Users;
