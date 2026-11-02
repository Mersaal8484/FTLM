import React, { useState, useEffect, useCallback } from 'react';
import { Plus, MapPin, Building2, Layers, Trash2, Pencil, ChevronDown, ChevronUp, Save, X, AlertCircle, Zap } from 'lucide-react';
import client from '../api/client';
import ConfirmModal from '../components/ConfirmModal';
import { PageHeader, Button, Modal, FormField, Input } from '../components/ui';

function LocationsManagement({ lang }) {
  const ar = lang === 'ar';

  const [branches, setBranches]     = useState([]);
  const [stations, setStations]     = useState([]);
  const [subStations, setSubStations] = useState([]);
  const [generators, setGenerators] = useState([]);
  const [loading, setLoading]       = useState(true);
  const [error, setError]           = useState('');

  const [activeTab, setActiveTab]   = useState('branches');
  const [showForm, setShowForm]     = useState(false);
  const [editItem, setEditItem]     = useState(null);
  const [editType, setEditType]     = useState(null);
  const [saving, setSaving]         = useState(false);
  const [deleteConfirm, setDeleteConfirm] = useState(null);
  const [expandedBranch, setExpandedBranch] = useState(null);
  const [expandedStation, setExpandedStation] = useState(null);
  const [expandedSubStation, setExpandedSubStation] = useState(null);

  const [branchForm, setBranchForm] = useState({ code: '', name: '', is_generation: false, isActive: true });
  const [stationForm, setStationForm] = useState({ code: '', name: '', location: '', capacity_kw: '', branch_id: '', isActive: true });
  const [subForm, setSubForm] = useState({ code: '', name: '', location: '', station_id: '', capacity_kw: '', isActive: true });

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

  const resetForms = () => {
    setBranchForm({ code: '', name: '', is_generation: false, isActive: true });
    setStationForm({ code: '', name: '', location: '', capacity_kw: '', branch_id: '', isActive: true });
    setSubForm({ code: '', name: '', location: '', station_id: '', capacity_kw: '', isActive: true });
    setEditItem(null);
    setEditType(null);
    setShowForm(false);
  };

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

  const tabs = [
    { key: 'branches',    icon: Building2, label: ar ? 'المحافظات' : 'Governorates', count: branches.length },
    { key: 'stations',    icon: MapPin,    label: ar ? 'المحطات' : 'Stations', count: stations.length },
    { key: 'subStations', icon: Layers,    label: ar ? 'المواقع/الفروع' : 'Sites/Branches', count: subStations.length },
  ];

  const getModalTitle = () => {
    const action = editItem ? (ar ? 'تعديل' : 'Edit') : (ar ? 'إضافة جديد' : 'Add New');
    const typeLabel = editType === 'branch' ? (ar ? 'محافظة' : 'Governorate') : editType === 'station' ? (ar ? 'محطة' : 'Station') : (ar ? 'موقع/فرع' : 'Site/Branch');
    return (
      <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
        {editItem ? <Pencil size={18} style={{ color: 'var(--primary)' }} /> : <Plus size={18} style={{ color: 'var(--primary)' }} />}
        {action} — {typeLabel}
      </div>
    );
  };

  return (
    <div>
      <PageHeader
        title={ar ? 'إدارة المواقع' : 'Locations Management'}
        subtitle={ar ? 'إدارة المحافظات والمحطات والمواقع/الفروع وربطها بالمولدات والخزانات' : 'Manage governorates, stations, and sites/branches — link them to generators and tanks'}
        actions={
          <Button size="sm" icon={Plus} onClick={() => openAdd(activeTab === 'branches' ? 'branch' : activeTab === 'stations' ? 'station' : 'subStation')}>
            {ar ? 'إضافة' : 'Add'}
          </Button>
        }
      />

      {/* Error */}
      {error && (
        <div className="glass-card" style={{ marginBottom: 16, borderColor: 'var(--danger)', padding: '12px 16px', display: 'flex', alignItems: 'center', gap: 10, color: 'var(--danger)' }}>
          <AlertCircle size={16} />
          <span style={{ fontSize: 13, flex: 1 }}>{error}</span>
          <button onClick={() => setError('')} style={{ background: 'none', border: 'none', color: 'var(--text-faint)', cursor: 'pointer' }}><X size={14} /></button>
        </div>
      )}

      {/* Tabs */}
      <div style={{ display: 'flex', gap: 6, marginBottom: 20 }}>
        {tabs.map(({ key, icon: Icon, label, count }) => (
          <Button key={key} variant={activeTab === key ? 'primary' : 'ghost'} size="sm" onClick={() => { setActiveTab(key); resetForms(); }} style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
            <Icon size={14} />
            {label}
            <span style={{ background: 'rgba(255,255,255,0.15)', borderRadius: 10, padding: '1px 8px', fontSize: 11 }}>{count}</span>
          </Button>
        ))}
      </div>

      {/* Tree View (Branches tab shows hierarchy) */}
      {activeTab === 'branches' && (
        <div className="glass-card" style={{ marginBottom: 16 }}>
          <div style={{ background: 'hsla(217,91%,60%,0.15)', padding: '10px 16px', fontSize: 13, fontWeight: 700, color: 'var(--primary)', borderBottom: '1px solid var(--border)', display: 'flex', alignItems: 'center', gap: 8 }}>
            <Building2 size={15} /> {ar ? 'شجرة المواقع' : 'Location Tree'}
          </div>
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
                      <span style={{ fontSize: 11, background: 'var(--primary-subtle)', color: 'var(--primary)', padding: '2px 8px', borderRadius: 8 }}>
                        {branchStations.length} {ar ? 'محطة' : 'stations'}
                      </span>
                      <Button variant="ghost" size="sm" style={{ padding: '4px 8px' }} icon={Pencil} onClick={() => openEdit(branch)} />
                      <Button variant="ghost" size="sm" style={{ padding: '4px 8px', color: 'var(--danger)' }} icon={Trash2} onClick={() => setDeleteConfirm({ type: 'branch', id: branch.id, name: branch.name })} />
                    </div>

                    {/* Stations under branch */}
                    {isExpanded && branchStations.map(st => {
                      const stSubs = subStations.filter(ss => ss.isActive !== false && (ss.station_id ?? ss.stationId) === st.id);
                      const isStExpanded = expandedStation === st.id;
                      return (
                        <div key={st.id}>
                          <div style={{ display: 'flex', alignItems: 'center', padding: '8px 16px 8px 40px', gap: 10, background: 'var(--primary-subtle)', borderTop: '1px solid var(--border)' }}>
                            <button onClick={() => setExpandedStation(isStExpanded ? null : st.id)}
                              style={{ background: 'none', border: 'none', color: 'var(--text-faint)', cursor: 'pointer', padding: 2 }}>
                              {isStExpanded ? <ChevronUp size={13} /> : <ChevronDown size={13} />}
                            </button>
                            <MapPin size={13} style={{ color: 'var(--success)', flexShrink: 0 }} />
                            <span style={{ flex: 1, fontSize: 13, color: 'var(--text)' }}>{st.name}</span>
                            <span style={{ fontSize: 11, color: 'var(--text-faint)' }}>{st.code}</span>
                            <span style={{ fontSize: 11, background: 'var(--success-subtle)', color: 'var(--success)', padding: '2px 8px', borderRadius: 8 }}>
                              {stSubs.length} {ar ? 'موقع/فرع' : 'sites/branches'}
                            </span>
                          </div>
                          {isStExpanded && stSubs.map(ss => (
                            <div key={ss.id} style={{ display: 'flex', alignItems: 'center', padding: '7px 16px 7px 64px', gap: 10, borderTop: '1px solid var(--border)', background: 'var(--purple-subtle)' }}>
                              <Layers size={12} style={{ color: 'var(--purple)', flexShrink: 0 }} />
                              <span style={{ flex: 1, fontSize: 12, color: 'var(--text-muted)' }}>{ss.name}</span>
                              <span style={{ fontSize: 11, color: 'var(--text-faint)' }}>{ss.code}</span>
                              <span style={{ fontSize: 10, background: ss.isActive ? 'var(--success-subtle)' : 'var(--danger-subtle)', color: ss.isActive ? 'var(--success)' : 'var(--danger)', padding: '2px 6px', borderRadius: 8 }}>
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
          <div style={{ background: 'hsla(217,91%,60%,0.15)', padding: '10px 16px', fontSize: 13, fontWeight: 700, color: 'var(--primary)', borderBottom: '1px solid var(--border)', display: 'flex', alignItems: 'center', gap: 8 }}>
            <MapPin size={15} /> {ar ? 'شجرة المحطات' : 'Station Tree'}
          </div>
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
                      <MapPin size={14} style={{ color: 'var(--success)', flexShrink: 0 }} />
                      <span style={{ flex: 1, fontWeight: 600, fontSize: 14, color: 'var(--text)' }}>{station.name}</span>
                      <span style={{ fontSize: 11, color: 'var(--text-faint)' }}>{station.code}</span>
                      <span style={{ fontSize: 11, background: 'var(--primary-subtle)', color: 'var(--primary)', padding: '2px 8px', borderRadius: 8 }}>
                        {station.branchName || branches.find(b => b.id === station.branchId)?.name || '—'}
                      </span>
                      <span style={{ fontSize: 11, background: 'var(--success-subtle)', color: 'var(--success)', padding: '2px 8px', borderRadius: 8 }}>
                        {stationSubs.length} {ar ? 'موقع/فرع' : 'sites/branches'}
                      </span>
                      <Button variant="ghost" size="sm" style={{ padding: '4px 8px' }} icon={Pencil} onClick={() => openEdit(station)} />
                      <Button variant="ghost" size="sm" style={{ padding: '4px 8px', color: 'var(--danger)' }} icon={Trash2} onClick={() => setDeleteConfirm({ type: 'station', id: station.id, name: station.name })} />
                    </div>

                    {/* SubStations under station */}
                    {isExpanded && stationSubs.map(ss => (
                      <div key={ss.id} style={{ display: 'flex', alignItems: 'center', padding: '8px 16px 8px 40px', gap: 10, background: 'var(--purple-subtle)', borderTop: '1px solid var(--border)' }}>
                        <Layers size={13} style={{ color: 'var(--purple)', flexShrink: 0 }} />
                        <span style={{ flex: 1, fontSize: 13, color: 'var(--text)' }}>{ss.name}</span>
                        <span style={{ fontSize: 11, color: 'var(--text-faint)' }}>{ss.code}</span>
                        {ss.capacityKW !== null && ss.capacityKW !== undefined && (
                          <span style={{ fontSize: 11, background: 'var(--warning-subtle)', color: 'var(--warning)', padding: '2px 8px', borderRadius: 8 }}>{ss.capacityKW} kW</span>
                        )}
                        <span style={{ fontSize: 10, background: ss.isActive ? 'var(--success-subtle)' : 'var(--danger-subtle)', color: ss.isActive ? 'var(--success)' : 'var(--danger)', padding: '2px 6px', borderRadius: 8 }}>
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
          <div style={{ background: 'hsla(217,91%,60%,0.15)', padding: '10px 16px', fontSize: 13, fontWeight: 700, color: 'var(--primary)', borderBottom: '1px solid var(--border)', display: 'flex', alignItems: 'center', gap: 8 }}>
            <Layers size={15} /> {ar ? 'شجرة المواقع والمولدات' : 'Sites & Generators Tree'}
          </div>
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
                      <Layers size={14} style={{ color: 'var(--purple)', flexShrink: 0 }} />
                      <span style={{ flex: 1, fontWeight: 600, fontSize: 14, color: 'var(--text)' }}>{ss.name}</span>
                      <span style={{ fontSize: 11, color: 'var(--text-faint)' }}>{ss.code}</span>
                      <span style={{ fontSize: 11, background: 'var(--primary-subtle)', color: 'var(--primary)', padding: '2px 8px', borderRadius: 8 }}>
                        {stationName}
                      </span>
                      {ss.capacityKW !== null && ss.capacityKW !== undefined && (
                        <span style={{ fontSize: 11, background: 'var(--warning-subtle)', color: 'var(--warning)', padding: '2px 8px', borderRadius: 8 }}>{ss.capacityKW} kW</span>
                      )}
                      <span style={{ fontSize: 10, background: ss.isActive ? 'var(--success-subtle)' : 'var(--danger-subtle)', color: ss.isActive ? 'var(--success)' : 'var(--danger)', padding: '2px 6px', borderRadius: 8 }}>
                        {ss.isActive ? (ar ? 'نشط' : 'Active') : (ar ? 'غير نشط' : 'Inactive')}
                      </span>
                      <Button variant="ghost" size="sm" style={{ padding: '4px 8px' }} icon={Pencil} onClick={() => openEdit(ss)} />
                      <Button variant="ghost" size="sm" style={{ padding: '4px 8px', color: 'var(--danger)' }} icon={Trash2} onClick={() => setDeleteConfirm({ type: 'subStation', id: ss.id, name: ss.name })} />
                    </div>

                    {/* Generators under substation */}
                    {isExpanded && ssGens.map(gen => (
                      <div key={gen.id} style={{ display: 'flex', alignItems: 'center', padding: '8px 16px 8px 40px', gap: 10, background: 'var(--success-subtle)', borderTop: '1px solid var(--border)' }}>
                        <Zap size={13} style={{ color: 'var(--success)', flexShrink: 0 }} />
                        <span style={{ flex: 1, fontSize: 13, color: 'var(--text)' }}>{gen.name}</span>
                        <span style={{ fontSize: 11, color: 'var(--text-faint)' }}>{gen.code}</span>
                        {gen.nominalPower > 0 && (
                          <span style={{ fontSize: 11, background: 'var(--warning-subtle)', color: 'var(--warning)', padding: '2px 8px', borderRadius: 8 }}>{gen.nominalPower} kW</span>
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
        <Modal
          open={showForm}
          onClose={resetForms}
          title={getModalTitle()}
          maxWidth={520}
          footer={
            <>
              <Button variant="ghost" onClick={resetForms}>{ar ? 'إلغاء' : 'Cancel'}</Button>
              <Button variant="primary" onClick={handleSave} disabled={saving} icon={Save}>
                {saving ? (ar ? 'جارٍ الحفظ...' : 'Saving...') : (ar ? 'حفظ الموقع' : 'Save Location')}
              </Button>
            </>
          }
        >
          {/* ── Branch Form ── */}
          {editType === 'branch' && (<>
            <FormField label={ar ? 'كود المحافظة' : 'Branch Code'}>
              <Input value={branchForm.code} onChange={e => setBranchForm(f => ({ ...f, code: e.target.value }))} placeholder="GOV-01" />
            </FormField>
            <FormField label={ar ? 'اسم المحافظة' : 'Governorate Name'}>
              <Input value={branchForm.name} onChange={e => setBranchForm(f => ({ ...f, name: e.target.value }))} placeholder={ar ? 'صنعاء' : "Sana'a"} />
            </FormField>
            <div className="form-group" style={{ marginTop: 8 }}>
              <label className="checkbox-label">
                <input type="checkbox" checked={branchForm.is_generation} onChange={e => setBranchForm(f => ({ ...f, is_generation: e.target.checked }))} />
                <span>{ar ? 'جهة توليد' : 'Generation Entity'}</span>
              </label>
            </div>
          </>)}

          {/* ── Station Form ── */}
          {editType === 'station' && (<>
            <FormField label={ar ? 'كود المحطة' : 'Station Code'}>
              <Input value={stationForm.code} onChange={e => setStationForm(f => ({ ...f, code: e.target.value }))} placeholder="STA-01" />
            </FormField>
            <FormField label={ar ? 'اسم المحطة' : 'Station Name'}>
              <Input value={stationForm.name} onChange={e => setStationForm(f => ({ ...f, name: e.target.value }))} placeholder={ar ? 'محطة الوقود المركزية' : 'Central Fuel Station'} />
            </FormField>
            <FormField label={ar ? 'المحافظة (Branch)' : 'Governorate (Branch)'}>
              <select className="form-select" value={stationForm.branch_id}
                onChange={e => setStationForm(f => ({ ...f, branch_id: e.target.value }))}>
                <option value="">— {ar ? 'اختر' : 'Select'} —</option>
                {branches.map(b => <option key={b.id} value={b.id}>{b.name}</option>)}
              </select>
            </FormField>
            {branches.length === 0 && (
              <p style={{ fontSize: 11, color: 'var(--warning)', marginTop: 4, marginBottom: 12 }}>
                {ar ? '⚠️ أضف محافظة أولاً' : '⚠️ Add a governorate first'}
              </p>
            )}
            <FormField label={ar ? 'الموقع / العنوان' : 'Location / Address'}>
              <Input value={stationForm.location} onChange={e => setStationForm(f => ({ ...f, location: e.target.value }))} />
            </FormField>
            <FormField label={ar ? 'السعة (KW)' : 'Capacity (KW)'}>
              <Input value={stationForm.capacity_kw} onChange={e => setStationForm(f => ({ ...f, capacity_kw: e.target.value }))} placeholder="0" />
            </FormField>
            <div className="form-group" style={{ marginTop: 8 }}>
              <label className="checkbox-label">
                <input type="checkbox" checked={stationForm.isActive} onChange={e => setStationForm(f => ({ ...f, isActive: e.target.checked }))} />
                <span>{ar ? 'نشط' : 'Active'}</span>
              </label>
            </div>
          </>)}

          {/* ── SubStation Form ── */}
          {editType === 'subStation' && (<>
            <FormField label={ar ? 'كود الموقع/الفرع' : 'Site/Branch Code'}>
              <Input value={subForm.code} onChange={e => setSubForm(f => ({ ...f, code: e.target.value }))} placeholder="SUB-01" />
            </FormField>
            <FormField label={ar ? 'اسم الموقع/الفرع' : 'Site/Branch Name'}>
              <Input value={subForm.name} onChange={e => setSubForm(f => ({ ...f, name: e.target.value }))} />
            </FormField>
            <FormField label={ar ? 'المحطة التابعة لها' : 'Parent Station'}>
              <select className="form-select" value={subForm.station_id}
                onChange={e => setSubForm(f => ({ ...f, station_id: e.target.value }))}>
                <option value="">— {ar ? 'اختر' : 'Select'} —</option>
                {stations.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
              </select>
            </FormField>
            {stations.length === 0 && (
              <p style={{ fontSize: 11, color: 'var(--warning)', marginTop: 4, marginBottom: 12 }}>
                {ar ? '⚠️ أضف محطة أولاً' : '⚠️ Add a station first'}
              </p>
            )}
            <FormField label={ar ? 'الموقع' : 'Location'}>
              <Input value={subForm.location} onChange={e => setSubForm(f => ({ ...f, location: e.target.value }))} />
            </FormField>
            <FormField label={ar ? 'السعة التوليدية (KW)' : 'Generation Capacity (KW)'}>
              <Input value={subForm.capacity_kw} onChange={e => setSubForm(f => ({ ...f, capacity_kw: e.target.value }))} placeholder="0" />
            </FormField>
            <div className="form-group" style={{ marginTop: 8 }}>
              <label className="checkbox-label">
                <input type="checkbox" checked={subForm.isActive} onChange={e => setSubForm(f => ({ ...f, isActive: e.target.checked }))} />
                <span>{ar ? 'نشط' : 'Active'}</span>
              </label>
            </div>
          </>)}
        </Modal>
      )}

      <ConfirmModal
        open={!!deleteConfirm}
        title={ar ? 'تأكيد الحذف' : 'Confirm Delete'}
        message={ar ? `هل أنت متأكد من حذف "${deleteConfirm?.name}"؟` : `Delete "${deleteConfirm?.name}"?`}
        confirmText={ar ? 'حذف' : 'Delete'}
        cancelText={ar ? 'إلغاء' : 'Cancel'}
        danger
        onConfirm={handleDelete}
        onCancel={() => setDeleteConfirm(null)}
      />
    </div>
  );
}

export default LocationsManagement;
