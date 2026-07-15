import React, { useEffect, useState, useMemo } from 'react';
import { Plus, Search, Edit2, Trash2, Zap, X, Save } from 'lucide-react';
import client from '../api/client';
import ConfirmModal from '../components/ConfirmModal';
import Pagination from '../components/Pagination';

const initialForm = {
  // بيانات المولد الأساسية
  name: '',
  serialNo: '',
  manufacturer: '',
  model: '',
  genType: '',
  ratedCapacityKw: '',
  powerFactor: '',
  actualPowerKw: '',
  voltage: '',
  frequency: '',
  owner: '',
  fuelConsumptionRatePerHour: '',
  // حالة التشغيل
  status: 'واقف',
  lastMaintenance: '',
  installationDate: '',
  notes: '',
  // الروابط
  branchId: '',
  station: '',
  subStation: '',
  ownershipType: 'ملك',
  controller: '',
  isActive: true,
};

const statusConfig = {
  'شغال': { cls: 'badge-success', dot: 'online', label: { ar: 'يعمل', en: 'Running' } },
  'واقف': { cls: 'badge-warning', dot: 'idle', label: { ar: 'خامل', en: 'Idle' } },
  'تحت الصيانة': { cls: 'badge-danger', dot: 'offline', label: { ar: 'صيانة', en: 'Maintenance' } },
};

function StatusBadge({ status, lang }) {
  if (!status) {
    return (
      <span className="badge badge-secondary">
        <span className="status-dot idle" />
        {lang === 'ar' ? 'غير محدد' : 'Unknown'}
      </span>
    );
  }
  const cfg = statusConfig[status];
  if (!cfg) {
    return (
      <span className="badge badge-secondary">
        <span className="status-dot idle" />
        {status}
      </span>
    );
  }
  return (
    <span className={`badge ${cfg.cls}`}>
      <span className={`status-dot ${cfg.dot}`} />
      {cfg.label[lang] || status}
    </span>
  );
}

function Generators({ t, lang, currentUser }) {
  const ar = lang === 'ar';
  
  // Permission helpers
  const perms = currentUser?.permissions?.generators || {};
  const canAdd = currentUser?.role === 'admin' || perms.add || perms.create;
  const canEdit = currentUser?.role === 'admin' || perms.edit;
  const canDelete = currentUser?.role === 'admin' || perms.delete;
  const [generators, setGenerators] = useState([]);
  const [showModal, setShowModal] = useState(false);
  const [editId, setEditId] = useState(null);
  const [form, setForm] = useState(initialForm);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState(null);
  const [search, setSearch] = useState('');
  const [confirmDelete, setConfirmDelete] = useState({ open: false, id: null });
  const [page, setPage] = useState(1);
  const PAGE_SIZE = 20;
  
  const [stations, setStations] = useState([]);
  const [subStations, setSubStations] = useState([]);
  const [controllers, setControllers] = useState([]);
  const [genTypes, setGenTypes] = useState([]);
  const [voltages, setVoltages] = useState([]);
  const [frequencies, setFrequencies] = useState([]);
  const [ownershipTypes, setOwnershipTypes] = useState([]);
  const [statuses, setStatuses] = useState([]);
  const [branches, setBranches] = useState([]);

  const load = async () => {
    setLoading(true); setError(null);
    try {
      const requests = [
        client.get('/api/generators', { params: { _t: Date.now() } }),
        client.get('/api/stations', { params: { _t: Date.now() } }),
        client.get('/api/sub-stations', { params: { _t: Date.now() } }),
        client.get('/api/lookups/by-category/GeneratorType', { params: { _t: Date.now() } }),
        client.get('/api/lookups/by-category/Voltage', { params: { _t: Date.now() } }),
        client.get('/api/lookups/by-category/Frequency', { params: { _t: Date.now() } }),
        client.get('/api/lookups/by-category/OwnershipType', { params: { _t: Date.now() } }),
        client.get('/api/lookups/by-category/GeneratorStatus', { params: { _t: Date.now() } }),
        client.get('/api/branches', { params: { _t: Date.now() } }),
      ];

      if (canAdd || canEdit) {
        requests.push(client.get('/api/controllers', { params: { _t: Date.now() } }));
      }

      const results = await Promise.allSettled(requests);
      const [genRes, stRes, subRes, genTypeRes, voltRes, freqRes, ownRes, statRes, brRes, ctrlRes] = results;
      if (genRes.status !== 'fulfilled') throw genRes.reason;

      setGenerators(Array.isArray(genRes.value.data?.data) ? genRes.value.data.data : Array.isArray(genRes.value.data) ? genRes.value.data : []);
      setStations(stRes.status === 'fulfilled' && Array.isArray(stRes.value.data) ? stRes.value.data : []);
      setSubStations(subRes.status === 'fulfilled' && Array.isArray(subRes.value.data) ? subRes.value.data.filter(s => s.isActive !== false) : []);
      setGenTypes(genTypeRes.status === 'fulfilled' && Array.isArray(genTypeRes.value.data) ? genTypeRes.value.data : []);
      setVoltages(voltRes.status === 'fulfilled' && Array.isArray(voltRes.value.data) ? voltRes.value.data : []);
      setFrequencies(freqRes.status === 'fulfilled' && Array.isArray(freqRes.value.data) ? freqRes.value.data : []);
      setOwnershipTypes(ownRes.status === 'fulfilled' && Array.isArray(ownRes.value.data) ? ownRes.value.data : []);
      setStatuses(statRes.status === 'fulfilled' && Array.isArray(statRes.value.data) ? statRes.value.data : []);
      setBranches(brRes.status === 'fulfilled' && Array.isArray(brRes.value.data) ? brRes.value.data : []);
      const ctrlData = ctrlRes?.status === 'fulfilled' ? ctrlRes.value.data : [];
      setControllers(Array.isArray(ctrlData) ? ctrlData : (ctrlData.controllers || []));
    } catch { setError(t.noData); }
    finally { setLoading(false); }
  };

  const filteredStations = useMemo(() => {
    if (!form.branchId) return [];
    return stations.filter(s => s.branchId === Number(form.branchId));
  }, [stations, form.branchId]);

  const filteredSubStations = useMemo(() => {
    if (!form.station) return [];
    return subStations.filter(ss => ss.stationId === Number(form.station) || ss.station_id === form.station);
  }, [subStations, form.station]);

  useEffect(() => { load(); }, []); // eslint-disable-line react-hooks/exhaustive-deps

  const openAdd = () => { setForm(initialForm); setEditId(null); setShowModal(true); setError(null); };
  const openEdit = (gen) => {
    const subStationId = gen.subStationId || gen.subStation?.id || gen.subStation || '';
    const subStationObj = subStationId ? subStations.find(ss => String(ss.id) === String(subStationId)) : null;
    const stationId = subStationObj?.station_id || gen.stationId || gen.station?.id || '';
    const stationObj = stationId ? stations.find(s => String(s.id) === String(stationId)) : null;
    const branchId = stationObj?.branchId || '';
    setForm({
      name: gen.name || '',
      serialNo: gen.serialNumber || '',
      manufacturer: gen.manufacturer || '',
      model: gen.model || '',
      genType: gen.generatorTypeLookupName || gen.genType || '',
      ratedCapacityKw: gen.nominalPower || gen.ratedCapacityKw || '',
      actualPowerKw: gen.actualPowerKW || gen.actualPowerKw || '',
      fuelConsumptionRatePerHour: gen.fuelConsumptionRatePerHour ?? gen.fuelConsumption ?? '',
      owner: gen.ownerName || gen.owner || '',
      voltage: gen.voltageLookupName || gen.voltage || '',
      frequency: gen.frequencyLookupName || gen.frequency || '',
      powerFactor: gen.powerFactor || '',
      ownershipType: gen.ownershipTypeLookupName || gen.ownershipType || 'ملك',
      status: gen.statusLookupName || gen.status || 'واقف',
      installationDate: gen.installationDate ? gen.installationDate.substring(0, 10) : '',
      notes: gen.notes || '',
      branchId: branchId,
      station: stationId,
      subStation: subStationId,
      controller: controllers.find(c => c.generatorId === gen.id)?.id || '',
      isActive: gen.isActive !== false,
    });
    setEditId(getId(gen)); setShowModal(true); setError(null);
  };
  const closeModal = () => { setShowModal(false); setError(null); };
  const fld = (k, v) => setForm(f => ({ ...f, [k]: v }));

  const handleSubmit = async () => {
    if (!form.name) { setError(ar ? 'اسم المولد مطلوب' : 'Generator name is required'); return; }
    if (!form.subStation) { setError(ar ? 'المنطقة (وحدة فرعية) مطلوبة' : 'SubStation is required'); return; }
    if (!form.ratedCapacityKw || Number(form.ratedCapacityKw) <= 0) { setError(ar ? 'القدرة الاسمية مطلوبة ويجب أن تكون أكبر من صفر' : 'Nominal power is required and must be greater than zero'); return; }
    if (!form.fuelConsumptionRatePerHour || Number(form.fuelConsumptionRatePerHour) <= 0) { setError(ar ? 'معدل استهلاك الوقود مطلوب ويجب أن يكون أكبر من صفر' : 'Fuel consumption rate is required and must be greater than zero'); return; }
    setSaving(true); setError(null);
    try {
      let generatorId;
      if (editId) {
        const payload = {
          SubStationId: Number(form.subStation),
          Name: form.name,
          SerialNumber: form.serialNo || null,
          Model: form.model || null,
          Manufacturer: form.manufacturer || null,
          NominalPower: Number(form.ratedCapacityKw) || 0,
          ActualPowerKW: Number(form.actualPowerKw) || 0,
          FuelConsumptionRatePerHour: Number(form.fuelConsumptionRatePerHour) || 0,
          OwnerName: form.owner || null,
          PowerFactor: form.powerFactor ? Number(form.powerFactor) : null,
          InstallationDate: form.installationDate || null,
          Notes: form.notes || null,
          GeneratorTypeLookupName: form.genType || null,
          VoltageLookupName: form.voltage || null,
          FrequencyLookupName: form.frequency || null,
          OwnershipTypeLookupName: form.ownershipType || null,
          StatusLookupName: form.status || null,
          IsActive: form.isActive,
        };
        await client.put(`/api/generators/${editId}`, payload);
        generatorId = editId;
      } else {
        const genCode = (form.name || 'GEN').substring(0, 12).toUpperCase().replace(/\s+/g, '') + '-' + Date.now().toString(36).toUpperCase().slice(-4);
        const payload = {
          SubStationId: Number(form.subStation),
          Name: form.name,
          SerialNumber: form.serialNo || genCode,
          Model: form.model || null,
          Manufacturer: form.manufacturer || null,
          NominalPower: Number(form.ratedCapacityKw) || 0,
          ActualPowerKW: Number(form.actualPowerKw) || 0,
          FuelConsumptionRatePerHour: Number(form.fuelConsumptionRatePerHour) || 0,
          OwnerName: form.owner || null,
          PowerFactor: form.powerFactor ? Number(form.powerFactor) : null,
          InstallationDate: form.installationDate || null,
          Notes: form.notes || null,
          GeneratorTypeLookupName: form.genType || null,
          VoltageLookupName: form.voltage || null,
          FrequencyLookupName: form.frequency || null,
          OwnershipTypeLookupName: form.ownershipType || null,
          StatusLookupName: form.status || null,
          IsActive: form.isActive,
        };
        const res = await client.post('/api/generators', payload);
        generatorId = res.data?.id || res.data?.data?.id;
      }

      // ── ربط/إلغاء ربط الشاشة ──
      if (generatorId) {
        const selectedCtrlId = form.controller ? Number(form.controller) : null;
        const prevCtrl = controllers.find(c => c.generatorId === generatorId && c.id !== selectedCtrlId);
        if (prevCtrl) {
          await client.patch(`/api/controllers/${prevCtrl.id}/assign-generator`, { generatorId: null });
        }
        if (selectedCtrlId) {
          await client.patch(`/api/controllers/${selectedCtrlId}/assign-generator`, { generatorId });
        }
      }

      closeModal(); await load();
    } catch (err) {
      const msg = err.response?.data?.message || err.response?.data?.title || err.message;
      setError(msg || t.noData);
    }
    finally { setSaving(false); }
  };

  const handleDelete = async (id) => {
    setConfirmDelete({ open: true, id });
  };

  const confirmDeleteAction = async () => {
    const id = confirmDelete.id;
    setConfirmDelete({ open: false, id: null });
    try { await client.delete(`/api/generators/${id}`); setGenerators(g => g.filter(x => getId(x) !== id)); }
    catch (err) {
      if (err.response?.status === 404) {
        console.warn(`Generator ${id} not found in DB — removing from UI`);
        setGenerators(g => g.filter(x => getId(x) !== id));
      } else { setError(t.noData); }
    }
  };

  const getId = (value) => value?.id || value?._id || value || '';
  const displayName = (value) => {
    if (value && typeof value === 'object') return '';
    const text = value?.toString().trim();
    if (!text) return '';
    if (/^[a-f0-9]{24}$/i.test(text)) return '';
    return text;
  };
  const stationById = new Map(stations.map(station => [getId(station), station]));
  const subStationById = new Map(subStations.map(site => [getId(site), site]));
  const stationNameFor = (gen) => {
    const stationId = getId(gen.station);
    return displayName(gen.station?.name) ||
      displayName(stationById.get(stationId)?.name) ||
      displayName(gen.stationName) ||
      displayName(gen.station);
  };
  const subStationFor = (gen) => {
    const siteId = getId(gen.subStation);
    return gen.subStation?.name
      ? gen.subStation
      : subStationById.get(siteId) || null;
  };
  const subStationNameFor = (gen) => {
    const site = subStationFor(gen);
    return displayName(site?.name) ||
      displayName(site?.subStationName) ||
      displayName(gen.subStation?.name) ||
      displayName(gen.subStationName);
  };
  const generatorSitePath = (gen) => {
    const stationName = stationNameFor(gen);
    const siteName = subStationNameFor(gen);
    if (stationName && siteName && stationName !== siteName) {
      return `${stationName} ← ${siteName}`;
    }
    return siteName || stationName || (ar ? 'غير محدد (بدون موقع)' : 'Unassigned');
  };
  const generatorSiteName = (gen) => {
    return subStationNameFor(gen) || stationNameFor(gen) || (ar ? 'غير محدد' : 'Unassigned');
  };

  const filtered = generators.filter(g => {
    const q = search.toLowerCase();
    return !search ||
      g.name?.toLowerCase().includes(q) ||
      generatorSitePath(g).toLowerCase().includes(q) ||
      g.manufacturer?.toLowerCase().includes(q);
  });

  const filteredTotal = filtered.length;
  const totalPages = Math.max(1, Math.ceil(filteredTotal / PAGE_SIZE));
  const safePage = Math.min(page, totalPages);
  const paginatedFiltered = filtered.slice((safePage - 1) * PAGE_SIZE, safePage * PAGE_SIZE);

  // Reset page when search changes
  const handleSearchChange = (e) => { setSearch(e.target.value); setPage(1); };

  // Section header style inside modal
  const secHdr = {
    fontSize: 11, fontWeight: 700, textTransform: 'uppercase', letterSpacing: '0.8px',
    color: 'var(--primary)', padding: '14px 0 8px', marginTop: 8,
    borderBottom: '1px solid var(--border)', marginBottom: 12,
  };

  // ── تجميع المولدات بناءً على الموقع/الفرع مع إظهار المحطة الأم ──
  const groupedGenerators = {};
  paginatedFiltered.forEach(gen => {
    const stationId = getId(gen.station);
    const siteId = getId(gen.subStation);
    const groupKey = siteId || stationId || 'unassigned';
    if (!groupedGenerators[groupKey]) {
      groupedGenerators[groupKey] = {
        label: generatorSitePath(gen),
        gens: [],
      };
    }
    groupedGenerators[groupKey].gens.push(gen);
  });

  return (
    <div>
      <div className="page-header">
        <div className="page-header-row">
          <div>
            <h1>{t.generators}</h1>
            <p>{t.manageGenerators}</p>
          </div>
          {canAdd && (
            <button className="btn btn-primary" onClick={openAdd}>
              <Plus size={16} /> {t.addGenerator}
            </button>
          )}
        </div>
      </div>

      {/* Search */}
      <div style={{ marginBottom: 20 }}>
        <div className="search-bar" style={{ maxWidth: 340 }}>
          <Search size={16} />
          <input className="form-control"
            placeholder={ar ? 'بحث بالاسم أو الشركة أو الموقع...' : 'Search by name, manufacturer or location...'}
            value={search} onChange={handleSearchChange} />
        </div>
      </div>

      {error && !showModal && <div className="alert alert-danger">{error}</div>}

      {loading ? (
        <div className="spinner-wrap"><div className="ft-spinner" /></div>
      ) : filtered.length === 0 ? (
        <div className="empty-state"><Zap size={48} /><h3>{t.noData}</h3></div>
      ) : (<>
        <div className="grouped-container">
          {Object.entries(groupedGenerators).map(([groupKey, group]) => (
            <div key={groupKey} className="station-group" style={{ marginBottom: 40 }}>
              <div className="station-group-header" style={{ display: 'flex', alignItems: 'center', gap: 15, padding: '15px 20px', background: 'var(--bg-card)', border: '1px solid var(--border)', borderBottom: '2px solid var(--primary)', borderRadius: '12px', marginBottom: 20, boxShadow: '0 4px 15px rgba(0,0,0,0.1)' }}>
                <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', width: 42, height: 42, borderRadius: 10, background: 'var(--primary-light)', color: 'var(--primary)' }}>
                </div>
                <div>
                  <h3 style={{ margin: 0, fontSize: 18, fontWeight: 'bold', color: 'var(--text-main)' }}>{group.label}</h3>
                  <div style={{ fontSize: 12, color: 'var(--text-muted)', marginTop: 4 }}>{ar ? 'المولدات التابعة لهذا الموقع' : 'Generators in this location'}</div>
                </div>
                <span style={{ marginRight: ar ? 'auto' : 0, marginLeft: ar ? 0 : 'auto', padding: '6px 14px', background: 'var(--primary)', color: '#fff', borderRadius: 20, fontSize: 13, fontWeight: 'bold' }}>
                  {group.gens.length} {ar ? 'مولدات' : 'Generators'}
                </span>
              </div>
              <div className="grid-auto">
                {group.gens.map(gen => (
                  <div key={gen.id} className="gen-card">
                    <div className="gen-card-header">
                      <div>
                        <div className="gen-name">{gen.name}</div>
                        <div className="gen-model">{gen.manufacturer} {gen.model && `— ${gen.model}`}</div>
                      </div>
                       <StatusBadge status={gen.statusLookupName || gen.status} lang={lang} />
                       <span className={`status-badge ${gen.isActive !== false ? 'status-active' : 'status-inactive'}`}>
                         {gen.isActive !== false ? (ar ? 'نشط' : 'Active') : (ar ? 'غير نشط' : 'Inactive')}
                       </span>
                    </div>
                    <div className="gen-stats">
                      <div className="gen-stat-item">
                        <div className="gen-stat-label">{ar ? 'القدرة الاسمية' : 'Nominal Power'}</div>
                        <div className="gen-stat-value">{(gen.nominalPower || gen.ratedCapacityKw) ? `${gen.nominalPower || gen.ratedCapacityKw} KW` : '—'}</div>
                      </div>
                      <div className="gen-stat-item">
                        <div className="gen-stat-label">{ar ? 'القدرة الفعلية' : 'Actual Power'}</div>
                        <div className="gen-stat-value">{(gen.actualPowerKW || gen.actualPowerKw) ? `${gen.actualPowerKW || gen.actualPowerKw} KW` : '—'}</div>
                      </div>
                      <div className="gen-stat-item">
                        <div className="gen-stat-label">{ar ? 'نوع المولد' : 'Type'}</div>
                        <div className="gen-stat-value">{gen.generatorTypeLookupName || gen.genType || '—'}</div>
                      </div>
                      <div className="gen-stat-item">
                        <div className="gen-stat-label">{ar ? 'الموقع' : 'Location'}</div>
                        <div className="gen-stat-value" style={{ fontSize: 12 }}>{generatorSiteName(gen)}</div>
                      </div>
                      <div className="gen-stat-item">
                        <div className="gen-stat-label">{ar ? 'الساعات الحالية' : 'Current Hours'}</div>
                        <div className="gen-stat-value">{gen.lastHoursRecord ? `${gen.lastHoursRecord} h` : (gen.initialHoursRecord ? `${gen.initialHoursRecord} h` : '—')}</div>
                      </div>
                      <div className="gen-stat-item">
                        <div className="gen-stat-label">{ar ? 'قراءة الطاقة الحالية' : 'Current Energy'}</div>
                        <div className="gen-stat-value">{gen.lastPowerRecord ? `${gen.lastPowerRecord} KWh` : (gen.initialPowerRecord ? `${gen.initialPowerRecord} KWh` : '—')}</div>
                      </div>
                      <div className="gen-stat-item">
                        <div className="gen-stat-label">{ar ? 'الفولت' : 'Voltage'}</div>
                        <div className="gen-stat-value">{gen.voltageLookupName || gen.voltage || '—'}</div>
                      </div>
                      <div className="gen-stat-item">
                        <div className="gen-stat-label">{ar ? 'التردد' : 'Frequency'}</div>
                        <div className="gen-stat-value">{gen.frequencyLookupName || gen.frequency || '—'}</div>
                      </div>
                    </div>
                    <div className="gen-card-actions">
                      {canEdit && (
                        <button className="btn btn-warning btn-sm" onClick={() => openEdit(gen)}>
                          <Edit2 size={13} /> {t.edit}
                        </button>
                      )}
                      {canDelete && (
                        <button className="btn btn-danger btn-sm" onClick={() => handleDelete(getId(gen))}>
                          <Trash2 size={13} /> {t.delete}
                        </button>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          ))}
        </div>
        {filteredTotal > PAGE_SIZE && (
          <Pagination page={safePage} totalPages={totalPages} total={filteredTotal} pageSize={PAGE_SIZE} lang={lang}
            onPageChange={setPage} />
        )}
      </>)}
      {showModal && (
        <div className="modal-overlay" onClick={e => e.target === e.currentTarget && closeModal()}>
          <div className="modal-box" style={{ maxWidth: 680 }}>
            <div className="modal-header">
              <div className="modal-title">{editId ? (ar ? 'تعديل بيانات المولد' : 'Edit Generator') : (ar ? 'إضافة مولد جديد' : 'Add New Generator')}</div>
              <button className="modal-close" onClick={closeModal}><X size={18} /></button>
            </div>
            <div className="modal-body">
              {error && <div className="alert alert-danger">{error}</div>}

              {/* Section 1 — بيانات المولد الأساسية */}
              <div style={secHdr}>{ar ? 'بيانات المولد الأساسية' : 'Generator Basic Data'}</div>
              <div className="form-row">
                <div className="form-group">
                  <label className="form-label">{ar ? 'اسم المولد *' : 'Generator Name *'}</label>
                  <input className="form-control" value={form.name} onChange={e => fld('name', e.target.value)} />
                </div>
              </div>
              <div className="form-row">
                <div className="form-group">
                  <label className="form-label">{ar ? 'الفرع *' : 'Branch *'}</label>
                  <select className="form-select" value={form.branchId} onChange={e => {
                    fld('branchId', e.target.value);
                    fld('station', '');
                    fld('subStation', '');
                  }}>
                    <option value="">— {ar ? 'اختر الفرع' : 'Select Branch'} —</option>
                    {branches.map(b => <option key={b.id} value={b.id}>{b.name}</option>)}
                  </select>
                </div>
                <div className="form-group">
                  <label className="form-label">{ar ? 'المحطة' : 'Station'}</label>
                  <select className="form-select" value={form.station} onChange={e => {
                    fld('station', e.target.value);
                    fld('subStation', '');
                  }} disabled={!form.branchId}>
                    <option value="">— {ar ? 'اختر المحطة' : 'Select Station'} —</option>
                    {filteredStations.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
                  </select>
                </div>
                <div className="form-group">
                  <label className="form-label">{ar ? 'المنطقة (وحدة فرعية)' : 'Area (SubStation)'}</label>
                  <select className="form-select" value={form.subStation} onChange={e => fld('subStation', e.target.value)} disabled={!form.station}>
                    <option value="">— {ar ? 'اختر المنطقة' : 'Select Area'} —</option>
                    {filteredSubStations.map(ss => (
                      <option key={ss.id} value={ss.id}>{ss.name}</option>
                    ))}
                  </select>
                </div>
              </div>
              <div className="form-row">
                <div className="form-group">
                  <label className="form-label">{ar ? 'الرقم التسلسلي' : 'Serial No.'}</label>
                  <input className="form-control" value={form.serialNo} onChange={e => fld('serialNo', e.target.value)} />
                </div>
                <div className="form-group">
                  <label className="form-label">{ar ? 'الشركة المصنعة' : 'Manufacturer'}</label>
                  <input className="form-control" value={form.manufacturer} onChange={e => fld('manufacturer', e.target.value)} />
                </div>
              </div>
              <div className="form-row">
                <div className="form-group">
                  <label className="form-label">{ar ? 'الموديل' : 'Model'}</label>
                  <input className="form-control" value={form.model} onChange={e => fld('model', e.target.value)} />
                </div>
                <div className="form-group">
                  <label className="form-label">{ar ? 'نوع المولد' : 'Generator Type'}</label>
                  <select className="form-select" value={form.genType} onChange={e => fld('genType', e.target.value)}>
                    <option value="">— {ar ? 'اختر' : 'Select'} —</option>
                    {genTypes.map(item => (
                      <option key={item.id} value={item.name}>{item.name}</option>
                    ))}
                  </select>
                </div>
              </div>
              <div className="form-row">
                <div className="form-group">
                  <label className="form-label">{ar ? 'القدرة الاسمية (تصنيف المولد)' : 'Nominal Capacity'}</label>
                  <input className="form-control" type="number" value={form.ratedCapacityKw} onChange={e => fld('ratedCapacityKw', e.target.value)} />
                </div>
              </div>

              {/* Section — بيانات شاشة التحكم */}
              <div style={secHdr}>{ar ? 'بيانات شاشة التحكم' : 'Controller Data'}</div>
              <div className="form-row">
                <div className="form-group">
                  <label className="form-label" style={{ color: 'var(--warning)' }}>{ar ? 'شاشة التحكم المرتبطة' : 'Linked Controller'}</label>
                  <select className="form-select" value={form.controller} onChange={e => {
                    const ctrlId = e.target.value;
                    fld('controller', ctrlId);
                  }}>
                    <option value="">— {ar ? 'اختر الشاشة' : 'Select Screen'} —</option>
                    {controllers.filter(c => !c.generatorId || c.generatorId === editId).map(c => (
                      <option key={c.id} value={c.id}>{c.name} ({c.code}) — {c.controllerType}</option>
                    ))}
                  </select>
                </div>
                <div className="form-group">
                  <label className="form-label">{ar ? 'وحدة الطاقة' : 'Energy Unit'}</label>
                  <input className="form-control" readOnly
                    value={controllers.find(c => c.id === Number(form.controller))?.energyUnit || 'KW'}
                    style={{ background: 'var(--card-2)', color: 'var(--secondary)', fontWeight: 600 }} />
                </div>
              </div>
              <div className="form-row">
                <div className="form-group">
                  <label className="form-label">{ar ? 'قراءة عداد الطاقة الحالية' : 'Current Energy Reading'}</label>
                  <input className="form-control" readOnly
                    value={controllers.find(c => c.id === Number(form.controller))?.energyReading || 0}
                    style={{ background: 'var(--card-2)' }} />
                </div>
                <div className="form-group">
                  <label className="form-label">{ar ? 'قراءة ساعات التشغيل الحالية' : 'Current Operating Hours'}</label>
                  <input className="form-control" readOnly
                    value={controllers.find(c => c.id === Number(form.controller))?.operatingHours || 0}
                    style={{ background: 'var(--card-2)' }} />
                </div>
              </div>
              <div className="form-row">
                <div className="form-group">
                  <label className="form-label">{ar ? 'معامل القدرة' : 'Power Factor'}</label>
                  <input className="form-control" value={form.powerFactor} onChange={e => fld('powerFactor', e.target.value)} />
                </div>
                <div className="form-group">
                  <label className="form-label">{ar ? 'القدرة الفعلية (KW)' : 'Actual Power (KW)'}</label>
                  <input className="form-control" type="number" value={form.actualPowerKw} onChange={e => fld('actualPowerKw', e.target.value)} />
                </div>
                <div className="form-group">
                  <label className="form-label">{ar ? 'معدل استهلاك الوقود (لتر/KWh)' : 'Fuel Consumption Rate (L/KWh)'}</label>
                  <input className="form-control" value={form.fuelConsumptionRatePerHour} onChange={e => fld('fuelConsumptionRatePerHour', e.target.value)} />
                </div>
              </div>
              <div className="form-row">
                <div className="form-group">
                  <label className="form-label">{ar ? 'مستوى الفولتات' : 'Voltage Level'}</label>
                  <select className="form-select" value={form.voltage} onChange={e => fld('voltage', e.target.value)}>
                    <option value="">—</option>
                    {voltages.map(item => (
                      <option key={item.id} value={item.name}>{item.name}</option>
                    ))}
                  </select>
                </div>
                <div className="form-group">
                  <label className="form-label">{ar ? 'التردد' : 'Frequency'}</label>
                  <select className="form-select" value={form.frequency} onChange={e => fld('frequency', e.target.value)}>
                    <option value="">—</option>
                    {frequencies.map(item => (
                      <option key={item.id} value={item.name}>{item.name}</option>
                    ))}
                  </select>
                </div>
              </div>
              <div className="form-row">
                <div className="form-group">
                  <label className="form-label">{ar ? 'اسم المالك' : 'Owner Name'}</label>
                  <input className="form-control" value={form.owner} onChange={e => fld('owner', e.target.value)} />
                </div>
                <div className="form-group">
                  <label className="form-label">{ar ? 'نوع الملكية' : 'Ownership Type'}</label>
                  <select className="form-select" value={form.ownershipType} onChange={e => fld('ownershipType', e.target.value)}>
                    <option value="">—</option>
                    {ownershipTypes.map(item => (
                      <option key={item.id} value={item.name}>{item.name}</option>
                    ))}
                  </select>
                </div>
                <div className="form-group">
                  <label className="form-label">{ar ? 'حالة التشغيل' : 'Operating Status'}</label>
                  <select className="form-select" value={form.status} onChange={e => fld('status', e.target.value)}>
                    <option value="">—</option>
                    {statuses.map(item => (
                      <option key={item.id} value={item.name}>{item.name}</option>
                    ))}
                  </select>
                </div>
              </div>

              {/* Section 3 — صيانة وتواريخ */}
              <div style={secHdr}>{ar ? 'بيانات الصيانة' : 'Maintenance Data'}</div>
              <div className="form-row">
                <div className="form-group">
                  <label className="form-label">{ar ? 'تاريخ التركيب' : 'Installation Date'}</label>
                  <input className="form-control" type="date" value={form.installationDate} onChange={e => fld('installationDate', e.target.value)} />
                </div>
                <div className="form-group">
                  <label className="form-label">{ar ? 'آخر صيانة' : 'Last Maintenance'}</label>
                  <input className="form-control" type="date" value={form.lastMaintenance} onChange={e => fld('lastMaintenance', e.target.value)} />
                </div>
              </div>
              <div className="form-group">
                <label className="form-label">{ar ? 'ملاحظات' : 'Notes'}</label>
                <textarea className="form-control" rows={2} value={form.notes}
                  onChange={e => fld('notes', e.target.value)}
                  style={{ resize: 'vertical', fontFamily: 'inherit' }} />
              </div>
              <div className="form-group" style={{ marginTop: 8 }}>
                <label className="checkbox-label">
                  <input type="checkbox" checked={form.isActive} onChange={e => fld('isActive', e.target.checked)} />
                  <span>{ar ? 'نشط' : 'Active'}</span>
                </label>
              </div>
            </div>
            <div className="modal-footer">
              <button className="btn btn-ghost" onClick={closeModal}>{ar ? 'إلغاء' : 'Cancel'}</button>
              <button className="btn btn-primary" onClick={handleSubmit} disabled={saving}>
                {saving
                  ? <><span className="ft-spinner" style={{ width: 14, height: 14 }} /> {ar ? 'جاري الحفظ...' : 'Saving...'}</>
                  : <><Save size={14} /> {editId ? (ar ? 'تحديث المولد' : 'Update Generator') : (ar ? 'إضافة مولد' : 'Add Generator')}</>}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Delete Confirmation Modal */}
      <ConfirmModal
        open={confirmDelete.open}
        title={ar ? 'حذف المولد' : 'Delete Generator'}
        message={ar ? 'هل تريد حذف هذا المولد؟ هذا الإجراء لا يمكن التراجع عنه.' : 'Delete this generator? This action cannot be undone.'}
        confirmText={ar ? 'نعم، حذف' : 'Yes, Delete'}
        cancelText={ar ? 'إلغاء' : 'Cancel'}
        danger
        onConfirm={confirmDeleteAction}
        onCancel={() => setConfirmDelete({ open: false, id: null })}
      />
    </div>
  );
}

export default Generators;
