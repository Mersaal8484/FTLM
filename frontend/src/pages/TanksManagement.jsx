import React, { useState, useEffect, useCallback, useMemo } from 'react';
import { useSearchParams } from 'react-router-dom';
import { Plus, Droplets, Pencil, Trash2, Save, X, AlertCircle, RefreshCw, MapPin } from 'lucide-react';
import client from '../api/client';
import ConfirmModal from '../components/ConfirmModal';
import { PageHeader, Button, Modal, FormField, Input } from '../components/ui';
import TankSetup from './TankSetup';

const emptyForm = {
  code: '', name: '',
  capacityLiters: '', minAlertLiters: '', deadStockLiters: '', actualStockLiters: '',
  fuelLookupId: '',
  branchId: '', stationId: '', subStation: '',
  isActive: true,
  notes: '',
};

function TankTabs({ activeTab, onChange, ar }) {
  const tabs = [
    { id: 'basic', label: ar ? '\u0627\u0644\u0628\u064a\u0627\u0646\u0627\u062a \u0627\u0644\u0623\u0633\u0627\u0633\u064a\u0629' : 'Basic data' },
    { id: 'calibration', label: ar ? '\u0627\u0644\u0645\u0639\u0627\u064a\u0631\u0629 \u0648\u0627\u0644\u0646\u0642\u0637\u0629 \u0627\u0644\u0645\u064a\u062a\u0629' : 'Calibration & dead stock' },
  ];

  return (
    <div className="ft-tabs" style={{ marginBottom: 24 }} aria-label={ar ? '\u0623\u0642\u0633\u0627\u0645 \u0627\u0644\u062e\u0632\u0627\u0646\u0627\u062a' : 'Tank sections'}>
      {tabs.map(tab => (
        <button key={tab.id} type="button" className={`ft-tab ${activeTab === tab.id ? 'active' : ''}`} onClick={() => onChange(tab.id)}>
          {tab.label}
        </button>
      ))}
    </div>
  );
}

function TanksManagement({ lang, currentUser }) {
  const ar = lang === 'ar';
  const [searchParams, setSearchParams] = useSearchParams();
  const activeTab = searchParams.get('tab') === 'calibration' ? 'calibration' : 'basic';
  const setActiveTab = (tab) => setSearchParams(tab === 'calibration' ? { tab } : {});

  const perms = currentUser?.permissions?.tanks || {};
  const canAdd = currentUser?.role === 'admin' || perms.create || perms.add;
  const canEdit = currentUser?.role === 'admin' || perms.edit;
  const canDelete = currentUser?.role === 'admin' || perms.delete;

  const [tanks, setTanks]           = useState([]);
  const [branches, setBranches]     = useState([]);
  const [subStations, setSubStations] = useState([]);
  const [stations, setStations]     = useState([]);
  const [fuelTypes, setFuelTypes]   = useState([]);
  const [loading, setLoading]       = useState(true);
  const [error, setError]           = useState('');

  const [showForm, setShowForm]     = useState(false);
  const [editItem, setEditItem]     = useState(null);
  const [form, setForm]             = useState(emptyForm);
  const [formError, setFormError]   = useState('');
  const [saving, setSaving]         = useState(false);
  const [deleteConfirm, setDeleteConfirm] = useState(null);
  const [search, setSearch]         = useState('');

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

  const resetForm = () => { setForm(emptyForm); setEditItem(null); setShowForm(false); setFormError(''); };

  const getFillPct = (tank) => {
    const capacity = Number(tank.capacityLiters) || 0;
    if (capacity <= 0) return null;
    const actual = Number(tank.meterReadingActual) || Number(tank.actualStockLiters) || 0;
    if (actual <= 0) return null;
    return Math.min(100, Math.round((actual / capacity) * 100));
  };

  const getFillColor = (pct) => {
    if (pct === null) return 'var(--text-faint)';
    if (pct <= 20) return 'var(--danger)';
    if (pct <= 50) return 'var(--warning)';
    return 'var(--success)';
  };

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

  const groupedTanks = {};
  filtered.forEach(tank => {
    const stationName = getStaName(tank.subStationId);
    if (!groupedTanks[stationName]) groupedTanks[stationName] = [];
    groupedTanks[stationName].push(tank);
  });

  if (activeTab === 'calibration') {
    return (
      <div>
        <PageHeader
          title={ar ? '\u0625\u0639\u062f\u0627\u062f \u062e\u0632\u0627\u0646\u0627\u062a \u0627\u0644\u0648\u0642\u0648\u062f' : 'Tanks Management'}
          subtitle={ar ? '\u0625\u062f\u0627\u0631\u0629 \u0628\u064a\u0627\u0646\u0627\u062a \u0627\u0644\u062e\u0632\u0627\u0646\u0627\u062a \u0648\u0645\u0639\u0627\u064a\u0631\u062a\u0647\u0627' : 'Manage tank details and calibration'}
        />
        <TankTabs activeTab={activeTab} onChange={setActiveTab} ar={ar} />
        <TankSetup embedded />
      </div>
    );
  }

  return (
    <div>
      <PageHeader
        title={ar ? 'إعداد خزانات الوقود' : 'Tanks Management'}
        subtitle={ar ? 'إضافة وتعديل خزانات الوقود وربطها بالمواقع والمولدات' : 'Add and manage fuel tanks, link them to locations and generators'}
        actions={
          <div style={{ display: 'flex', gap: 8 }}>
            <Button variant="ghost" size="sm" icon={RefreshCw} onClick={fetchAll} title={ar ? 'تحديث' : 'Refresh'} />
            {canAdd && (
              <Button icon={Plus} onClick={openAdd}>{ar ? 'خزان جديد' : 'New Tank'}</Button>
            )}
          </div>
        }
      />
      <TankTabs activeTab={activeTab} onChange={setActiveTab} ar={ar} />

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
            { label: ar?'إجمالي السعة (لتر)':'Total Capacity (L)', value: tanks.reduce((a, t) => a + (t.capacityLiters || 0), 0).toLocaleString('en-US'), color: 'var(--success)' },
            { label: ar?'المخزون الفعلي (لتر)':'Actual Stock (L)', value: tanks.reduce((a, t) => a + (t.actualStockLiters || 0), 0).toLocaleString('en-US'), color: 'var(--purple)' },
            { label: ar?'خزانات بدون قراءة':'No Reading', value: tanks.filter(t => !getFillPct(t)).length, color: 'var(--warning)' },
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
        <Input value={search} onChange={e => setSearch(e.target.value)} placeholder={ar ? '🔍 بحث بالاسم أو الكود...' : '🔍 Search by name, code...'} style={{ maxWidth: 360 }} />
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
              <div className="station-group-header" style={{ marginBottom: 20 }}>
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
                          <span style={{ fontSize: 10, background: 'var(--danger-subtle)', color: 'var(--danger)', padding: '2px 8px', borderRadius: 8 }}>
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
                              {Number(tank.meterReadingActual || 0).toLocaleString('en-US')} / {Number(tank.capacityLiters || 0).toLocaleString('en-US')} L
                            </div>
                          )}
                        </div>

                        {/* Data Rows */}
                        {[
                          { label: ar?'الكود':'Code', value: tank.code, mono: true },
                          { label: ar?'السعة الكلية':'Capacity', value: `${Number(tank.capacityLiters || 0).toLocaleString('en-US')} L` },
                          { label: ar?'الحد الأدنى للمخزون':'Min Stock', value: tank.minAlertLiters ? `${Number(tank.minAlertLiters).toLocaleString('en-US')} L` : '—' },
                          { label: ar?'النقطة الميتة':'Dead Stock', value: tank.deadStockLiters ? `${Number(tank.deadStockLiters).toLocaleString('en-US')} L` : '—' },
                          { label: ar?'المخزون الفعلي':'Actual Stock', value: tank.actualStockLiters ? `${Number(tank.actualStockLiters).toLocaleString('en-US')} L` : '—' },
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
                            <Button variant="ghost" size="sm" style={{ flex: 1 }} icon={Pencil} onClick={() => openEdit(tank)}>
                              {ar ? 'تعديل' : 'Edit'}
                            </Button>
                          )}
                          {canDelete && (
                            <Button variant="ghost" size="sm" style={{ flex: 1, color: 'var(--danger)' }} icon={Trash2} onClick={() => setDeleteConfirm({ id: tank.id, name: tank.name })}>
                              {ar ? 'حذف' : 'Delete'}
                            </Button>
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
        <Modal
          open={showForm}
          onClose={resetForm}
          title={
            <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
              <Droplets size={18} style={{ color: 'var(--primary)' }} />
              {editItem ? (ar ? 'تعديل الخزان' : 'Edit Tank') : (ar ? 'إضافة خزان جديد' : 'Add New Tank')}
            </div>
          }
          maxWidth={600}
          footer={
            <>
              <Button variant="ghost" onClick={resetForm}>{ar ? 'إلغاء' : 'Cancel'}</Button>
              <Button variant="primary" onClick={handleSave} disabled={saving} icon={Save}>
                {saving ? (ar ? 'جارٍ الحفظ...' : 'Saving...') : (ar ? 'حفظ الخزان' : 'Save Tank')}
              </Button>
            </>
          }
        >
          {formError && (
            <div className="alert-inline alert-inline-danger" style={{ marginBottom: 16 }}>
              <AlertCircle size={16} style={{ flexShrink: 0, marginTop: 1 }} />
              {formError}
            </div>
          )}

          <div className="form-row">
            <FormField label={ar ? 'كود الخزان' : 'Tank Code'}>
              <Input value={form.code} onChange={e => setForm(f => ({ ...f, code: e.target.value }))} placeholder="TK-01" />
            </FormField>
            <FormField label={ar ? 'اسم الخزان' : 'Tank Name'}>
              <Input value={form.name} onChange={e => setForm(f => ({ ...f, name: e.target.value }))} placeholder={ar ? 'مثال: خزان A1' : 'e.g. Tank A1'} />
            </FormField>
          </div>
          <div className="form-row">
            <FormField label={ar ? 'نوع الوقود' : 'Fuel Type'}>
              <select className="form-select" value={form.fuelLookupId}
                onChange={e => setForm(f => ({ ...f, fuelLookupId: e.target.value }))}>
                <option value="">— {ar ? 'اختر النوع' : 'Select Type'} —</option>
                {fuelTypes.map(t => <option key={t.id} value={t.id}>{t.name}</option>)}
              </select>
            </FormField>
            <FormField label={ar ? 'سعة الخزان (لتر)' : 'Capacity (L)'}>
              <Input type="number" value={form.capacityLiters} onChange={e => setForm(f => ({ ...f, capacityLiters: e.target.value }))} placeholder="0" />
            </FormField>
          </div>
          <div className="form-row">
            <FormField label={ar ? 'الحد الأدنى للمخزون (لتر)' : 'Minimum Stock Level (L)'}>
              <Input type="number" value={form.minAlertLiters} onChange={e => setForm(f => ({ ...f, minAlertLiters: e.target.value }))} placeholder="0" />
            </FormField>
            <FormField label={ar ? 'النقطة الميتة (لتر)' : 'Dead Stock (L)'}>
              <Input type="number" value={form.deadStockLiters} onChange={e => setForm(f => ({ ...f, deadStockLiters: e.target.value }))} placeholder="0" />
            </FormField>
          </div>
          <div className="form-row">
            <FormField label={ar ? 'المخزون الفعلي (لتر)' : 'Actual Stock (L)'}>
              <Input type="number" value={form.actualStockLiters} onChange={e => setForm(f => ({ ...f, actualStockLiters: e.target.value }))} placeholder="0" />
            </FormField>
          </div>

          <div className="form-section-header">{ar ? 'الموقع' : 'Location'}</div>
          <div className="form-row">
            <FormField label={ar ? 'الفرع' : 'Branch'}>
              <select className="form-select" value={form.branchId}
                onChange={e => setForm(f => ({ ...f, branchId: e.target.value, stationId: '', subStation: '' }))}>
                <option value="">— {ar ? 'كل الفروع' : 'All Branches'} —</option>
                {branches.map(b => <option key={b.id} value={b.id}>{b.name}</option>)}
              </select>
            </FormField>
            <FormField label={ar ? 'المحطة' : 'Station'}>
              <select className="form-select" value={form.stationId}
                onChange={e => setForm(f => ({ ...f, stationId: e.target.value, subStation: '' }))}
                disabled={!form.branchId && form.branchId !== ''}>
                <option value="">— {ar ? 'اختر المحطة' : 'Select Station'} —</option>
                {filteredStations.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
              </select>
            </FormField>
          </div>
          <FormField label={ar ? 'ربط الخزان بالمنطقة (الموقع)' : 'Link Tank to Area (Location)'} required error={!form.subStation && formError ? (ar ? 'يجب ربط الخزان بمنطقة.' : 'Tank must be linked to an area.') : null}>
            <select className="form-select" value={form.subStation}
              onChange={e => setForm(f => ({ ...f, subStation: e.target.value }))}
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
          </FormField>
          {subStations.length === 0 && (
            <p style={{ fontSize: 11, color: 'var(--warning)', marginTop: 4, marginBottom: 12 }}>
              {ar ? '⚠️ أضف مواقع من صفحة "إدارة المواقع" أولاً' : '⚠️ Add locations from "Locations Management" first'}
            </p>
          )}

          <div className="form-section-header">{ar ? 'إضافية' : 'Additional'}</div>
          <FormField label={ar ? 'ملاحظات' : 'Notes'}>
            <textarea className="form-control" rows={2} value={form.notes}
              onChange={e => setForm(f => ({ ...f, notes: e.target.value }))}
              placeholder={ar ? 'ملاحظات إضافية...' : 'Additional notes...'}
              style={{ resize: 'vertical', fontFamily: 'inherit' }} />
          </FormField>
          <div className="form-group" style={{ marginTop: 8 }}>
            <label className="checkbox-label">
              <input type="checkbox" checked={form.isActive} onChange={e => setForm(f => ({ ...f, isActive: e.target.checked }))} />
              <span>{ar ? 'الخزان نشط' : 'Tank is Active'}</span>
            </label>
          </div>
        </Modal>
      )}

      <ConfirmModal
        open={!!deleteConfirm}
        title={ar ? 'تأكيد الحذف' : 'Confirm Delete'}
        message={ar ? `هل أنت متأكد من حذف خزان "${deleteConfirm?.name}"؟` : `Delete tank "${deleteConfirm?.name}"?`}
        confirmText={ar ? 'حذف' : 'Delete'}
        cancelText={ar ? 'إلغاء' : 'Cancel'}
        danger
        onConfirm={handleDelete}
        onCancel={() => setDeleteConfirm(null)}
      />
    </div>
  );
}

export default TanksManagement;
