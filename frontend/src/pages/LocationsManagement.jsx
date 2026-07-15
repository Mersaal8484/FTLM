import React, { useState, useEffect, useCallback } from 'react';
import { Plus, MapPin, Building2, Layers, Trash2, Pencil, ChevronDown, ChevronUp, Save, X, AlertCircle, Zap } from 'lucide-react';
import client from '../api/client';

/**
 * صفحة إدارة المواقع
 * الهيكل: Branch (محافظة) → Station (محطة) → SubStation (موقع/فرع)
 * الـ backend routes:
 *   /api/branches      → Branch model
 *   /api/stations      → Station model (branch_id)
 *   /api/sub-stations  → SubStation model (station_id)
 */

function LocationsManagement({ lang }) {
  const ar = lang === 'ar';

  // ─── Data ───────────────────────────────────────────────────────────────────
  const [branches, setBranches]     = useState([]);
  const [stations, setStations]     = useState([]);
  const [subStations, setSubStations] = useState([]);
  const [generators, setGenerators] = useState([]);
  const [loading, setLoading]       = useState(true);
  const [error, setError]           = useState('');

  // ─── UI State ────────────────────────────────────────────────────────────────
  const [activeTab, setActiveTab]   = useState('branches'); // branches | stations | subStations
  const [showForm, setShowForm]     = useState(false);
  const [editItem, setEditItem]     = useState(null);
  const [editType, setEditType]     = useState(null); // 'branch' | 'station' | 'subStation'
  const [saving, setSaving]         = useState(false);
  const [deleteConfirm, setDeleteConfirm] = useState(null);
  const [expandedBranch, setExpandedBranch] = useState(null);
  const [expandedStation, setExpandedStation] = useState(null);
  const [expandedSubStation, setExpandedSubStation] = useState(null);

  // ─── Forms ───────────────────────────────────────────────────────────────────
  const [branchForm, setBranchForm] = useState({ code: '', name: '', is_generation: false, isActive: true });
  const [stationForm, setStationForm] = useState({ code: '', name: '', location: '', capacity_kw: '', branch_id: '', isActive: true });
  const [subForm, setSubForm] = useState({ code: '', name: '', location: '', station_id: '', capacity_kw: '', isActive: true });

  // ─── Fetch ───────────────────────────────────────────────────────────────────
  const fetchAll = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const [b, s, ss, g] = await Promise.all([
        client.get('/api/branches'),
        client.get('/api/stations'),
        client.get('/api/sub-stations', { params: { includeInactive: true } }),
        client.get('/api/generators', { params: { pageSize: 1000 } }),
      ]);
      setBranches(b.data);
      setStations(s.data);
      setSubStations(ss.data);
      setGenerators(g.data?.data || g.data || []);
    } catch (e) {
      setError(ar ? 'فشل تحميل البيانات. تأكد من تشغيل الخادم.' : 'Failed to load data. Check server.');
    } finally {
      setLoading(false);
    }
  }, [ar]);

  useEffect(() => { fetchAll(); }, [fetchAll]);

  // ─── Helpers ─────────────────────────────────────────────────────────────────
  const resetForms = () => {
    setBranchForm({ code: '', name: '', is_generation: false, isActive: true });
    setStationForm({ code: '', name: '', location: '', capacity_kw: '', branch_id: '', isActive: true });
    setSubForm({ code: '', name: '', status: 'ACTIVE', location: '', station_id: '', capacity_kw: '', has_internal_count: false });
    setEditItem(null);
    setEditType(null);
    setShowForm(false);
  };

  // Detect entity type from item fields
  const detectType = (item) => {
    if (item.station_id !== undefined || item.stationId !== undefined) return 'subStation';
    if (item.branchId !== undefined || item.branch_id !== undefined) return 'station';
    if (item.isGeneration !== undefined || item.is_generation !== undefined) return 'branch';
    return activeTab === 'stations' ? 'station' : activeTab === 'subStations' ? 'subStation' : 'branch';
  };

  const openAdd = (type) => { resetForms(); setEditType(type || activeTab); setShowForm(true); };
  const openEdit = (item) => {
    const type = detectType(item);
    setEditItem(item);
    setEditType(type);
    if (type === 'branch') {
      setBranchForm({ code: item.code, name: item.name, is_generation: item.isGeneration ?? item.is_generation ?? false, isActive: item.isActive });
    } else if (type === 'station') {
      setStationForm({ code: item.code, name: item.name, location: item.location || '', capacity_kw: item.capacityKW ?? '', branch_id: item.branchId ?? '', isActive: item.isActive });
    } else {
      setSubForm({ code: item.code, name: item.name, location: item.location || '', station_id: item.stationId ?? '', capacity_kw: item.capacityKW ?? '', isActive: item.isActive });
    }
    setShowForm(true);
  };

  const normalizeCode = (code) => String(code || '').trim().replace(/\s+/g, '').toUpperCase();

  // ─── CRUD ────────────────────────────────────────────────────────────────────
  const handleSave = async () => {
    setSaving(true);
    try {
      if (editType === 'branch') {
        const payload = { isGeneration: branchForm.is_generation, code: normalizeCode(branchForm.code), name: branchForm.name };
        if (editItem) await client.put(`/api/branches/${editItem.id}`, payload);
        else await client.post('/api/branches', payload);
      } else if (editType === 'station') {
        const payload = {
          branchId: Number(stationForm.branch_id),
          name: stationForm.name,
          code: normalizeCode(stationForm.code),
          location: stationForm.location || null,
          capacityKW: stationForm.capacity_kw ? Number(stationForm.capacity_kw) : null,
          isActive: stationForm.isActive,
        };
        if (editItem) await client.put(`/api/stations/${editItem.id}`, payload);
        else await client.post('/api/stations', payload);
      } else {
        const payload = {
          stationId: Number(subForm.station_id),
          name: subForm.name,
          code: normalizeCode(subForm.code),
          location: subForm.location || null,
          capacityKW: subForm.capacity_kw ? Number(subForm.capacity_kw) : null,
          isActive: subForm.isActive,
        };
        if (editItem) await client.put(`/api/sub-stations/${editItem.id}`, payload);
        else await client.post('/api/sub-stations', payload);
      }
      await fetchAll();
      resetForms();
    } catch (e) {
      console.error(e);
      setError(e.response?.data?.msg || (ar ? 'فشل الحفظ. تحقق من البيانات.' : 'Save failed. Check your data.'));
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async () => {
    if (!deleteConfirm) return;
    try {
      const { type, id } = deleteConfirm;
      if (type === 'branch') await client.delete(`/api/branches/${id}`);
      else if (type === 'station') await client.delete(`/api/stations/${id}`);
      else await client.delete(`/api/sub-stations/${id}`);
      await fetchAll();
    } catch (e) { setError(e.response?.data?.msg || (ar ? 'فشل الحذف' : 'Delete failed')); }
    finally { setDeleteConfirm(null); }
  };

  // ─── Styles (matches FuelTrack design system) ────────────────────────────────
  const secTitle = { background: 'hsla(217,91%,60%,0.15)', padding: '10px 16px', fontSize: 13, fontWeight: 700, color: 'var(--primary)', borderBottom: '1px solid var(--border)', display: 'flex', alignItems: 'center', gap: 8 };
  const inp = (val, onChange, placeholder = '') => (
    <input value={val} onChange={e => onChange(e.target.value)} className="form-control" placeholder={placeholder}
      style={{ padding: '8px 12px', fontSize: 13 }} />
  );
  const sel = (val, onChange, options) => (
    <select value={val} onChange={e => onChange(e.target.value)} className="form-select"
      style={{ padding: '8px 12px', fontSize: 13 }}>
      <option value="">— {ar ? 'اختر' : 'Select'} —</option>
      {options.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
    </select>
  );

  const tabs = [
    { key: 'branches',    icon: Building2, label: ar ? 'المحافظات' : 'Governorates', count: branches.length },
    { key: 'stations',    icon: MapPin,    label: ar ? 'المحطات' : 'Stations', count: stations.length },
    { key: 'subStations', icon: Layers,    label: ar ? 'المواقع/الفروع' : 'Sites/Branches', count: subStations.length },
  ];

  return (
    <div>
      {/* Page Header */}
      <div className="page-header">
        <div className="page-header-row">
          <div>
            <h1>{ar ? 'إدارة المواقع' : 'Locations Management'}</h1>
            <p>{ar ? 'إدارة المحافظات والمحطات والمواقع/الفروع وربطها بالمولدات والخزانات' : 'Manage governorates, stations, and sites/branches — link them to generators and tanks'}</p>
          </div>
          <button className="btn btn-primary btn-sm" onClick={() => openAdd(activeTab === 'branches' ? 'branch' : activeTab === 'stations' ? 'station' : 'subStation')}>
            <Plus size={15} /> {ar ? 'إضافة' : 'Add'}
          </button>
        </div>
      </div>

      {/* Error */}
      {error && (
        <div className="glass-card" style={{ marginBottom: 16, borderColor: 'var(--danger)', padding: '12px 16px', display: 'flex', alignItems: 'center', gap: 10, color: 'var(--danger)' }}>
          <AlertCircle size={16} />
          <span style={{ fontSize: 13 }}>{error}</span>
          <button onClick={() => setError('')} style={{ marginRight: 'auto', background: 'none', border: 'none', color: 'var(--text-faint)', cursor: 'pointer' }}><X size={14} /></button>
        </div>
      )}

      {/* Tabs */}
      <div style={{ display: 'flex', gap: 6, marginBottom: 20 }}>
        {tabs.map(({ key, icon: Icon, label, count }) => (
          <button key={key} onClick={() => { setActiveTab(key); resetForms(); }}
            className={activeTab === key ? 'btn btn-primary btn-sm' : 'btn btn-ghost btn-sm'}
            style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
            <Icon size={14} />
            {label}
            <span style={{ background: 'rgba(255,255,255,0.15)', borderRadius: 10, padding: '1px 8px', fontSize: 11 }}>{count}</span>
          </button>
        ))}
      </div>

      {/* Tree View (Branches tab shows hierarchy) */}
      {activeTab === 'branches' && (
        <div className="glass-card" style={{ marginBottom: 16 }}>
          <div style={secTitle}><Building2 size={15} /> {ar ? 'شجرة المواقع' : 'Location Tree'}</div>
          {loading ? <div style={{ padding: 24, textAlign: 'center', color: 'var(--text-faint)' }}>...</div> : (
            branches.length === 0 ? (
              <div style={{ padding: 32, textAlign: 'center', color: 'var(--text-faint)', fontSize: 13 }}>
                {ar ? 'لا توجد محافظات. ابدأ بإضافة محافظة.' : 'No governorates. Start by adding one.'}
              </div>
            ) : (
              branches.map(branch => {
                const branchStations = stations.filter(s => s.branchId === branch.id);
                const isExpanded = expandedBranch === branch.id;
                return (
                  <div key={branch.id} style={{ borderBottom: '1px solid var(--border)' }}>
                    {/* Branch Row */}
                    <div style={{ display: 'flex', alignItems: 'center', padding: '10px 16px', gap: 10 }}>
                      <button onClick={() => setExpandedBranch(isExpanded ? null : branch.id)}
                        style={{ background: 'none', border: 'none', color: 'var(--text-faint)', cursor: 'pointer', padding: 2 }}>
                        {isExpanded ? <ChevronUp size={14} /> : <ChevronDown size={14} />}
                      </button>
                      <Building2 size={14} style={{ color: 'var(--primary)', flexShrink: 0 }} />
                      <span style={{ flex: 1, fontWeight: 600, fontSize: 14, color: 'var(--text)' }}>{branch.name}</span>
                      <span style={{ fontSize: 11, color: 'var(--text-faint)' }}>{branch.code}</span>
                      <span style={{ fontSize: 11, background: 'rgba(99,179,237,0.12)', color: 'var(--primary)', padding: '2px 8px', borderRadius: 8 }}>
                        {branchStations.length} {ar ? 'محطة' : 'stations'}
                      </span>
                      <button className="btn btn-ghost btn-sm" style={{ padding: '4px 8px' }} onClick={() => openEdit(branch)}>
                        <Pencil size={12} />
                      </button>
                      <button className="btn btn-ghost btn-sm" style={{ padding: '4px 8px', color: 'var(--danger)' }}
                        onClick={() => setDeleteConfirm({ type: 'branch', id: branch.id, name: branch.name })}>
                        <Trash2 size={12} />
                      </button>
                    </div>

                    {/* Stations under branch */}
                    {isExpanded && branchStations.map(st => {
                      const stSubs = subStations.filter(ss => ss.isActive !== false && (ss.station_id ?? ss.stationId) === st.id);
                      const isStExpanded = expandedStation === st.id;
                      return (
                        <div key={st.id}>
                          <div style={{ display: 'flex', alignItems: 'center', padding: '8px 16px 8px 40px', gap: 10, background: 'rgba(99,179,237,0.04)', borderTop: '1px solid var(--border)' }}>
                            <button onClick={() => setExpandedStation(isStExpanded ? null : st.id)}
                              style={{ background: 'none', border: 'none', color: 'var(--text-faint)', cursor: 'pointer', padding: 2 }}>
                              {isStExpanded ? <ChevronUp size={13} /> : <ChevronDown size={13} />}
                            </button>
                            <MapPin size={13} style={{ color: '#10b981', flexShrink: 0 }} />
                            <span style={{ flex: 1, fontSize: 13, color: 'var(--text)' }}>{st.name}</span>
                            <span style={{ fontSize: 11, color: 'var(--text-faint)' }}>{st.code}</span>
                            <span style={{ fontSize: 11, background: 'rgba(16,185,129,0.12)', color: '#10b981', padding: '2px 8px', borderRadius: 8 }}>
                              {stSubs.length} {ar ? 'موقع/فرع' : 'sites/branches'}
                            </span>
                          </div>
                          {isStExpanded && stSubs.map(ss => (
                            <div key={ss.id} style={{ display: 'flex', alignItems: 'center', padding: '7px 16px 7px 64px', gap: 10, borderTop: '1px solid var(--border)', background: 'rgba(139,92,246,0.03)' }}>
                              <Layers size={12} style={{ color: '#8b5cf6', flexShrink: 0 }} />
                              <span style={{ flex: 1, fontSize: 12, color: 'var(--text-muted)' }}>{ss.name}</span>
                              <span style={{ fontSize: 11, color: 'var(--text-faint)' }}>{ss.code}</span>
                              <span style={{ fontSize: 10, background: ss.isActive ? 'rgba(16,185,129,0.12)' : 'rgba(239,68,68,0.12)', color: ss.isActive ? '#10b981' : '#ef4444', padding: '2px 6px', borderRadius: 8 }}>
                                {ss.isActive ? (ar ? 'نشط' : 'Active') : (ar ? 'غير نشط' : 'Inactive')}
                              </span>
                            </div>
                          ))}
                        </div>
                      );
                    })}
                  </div>
                );
              })
            )
          )}
        </div>
      )}

      {/* Stations Tree */}
      {activeTab === 'stations' && (
        <div className="glass-card" style={{ marginBottom: 16 }}>
          <div style={secTitle}><MapPin size={15} /> {ar ? 'شجرة المحطات' : 'Station Tree'}</div>
          {loading ? <div style={{ padding: 24, textAlign: 'center', color: 'var(--text-faint)' }}>...</div> : (
            stations.length === 0 ? (
              <div style={{ padding: 32, textAlign: 'center', color: 'var(--text-faint)', fontSize: 13 }}>
                {ar ? 'لا توجد محطات. ابدأ بإضافة محطة.' : 'No stations. Start by adding one.'}
              </div>
            ) : (
              stations.map(station => {
                const stationSubs = subStations.filter(ss => ss.isActive !== false && (ss.station_id ?? ss.stationId) === station.id);
                const isExpanded = expandedStation === station.id;
                return (
                  <div key={station.id} style={{ borderBottom: '1px solid var(--border)' }}>
                    {/* Station Row */}
                    <div style={{ display: 'flex', alignItems: 'center', padding: '10px 16px', gap: 10 }}>
                      <button onClick={() => setExpandedStation(isExpanded ? null : station.id)}
                        style={{ background: 'none', border: 'none', color: 'var(--text-faint)', cursor: 'pointer', padding: 2 }}>
                        {isExpanded ? <ChevronUp size={14} /> : <ChevronDown size={14} />}
                      </button>
                      <MapPin size={14} style={{ color: '#10b981', flexShrink: 0 }} />
                      <span style={{ flex: 1, fontWeight: 600, fontSize: 14, color: 'var(--text)' }}>{station.name}</span>
                      <span style={{ fontSize: 11, color: 'var(--text-faint)' }}>{station.code}</span>
                      <span style={{ fontSize: 11, background: 'rgba(99,179,237,0.12)', color: 'var(--primary)', padding: '2px 8px', borderRadius: 8 }}>
                        {station.branchName || branches.find(b => b.id === station.branchId)?.name || '—'}
                      </span>
                      <span style={{ fontSize: 11, background: 'rgba(16,185,129,0.12)', color: '#10b981', padding: '2px 8px', borderRadius: 8 }}>
                        {stationSubs.length} {ar ? 'موقع/فرع' : 'sites/branches'}
                      </span>
                      <button className="btn btn-ghost btn-sm" style={{ padding: '4px 8px' }} onClick={() => openEdit(station)}>
                        <Pencil size={12} />
                      </button>
                      <button className="btn btn-ghost btn-sm" style={{ padding: '4px 8px', color: 'var(--danger)' }}
                        onClick={() => setDeleteConfirm({ type: 'station', id: station.id, name: station.name })}>
                        <Trash2 size={12} />
                      </button>
                    </div>

                    {/* SubStations under station */}
                    {isExpanded && stationSubs.map(ss => (
                      <div key={ss.id} style={{ display: 'flex', alignItems: 'center', padding: '8px 16px 8px 40px', gap: 10, background: 'rgba(139,92,246,0.04)', borderTop: '1px solid var(--border)' }}>
                        <Layers size={13} style={{ color: '#8b5cf6', flexShrink: 0 }} />
                        <span style={{ flex: 1, fontSize: 13, color: 'var(--text)' }}>{ss.name}</span>
                        <span style={{ fontSize: 11, color: 'var(--text-faint)' }}>{ss.code}</span>
                        {ss.capacityKW !== null && ss.capacityKW !== undefined && (
                          <span style={{ fontSize: 11, background: 'rgba(245,158,11,0.12)', color: '#f59e0b', padding: '2px 8px', borderRadius: 8 }}>{ss.capacityKW} kW</span>
                        )}
                        <span style={{ fontSize: 10, background: ss.isActive ? 'rgba(16,185,129,0.12)' : 'rgba(239,68,68,0.12)', color: ss.isActive ? '#10b981' : '#ef4444', padding: '2px 6px', borderRadius: 8 }}>
                          {ss.isActive ? (ar ? 'نشط' : 'Active') : (ar ? 'غير نشط' : 'Inactive')}
                        </span>
                      </div>
                    ))}
                  </div>
                );
              })
            )
          )}
        </div>
      )}

      {/* SubStations → Generators Tree */}
      {activeTab === 'subStations' && (
        <div className="glass-card" style={{ marginBottom: 16 }}>
          <div style={secTitle}><Layers size={15} /> {ar ? 'شجرة المواقع والمولدات' : 'Sites & Generators Tree'}</div>
          {loading ? <div style={{ padding: 24, textAlign: 'center', color: 'var(--text-faint)' }}>...</div> : (
            subStations.length === 0 ? (
              <div style={{ padding: 32, textAlign: 'center', color: 'var(--text-faint)', fontSize: 13 }}>
                {ar ? 'لا توجد مواقع لإظهار مولّداتها' : 'No sites to show generators for'}
              </div>
            ) : (
              subStations.map(ss => {
                const stationName = stations.find(s => s.id === (ss.station_id ?? ss.stationId))?.name || '—';
                const ssGens = generators.filter(g => (g.subStationId ?? g.sub_station_id) === ss.id);
                const isExpanded = expandedSubStation === ss.id;
                return (
                  <div key={ss.id} style={{ borderBottom: '1px solid var(--border)' }}>
                    {/* SubStation Row */}
                    <div style={{ display: 'flex', alignItems: 'center', padding: '10px 16px', gap: 10 }}>
                      <button onClick={() => setExpandedSubStation(isExpanded ? null : ss.id)}
                        style={{ background: 'none', border: 'none', color: 'var(--text-faint)', cursor: 'pointer', padding: 2 }}>
                        {isExpanded ? <ChevronUp size={14} /> : <ChevronDown size={14} />}
                      </button>
                      <Layers size={14} style={{ color: '#8b5cf6', flexShrink: 0 }} />
                      <span style={{ flex: 1, fontWeight: 600, fontSize: 14, color: 'var(--text)' }}>{ss.name}</span>
                      <span style={{ fontSize: 11, color: 'var(--text-faint)' }}>{ss.code}</span>
                      <span style={{ fontSize: 11, background: 'rgba(99,179,237,0.12)', color: 'var(--primary)', padding: '2px 8px', borderRadius: 8 }}>
                        {stationName}
                      </span>
                      {ss.capacityKW !== null && ss.capacityKW !== undefined && (
                        <span style={{ fontSize: 11, background: 'rgba(245,158,11,0.12)', color: '#f59e0b', padding: '2px 8px', borderRadius: 8 }}>{ss.capacityKW} kW</span>
                      )}
                      <span style={{ fontSize: 10, background: ss.isActive ? 'rgba(16,185,129,0.12)' : 'rgba(239,68,68,0.12)', color: ss.isActive ? '#10b981' : '#ef4444', padding: '2px 6px', borderRadius: 8 }}>
                        {ss.isActive ? (ar ? 'نشط' : 'Active') : (ar ? 'غير نشط' : 'Inactive')}
                      </span>
                      <button className="btn btn-ghost btn-sm" style={{ padding: '4px 8px' }} onClick={() => openEdit(ss)}>
                        <Pencil size={12} />
                      </button>
                      <button className="btn btn-ghost btn-sm" style={{ padding: '4px 8px', color: 'var(--danger)' }}
                        onClick={() => setDeleteConfirm({ type: 'subStation', id: ss.id, name: ss.name })}>
                        <Trash2 size={12} />
                      </button>
                    </div>

                    {/* Generators under substation */}
                    {isExpanded && ssGens.map(gen => (
                      <div key={gen.id} style={{ display: 'flex', alignItems: 'center', padding: '8px 16px 8px 40px', gap: 10, background: 'rgba(16,185,129,0.04)', borderTop: '1px solid var(--border)' }}>
                        <Zap size={13} style={{ color: '#10b981', flexShrink: 0 }} />
                        <span style={{ flex: 1, fontSize: 13, color: 'var(--text)' }}>{gen.name}</span>
                        <span style={{ fontSize: 11, color: 'var(--text-faint)' }}>{gen.code}</span>
                        {gen.nominalPower > 0 && (
                          <span style={{ fontSize: 11, background: 'rgba(245,158,11,0.12)', color: '#f59e0b', padding: '2px 8px', borderRadius: 8 }}>{gen.nominalPower} kW</span>
                        )}
                      </div>
                    ))}
                    {isExpanded && ssGens.length === 0 && (
                      <div style={{ padding: '8px 16px 8px 40px', color: 'var(--text-faint)', fontSize: 12, borderTop: '1px solid var(--border)' }}>
                        {ar ? 'لا توجد مولدات مرتبطة' : 'No linked generators'}
                      </div>
                    )}
                  </div>
                );
              })
            )
          )}
        </div>
      )}

      {/* ─── Add / Edit Modal ─────────────────────────────────────────────── */}
      {showForm && (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.6)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000, padding: 16 }}>
          <div className="glass-card" style={{ width: '100%', maxWidth: 520, maxHeight: '90vh', overflowY: 'auto', margin: 0 }}>
            <div style={secTitle}>
              {editItem ? <Pencil size={15} /> : <Plus size={15} />}
              {editItem
                ? (ar ? 'تعديل' : 'Edit') + ' — ' + (editType === 'branch' ? (ar?'محافظة':'Governorate') : editType === 'station' ? (ar?'محطة':'Station') : (ar?'موقع/فرع':'Site/Branch'))
                : (ar ? 'إضافة جديد' : 'Add New') + ' — ' + (editType === 'branch' ? (ar?'محافظة':'Governorate') : editType === 'station' ? (ar?'محطة':'Station') : (ar?'موقع/فرع':'Site/Branch'))
              }
              <button onClick={resetForms} style={{ marginRight: 'auto', background: 'none', border: 'none', color: 'var(--text-faint)', cursor: 'pointer', padding: 4 }}><X size={16} /></button>
            </div>

            <div className="glass-card-body" style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>

              {/* ── Branch Form ── */}
              {editType === 'branch' && (<>
                <div className="form-group" style={{ margin: 0 }}>
                  <label className="form-label">{ar ? 'كود المحافظة' : 'Branch Code'}</label>
                  {inp(branchForm.code, v => setBranchForm(f => ({ ...f, code: v })), 'GOV-01')}
                </div>
                <div className="form-group" style={{ margin: 0 }}>
                  <label className="form-label">{ar ? 'اسم المحافظة' : 'Governorate Name'}</label>
                  {inp(branchForm.name, v => setBranchForm(f => ({ ...f, name: v })), ar?'صنعاء':'Sana\'a')}
                </div>
                <div className="form-group" style={{ margin: 0 }}>
                  <label className="form-label" style={{ display: 'flex', alignItems: 'center', gap: 8, cursor: 'pointer' }}>
                    <input type="checkbox" checked={branchForm.is_generation} onChange={e => setBranchForm(f => ({ ...f, is_generation: e.target.checked }))} />
                    {ar ? 'جهة توليد' : 'Generation Entity'}
                  </label>
                </div>
              </>)}

              {/* ── Station Form ── */}
              {editType === 'station' && (<>
                <div className="form-group" style={{ margin: 0 }}>
                  <label className="form-label">{ar ? 'كود المحطة' : 'Station Code'}</label>
                  {inp(stationForm.code, v => setStationForm(f => ({ ...f, code: v })), 'STA-01')}
                </div>
                <div className="form-group" style={{ margin: 0 }}>
                  <label className="form-label">{ar ? 'اسم المحطة' : 'Station Name'}</label>
                  {inp(stationForm.name, v => setStationForm(f => ({ ...f, name: v })), ar?'محطة الوقود المركزية':'Central Fuel Station')}
                </div>
                <div className="form-group" style={{ margin: 0 }}>
                  <label className="form-label">{ar ? 'المحافظة (Branch)' : 'Governorate (Branch)'}</label>
                  {sel(stationForm.branch_id, v => setStationForm(f => ({ ...f, branch_id: v })),
                    branches.map(b => ({ value: b.id, label: b.name })))}
                  {branches.length === 0 && <p style={{ fontSize: 11, color: 'var(--warning)', marginTop: 4 }}>{ar ? '⚠️ أضف محافظة أولاً' : '⚠️ Add a governorate first'}</p>}
                </div>
                <div className="form-group" style={{ margin: 0 }}>
                  <label className="form-label">{ar ? 'الموقع / العنوان' : 'Location / Address'}</label>
                  {inp(stationForm.location, v => setStationForm(f => ({ ...f, location: v })))}
                </div>
                <div className="form-group" style={{ margin: 0 }}>
                  <label className="form-label">{ar ? 'السعة (KW)' : 'Capacity (KW)'}</label>
                  {inp(stationForm.capacity_kw, v => setStationForm(f => ({ ...f, capacity_kw: v })), '0')}
                </div>
                <div className="form-group" style={{ margin: 0, display: 'flex', alignItems: 'center', gap: 10 }}>
                  <input
                    type="checkbox"
                    id="stationIsActive"
                    checked={stationForm.isActive}
                    onChange={e => setStationForm(f => ({ ...f, isActive: e.target.checked }))}
                    style={{ width: 18, height: 18, cursor: 'pointer' }}
                  />
                  <label htmlFor="stationIsActive" className="form-label" style={{ margin: 0, cursor: 'pointer' }}>{ar ? 'نشط' : 'Active'}</label>
                </div>
              </>)}

              {/* ── SubStation Form ── */}
              {editType === 'subStation' && (<>
                <div className="form-group" style={{ margin: 0 }}>
                  <label className="form-label">{ar ? 'كود الموقع/الفرع' : 'Site/Branch Code'}</label>
                  {inp(subForm.code, v => setSubForm(f => ({ ...f, code: v })), 'SUB-01')}
                </div>
                <div className="form-group" style={{ margin: 0 }}>
                  <label className="form-label">{ar ? 'اسم الموقع/الفرع' : 'Site/Branch Name'}</label>
                  {inp(subForm.name, v => setSubForm(f => ({ ...f, name: v })))}
                </div>
                <div className="form-group" style={{ margin: 0 }}>
                  <label className="form-label">{ar ? 'المحطة التابعة لها' : 'Parent Station'}</label>
                  {sel(subForm.station_id, v => setSubForm(f => ({ ...f, station_id: v })),
                    stations.map(s => ({ value: s.id, label: s.name })))}
                  {stations.length === 0 && <p style={{ fontSize: 11, color: 'var(--warning)', marginTop: 4 }}>{ar ? '⚠️ أضف محطة أولاً' : '⚠️ Add a station first'}</p>}
                </div>
                <div className="form-group" style={{ margin: 0 }}>
                  <label className="form-label">{ar ? 'الموقع' : 'Location'}</label>
                  {inp(subForm.location, v => setSubForm(f => ({ ...f, location: v })))}
                </div>
                <div className="form-group" style={{ margin: 0 }}>
                  <label className="form-label">{ar ? 'السعة التوليدية (KW)' : 'Generation Capacity (KW)'}</label>
                  {inp(subForm.capacity_kw, v => setSubForm(f => ({ ...f, capacity_kw: v })), '0')}
                </div>
                <div className="form-group" style={{ margin: 0, display: 'flex', alignItems: 'center', gap: 10 }}>
                  <input
                    type="checkbox"
                    id="subStationIsActive"
                    checked={subForm.isActive}
                    onChange={e => setSubForm(f => ({ ...f, isActive: e.target.checked }))}
                    style={{ width: 18, height: 18, cursor: 'pointer' }}
                  />
                  <label htmlFor="subStationIsActive" className="form-label" style={{ margin: 0, cursor: 'pointer' }}>{ar ? 'نشط' : 'Active'}</label>
                </div>
              </>)}

              <div style={{ display: 'flex', gap: 10, paddingTop: 8, borderTop: '1px solid var(--border)' }}>
                <button className="btn btn-ghost btn-sm" style={{ flex: 1 }} onClick={resetForms}>{ar ? 'إلغاء' : 'Cancel'}</button>
                <button className="btn btn-primary btn-sm" style={{ flex: 2 }} onClick={handleSave} disabled={saving}>
                  <Save size={13} /> {saving ? '...' : (ar ? 'حفظ الموقع' : 'Save Location')}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* ─── Delete Confirm ───────────────────────────────────────────────── */}
      {deleteConfirm && (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.6)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1001, padding: 16 }}>
          <div className="glass-card" style={{ width: '100%', maxWidth: 380, margin: 0 }}>
            <div className="glass-card-body" style={{ textAlign: 'center' }}>
              <AlertCircle size={36} style={{ color: 'var(--danger)', marginBottom: 12 }} />
              <h3 style={{ fontSize: 15, fontWeight: 700, marginBottom: 8 }}>{ar ? 'تأكيد الحذف' : 'Confirm Delete'}</h3>
              <p style={{ fontSize: 13, color: 'var(--text-muted)', marginBottom: 20 }}>
                {ar ? `هل أنت متأكد من حذف "${deleteConfirm.name}"؟` : `Delete "${deleteConfirm.name}"?`}
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

export default LocationsManagement;
