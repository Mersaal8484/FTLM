import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { AlertCircle, Gauge, Pencil, Plus, Save, Trash2, X, Search, RefreshCw, Zap, Fuel, Radio, MapPin } from 'lucide-react';
import client from '../api/client';
import ConfirmModal from '../components/ConfirmModal';
import { PageHeader, Button, Modal, FormField, Input } from '../components/ui';

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

    setEditItem(item);
    setForm({
      branchId: item.branchId ?? '',
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

  const tabs = [
    { key: 'fuel',      label: ar ? 'عداد الوقود' : 'Fuel Meter',     icon: <Fuel size={14} />  },
    { key: 'generator', label: ar ? 'عداد المولدات' : 'Gen Meter',    icon: <Zap size={14} />   },
    { key: 'energy',    label: ar ? 'عداد الطاقة' : 'Energy Meter',   icon: <Radio size={14} /> },
  ];

  return (
    <div>
      <PageHeader
        title={ar ? 'العدادات' : 'Meters'}
        subtitle={ar ? 'إدارة العدادات المرتبطة بالمواقع والمولدات' : 'Manage meters linked to sites and generators'}
        actions={
          <div style={{ display: 'flex', gap: 8 }}>
            <Button variant="ghost" size="sm" icon={RefreshCw} onClick={fetchAll} />
            <Button icon={Plus} onClick={openAdd}>{ar ? 'جديد' : 'New'}</Button>
          </div>
        }
      />

      {/* KPI Cards */}
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

      {/* Search */}
      <div style={{ marginBottom: 20 }}>
        <div className="search-bar" style={{ maxWidth: 400 }}>
          <Search size={16} />
          <Input value={search} onChange={e => setSearch(e.target.value)} placeholder={ar ? 'بحث بالاسم، الرقم التسلسلي، أو الموقع...' : 'Search name, serial, or site...'} />
        </div>
      </div>

      {/* Error banner */}
      {error && (
        <div className="section-card" style={{ marginBottom: 16, borderColor: 'var(--danger)', padding: '12px 16px', display: 'flex', alignItems: 'center', gap: 10, color: 'var(--danger)' }}>
          <AlertCircle size={16} />
          <span style={{ fontSize: 13, flex: 1 }}>{error}</span>
          <button onClick={() => setError('')} style={{ marginInlineEnd: 'auto', background: 'none', border: 'none', color: 'var(--text-faint)', cursor: 'pointer' }}><X size={14} /></button>
        </div>
      )}

      {/* Cards */}
      {loading ? (
        <div className="spinner-wrap"><div className="ft-spinner" /></div>
      ) : filtered.length === 0 ? (
        <div className="empty-state"><Gauge size={48} /><h3>{ar ? 'لا توجد عدادات' : 'No meters'}</h3></div>
      ) : (
        <div className="grid-auto">
           {filtered.map((meter, i) => {
             const typeCode = meterTypes.find(mt => mt.name === meter.meterType)?.code;
             const cfg = METER_ICON_MAP[typeCode] || METER_DEFAULT_ICON;
             const linkedGen = meter.generatorId ? generators.find(g => String(g.id) === String(meter.generatorId)) : null;
             return (
              <div key={meter.id} className="meter-card">
                {/* Card Header */}
                <div className="meter-card-header">
                  <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                    <span style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', width: 32, height: 32, borderRadius: 8, background: `${cfg.color}18`, color: cfg.color, flexShrink: 0 }}>
                      {cfg.icon}
                    </span>
                    <div style={{ minWidth: 0, flex: 1 }}>
                      <div className="meter-name" style={{ fontSize: 14, fontWeight: 700, color: 'var(--text-main)', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                        {meter.name || (ar ? 'عداد بدون اسم' : 'Unnamed Meter')}
                      </div>
                      <div style={{ fontSize: 11, color: 'var(--text-muted)', marginTop: 2, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                        {meter.serialNumber || '-'}
                      </div>
                    </div>
                  </div>
                  <span className={`status-badge ${meter.isActive !== false ? 'status-active' : 'status-inactive'}`}>
                    {meter.isActive !== false ? (ar ? 'نشط' : 'Active') : (ar ? 'غير نشط' : 'Inactive')}
                  </span>
                </div>

                {/* Card Body */}
                <div className="meter-card-body">
                  {/* Location */}
                  <div style={{ display: 'flex', alignItems: 'center', gap: 5, fontSize: 12, color: 'var(--text-muted)', marginBottom: 8 }}>
                    <MapPin size={10} />
                    <span>{locationName(meter)}</span>
                  </div>
                  {linkedGen && (
                    <div style={{ display: 'flex', alignItems: 'center', gap: 5, fontSize: 12, color: 'var(--text-muted)', marginBottom: 8 }}>
                      <Zap size={10} />
                      <span>{linkedGen.name}</span>
                    </div>
                  )}

                   {/* Stats Grid */}
                   <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '6px 8px', fontSize: 11 }}>
                     <div className="meter-stat">
                       <div className="meter-stat-label">{ar ? 'طاقة' : 'Energy'}</div>
                       <div className="meter-stat-value">{Number(meter.energyReading || 0).toLocaleString('en-US')} kWh</div>
                     </div>
                     {typeCode !== 'ExportEnergy' && (
                       <div className="meter-stat">
                         <div className="meter-stat-label">{ar ? 'ساعات' : 'Hours'}</div>
                         <div className="meter-stat-value">{Number(meter.hoursReading || 0).toLocaleString('en-US')} h</div>
                       </div>
                     )}
                     {typeCode === 'ExportEnergy' && (
                       <div className="meter-stat">
                         <div className="meter-stat-label">{ar ? 'نوع العداد' : 'Meter Type'}</div>
                         <div className="meter-stat-value" style={{ color: cfg.color, fontWeight: 600 }}>{meter.meterType || '—'}</div>
                       </div>
                     )}
                   </div>
                </div>

                {/* Card Footer */}
                <div className="meter-card-footer">
                  <Button variant="ghost" size="sm" onClick={() => openEdit(meter)}>
                    <Pencil size={13} /> {ar ? 'تعديل' : 'Edit'}
                  </Button>
                  <Button variant="danger" size="sm" onClick={() => setDeleteConfirm(meter)}>
                    <Trash2 size={13} /> {ar ? 'حذف' : 'Delete'}
                  </Button>
                </div>
              </div>
            );
           })}
        </div>
      )}

      {/* ══════════════ Form Modal ══════════════ */}
      {showForm && (
        <Modal
          open={showForm}
          onClose={resetForm}
          title={
            <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
              {editItem ? <Pencil size={18} style={{ color: 'var(--primary)' }} /> : <Plus size={18} style={{ color: 'var(--primary)' }} />}
              {editItem ? (ar ? 'تعديل العداد' : 'Edit Meter') : (ar ? 'عداد جديد' : 'New Meter')}
            </div>
          }
          maxWidth={520}
          footer={
            <>
              <Button variant="ghost" onClick={resetForm}>{ar ? 'إلغاء' : 'Cancel'}</Button>
              <Button variant="primary" onClick={handleSave} disabled={saving} icon={Save}>
                {saving ? <><span className="ft-spinner" style={{ width: 14, height: 14 }} /> {ar ? 'جاري الحفظ...' : 'Saving...'}</> : <>{ar ? 'حفظ العداد' : 'Save Meter'}</>}
              </Button>
            </>
          }
        >
          {/* Tab bar */}
          <div style={{ display: 'flex', gap: 2, marginBottom: 16, background: 'var(--bg)', borderRadius: 10, padding: 3 }}>
            {tabs.map(t => (
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
          <div className="form-section-header">{ar ? 'الموقع' : 'Location'}</div>

          <FormField label={ar ? 'المحافظة *' : 'Governorate *'} required error={!form.subStationId && form.meterName ? (ar ? 'الرجاء اختيار المحطة والموقع' : 'Please select station and sub-station') : null}>
            <select className="form-select" value={form.branchId} onChange={e => fld('branchId', e.target.value)}>
              <option value="">— {ar ? 'اختر' : 'Select'} —</option>
              {branchOptions.map(b => <option key={b.value} value={b.value}>{b.label}</option>)}
            </select>
          </FormField>
          {form.branchId && (
            <FormField label={ar ? 'المحطة *' : 'Station *'}>
              <select className="form-select" value={form.stationId} onChange={e => fld('stationId', e.target.value)}>
                <option value="">— {ar ? 'اختر' : 'Select'} —</option>
                {filteredStations.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
              </select>
            </FormField>
          )}
          {form.stationId && (
            <FormField label={ar ? 'الموقع/الفرع *' : 'Site / Branch *'}>
              <select className="form-select" value={form.subStationId} onChange={e => fld('subStationId', e.target.value)}>
                <option value="">— {ar ? 'اختر' : 'Select'} —</option>
                {filteredSubStations.map(ss => <option key={ss.id} value={ss.id}>{ss.name}</option>)}
              </select>
            </FormField>
          )}
          {activeTab === 'generator' && form.subStationId && (
            <FormField label={ar ? 'المولد *' : 'Generator *'}>
              <select className="form-select" value={form.generatorId} onChange={e => fld('generatorId', e.target.value)}>
                <option value="">— {ar ? 'اختر' : 'Select'} —</option>
                {filteredGenerators.map(g => <option key={g.id} value={g.id}>{g.name}</option>)}
              </select>
            </FormField>
          )}

          <div className="form-section-header">{ar ? 'بيانات العداد' : 'Meter Details'}</div>

          <FormField label={ar ? 'اسم العداد *' : 'Meter Name *'} required error={!form.meterName ? (ar ? 'الرجاء تعبئة اسم العداد' : 'Meter name is required') : null}>
            <Input value={form.meterName} onChange={e => fld('meterName', e.target.value)} />
          </FormField>
          <FormField label={ar ? 'الرقم التسلسلي *' : 'Serial Number *'} required error={!form.serialNumber ? (ar ? 'الرقم التسلسلي مطلوب' : 'Serial number is required') : null}>
            <Input value={form.serialNumber} onChange={e => fld('serialNumber', e.target.value)} />
          </FormField>

          <div className="form-section-header">{ar ? 'القراءات' : 'Readings'}</div>

          {activeTab !== 'energy' ? (
            <div className="form-row">
              <FormField label={ar ? 'قراءة العياري' : 'Standard Reading'}>
                <Input type="number" value={form.energyReading} onChange={e => fld('energyReading', e.target.value)} placeholder="0" />
              </FormField>
              <FormField label={ar ? 'قراءة المشاهد' : 'Running Reading'}>
                <Input type="number" value={form.hoursReading} onChange={e => fld('hoursReading', e.target.value)} placeholder="0" />
              </FormField>
            </div>
          ) : (
            <FormField label={ar ? 'قراءة الطاقة' : 'Reading'}>
              <Input type="number" value={form.energyReading} onChange={e => fld('energyReading', e.target.value)} placeholder="0" />
            </FormField>
          )}

          <div className="form-group" style={{ marginTop: 12 }}>
            <label className="checkbox-label">
              <input type="checkbox" checked={form.isActive !== false} onChange={e => fld('isActive', e.target.checked)} />
              <span>{ar ? 'العداد نشط' : 'Active Meter'}</span>
            </label>
          </div>
        </Modal>
      )}

      <ConfirmModal
        open={!!deleteConfirm}
        title={ar ? 'تأكيد الحذف' : 'Confirm Delete'}
        message={ar ? `هل أنت متأكد من حذف العداد "${deleteConfirm?.name}"؟` : `Delete meter "${deleteConfirm?.name}"?`}
        confirmText={ar ? 'حذف' : 'Delete'}
        cancelText={ar ? 'إلغاء' : 'Cancel'}
        danger
        onConfirm={handleDelete}
        onCancel={() => setDeleteConfirm(null)}
      />
    </div>
  );
}

export default MetersManagement;
