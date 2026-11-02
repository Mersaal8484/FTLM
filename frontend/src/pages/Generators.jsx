import React, { useEffect, useState, useMemo } from 'react';
import { Plus, Search, Edit2, Trash2, Zap, MapPin, ChevronDown, ChevronRight, Save } from 'lucide-react';
import client from '../api/client';
import ConfirmModal from '../components/ConfirmModal';
import Pagination from '../components/Pagination';
import { PageHeader, Button, Modal, FormField, Input } from '../components/ui';

const initialForm = {
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
  status: '',
  lastMaintenance: '',
  installationDate: '',
  notes: '',
  branchId: '',
  station: '',
  subStation: '',
  ownershipType: '',
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
  const [expandedStations, setExpandedStations] = useState(new Set());
  const [expandedSites, setExpandedSites] = useState(new Set());

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
    const stationId = subStationObj?.station_id || subStationObj?.stationId || gen.stationId || gen.station?.id || '';
    const stationObj = stationId ? stations.find(s => String(s.id) === String(stationId)) : null;
    const branchId = stationObj?.branchId || '';
    setForm({
      name: gen.name || '',
      serialNo: gen.serialNumber || '',
      manufacturer: gen.manufacturer || '',
      model: gen.model || '',
      genType: gen.generatorTypeLookupId || '',
      ratedCapacityKw: gen.nominalPower || '',
      actualPowerKw: gen.actualPowerKW || '',
      fuelConsumptionRatePerHour: gen.fuelConsumptionRatePerHour ?? '',
      owner: gen.ownerName || '',
      voltage: gen.voltageLookupId || '',
      frequency: gen.frequencyLookupId || '',
      powerFactor: gen.powerFactor || '',
      ownershipType: gen.ownershipTypeLookupId || '',
      status: gen.statusLookupId || '',
      installationDate: gen.installationDate ? gen.installationDate.substring(0, 10) : '',
      notes: gen.notes || '',
      branchId: branchId ? String(branchId) : '',
      station: stationId ? String(stationId) : '',
      subStation: subStationId ? String(subStationId) : '',
      controller: gen.controllerName ? controllers.find(c => c.name === gen.controllerName)?.id || '' : '',
      isActive: gen.isActive !== false,
    });
    setEditId(getId(gen)); setShowModal(true); setError(null);
  };
  const closeModal = () => { setShowModal(false); setError(null); };
  const fld = (k, v) => setForm(f => ({ ...f, [k]: v }));

  const fieldError = (key) => {
    if (key === 'name' && !form.name) return ar ? 'اسم المولد مطلوب' : 'Generator name is required';
    if (key === 'subStation' && !form.subStation) return ar ? 'المنطقة (وحدة فرعية) مطلوبة' : 'SubStation is required';
    if (key === 'ratedCapacityKw' && (!form.ratedCapacityKw || Number(form.ratedCapacityKw) <= 0)) return ar ? 'القدرة الاسمية مطلوبة ويجب أن تكون أكبر من صفر' : 'Nominal power is required and must be greater than zero';
    if (key === 'fuelConsumptionRatePerHour' && (!form.fuelConsumptionRatePerHour || Number(form.fuelConsumptionRatePerHour) <= 0)) return ar ? 'معدل استهلاك الوقود مطلوب ويجب أن يكون أكبر من صفر' : 'Fuel consumption rate is required and must be greater than zero';
    return null;
  };

  const handleSubmit = async () => {
    const err = fieldError('name') || fieldError('subStation') || fieldError('ratedCapacityKw') || fieldError('fuelConsumptionRatePerHour');
    if (err) { setError(err); return; }
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
          GeneratorTypeLookupId: form.genType ? Number(form.genType) : null,
          VoltageLookupId: form.voltage ? Number(form.voltage) : null,
          FrequencyLookupId: form.frequency ? Number(form.frequency) : null,
          OwnershipTypeLookupId: form.ownershipType ? Number(form.ownershipType) : null,
          StatusLookupId: form.status ? Number(form.status) : null,
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
          GeneratorTypeLookupId: form.genType ? Number(form.genType) : null,
          VoltageLookupId: form.voltage ? Number(form.voltage) : null,
          FrequencyLookupId: form.frequency ? Number(form.frequency) : null,
          OwnershipTypeLookupId: form.ownershipType ? Number(form.ownershipType) : null,
          StatusLookupId: form.status ? Number(form.status) : null,
          IsActive: form.isActive,
        };
        const res = await client.post('/api/generators', payload);
        generatorId = res.data?.id || res.data?.data?.id;
      }

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
    const siteId = gen.subStationId || getId(gen.subStation);
    const site = siteId ? subStations.find(s => String(s.id) === String(siteId)) : null;
    const stationId = site?.stationId || site?.station_id || gen.stationId || getId(gen.station);
    const station = stationId ? stations.find(s => String(s.id) === String(stationId)) : null;
    return displayName(station?.name) || displayName(site?.stationName) || displayName(gen.stationName);
  };

  const branchNameFor = (gen) => {
    const siteId = gen.subStationId || getId(gen.subStation);
    const site = siteId ? subStations.find(s => String(s.id) === String(siteId)) : null;
    const stationId = site?.stationId || site?.station_id || gen.stationId || getId(gen.station);
    const station = stationId ? stations.find(s => String(s.id) === String(stationId)) : null;
    const branchId = station?.branchId || station?.branch_id;
    const branch = branchId ? branches.find(b => String(b.id) === String(branchId)) : null;
    return displayName(branch?.name);
  };

  const subStationFor = (gen) => {
    const siteId = gen.subStationId || getId(gen.subStation);
    if (!siteId) return null;
    return subStationById.get(String(siteId)) || subStations.find(s => String(s.id) === String(siteId)) || null;
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

  const handleSearchChange = (e) => { setSearch(e.target.value); setPage(1); };

  const generatorTree = useMemo(() => {
    const tree = {};
    paginatedFiltered.forEach(gen => {
      const site = subStationFor(gen);
      const siteId = gen.subStationId || getId(site) || getId(gen.subStation) || 'unassigned-site';
      const siteName = subStationNameFor(gen);

      const stationId = (site?.stationId || site?.station_id || gen.stationId || getId(gen.station)) || 'unknown-station';
      const station = stations.find(s => String(s.id) === String(stationId)) ||
                      { id: stationId, name: stationNameFor(gen) };
      const resolvedStationId = getId(station) || stationId || 'unknown-station';
      const stationName = displayName(station?.name) || stationNameFor(gen) || (ar ? 'محطة غير محددة' : 'Unnamed Station');

      const siteNameFinal = (siteName && siteName !== stationName) ? siteName : (siteName || (ar ? 'موقع غير محدد' : 'Unassigned Site'));

      if (!tree[resolvedStationId]) {
        tree[resolvedStationId] = { stationName, sites: {} };
      }
      const stationObj = tree[resolvedStationId];
      const siteKey = `${resolvedStationId}-${siteId}`;
      if (!stationObj.sites[siteKey]) {
        stationObj.sites[siteKey] = { siteId, siteName: siteNameFinal, gens: [] };
       }
       stationObj.sites[siteKey].gens.push(gen);
     });

     const sorted = Object.entries(tree)
       .map(([stationId, data]) => ({
         stationId,
         stationName: data.stationName,
         sites: Object.entries(data.sites).map(([siteKey, siteData]) => ({
           siteKey, siteId: siteData.siteId, siteName: siteData.siteName, gens: siteData.gens
         }))
       }))
       .sort((a, b) => (a.stationName || '').localeCompare(b.stationName || ''));

     return sorted;
   }, [paginatedFiltered, stations, subStations, ar]);

   const toggleStation = (stationId) => {
     setExpandedStations(prev => {
       const next = new Set(prev);
       if (next.has(stationId)) next.delete(stationId);
       else next.add(stationId);
       return next;
     });
   };

   const toggleSite = (siteKey) => {
     setExpandedSites(prev => {
       const next = new Set(prev);
       if (next.has(siteKey)) next.delete(siteKey);
       else next.add(siteKey);
       return next;
     });
   };

  return (
    <div>
      <PageHeader
        title={t.generators}
        subtitle={t.manageGenerators}
        actions={canAdd ? (
          <Button icon={Plus} onClick={openAdd}>{t.addGenerator}</Button>
        ) : null}
      />

      {/* Search */}
      <div style={{ marginBottom: 20 }}>
        <div className="search-bar" style={{ maxWidth: 340 }}>
          <Search size={16} />
          <input className="form-control"
            placeholder={ar ? 'بحث بالاسم أو الشركة أو الموقع...' : 'Search by name, manufacturer or location...'}
            value={search} onChange={handleSearchChange} />
        </div>
      </div>

      {error && !showModal && <div className="alert-inline alert-inline-danger">{error}</div>}

      {loading ? (
        <div className="spinner-wrap"><div className="ft-spinner" /></div>
      ) : filtered.length === 0 ? (
        <div className="empty-state"><Zap size={48} /><h3>{t.noData}</h3></div>
      ) : (<>
        <div className="grouped-container">
          {generatorTree.map(({ stationId, stationName, sites }) => {
            const stationExpanded = expandedStations.has(stationId);
            const stationGenCount = sites.reduce((sum, s) => sum + s.gens.length, 0);
            return (
              <div key={stationId} className="station-group" style={{ marginBottom: 28 }}>
                {/* Station Header */}
                <div className="station-group-header" style={{ marginBottom: 4, cursor: 'pointer' }}
                  onClick={() => toggleStation(stationId)}>
                  <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', width: 32, height: 32, borderRadius: 8, background: 'var(--primary-light)', color: 'var(--primary)', flexShrink: 0 }}>
                    {stationExpanded ? <ChevronDown size={18} /> : <ChevronRight size={18} />}
                  </div>
                  <div style={{ flex: 1, minWidth: 0 }}>
                    <h3 style={{ margin: 0, fontSize: 16, fontWeight: 'bold', color: 'var(--text-main)' }}>{stationName || (ar ? 'محطة غير محددة' : 'Unnamed Station')}</h3>
                    <div style={{ fontSize: 11, color: 'var(--text-muted)', marginTop: 2 }}>{stationGenCount} {ar ? 'مولد' : 'generators'}</div>
                  </div>
                </div>

                {/* Sites */}
                {stationExpanded && sites.map(({ siteKey, siteName, gens }) => {
                  const siteExpanded = expandedSites.has(siteKey);
                  return (
                    <div key={siteKey} style={{ paddingLeft: 24, marginBottom: 8 }}>
                      <div className="site-group-header" style={{ display: 'flex', alignItems: 'center', gap: 8, padding: '8px 16px', background: 'var(--card-2)', border: '1px solid var(--border)', borderRadius: 8, cursor: 'pointer', marginBottom: 6 }}
                        onClick={() => toggleSite(siteKey)}>
                        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', width: 28, height: 28, borderRadius: 6, background: 'rgba(255,255,255,0.1)', color: 'var(--text-muted)', flexShrink: 0 }}>
                          {siteExpanded ? <ChevronDown size={14} /> : <ChevronRight size={14} />}
                        </div>
                        <MapPin size={14} style={{ color: 'var(--primary)', flexShrink: 0 }} />
                        <span style={{ fontSize: 13, fontWeight: 600, color: 'var(--text-main)', flex: 1 }}>{siteName}</span>
                        <span style={{ fontSize: 11, color: 'var(--text-muted)', background: 'rgba(255,255,255,0.1)', padding: '2px 8px', borderRadius: 10 }}>{gens.length} {ar ? 'مولد' : 'gen'}</span>
                      </div>

                      {/* Generators */}
                      {siteExpanded && (
                        <div className="grid-auto" style={{ paddingLeft: 8 }}>
                          {gens.map(gen => (
                            <div key={gen.id} className="gen-card">
                               <div className="gen-card-header">
                                 <div>
                                   <div className="gen-name">{gen.name}</div>
                                   <div className="gen-model">{gen.manufacturer} {gen.model && `— ${gen.model}`}</div>
                                 </div>
                                 <div style={{ display: 'flex', gap: 6, alignItems: 'center' }}>
                                  <StatusBadge status={gen.statusLookupName || gen.status} lang={lang} />
                                  <span className={`status-badge ${gen.isActive !== false ? 'status-active' : 'status-inactive'}`}>
                                    {gen.isActive !== false ? (ar ? 'نشط' : 'Active') : (ar ? 'غير نشط' : 'Inactive')}
                                  </span>
                                </div>
                               </div>
                               <div className="gen-stats">
                                 <div className="gen-stat-item">
                                   <div className="gen-stat-label">{ar ? 'القدرة الفعلية' : 'Actual Power'}</div>
                                   <div className="gen-stat-value">{(gen.actualPowerKW || gen.actualPowerKw) ? `${gen.actualPowerKW || gen.actualPowerKw} KW` : '—'}</div>
                                 </div>
                                 <div className="gen-stat-item">
                                   <div className="gen-stat-label">{ar ? 'نوع المولد' : 'Type'}</div>
                                   <div className="gen-stat-value">{gen.generatorTypeLookupName || gen.genType || '—'}</div>
                                 </div>
                                 <div className="gen-stat-item">
                                   <div className="gen-stat-label">{ar ? 'الشاشة المرتبطة' : 'Associated Screen'}</div>
                                   <div className="gen-stat-value">
                                     {gen.controllerName || gen.controllerCode
                                       ? `${gen.controllerName || gen.controllerCode}${gen.controllerCode && gen.controllerName ? ` (${gen.controllerCode})` : ''}`
                                       : (ar ? 'غير مرتبطة' : 'Not linked')}
                                   </div>
                                 </div>
                               </div>
                              <div className="gen-card-actions">
                                {canEdit && (
                                  <Button variant="warning" size="sm" icon={Edit2} onClick={() => openEdit(gen)}>
                                    {t.edit}
                                  </Button>
                                )}
                                {canDelete && (
                                   <Button variant="danger" size="sm" icon={Trash2} onClick={() => handleDelete(getId(gen))}>
                                     {t.delete}
                                   </Button>
                                )}
                              </div>
                            </div>
                          ))}
                        </div>
                      )}
                    </div>
                  );
                })}
              </div>
            );
          })}
        </div>
        {filteredTotal > PAGE_SIZE && (
          <Pagination page={safePage} totalPages={totalPages} total={filteredTotal} pageSize={PAGE_SIZE} lang={lang}
            onPageChange={setPage} />
        )}
      </>)}
      {showModal && (
        <Modal
          open={showModal}
          onClose={closeModal}
          title={editId ? (ar ? 'تعديل بيانات المولد' : 'Edit Generator') : (ar ? 'إضافة مولد جديد' : 'Add New Generator')}
          maxWidth={680}
          footer={
            <>
              <Button variant="ghost" onClick={closeModal}>{ar ? 'إلغاء' : 'Cancel'}</Button>
              <Button variant="primary" onClick={handleSubmit} disabled={saving} icon={Save}>
                {saving
                  ? ar ? 'جاري الحفظ...' : 'Saving...'
                  : editId ? (ar ? 'تحديث المولد' : 'Update Generator') : (ar ? 'إضافة مولد' : 'Add Generator')}
              </Button>
            </>
          }
        >
          {error && <div className="alert-inline alert-inline-danger">{error}</div>}

          <div className="form-section-header">{ar ? 'بيانات المولد الأساسية' : 'Generator Basic Data'}</div>
          <FormField label={ar ? 'اسم المولد *' : 'Generator Name *'} required error={fieldError('name')}>
            <Input value={form.name} onChange={e => fld('name', e.target.value)} />
          </FormField>
          <div className="form-row">
            <FormField label={ar ? 'المحافظة *' : 'Governorate *'} required error={fieldError('branchId')}>
              <select className="form-select" value={form.branchId} onChange={e => {
                fld('branchId', e.target.value);
                fld('station', '');
                fld('subStation', '');
              }}>
                <option value="">— {ar ? 'اختر المحافظة' : 'Select Governorate'} —</option>
                {branches.map(b => <option key={b.id} value={b.id}>{b.name}</option>)}
              </select>
            </FormField>
            {form.branchId && (
              <FormField label={ar ? 'المحطة *' : 'Station *'} required error={fieldError('station')}>
                <select className="form-select" value={form.station} onChange={e => {
                  fld('station', e.target.value);
                  fld('subStation', '');
                }}>
                  <option value="">— {ar ? 'اختر المحطة' : 'Select Station'} —</option>
                  {filteredStations.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
                </select>
              </FormField>
            )}
            {form.station && (
              <FormField label={ar ? 'الموقع/الفرع *' : 'Site / Branch *'} required error={fieldError('subStation')}>
                <select className="form-select" value={form.subStation} onChange={e => fld('subStation', e.target.value)}>
                  <option value="">— {ar ? 'اختر الموقع' : 'Select Site'} —</option>
                  {filteredSubStations.map(ss => (
                    <option key={ss.id} value={ss.id}>{ss.name}</option>
                  ))}
                </select>
              </FormField>
             )}
           </div>
           <div className="form-row">
             <FormField label={ar ? 'الرقم التسلسلي' : 'Serial No.'}>
               <Input value={form.serialNo} onChange={e => fld('serialNo', e.target.value)} />
             </FormField>
             <FormField label={ar ? 'الشركة المصنعة' : 'Manufacturer'}>
               <Input value={form.manufacturer} onChange={e => fld('manufacturer', e.target.value)} />
             </FormField>
           </div>
           <div className="form-row">
             <FormField label={ar ? 'الموديل' : 'Model'}>
               <Input value={form.model} onChange={e => fld('model', e.target.value)} />
             </FormField>
             <FormField label={ar ? 'نوع المولد' : 'Generator Type'}>
               <select className="form-select" value={form.genType} onChange={e => fld('genType', e.target.value)}>
                 <option value="">— {ar ? 'اختر' : 'Select'} —</option>
                 {genTypes.map(item => (
                   <option key={item.id} value={item.id}>{item.name}</option>
                 ))}
               </select>
             </FormField>
            </div>
           <div className="form-row">
             <FormField label={ar ? 'القدرة الاسمية (تصنيف المولد)' : 'Nominal Capacity'}>
               <Input type="number" value={form.ratedCapacityKw} onChange={e => fld('ratedCapacityKw', e.target.value)} />
             </FormField>
           </div>

           <div className="form-section-header">{ar ? 'بيانات شاشة التحكم' : 'Controller Data'}</div>
           <div className="form-row">
             <FormField label={ar ? 'شاشة التحكم المرتبطة' : 'Linked Controller'} error={null}>
               <select className="form-select" value={form.controller} onChange={e => {
                 const ctrlId = e.target.value;
                 fld('controller', ctrlId);
               }}>
                 <option value="">— {ar ? 'اختر الشاشة' : 'Select Screen'} —</option>
                 {controllers.filter(c => !c.generatorId || c.generatorId === editId).map(c => (
                   <option key={c.id} value={c.id}>{c.name} ({c.code}) — {c.controllerType}</option>
                 ))}
               </select>
             </FormField>
             <FormField label={ar ? 'وحدة الطاقة' : 'Energy Unit'}>
               <Input readOnly value={controllers.find(c => c.id === Number(form.controller))?.energyUnit || 'KW'} style={{ background: 'var(--card-2)', color: 'var(--secondary)', fontWeight: 600 }} />
             </FormField>
           </div>
           <div className="form-row">
             <FormField label={ar ? 'قراءة عداد الطاقة الحالية' : 'Current Energy Reading'}>
               <Input readOnly value={controllers.find(c => c.id === Number(form.controller))?.energyReading || 0} style={{ background: 'var(--card-2)' }} />
             </FormField>
             <FormField label={ar ? 'قراءة ساعات التشغيل الحالية' : 'Current Operating Hours'}>
               <Input readOnly value={controllers.find(c => c.id === Number(form.controller))?.operatingHours || 0} style={{ background: 'var(--card-2)' }} />
             </FormField>
           </div>
           <div className="form-row">
             <FormField label={ar ? 'معامل القدرة' : 'Power Factor'}>
               <Input value={form.powerFactor} onChange={e => fld('powerFactor', e.target.value)} />
             </FormField>
             <FormField label={ar ? 'القدرة الفعلية (KW)' : 'Actual Power (KW)'}>
               <Input type="number" value={form.actualPowerKw} onChange={e => fld('actualPowerKw', e.target.value)} />
             </FormField>
             <FormField label={ar ? 'معدل استهلاك الوقود (لتر/KWh)' : 'Fuel Consumption Rate (L/KWh)'}>
               <Input value={form.fuelConsumptionRatePerHour} onChange={e => fld('fuelConsumptionRatePerHour', e.target.value)} />
             </FormField>
           </div>
           <div className="form-row">
             <FormField label={ar ? 'مستوى الفولتات' : 'Voltage Level'}>
               <select className="form-select" value={form.voltage} onChange={e => fld('voltage', e.target.value)}>
                 <option value="">—</option>
                  {voltages.map(item => (
                    <option key={item.id} value={item.id}>{item.name}</option>
                  ))}
                </select>
              </FormField>
              <FormField label={ar ? 'التردد' : 'Frequency'}>
                <select className="form-select" value={form.frequency} onChange={e => fld('frequency', e.target.value)}>
                  <option value="">—</option>
                  {frequencies.map(item => (
                    <option key={item.id} value={item.id}>{item.name}</option>
                  ))}
               </select>
             </FormField>
           </div>
           <div className="form-row">
             <FormField label={ar ? 'اسم المالك' : 'Owner Name'}>
               <Input value={form.owner} onChange={e => fld('owner', e.target.value)} />
             </FormField>
             <FormField label={ar ? 'نوع الملكية' : 'Ownership Type'}>
               <select className="form-select" value={form.ownershipType} onChange={e => fld('ownershipType', e.target.value)}>
                 <option value="">—</option>
                  {ownershipTypes.map(item => (
                    <option key={item.id} value={item.id}>{item.name}</option>
                  ))}
                </select>
              </FormField>
              <FormField label={ar ? 'حالة التشغيل' : 'Operating Status'}>
                <select className="form-select" value={form.status} onChange={e => fld('status', e.target.value)}>
                  <option value="">—</option>
                  {statuses.map(item => (
                    <option key={item.id} value={item.id}>{item.name}</option>
                  ))}
               </select>
             </FormField>
           </div>

           <div className="form-section-header">{ar ? 'بيانات الصيانة' : 'Maintenance Data'}</div>
           <div className="form-row">
             <FormField label={ar ? 'تاريخ التركيب' : 'Installation Date'}>
               <Input type="date" value={form.installationDate} onChange={e => fld('installationDate', e.target.value)} />
             </FormField>
             <FormField label={ar ? 'آخر صيانة' : 'Last Maintenance'}>
               <Input type="date" value={form.lastMaintenance} onChange={e => fld('lastMaintenance', e.target.value)} />
             </FormField>
           </div>
           <FormField label={ar ? 'ملاحظات' : 'Notes'}>
             <textarea className="form-control" rows={2} value={form.notes}
               onChange={e => fld('notes', e.target.value)}
               style={{ resize: 'vertical', fontFamily: 'inherit' }} />
           </FormField>
           <div className="form-group" style={{ marginTop: 8 }}>
             <label className="checkbox-label">
               <input type="checkbox" checked={form.isActive} onChange={e => fld('isActive', e.target.checked)} />
               <span>{ar ? 'نشط' : 'Active'}</span>
             </label>
           </div>
         </Modal>
       )}

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
