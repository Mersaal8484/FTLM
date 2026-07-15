import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { AlertCircle, Gauge, Pencil, Plus, Save, Trash2, X, Search, RefreshCw, Zap, Fuel, Radio, MapPin } from 'lucide-react';
import client from '../api/client';

const METER_ICON_MAP = {
  'GeneratorMeter': { icon: <Zap size={13} />,    color: 'var(--primary)',   colorVar: 'primary' },
  'FuelDrain':      { icon: <Fuel size={13} />,   color: 'var(--warning)',   colorVar: 'warning' },
  'ExportEnergy':   { icon: <Radio size={13} />,  color: 'var(--secondary)', colorVar: 'secondary' },
};
const METER_DEFAULT_ICON = { icon: <Gauge size={13} />, color: 'var(--text-muted)', colorVar: 'secondary' };

function MeterTypeBadge({ type, code }) {
  const cfg = METER_ICON_MAP[code] || METER_DEFAULT_ICON;
  return (
    <span className="ctrl-gen-badge" style={{ background: `${cfg.color}18`, color: cfg.color }}>
      {cfg.icon}
      {type}
    </span>
  );
}

function MetersManagement({ lang }) {
  const ar = lang === 'ar';

  const [meters, setMeters] = useState([]);
  const [branches, setBranches] = useState([]);
  const [stations, setStations] = useState([]);
  const [subStations, setSubStations] = useState([]);
  const [generators, setGenerators] = useState([]);
  const [tanks, setTanks] = useState([]);
  const [meterTypes, setMeterTypes] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [search, setSearch] = useState('');
  const [showForm, setShowForm] = useState(false);
  const [editItem, setEditItem] = useState(null);
  const [saving, setSaving] = useState(false);
  const [deleteConfirm, setDeleteConfirm] = useState(null);
  const [activeTab, setActiveTab] = useState('fuel');
  const [form, setForm] = useState({
    branchId: '',
    stationId: '',
    subStationId: '',
    generatorId: '',
    meterName: '',
    serialNumber: '',
    hoursReading: '',
    energyReading: '',
    isActive: true,
  });

  const fetchAll = useCallback(async () => {
    setLoading(true); setError('');
    try {
      const [m, b, s, ss, g, t, mt] = await Promise.all([
        client.get('/api/meters'), client.get('/api/branches'), client.get('/api/stations'), client.get('/api/sub-stations'),
        client.get('/api/generators'), client.get('/api/fuel-tanks'),
        client.get('/api/lookups/by-category/MeterType'),
      ]);
      setMeters(Array.isArray(m.data) ? m.data : []);
      setBranches(Array.isArray(b.data) ? b.data : []);
      setStations(Array.isArray(s.data) ? s.data : []);
      setSubStations(Array.isArray(ss.data) ? ss.data.filter(s => s.isActive !== false) : []);
      setGenerators(Array.isArray(g.data) ? g.data : []);
      setTanks(Array.isArray(t.data) ? t.data : []);
      setMeterTypes(Array.isArray(mt.data) ? mt.data : []);
    } catch {
      setError(ar ? 'فشل تحميل البيانات. تأكد من تشغيل الخادم.' : 'Failed to load data. Check server.');
    } finally { setLoading(false); }
  }, [ar]);

  useEffect(() => { fetchAll(); }, [fetchAll]);

  const resetForm = () => {
    setForm({ branchId: '', stationId: '', subStationId: '', generatorId: '', meterName: '', serialNumber: '', hoursReading: '', energyReading: '', isActive: true });
    setActiveTab('fuel'); setEditItem(null); setShowForm(false);
  };

  const openAdd  = () => { resetForm(); setShowForm(true); };

  const typeCodeForTab = (tab) => {
    if (tab === 'fuel') return 'FuelDrain';
    if (tab === 'generator') return 'GeneratorMeter';
    return 'ExportEnergy';
  };

  const openEdit = (item) => {
    const typeCode = meterTypes.find(mt => mt.name === item.meterType)?.code || 'FuelDrain';
    let tab = 'fuel';
    if (typeCode === 'GeneratorMeter') tab = 'generator';
    else if (typeCode === 'ExportEnergy') tab = 'energy';
    setActiveTab(tab);

    const station = stations.find(s => s.id === item.stationId);

    setEditItem(item);
    setForm({
      branchId: station?.branchId ?? '',
      stationId: item.stationId ?? '',
      subStationId: item.subStationId ?? '',
      generatorId: item.generatorId ?? '',
      meterName: item.name || '',
      serialNumber: item.serialNumber || '',
      hoursReading: item.hoursReading ?? '',
      energyReading: item.energyReading ?? '',
      isActive: item.isActive ?? true,
    });
    setShowForm(true);
  };

  const switchTab = (tab) => {
    setActiveTab(tab);
    if (!editItem) {
      setForm(f => ({ ...f, branchId: '', stationId: '', subStationId: '', generatorId: '' }));
    }
  };

  const fld = (k, v) => setForm(f => ({ ...f, [k]: v }));

  const branchOptions = useMemo(() => branches.map(b => ({ value: b.id, label: b.name })), [branches]);

  const filteredStations = useMemo(() => {
    if (!form.branchId) return [];
    return stations.filter(s => s.branchId === Number(form.branchId));
  }, [stations, form.branchId]);

  const filteredSubStations = useMemo(() => {
    if (!form.stationId) return [];
    return subStations.filter(ss => ss.stationId === Number(form.stationId));
  }, [subStations, form.stationId]);

  const filteredGenerators = useMemo(() => {
    if (!form.subStationId) return [];
    return generators.filter(g => g.subStationId === Number(form.subStationId));
  }, [generators, form.subStationId]);

  const locationName = (meter) => {
    const stationName = meter.stationName || '—';
    const siteName = meter.subStationName;
    return siteName ? `${stationName} - ${siteName}` : stationName;
  };

  const handleSave = async () => {
    if (!form.meterName || !form.serialNumber) {
      setError(ar ? 'الرجاء تعبئة اسم العداد والرقم التسلسلي' : 'Please fill meter name and serial number'); return;
    }
    if (activeTab !== 'energy' && !form.subStationId) {
      setError(ar ? 'الرجاء اختيار المحطة والموقع' : 'Please select station and sub-station'); return;
    }
    if (activeTab === 'energy' && !form.subStationId) {
      setError(ar ? 'عداد الطاقة يجب ربطه بموقع/فرع محدد' : 'Energy meter must be linked to a specific site'); return;
    }
    if (activeTab === 'generator' && !form.generatorId) {
      setError(ar ? 'الرجاء اختيار المولد المرتبط' : 'Please select the linked generator'); return;
    }

    const meterType = meterTypes.find(mt => mt.code === typeCodeForTab(activeTab));
    setSaving(true); setError('');
    try {
      const payload = {
        meterName: form.meterName,
        code: null,
        stationId: form.stationId ? Number(form.stationId) : null,
        subStationId: form.subStationId ? Number(form.subStationId) : null,
        meterType: meterType?.name,
        meterTypeLookupId: meterType?.id,
        serialNumber: form.serialNumber,
        generatorId: activeTab === 'generator' && form.generatorId ? Number(form.generatorId) : null,
        fuelTankId: null,
        hoursReading: Number(form.hoursReading) || 0,
        energyReading: Number(form.energyReading) || 0,
        isActive: form.isActive !== undefined ? form.isActive : true,
      };
      if (editItem) await client.put(`/api/meters/${editItem.id}`, payload);
      else           await client.post('/api/meters', payload);
      await fetchAll(); resetForm();
    } catch (e) {
      setError(e.response?.data?.msg || e.response?.data?.message || (ar ? 'فشل الحفظ. تحقق من البيانات.' : 'Save failed. Check your data.'));
    } finally { setSaving(false); }
  };

  const handleDelete = async () => {
    if (!deleteConfirm) return;
    try { await client.delete(`/api/meters/${deleteConfirm.id}`); await fetchAll(); }
    catch (e) { setError(e.response?.data?.msg || e.response?.data?.message || (ar ? 'فشل الحذف' : 'Delete failed')); }
    finally { setDeleteConfirm(null); }
  };

  const filtered = meters.filter(m =>
    !search ||
    m.name?.toLowerCase().includes(search.toLowerCase()) ||
    m.serialNumber?.toLowerCase().includes(search.toLowerCase()) ||
    m.meterType?.includes(search) ||
    locationName(m).toLowerCase().includes(search.toLowerCase())
  );

  const total       = meters.length;
  const genLookup   = meterTypes.find(t => t.code === 'GeneratorMeter');
  const fuelLookup  = meterTypes.find(t => t.code === 'FuelDrain');
  const gridLookup  = meterTypes.find(t => t.code === 'ExportEnergy');
  const genCount    = genLookup  ? meters.filter(m => m.meterType === genLookup.name).length  : 0;
  const fuelCount   = fuelLookup ? meters.filter(m => m.meterType === fuelLookup.name).length : 0;
  const gridCount   = gridLookup ? meters.filter(m => m.meterType === gridLookup.name).length : 0;

  const inp = (val, onChange, placeholder = '', type = 'text') => (
    <input type={type} value={val} onChange={e => onChange(e.target.value)} className="form-control" placeholder={placeholder} />
  );
  const sel = (val, onChange, options) => (
    <select value={val} onChange={e => onChange(e.target.value)} className="form-select">
      <option value="">- {ar ? 'اختر' : 'Select'} -</option>
      {options.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
    </select>
  );

  return (
    <div>
      {/* ── Header ── */}
      <div className="page-header">
        <div className="page-header-row">
          <div>
            <h1>{ar ? 'العدادات' : 'Meters'}</h1>
            <p>{ar ? 'إدارة العدادات المرتبطة بالمواقع والمولدات' : 'Manage meters linked to sites and generators'}</p>
          </div>
          <div style={{ display: 'flex', gap: 8 }}>
            <button className="btn btn-ghost" onClick={fetchAll}><RefreshCw size={15} /></button>
            <button className="btn btn-primary" onClick={openAdd}>
              <Plus size={16} /> {ar ? 'جديد' : 'New'}
            </button>
          </div>
        </div>
      </div>

      {/* ── KPI Cards ── */}
      <div className="kpi-grid">
        {[
          { label: ar ? 'الإجمالي' : 'Total',           value: total,     icon: <Gauge size={20} />,  colorClass: 'primary' },
          ...(genLookup  ? [{ label: genLookup.name,  value: genCount,  icon: <Zap size={20} />,   colorClass: METER_ICON_MAP[genLookup.code]?.colorVar  || 'primary' }] : []),
          ...(fuelLookup ? [{ label: fuelLookup.name, value: fuelCount, icon: <Fuel size={20} />,  colorClass: METER_ICON_MAP[fuelLookup.code]?.colorVar || 'warning' }] : []),
          ...(gridLookup ? [{ label: gridLookup.name, value: gridCount, icon: <Radio size={20} />, colorClass: METER_ICON_MAP[gridLookup.code]?.colorVar || 'secondary' }] : []),
        ].map((s, i) => (
          <div key={i} className="kpi-card" style={{ '--kpi-color': `var(--${s.colorClass === 'warning' ? 'warning' : s.colorClass === 'secondary' ? 'secondary' : 'primary'})` }}>
            <div className={`kpi-icon ${s.colorClass}`}>{s.icon}</div>
            <div><div className="kpi-value">{s.value}</div><div className="kpi-label">{s.label}</div></div>
          </div>
        ))}
      </div>

      {/* ── Search ── */}
      <div style={{ marginBottom: 20 }}>
        <div className="search-bar" style={{ maxWidth: 400 }}>
          <Search size={16} />
          <input className="form-control"
            placeholder={ar ? 'بحث بالاسم، الرقم التسلسلي، أو الموقع...' : 'Search name, serial, or site...'}
            value={search} onChange={e => setSearch(e.target.value)} />
        </div>
      </div>

      {/* ── Error banner ── */}
      {error && (
        <div className="glass-card" style={{ marginBottom: 16, borderColor: 'var(--danger)', padding: '12px 16px', display: 'flex', alignItems: 'center', gap: 10, color: 'var(--danger)' }}>
          <AlertCircle size={16} />
          <span style={{ fontSize: 13 }}>{error}</span>
          <button onClick={() => setError('')} style={{ marginRight: 'auto', background: 'none', border: 'none', color: 'var(--text-faint)', cursor: 'pointer' }}><X size={14} /></button>
        </div>
      )}

      {/* ── Cards ── */}
      {loading ? (
        <div className="spinner-wrap"><div className="ft-spinner" /></div>
      ) : filtered.length === 0 ? (
        <div className="empty-state"><Gauge size={48} /><h3>{ar ? 'لا توجد عدادات' : 'No meters'}</h3></div>
      ) : (
        <div className="grid-auto">
           {filtered.map((meter, i) => {
             const typeCode = meterTypes.find(mt => mt.name === meter.meterType)?.code;
             return (
              <div key={meter.id} className="ctrl-card" style={{
                borderColor: meter.isActive ? 'var(--success)' : 'var(--danger)',
                borderWidth: 2,
              }}>
                <div className="ctrl-card-body">
                  <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
                    <MeterTypeBadge type={meter.meterType} code={typeCode} />
                    <span className={meter.isActive ? 'status-badge status-active' : 'status-badge status-inactive'}>
                      {meter.isActive ? (ar ? 'نشط' : 'Active') : (ar ? 'غير نشط' : 'Inactive')}
                    </span>
                  </div>

                  <div style={{ marginTop: 6, display: 'flex', alignItems: 'center', gap: 6, fontSize: 12, color: 'var(--text-muted)' }}>
                    <MapPin size={11} />
                    {locationName(meter)}
                  </div>

                  <div className="ctrl-card-stats">
                    <div className="ctrl-stat">
                       <div className="ctrl-stat-label">{ar ? 'القراءة العياري' : 'Std Rdg'}</div>
                      <div className="ctrl-stat-value">{meter.energyReading ?? 0}</div>
                    </div>
                    {typeCode !== 'ExportEnergy' && (
                      <div className="ctrl-stat">
                        <div className="ctrl-stat-label">{ar ? 'القراءة المشاهد' : 'Run Rdg'}</div>
                        <div className="ctrl-stat-value">{meter.hoursReading ?? 0}</div>
                      </div>
                    )}
                  </div>
                </div>

                <div className="ctrl-card-footer">
                  <button className="btn btn-sm btn-ghost" onClick={() => openEdit(meter)}>
                    <Pencil size={13} /> {ar ? 'تعديل' : 'Edit'}
                  </button>
                  <button className="btn btn-sm btn-danger" onClick={() => setDeleteConfirm(meter)}>
                    <Trash2 size={13} /> {ar ? 'حذف' : 'Delete'}
                  </button>
                </div>
              </div>
            );
          })}
        </div>
      )}

      {/* ══════════════ Form Modal ══════════════ */}
      {showForm && (
        <div className="modal-overlay" onClick={e => e.target === e.currentTarget && resetForm()}>
          <div className="ctrl-modal-box" style={{ maxWidth: 520 }}>
            {/* Header */}
            <div className="ctrl-modal-header">
              <div style={{ fontSize: 16, fontWeight: 700, color: 'var(--text)', display: 'flex', alignItems: 'center', gap: 8 }}>
                {editItem ? <Pencil size={16} /> : <Plus size={16} />}
                {editItem ? (ar ? 'تعديل العداد' : 'Edit Meter') : (ar ? 'عداد جديد' : 'New Meter')}
              </div>
              <button className="modal-close" onClick={resetForm}><X size={18} /></button>
            </div>

            {/* Body */}
            <div className="ctrl-modal-body">
              {/* Tab bar */}
              <div style={{ display: 'flex', gap: 2, marginBottom: 16, background: 'var(--bg)', borderRadius: 10, padding: 3 }}>
                {[
                  { key: 'fuel',      label: ar ? 'عداد الوقود' : 'Fuel Meter',     icon: <Fuel size={14} />  },
                  { key: 'generator', label: ar ? 'عداد المولدات' : 'Gen Meter',    icon: <Zap size={14} />   },
                  { key: 'energy',    label: ar ? 'عداد الطاقة' : 'Energy Meter',   icon: <Radio size={14} /> },
                ].map(t => (
                  <button key={t.key} type="button" onClick={() => switchTab(t.key)} style={{
                    flex: 1, padding: '8px 6px', borderRadius: 8, border: 'none',
                    background: activeTab === t.key ? 'var(--card)' : 'transparent',
                    color: activeTab === t.key ? 'var(--text)' : 'var(--text-muted)',
                    cursor: 'pointer', fontSize: 12, fontWeight: 600,
                    display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 5,
                    transition: 'all 0.2s',
                  }}>
                    {t.icon}{t.label}
                  </button>
                ))}
              </div>

              {/* Location */}
              <div className="ctrl-tab-section-title">{ar ? 'الموقع' : 'Location'}</div>

              <>
                <div className="form-group">
                  <label className="form-label">{ar ? 'المحافظة *' : 'Governorate *'}</label>
                  {sel(form.branchId, v => fld('branchId', v), branchOptions)}
                </div>
                {form.branchId && (
                  <div className="form-group">
                    <label className="form-label">{ar ? 'المحطة *' : 'Station *'}</label>
                    {sel(form.stationId, v => fld('stationId', v), filteredStations.map(s => ({ value: s.id, label: s.name })))}
                  </div>
                )}
                {form.stationId && (
                  <div className="form-group">
                    <label className="form-label">{ar ? 'الموقع/الفرع *' : 'Site / Branch *'}</label>
                    {sel(form.subStationId, v => fld('subStationId', v), filteredSubStations.map(ss => ({ value: ss.id, label: ss.name })))}
                  </div>
                )}
                {activeTab === 'generator' && form.subStationId && (
                  <div className="form-group">
                    <label className="form-label">{ar ? 'المولد *' : 'Generator *'}</label>
                    {sel(form.generatorId, v => fld('generatorId', v), filteredGenerators.map(g => ({ value: g.id, label: g.name })))}
                  </div>
                )}
              </>

              <div className="ctrl-tab-divider" />
              <div className="ctrl-tab-section-title">{ar ? 'بيانات العداد' : 'Meter Details'}</div>

              <div className="form-group">
                <label className="form-label">{ar ? 'اسم العداد *' : 'Meter Name *'}</label>
                {inp(form.meterName, v => fld('meterName', v))}
              </div>
              <div className="form-group">
                <label className="form-label">{ar ? 'الرقم التسلسلي *' : 'Serial Number *'}</label>
                {inp(form.serialNumber, v => fld('serialNumber', v))}
              </div>

              <div className="ctrl-tab-divider" />
              <div className="ctrl-tab-section-title">{ar ? 'القراءات' : 'Readings'}</div>

              {activeTab !== 'energy' ? (
                <div className="form-row">
                  <div className="form-group">
                    <label className="form-label">{ar ? 'قراءة العياري' : 'Standard Reading'}</label>
                    {inp(form.energyReading, v => fld('energyReading', v), '0', 'number')}
                  </div>
                  <div className="form-group">
                    <label className="form-label">{ar ? 'قراءة المشاهد' : 'Running Reading'}</label>
                    {inp(form.hoursReading, v => fld('hoursReading', v), '0', 'number')}
                  </div>
                </div>
              ) : (
                <div className="form-group">
                  <label className="form-label">{ar ? 'قراءة الطاقة' : 'Reading'}</label>
                  {inp(form.energyReading, v => fld('energyReading', v), '0', 'number')}
                </div>
              )}

              <div className="form-group" style={{ marginTop: 12 }}>
                <label style={{ display: 'flex', alignItems: 'center', gap: 8, cursor: 'pointer' }}>
                  <input type="checkbox" checked={form.isActive !== false} onChange={e => fld('isActive', e.target.checked)} style={{ width: 16, height: 16, accentColor: 'var(--primary)', cursor: 'pointer' }} />
                  <span style={{ fontSize: 13, color: 'var(--text)' }}>{ar ? 'العداد نشط' : 'Active Meter'}</span>
                </label>
              </div>
            </div>

            {/* Footer */}
            <div className="ctrl-modal-footer">
              <button className="btn btn-ghost" onClick={resetForm}>{ar ? 'إلغاء' : 'Cancel'}</button>
              <button className="btn btn-primary" onClick={handleSave} disabled={saving}>
                {saving ? <><span className="ft-spinner" style={{ width: 14, height: 14 }} /> {ar ? 'جاري الحفظ...' : 'Saving...'}</> : <><Save size={14} /> {ar ? 'حفظ العداد' : 'Save Meter'}</>}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* ══════════════ Delete Confirm ══════════════ */}
      {deleteConfirm && (
        <div className="modal-overlay" onClick={e => e.target === e.currentTarget && setDeleteConfirm(null)}>
          <div className="ctrl-modal-box" style={{ maxWidth: 380 }}>
            <div className="ctrl-modal-header">
              <div style={{ fontSize: 16, fontWeight: 700, color: 'var(--danger)', display: 'flex', alignItems: 'center', gap: 8 }}>
                <AlertCircle size={18} /> {ar ? 'تأكيد الحذف' : 'Confirm Delete'}
              </div>
              <button className="modal-close" onClick={() => setDeleteConfirm(null)}><X size={18} /></button>
            </div>
            <div className="ctrl-modal-body" style={{ textAlign: 'center' }}>
              <p style={{ fontSize: 13, color: 'var(--text-muted)', marginBottom: 0 }}>
                {ar ? `هل أنت متأكد من حذف العداد "${deleteConfirm.name}"؟` : `Delete meter "${deleteConfirm.name}"?`}
              </p>
            </div>
            <div className="ctrl-modal-footer">
              <button className="btn btn-ghost" onClick={() => setDeleteConfirm(null)}>{ar ? 'إلغاء' : 'Cancel'}</button>
              <button className="btn btn-danger" onClick={handleDelete}>
                <Trash2 size={13} /> {ar ? 'حذف' : 'Delete'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

export default MetersManagement;
