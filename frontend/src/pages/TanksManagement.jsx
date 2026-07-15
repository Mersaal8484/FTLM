import React, { useState, useEffect, useCallback, useMemo } from 'react';
import { Plus, Droplets, Pencil, Trash2, Save, X, AlertCircle, RefreshCw, MapPin } from 'lucide-react';
import client from '../api/client';

/**
 * صفحة إعداد خزانات الوقود
 * يتصل بـ:
 *   GET/POST/PUT/DELETE /api/fuel-tanks
 *   GET /api/sub-stations  → لربط الخزان بالمنطقة (subStation)
 *   GET /api/stations      → لعرض اسم المحطة
 *   GET /api/lookups/by-category/FuelType → لأنواع الوقود
 */

const emptyForm = {
  code: '', name: '',
  capacityLiters: '', minAlertLiters: '', deadStockLiters: '', actualStockLiters: '',
  fuelLookupId: '',
  branchId: '', stationId: '', subStation: '',
  isActive: true,
  notes: '',
};

function TanksManagement({ lang, currentUser }) {
  const ar = lang === 'ar';

  // Permission helpers
  const perms = currentUser?.permissions?.tanks || {};
  const canAdd = currentUser?.role === 'admin' || perms.create || perms.add;
  const canEdit = currentUser?.role === 'admin' || perms.edit;
  const canDelete = currentUser?.role === 'admin' || perms.delete;

  // ─── Data ────────────────────────────────────────────────────────────────────
  const [tanks, setTanks]           = useState([]);
  const [branches, setBranches]     = useState([]);
  const [subStations, setSubStations] = useState([]);
  const [stations, setStations]     = useState([]);
  const [fuelTypes, setFuelTypes]   = useState([]);
  const [loading, setLoading]       = useState(true);
  const [error, setError]           = useState('');

  // ─── UI ──────────────────────────────────────────────────────────────────────
  const [showForm, setShowForm]     = useState(false);
  const [editItem, setEditItem]     = useState(null);
  const [form, setForm]             = useState(emptyForm);
  const [formError, setFormError]   = useState('');
  const [saving, setSaving]         = useState(false);
  const [deleteConfirm, setDeleteConfirm] = useState(null);
  const [search, setSearch]         = useState('');

  // ─── Fetch ───────────────────────────────────────────────────────────────────
  const fetchAll = useCallback(async () => {
    setLoading(true); setError('');
    try {
      const [t, ss, s, ft, br] = await Promise.all([
        client.get('/api/fuel-tanks', { params: { _t: Date.now() } }),
        client.get('/api/sub-stations', { params: { _t: Date.now() } }),
        client.get('/api/stations', { params: { _t: Date.now() } }),
        client.get('/api/lookups/by-category/FuelType', { params: { _t: Date.now() } }).catch(() => ({ data: [] })),
        client.get('/api/branches', { params: { _t: Date.now() } }).catch(() => ({ data: [] })),
      ]);
      setTanks(t.data?.map?.(tk => ({ ...tk, isActive: tk.isActive !== false })) ?? t.data);
      setSubStations(ss.data?.filter(s => s.isActive !== false));
      setStations(s.data);
      setFuelTypes(ft.data || []);
      setBranches(br.data ?? []);
    } catch {
      setError(ar ? 'فشل تحميل البيانات.' : 'Failed to load data.');
    } finally {
      setLoading(false);
    }
  }, [ar]);

  useEffect(() => { fetchAll(); }, [fetchAll]);

  // ─── Helpers ─────────────────────────────────────────────────────────────────
  const resetForm = () => { setForm(emptyForm); setEditItem(null); setShowForm(false); setFormError(''); };

  /** حساب نسبة ملء الخزان (0-100%) — يستخدم قراءة العداد إن وُجدت، وإلا المخزون الفعلي */
  const getFillPct = (tank) => {
    const capacity = Number(tank.capacityLiters) || 0;
    if (capacity <= 0) return null;
    const actual = Number(tank.meterReadingActual) || Number(tank.actualStockLiters) || 0;
    if (actual <= 0) return null;
    return Math.min(100, Math.round((actual / capacity) * 100));
  };

  const getFillColor = (pct) => {
    if (pct === null) return 'var(--text-faint)';
    if (pct <= 20) return '#ef4444';
    if (pct <= 50) return '#f59e0b';
    return '#10b981';
  };

  // ─── Frontend Validation ──────────────────────────────────────────────────────
  const validateForm = () => {
    const capacity = Number(form.capacityLiters);
    if (isNaN(capacity) || capacity <= 0) {
      setFormError(ar ? 'سعة الخزان يجب أن تكون أكبر من صفر.' : 'Tank capacity must be greater than zero.');
      return false;
    }
    if (!form.subStation) {
      setFormError(ar ? 'يجب ربط الخزان بمنطقة.' : 'Tank must be linked to an area.');
      return false;
    }
    setFormError('');
    return true;
  };

  const openAdd = () => { resetForm(); setShowForm(true); };
  const openEdit = (tank) => {
    const sub = subStations.find(s => s.id === tank.subStationId || s.subStationId === tank.subStationId);
    const staId = sub ? (sub.station_id ?? sub.stationId) : null;
    const sta = stations.find(s => s.id === staId);
    setForm({
      code: tank.code || '', name: tank.name || '',
      capacityLiters: tank.capacityLiters ?? '',
      minAlertLiters: tank.minAlertLiters ?? '',
      deadStockLiters: tank.deadStockLiters ?? '',
      actualStockLiters: tank.actualStockLiters ?? '',
      fuelLookupId: String(tank.fuelLookupId || tank.fuelLookup?.id || ''),
      branchId: String(sta?.branchId ?? sta?.branch_id ?? ''),
      stationId: String(staId ?? ''),
      subStation: String(tank.subStationId || ''),
      isActive: tank.isActive !== false,
      notes: tank.notes || '',
    });
    setEditItem(tank);
    setShowForm(true);
  };

  // ─── CRUD ────────────────────────────────────────────────────────────────────
  const handleSave = async () => {
    if (!validateForm()) return;

    setSaving(true);
    setFormError('');
    try {
      const payload = {
        name: form.name,
        code: form.code,
        capacityLiters: Number(form.capacityLiters),
        minAlertLiters: Number(form.minAlertLiters) || null,
        deadStockLiters: Number(form.deadStockLiters) || null,
        actualStockLiters: Number(form.actualStockLiters) || null,
        fuelLookupId: Number(form.fuelLookupId) || 0,
        subStationId: Number(form.subStation) || 0,
        isActive: form.isActive,
        notes: form.notes || null,
      };

      if (editItem) await client.put(`/api/fuel-tanks/${editItem.id}`, payload);
      else          await client.post('/api/fuel-tanks', payload);
      await fetchAll();
      resetForm();
    } catch (e) {
      const serverMsg = e.response?.data?.msg;
      const fallback  = ar ? 'فشل الحفظ. تحقق من البيانات وأعد المحاولة.' : 'Save failed. Please check the data and try again.';
      setFormError(serverMsg || fallback);
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async () => {
    if (!deleteConfirm) return;
    const { id } = deleteConfirm;
    try {
      await client.delete(`/api/fuel-tanks/${id}`);
      await fetchAll();
      } catch (err) {
        if (err.response?.status === 404) {
          setError(err.response?.data?.msg || (ar ? 'المورد غير موجود' : 'Not found'));
          setTanks(prev => prev.filter(t => t.id !== id));
        } else {
          setError(err.response?.data?.msg || (ar ? 'فشل الحذف' : 'Delete failed'));
        }
    }
    finally { setDeleteConfirm(null); }
  };

  // ─── Derived ─────────────────────────────────────────────────────────────────
  const filtered = tanks.filter(t =>
    t.name?.includes(search) || t.code?.includes(search)
  );

  const getSubName = (id) => subStations.find(s => s.id === id || s.subStationId === id)?.name || '—';
  const getStaName = (subId) => {
    const sub = subStations.find(s => s.id === subId || s.subStationId === subId);
    if (!sub) return ar ? 'غير محدد (بدون محطة)' : 'Unassigned';
    const staId = sub.station_id ?? sub.stationId;
    return stations.find(s => s.id === staId)?.name || (ar ? 'غير محدد (بدون محطة)' : 'Unassigned');
  };

  const filteredStations = useMemo(() =>
    form.branchId ? stations.filter(s => s.branchId === Number(form.branchId) || s.branch_id === Number(form.branchId)) : (form.branchId === '' ? stations : [])
  , [form.branchId, stations]);

  const filteredSubStations = useMemo(() =>
    form.stationId ? subStations.filter(s => (s.stationId ?? s.station_id) === Number(form.stationId)) : (form.stationId === '' ? subStations : [])
  , [form.stationId, subStations]);

  // ── تجميع الخزانات بناءً على المحطة (Group by Station) ──
  const groupedTanks = {};
  filtered.forEach(tank => {
    const stationName = getStaName(tank.subStationId);
    if (!groupedTanks[stationName]) groupedTanks[stationName] = [];
    groupedTanks[stationName].push(tank);
  });

  const secTitle = { background: 'hsla(217,91%,60%,0.15)', padding: '10px 16px', fontSize: 13, fontWeight: 700, color: 'var(--primary)', borderBottom: '1px solid var(--border)', display: 'flex', alignItems: 'center', gap: 8 };
  const inp = (val, onChange, placeholder = '', type = 'text') => (
    <input value={val} onChange={e => onChange(e.target.value)} className="form-control"
      placeholder={placeholder} type={type} style={{ padding: '8px 12px', fontSize: 13 }} />
  );

  return (
    <div>
      {/* Page Header */}
      <div className="page-header">
        <div className="page-header-row">
          <div>
            <h1>{ar ? 'إعداد خزانات الوقود' : 'Tanks Management'}</h1>
            <p>{ar ? 'إضافة وتعديل خزانات الوقود وربطها بالمواقع والمولدات' : 'Add and manage fuel tanks, link them to locations and generators'}</p>
          </div>
          <div style={{ display: 'flex', gap: 8 }}>
            <button className="btn btn-ghost btn-sm" onClick={fetchAll} title={ar ? 'تحديث' : 'Refresh'}><RefreshCw size={14} /></button>
            {canAdd && (
              <button className="btn btn-primary btn-sm" onClick={openAdd}><Plus size={15} /> {ar ? 'خزان جديد' : 'New Tank'}</button>
            )}
          </div>
        </div>
      </div>

      {/* Error */}
      {error && (
        <div className="glass-card" style={{ marginBottom: 16, padding: '12px 16px', display: 'flex', alignItems: 'center', gap: 10, color: 'var(--danger)', borderColor: 'var(--danger)' }}>
          <AlertCircle size={16} />
          <span style={{ fontSize: 13, flex: 1 }}>{error}</span>
          <button onClick={() => setError('')} style={{ background: 'none', border: 'none', color: 'var(--text-faint)', cursor: 'pointer' }}><X size={14} /></button>
        </div>
      )}

      {/* Summary Cards */}
      {!loading && (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(160px, 1fr))', gap: 12, marginBottom: 20 }}>
          {[
            { label: ar?'إجمالي الخزانات':'Total Tanks', value: tanks.length, color: 'var(--primary)' },
            { label: ar?'إجمالي السعة (لتر)':'Total Capacity (L)', value: tanks.reduce((a, t) => a + (t.capacityLiters || 0), 0).toLocaleString(), color: '#10b981' },
            { label: ar?'المخزون الفعلي (لتر)':'Actual Stock (L)', value: tanks.reduce((a, t) => a + (t.actualStockLiters || 0), 0).toLocaleString(), color: '#8b5cf6' },
            { label: ar?'خزانات بدون قراءة':'No Reading', value: tanks.filter(t => !getFillPct(t)).length, color: '#f59e0b' },
          ].map((c, i) => (
            <div key={i} className="glass-card" style={{ padding: '14px 16px', margin: 0 }}>
              <div style={{ fontSize: 22, fontWeight: 700, color: c.color }}>{c.value}</div>
              <div style={{ fontSize: 12, color: 'var(--text-faint)', marginTop: 4 }}>{c.label}</div>
            </div>
          ))}
        </div>
      )}

      {/* Search */}
      <div style={{ marginBottom: 16 }}>
        <input className="form-control" placeholder={ar ? '🔍 بحث بالاسم أو الكود...' : '🔍 Search by name, code...'}
          value={search} onChange={e => setSearch(e.target.value)} style={{ maxWidth: 360, padding: '8px 14px', fontSize: 13 }} />
      </div>

      {/* Tanks Grid */}
      {loading ? (
        <div style={{ textAlign: 'center', padding: 48, color: 'var(--text-faint)' }}>
          <RefreshCw size={24} style={{ animation: 'spin 1s linear infinite' }} />
        </div>
      ) : filtered.length === 0 ? (
        <div className="glass-card" style={{ textAlign: 'center', padding: 48 }}>
          <Droplets size={40} style={{ color: 'var(--text-faint)', marginBottom: 12 }} />
          <p style={{ color: 'var(--text-faint)', fontSize: 14 }}>
            {ar ? 'لا توجد خزانات. ابدأ بإضافة خزان جديد.' : 'No tanks found. Add your first tank.'}
          </p>
        </div>
      ) : (
        <div className="grouped-container">
          {Object.entries(groupedTanks).map(([stationName, stationTanks]) => (
            <div key={stationName} className="station-group" style={{ marginBottom: 40 }}>
              <div className="station-group-header" style={{ display: 'flex', alignItems: 'center', gap: 15, padding: '15px 20px', background: 'var(--bg-card)', border: '1px solid var(--border)', borderBottom: '2px solid var(--primary)', borderRadius: '12px', marginBottom: 20, boxShadow: '0 4px 15px rgba(0,0,0,0.1)' }}>
                <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', width: 42, height: 42, borderRadius: 10, background: 'var(--primary-light)', color: 'var(--primary)' }}>
                  <MapPin size={22} />
                </div>
                <div>
                  <h3 style={{ margin: 0, fontSize: 18, fontWeight: 'bold', color: 'var(--text-main)' }}>{stationName}</h3>
                  <div style={{ fontSize: 12, color: 'var(--text-muted)', marginTop: 4 }}>{ar ? 'الخزانات التابعة لهذا الموقع' : 'Tanks in this location'}</div>
                </div>
                <span style={{ marginRight: ar ? 'auto' : 0, marginLeft: ar ? 0 : 'auto', padding: '6px 14px', background: 'var(--primary)', color: '#fff', borderRadius: 20, fontSize: 13, fontWeight: 'bold' }}>
                  {stationTanks.length} {ar ? 'خزانات' : 'Tanks'}
                </span>
              </div>
              
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(300px, 1fr))', gap: 16 }}>
                {stationTanks.map(tank => {
                  const fuelLabel = fuelTypes.find(f => f.id === tank.fuelLookupId)?.name || tank.fuelLookup?.name || '—';
                  const fillPct = getFillPct(tank);
                  const fillColor = getFillColor(fillPct);
                  return (
                    <div key={tank.id} className="glass-card" style={{ margin: 0 }}>
                      {/* Card Header */}
                      <div style={{ display: 'flex', alignItems: 'center', gap: 10, padding: '14px 16px', borderBottom: '1px solid var(--border)' }}>
                        <Droplets size={18} style={{ color: 'var(--primary)', flexShrink: 0 }} />
                        <span style={{ flex: 1, fontWeight: 700, fontSize: 14, color: 'var(--text)' }}>{tank.name}</span>
                        <span style={{ fontSize: 10, background: 'hsla(217,91%,60%,0.15)', color: 'var(--primary)', padding: '2px 8px', borderRadius: 8, fontWeight: 600 }}>
                          {fuelLabel}
                        </span>
                        {tank.isActive === false && (
                          <span style={{ fontSize: 10, background: 'rgba(239,68,68,0.12)', color: '#ef4444', padding: '2px 8px', borderRadius: 8 }}>
                            {ar ? 'معطل' : 'Inactive'}
                          </span>
                        )}
                      </div>

                      <div style={{ padding: '14px 16px' }}>
                        {/* Fill Percentage Bar */}
                        <div style={{ marginBottom: 12 }}>
                          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 4 }}>
                            <span style={{ fontSize: 12, color: 'var(--text-faint)' }}>{ar ? 'نسبة الملء' : 'Fill Level'}</span>
                            <span style={{ fontSize: 14, fontWeight: 700, color: fillColor }}>
                              {fillPct !== null ? `${fillPct}%` : '—'}
                            </span>
                          </div>
                          <div style={{ height: 6, background: 'var(--border)', borderRadius: 3, overflow: 'hidden' }}>
                            <div style={{ height: '100%', width: `${fillPct || 0}%`, background: fillColor, borderRadius: 3, transition: 'width 0.3s ease' }} />
                          </div>
                          {fillPct !== null && (
                            <div style={{ fontSize: 10, color: 'var(--text-faint)', marginTop: 2, textAlign: 'left' }}>
                              {Number(tank.meterReadingActual || 0).toLocaleString()} / {Number(tank.capacityLiters || 0).toLocaleString()} L
                            </div>
                          )}
                        </div>

                        {/* Data Rows */}
                        {[
                          { label: ar?'الكود':'Code', value: tank.code, mono: true },
                          { label: ar?'السعة الكلية':'Capacity', value: `${Number(tank.capacityLiters || 0).toLocaleString()} L` },
                          { label: ar?'الحد الأدنى للمخزون':'Min Stock', value: tank.minAlertLiters ? `${Number(tank.minAlertLiters).toLocaleString()} L` : '—' },
                          { label: ar?'النقطة الميتة':'Dead Stock', value: tank.deadStockLiters ? `${Number(tank.deadStockLiters).toLocaleString()} L` : '—' },
                          { label: ar?'المخزون الفعلي':'Actual Stock', value: tank.actualStockLiters ? `${Number(tank.actualStockLiters).toLocaleString()} L` : '—' },
                          { label: ar?'المنطقة':'Area', value: getSubName(tank.subStationId), accent: true },
                        ].map((row, i) => (
                          <div key={i} style={{ display: 'flex', justifyContent: 'space-between', padding: '5px 0', borderBottom: '1px solid var(--border)', alignItems: 'center' }}>
                            <span style={{ fontSize: 12, color: 'var(--text-faint)' }}>{row.label}</span>
                            <span style={{ fontSize: 12, color: row.accent ? 'var(--primary)' : 'var(--text)', fontWeight: row.accent ? 600 : 400, fontFamily: row.mono ? 'monospace' : 'inherit' }}>
                              {row.value}
                            </span>
                          </div>
                        ))}

                        {/* Actions */}
                        <div style={{ display: 'flex', gap: 8, marginTop: 14 }}>
                          {canEdit && (
                            <button className="btn btn-ghost btn-sm" style={{ flex: 1 }} onClick={() => openEdit(tank)}>
                              <Pencil size={12} /> {ar ? 'تعديل' : 'Edit'}
                            </button>
                          )}
                          {canDelete && (
                            <button className="btn btn-ghost btn-sm" style={{ flex: 1, color: 'var(--danger)' }}
                              onClick={() => setDeleteConfirm({ id: tank.id, name: tank.name })}>
                              <Trash2 size={12} /> {ar ? 'حذف' : 'Delete'}
                            </button>
                          )}
                        </div>
                      </div>
                    </div>
                  );
                })}
              </div>
            </div>
          ))}
        </div>
      )}

      {/* ─── Add / Edit Modal ─────────────────────────────────────────────── */}
      {showForm && (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.6)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000, padding: 16 }}>
          <div className="glass-card" style={{ width: '100%', maxWidth: 600, maxHeight: '92vh', overflowY: 'auto', margin: 0 }}>
            <div style={secTitle}>
              <Droplets size={15} />
              {editItem ? (ar ? 'تعديل الخزان' : 'Edit Tank') : (ar ? 'إضافة خزان جديد' : 'Add New Tank')}
              <button onClick={resetForm} style={{ marginRight: 'auto', background: 'none', border: 'none', color: 'var(--text-faint)', cursor: 'pointer', padding: 4 }}><X size={16} /></button>
            </div>

            <div className="glass-card-body">
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 14 }}>

                {/* Code */}
                <div className="form-group" style={{ margin: 0 }}>
                  <label className="form-label">{ar ? 'كود الخزان' : 'Tank Code'}</label>
                  {inp(form.code, v => setForm(f => ({ ...f, code: v })), 'TK-01')}
                </div>

                {/* Name - full width */}
                <div className="form-group" style={{ margin: 0, gridColumn: '1 / -1' }}>
                  <label className="form-label">{ar ? 'اسم الخزان' : 'Tank Name'}</label>
                  {inp(form.name, v => setForm(f => ({ ...f, name: v })), ar ? 'مثال: خزان A1' : 'e.g. Tank A1')}
                </div>

                {/* Fuel Type */}
                <div className="form-group" style={{ margin: 0 }}>
                  <label className="form-label">{ar ? 'نوع الوقود' : 'Fuel Type'}</label>
                  <select className="form-select" value={form.fuelLookupId}
                    onChange={e => setForm(f => ({ ...f, fuelLookupId: e.target.value }))}
                    style={{ padding: '8px 12px', fontSize: 13 }}>
                    <option value="">— {ar ? 'اختر النوع' : 'Select Type'} —</option>
                    {fuelTypes.map(t => <option key={t.id} value={t.id}>{t.name}</option>)}
                  </select>
                </div>

                {/* Capacity */}
                <div className="form-group" style={{ margin: 0 }}>
                  <label className="form-label">{ar ? 'سعة الخزان (لتر)' : 'Capacity (L)'}</label>
                  {inp(form.capacityLiters, v => setForm(f => ({ ...f, capacityLiters: v })), '0', 'number')}
                </div>

                {/* Minimum Stock / Alert Threshold */}
                <div className="form-group" style={{ margin: 0 }}>
                  <label className="form-label">{ar ? 'الحد الأدنى للمخزون (لتر)' : 'Minimum Stock Level (L)'}</label>
                  {inp(form.minAlertLiters, v => setForm(f => ({ ...f, minAlertLiters: v })), '0', 'number')}
                </div>

                {/* Dead Stock / النقطة الميتة */}
                <div className="form-group" style={{ margin: 0 }}>
                  <label className="form-label">{ar ? 'النقطة الميتة (لتر)' : 'Dead Stock (L)'}</label>
                  {inp(form.deadStockLiters, v => setForm(f => ({ ...f, deadStockLiters: v })), '0', 'number')}
                  <label style={{fontSize:'.75rem',color:'#64748b'}}>{ar?'المخزون الفعلي (لتر)':'Actual Stock (L)'}</label>
                  {inp(form.actualStockLiters, v => setForm(f => ({ ...f, actualStockLiters: v })), '0', 'number')}
                </div>

                {/* Branch Select */}
                <div className="form-group" style={{ margin: 0 }}>
                  <label className="form-label">{ar ? 'الفرع' : 'Branch'}</label>
                  <select className="form-select" value={form.branchId}
                    onChange={e => setForm(f => ({ ...f, branchId: e.target.value, stationId: '', subStation: '' }))}
                    style={{ padding: '8px 12px', fontSize: 13 }}>
                    <option value="">— {ar ? 'كل الفروع' : 'All Branches'} —</option>
                    {branches.map(b => <option key={b.id} value={b.id}>{b.name}</option>)}
                  </select>
                </div>

                {/* Station Select */}
                <div className="form-group" style={{ margin: 0 }}>
                  <label className="form-label">{ar ? 'المحطة' : 'Station'}</label>
                  <select className="form-select" value={form.stationId}
                    onChange={e => setForm(f => ({ ...f, stationId: e.target.value, subStation: '' }))}
                    style={{ padding: '8px 12px', fontSize: 13 }}
                    disabled={!form.branchId && form.branchId !== ''}>
                    <option value="">— {ar ? 'اختر المحطة' : 'Select Station'} —</option>
                    {filteredStations.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
                  </select>
                </div>

                {/* SubStation Select */}
                <div className="form-group" style={{ margin: 0, gridColumn: '1 / -1' }}>
                  <label className="form-label" style={{ color: 'var(--primary)', fontWeight: 600 }}>
                    🔗 {ar ? 'ربط الخزان بالمنطقة (الموقع)' : 'Link Tank to Area (Location)'}
                  </label>
                  <select className="form-select" value={form.subStation}
                    onChange={e => setForm(f => ({ ...f, subStation: e.target.value }))}
                    style={{ padding: '8px 12px', fontSize: 13 }}
                    disabled={!form.stationId && form.stationId !== ''}>
                    <option value="">— {ar ? 'اختر المنطقة' : 'Select Area'} —</option>
                    {filteredSubStations.map(ss => {
                      const sta = stations.find(s => s.id === (ss.station_id ?? ss.stationId));
                      return (
                        <option key={ss.id} value={ss.id}>
                          {sta ? `${sta.name} ← ` : ''}{ss.name}
                        </option>
                      );
                    })}
                  </select>
                  {subStations.length === 0 && (
                    <p style={{ fontSize: 11, color: 'var(--warning)', marginTop: 4 }}>
                      {ar ? '⚠️ أضف مواقع من صفحة "إدارة المواقع" أولاً' : '⚠️ Add locations from "Locations Management" first'}
                    </p>
                  )}
                </div>

                {/* Notes - full width */}
                <div className="form-group" style={{ margin: 0, gridColumn: '1 / -1' }}>
                  <label className="form-label">{ar ? 'ملاحظات' : 'Notes'}</label>
                  <textarea className="form-control" rows={2} value={form.notes}
                    onChange={e => setForm(f => ({ ...f, notes: e.target.value }))}
                    placeholder={ar ? 'ملاحظات إضافية...' : 'Additional notes...'}
                    style={{ padding: '8px 12px', fontSize: 13, resize: 'vertical' }} />
                </div>

                {/* Active Status */}
                <div className="form-group" style={{ margin: 0, gridColumn: '1 / -1' }}>
                  <label style={{ display: 'flex', alignItems: 'center', gap: 8, cursor: 'pointer', fontSize: 13 }}>
                    <input type="checkbox" checked={form.isActive}
                      onChange={e => setForm(f => ({ ...f, isActive: e.target.checked }))}
                      style={{ width: 16, height: 16, accentColor: 'var(--primary)', cursor: 'pointer' }} />
                    {ar ? 'الخزان نشط' : 'Tank is Active'}
                  </label>
                </div>
              </div>

              {/* Inline validation / server error inside modal */}
              {formError && (
                <div style={{ display: 'flex', alignItems: 'flex-start', gap: 10, padding: '10px 14px', background: 'rgba(239,68,68,0.08)', border: '1px solid rgba(239,68,68,0.3)', borderRadius: 8, marginBottom: 12 }}>
                  <AlertCircle size={16} style={{ color: '#ef4444', flexShrink: 0, marginTop: 1 }} />
                  <span style={{ fontSize: 13, color: '#ef4444', lineHeight: 1.5 }}>{formError}</span>
                </div>
              )}

              <div style={{ display: 'flex', gap: 10, paddingTop: 16, borderTop: '1px solid var(--border)', marginTop: 0 }}>
                <button className="btn btn-ghost btn-sm" style={{ flex: 1 }} onClick={resetForm}>{ar ? 'إلغاء' : 'Cancel'}</button>
                <button className="btn btn-primary btn-sm" style={{ flex: 2 }} onClick={handleSave} disabled={saving}>
                  <Save size={13} /> {saving ? (ar ? 'جارٍ الحفظ...' : 'Saving...') : (ar ? 'حفظ الخزان' : 'Save Tank')}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* ─── Delete Confirm ───────────────────────────────────────────────── */}
      {deleteConfirm && (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.6)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1001, padding: 16 }}>
          <div className="glass-card" style={{ width: '100%', maxWidth: 380, margin: 0, textAlign: 'center' }}>
            <div className="glass-card-body">
              <AlertCircle size={36} style={{ color: 'var(--danger)', marginBottom: 12 }} />
              <h3 style={{ fontSize: 15, fontWeight: 700, marginBottom: 8 }}>{ar ? 'تأكيد الحذف' : 'Confirm Delete'}</h3>
              <p style={{ fontSize: 13, color: 'var(--text-muted)', marginBottom: 20 }}>
                {ar ? `هل أنت متأكد من حذف خزان "${deleteConfirm.name}"؟` : `Delete tank "${deleteConfirm.name}"?`}
              </p>
              <div style={{ display: 'flex', gap: 10 }}>
                <button className="btn btn-ghost btn-sm" style={{ flex: 1 }} onClick={() => setDeleteConfirm(null)}>{ar ? 'إلغاء' : 'Cancel'}</button>
                <button className="btn btn-sm" style={{ flex: 1, background: 'var(--danger)', color: '#fff', border: 'none' }} onClick={handleDelete}>
                  <Trash2 size={13} /> {ar ? 'حذف' : 'Delete'}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

export default TanksManagement;
