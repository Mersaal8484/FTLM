import React, { useState, useEffect, useMemo } from 'react';
import client from '../api/client';
import { Droplet, Plus, Edit2, Trash2, Check, X } from 'lucide-react';

function SetupOils({ lang }) {
  const ar = lang === 'ar';
  const [oilLookups, setOilLookups] = useState([]);
  const [oils, setOils] = useState([]);
  const [branches, setBranches] = useState([]);
  const [stations, setStations] = useState([]);
  const [subStations, setSubStations] = useState([]);
  const [loading, setLoading] = useState(true);
  const [newOil, setNewOil] = useState({ oilLookupId: '', currentStock: '', minStock: '', branchId: '', stationId: '', subStationId: '', isActive: true });
  const [editing, setEditing] = useState(null);
  const [showAddForm, setShowAddForm] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [successMsg, setSuccessMsg] = useState('');

  const fetchOils = async () => {
    setLoading(true);
    try {
      const ts = Date.now();
      const [oilsRes, stRes, subRes, lookupsRes, brRes] = await Promise.all([
        client.get(`/api/oils?t=${ts}`),
        client.get(`/api/stations?t=${ts}`),
        client.get(`/api/sub-stations?t=${ts}`),
        client.get(`/api/lookups/by-category/OilType?t=${ts}`),
        client.get(`/api/branches?t=${ts}`).catch(() => ({ data: [] })),
      ]);
      setOils(oilsRes.data);
      setStations(Array.isArray(stRes.data) ? stRes.data : []);
      setSubStations(Array.isArray(subRes.data) ? subRes.data.filter(s => s.isActive !== false) : []);
      setOilLookups(Array.isArray(lookupsRes.data) ? lookupsRes.data : []);
      setBranches(brRes.data ?? []);
    } catch {
      setError(ar ? 'فشل تحميل البيانات' : 'Failed to load data');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { fetchOils(); }, []); // eslint-disable-line react-hooks/exhaustive-deps

  const flash = (msg) => {
    setSuccessMsg(msg);
    setTimeout(() => setSuccessMsg(''), 3000);
  };

  const formBranchId = (editing ? editing.branchId : newOil.branchId) ?? '';
  const formStationId = (editing ? editing.stationId : newOil.stationId) ?? '';
  const formSubStationId = (editing ? editing.subStationId : newOil.subStationId) ?? '';

  const filteredStations = useMemo(() =>
    !formBranchId ? stations : stations.filter(s => s.branchId === Number(formBranchId) || s.branch_id === Number(formBranchId))
  , [formBranchId, stations]);

  const filteredSubStations = useMemo(() =>
    !formStationId ? subStations : subStations.filter(s => (s.stationId ?? s.station_id) === Number(formStationId))
  , [formStationId, subStations]);

  const handleAdd = async () => {
    if (!newOil.oilLookupId || !newOil.stationId) { 
      setError(ar ? 'نوع الزيت والمحطة مطلوبان' : 'Oil type and Station are required'); 
      return; 
    }
    setSaving(true); setError('');
    try {
      await client.post('/api/oils', {
        oilLookupId: Number(newOil.oilLookupId),
        currentStock: parseFloat(newOil.currentStock) || 0,
        minStock: parseFloat(newOil.minStock) || 0,
        stationId: Number(newOil.stationId),
        subStationId: newOil.subStationId ? Number(newOil.subStationId) : null,
        isActive: newOil.isActive,
      });
      setNewOil({ oilLookupId: '', currentStock: '', minStock: '', branchId: '', stationId: '', subStationId: '', isActive: true });
      setShowAddForm(false);
      await fetchOils();
      flash(ar ? 'تم إضافة نوع الزيت بنجاح ✓' : 'Oil type added successfully ✓');
    } catch (err) {
      setError(err.response?.data?.error || (ar ? 'حدث خطأ' : 'An error occurred'));
    } finally { setSaving(false); }
  };

  const handleUpdate = async (id) => {
    setSaving(true); setError('');
    try {
      await client.put(`/api/oils/${id}`, {
        oilLookupId: editing.oilLookupId ? Number(editing.oilLookupId) : undefined,
        currentStock: parseFloat(editing.currentStock) || 0,
        minStock: parseFloat(editing.minStock) || 0,
        stationId: editing.stationId ? Number(editing.stationId) : null,
        subStationId: editing.subStationId ? Number(editing.subStationId) : null,
        isActive: editing.isActive,
      });
      setEditing(null);
      await fetchOils();
      flash(ar ? 'تم التحديث بنجاح ✓' : 'Updated successfully ✓');
    } catch (err) {
      setError(err.response?.data?.error || (ar ? 'فشل التحديث' : 'Update failed'));
    } finally { setSaving(false); }
  };

  const handleDelete = async (id) => {
    if (!window.confirm(ar ? 'هل أنت متأكد من حذف هذا الزيت؟' : 'Are you sure you want to delete this oil?')) return;
    try {
      await client.delete(`/api/oils/${id}`);
      await fetchOils();
      flash(ar ? 'تم الحذف بنجاح ✓' : 'Deleted successfully ✓');
    } catch (err) {
      setError(ar ? 'فشل الحذف' : 'Failed to delete');
    }
  };

  return (
    <div>
      <div className="page-header">
        <div className="page-header-row">
          <div>
            <h1>{ar ? 'إعداد مخزون الزيت' : 'Oil Inventory Setup'}</h1>
            <p>{ar ? 'إعداد أنواع الزيوت وربطها بالمحطات والمناطق' : 'Setup oil types and link them to stations and areas'}</p>
          </div>
          <button className="btn btn-primary" onClick={() => { setShowAddForm(s => !s); setError(''); setEditing(null); }}>
            <Plus size={16} />
            {ar ? 'إضافة نوع زيت' : 'Add Oil Type'}
          </button>
        </div>
      </div>

      {error && (
        <div className="alert alert-danger" style={{ marginBottom: 16 }}>
          <X size={14} style={{ display: 'inline', marginLeft: 6 }} />{error}
        </div>
      )}
      {successMsg && (
        <div className="alert alert-success" style={{ marginBottom: 16 }}>{successMsg}</div>
      )}

      {/* Add / Edit Form */}
      {(showAddForm || editing) && (
        <div className="glass-card" style={{ marginBottom: 20, borderColor: 'hsla(217,91%,60%,0.3)' }}>
          <div className="glass-card-header">
            <div>
              <div className="glass-card-title">{editing ? (ar ? 'تعديل بيانات الزيت' : 'Edit Oil Data') : (ar ? 'إضافة نوع زيت جديد' : 'Add New Oil Type')}</div>
              <div className="glass-card-sub">{editing ? (ar ? 'تعديل الإعدادات الحالية' : 'Edit current settings') : (ar ? 'أدخل بيانات نوع الزيت الجديد' : 'Enter new oil type details')}</div>
            </div>
            <button className="btn btn-ghost btn-sm btn-icon" onClick={() => { setShowAddForm(false); setEditing(null); setError(''); }}>
              <X size={16} />
            </button>
          </div>
          <div className="glass-card-body">
            <div className="form-row" style={{ gridTemplateColumns: '2fr 1fr 1fr auto', alignItems: 'flex-end' }}>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">{ar ? 'نوع الزيت *' : 'Oil Type *'}</label>
                <select
                  className="form-select"
                  value={editing ? String(editing.oilLookupId || '') : newOil.oilLookupId}
                  onChange={e => editing ? setEditing({ ...editing, oilLookupId: e.target.value }) : setNewOil({ ...newOil, oilLookupId: e.target.value })}
                >
                  <option value="">— {ar ? 'اختر نوع الزيت' : 'Select Oil Type'} —</option>
                  {oilLookups.map(l => (
                    <option key={l.id} value={l.id}>{l.name}</option>
                  ))}
                </select>
              </div>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">{ar ? 'الكمية الحالية (لتر)' : 'Current Stock (L)'}</label>
                <input
                  className="form-control" type="number" min="0" step="0.1"
                  placeholder="0"
                  value={editing ? editing.currentStock : newOil.currentStock}
                  onChange={e => editing ? setEditing({ ...editing, currentStock: e.target.value }) : setNewOil({ ...newOil, currentStock: e.target.value })}
                />
              </div>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">{ar ? 'الحد الأدنى (لتر)' : 'Min Stock (L)'}</label>
                <input
                  className="form-control" type="number" min="0" step="0.1"
                  placeholder="0"
                  value={editing ? editing.minStock : newOil.minStock}
                  onChange={e => editing ? setEditing({ ...editing, minStock: e.target.value }) : setNewOil({ ...newOil, minStock: e.target.value })}
                />
              </div>
              {!editing && (
                <div style={{ paddingBottom: 0 }}>
                  <button className="btn btn-primary" onClick={handleAdd} disabled={saving}>
                    {saving ? <><span className="ft-spinner" style={{ width: 14, height: 14 }} /> {ar ? 'جاري الحفظ...' : 'Saving...'}</> : <><Check size={15} /> {ar ? 'إضافة زيت' : 'Add Oil'}</>}
                  </button>
                </div>
              )}
            </div>
            
            <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginTop: 12, marginBottom: 0 }}>
              <input type="checkbox" checked={editing ? editing.isActive !== false : newOil.isActive}
                onChange={e => editing ? setEditing({ ...editing, isActive: e.target.checked }) : setNewOil({ ...newOil, isActive: e.target.checked })}
                style={{ width: 16, height: 16, accentColor: 'var(--primary)', cursor: 'pointer' }} />
              <label style={{ fontSize: 13, cursor: 'pointer' }}>{ar ? 'الزيت نشط' : 'Oil is Active'}</label>
            </div>
            <div className="form-row" style={{ gridTemplateColumns: '1fr 1fr 1fr auto', alignItems: 'flex-end', marginTop: 12 }}>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">{ar ? 'الفرع' : 'Branch'}</label>
                <select className="form-select"
                  value={editing ? String(editing.branchId || '') : newOil.branchId}
                  onChange={e => editing ? setEditing({ ...editing, branchId: e.target.value, stationId: '', subStationId: '' }) : setNewOil({ ...newOil, branchId: e.target.value, stationId: '', subStationId: '' })}>
                  <option value="">— {ar ? 'كل الفروع' : 'All Branches'} —</option>
                  {branches.map(b => <option key={b.id} value={b.id}>{b.name}</option>)}
                </select>
              </div>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">{ar ? 'المحطة الأساسية *' : 'Station *'}</label>
                <select 
                  className="form-select" 
                  value={editing ? String(editing.stationId || '') : newOil.stationId} 
                  onChange={e => editing ? setEditing({ ...editing, stationId: e.target.value, subStationId: '' }) : setNewOil({ ...newOil, stationId: e.target.value, subStationId: '' })}
                  disabled={!formBranchId && formBranchId !== ''}>
                  <option value="">— {ar ? 'اختر المحطة' : 'Select Station'} —</option>
                  {filteredStations.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
                </select>
              </div>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">{ar ? 'المنطقة (وحدة فرعية)' : 'Area (SubStation)'}</label>
                <select 
                  className="form-select" 
                  value={editing ? String(editing.subStationId || '') : newOil.subStationId} 
                  onChange={e => editing ? setEditing({ ...editing, subStationId: e.target.value }) : setNewOil({ ...newOil, subStationId: e.target.value })} 
                  disabled={!formStationId && formStationId !== ''}>
                  <option value="">— {ar ? 'اختر المنطقة (اختياري)' : 'Select Area (Optional)'} —</option>
                  {filteredSubStations.map(ss => (
                    <option key={ss.id} value={ss.id}>{ss.name}</option>
                  ))}
                </select>
              </div>
              {editing && (
                <div style={{ paddingBottom: 0 }}>
                  <button className="btn btn-primary" onClick={() => handleUpdate(editing.id)} disabled={saving}>
                    {saving ? '...' : <><Check size={15} /> {ar ? 'تحديث البيانات' : 'Update'}</>}
                  </button>
                </div>
              )}
            </div>
          </div>
        </div>
      )}

      {/* Table */}
      <div className="glass-card">
        <div className="glass-card-header">
          <div>
            <div className="glass-card-title">{ar ? 'أنواع الزيوت المسجلة' : 'Registered Oil Types'}</div>
            <div className="glass-card-sub">{ar ? `${oils.length} نوع مسجل` : `${oils.length} types registered`}</div>
          </div>
        </div>

        {loading ? (
          <div className="spinner-wrap"><div className="ft-spinner" /></div>
        ) : oils.length === 0 ? (
          <div className="empty-state">
            <Droplet size={48} />
            <h3>{ar ? 'لا يوجد بيانات' : 'No data'}</h3>
            <p>{ar ? 'أضف إعدادات الزيوت للبدء' : 'Add oil setups to get started'}</p>
          </div>
        ) : (
          <div style={{ overflowX: 'auto' }}>
            <table className="ft-table">
              <thead>
                <tr>
                  <th>#</th>
                  <th>{ar ? 'نوع الزيت' : 'Oil Type'}</th>
                  <th>{ar ? 'الموقع/المحطة' : 'Station/Area'}</th>
                  <th>{ar ? 'الكمية الحالية' : 'Current Stock'}</th>
                  <th>{ar ? 'الحد الأدنى' : 'Min Stock'}</th>
                  <th>{ar ? 'الحالة' : 'Status'}</th>
                  <th>{ar ? 'الإجراءات' : 'Actions'}</th>
                </tr>
              </thead>
              <tbody>
                {oils.map((oil, idx) => (
                  <tr key={oil.id}>
                    <td style={{ color: 'var(--text-faint)', width: 40 }}>{idx + 1}</td>
                    <td style={{ fontWeight: 600 }}>{oil.oilLookupName}</td>
                    <td>
                      <div style={{ fontSize: 13 }}>{oil.stationName || '—'}</div>
                      {oil.subStationName && (
                        <div style={{ fontSize: 11, color: 'var(--text-muted)' }}>
                          {oil.subStationName}
                        </div>
                      )}
                    </td>
                    <td><span style={{ fontWeight: 700 }}>{oil.currentStock}</span> <span style={{ fontSize: 11, color: 'var(--text-muted)' }}>L</span></td>
                    <td><span style={{ color: 'var(--text-muted)' }}>{oil.minStock}</span> <span style={{ fontSize: 11, color: 'var(--text-muted)' }}>L</span></td>
                    <td>
                      <span className={`status-badge ${oil.isActive !== false ? 'status-active' : 'status-inactive'}`}>
                        {oil.isActive !== false ? (ar ? 'نشط' : 'Active') : (ar ? 'غير نشط' : 'Inactive')}
                      </span>
                    </td>
                    <td>
                      <div style={{ display: 'flex', gap: 6 }}>
                        <button className="btn btn-warning btn-sm" onClick={() => { setEditing({ ...oil }); setShowAddForm(false); }}>
                          <Edit2 size={13} /> {ar ? 'تعديل' : 'Edit'}
                        </button>
                        <button className="btn btn-danger btn-sm" onClick={() => handleDelete(oil.id)}>
                          <Trash2 size={13} /> {ar ? 'حذف' : 'Delete'}
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}

export default SetupOils;
