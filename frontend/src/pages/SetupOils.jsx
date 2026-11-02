import React, { useState, useEffect, useMemo } from 'react';
import client from '../api/client';
import ConfirmModal from '../components/ConfirmModal';
import { PageHeader, Button, Modal, FormField, Input } from '../components/ui';
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
  const [deleteConfirm, setDeleteConfirm] = useState(null);

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

  const handleDelete = async () => {
    if (!deleteConfirm) return;
    try {
      await client.delete(`/api/oils/${deleteConfirm.id}`);
      await fetchOils();
      flash(ar ? 'تم الحذف بنجاح ✓' : 'Deleted successfully ✓');
    } catch (err) {
      setError(ar ? 'فشل الحذف' : 'Failed to delete');
    } finally { setDeleteConfirm(null); }
  };

  const startEdit = (oil) => { setEditing({ ...oil }); setShowAddForm(false); setError(''); };
  const closeForm = () => { setShowAddForm(false); setEditing(null); setError(''); };

  return (
    <div>
      <PageHeader
        title={ar ? 'إعداد مخزون الزيت' : 'Oil Inventory Setup'}
        subtitle={ar ? 'إعداد أنواع الزيوت وربطها بالمحطات والمناطق' : 'Setup oil types and link them to stations and areas'}
        actions={
          <Button icon={Plus} onClick={() => { setShowAddForm(s => !s); setError(''); setEditing(null); }}>
            {ar ? 'إضافة نوع زيت' : 'Add Oil Type'}
          </Button>
        }
      />

      {error && (
        <div className="alert alert-inline-danger" style={{ marginBottom: 16 }}>
          <X size={14} style={{ display: 'inline', marginInlineEnd: 6 }} />{error}
        </div>
      )}
      {successMsg && (
        <div className="alert alert-inline-success" style={{ marginBottom: 16 }}>{successMsg}</div>
      )}

      {/* Add / Edit Form */}
      {(showAddForm || editing) && (
        <Modal
          open={true}
          onClose={closeForm}
          title={editing ? (ar ? 'تعديل بيانات الزيت' : 'Edit Oil Data') : (ar ? 'إضافة نوع زيت جديد' : 'Add New Oil Type')}
          maxWidth={700}
          footer={
            <>
              <Button variant="ghost" onClick={closeForm}>{ar ? 'إلغاء' : 'Cancel'}</Button>
              {editing ? (
                <Button variant="primary" onClick={() => handleUpdate(editing.id)} disabled={saving} icon={Check}>
                  {saving ? (ar ? 'جارٍ الحفظ...' : 'Saving...') : (ar ? 'تحديث البيانات' : 'Update')}
                </Button>
              ) : (
                <Button variant="primary" onClick={handleAdd} disabled={saving} icon={Check}>
                  {saving ? (ar ? 'جارٍ الحفظ...' : 'Saving...') : (ar ? 'إضافة زيت' : 'Add Oil')}
                </Button>
              )}
            </>
          }
        >
          <div className="form-row" style={{ gridTemplateColumns: '2fr 1fr 1fr', alignItems: 'flex-end' }}>
            <FormField label={ar ? 'نوع الزيت *' : 'Oil Type *'} required>
              <select className="form-select" value={editing ? String(editing.oilLookupId || '') : newOil.oilLookupId}
                onChange={e => editing ? setEditing({ ...editing, oilLookupId: e.target.value }) : setNewOil({ ...newOil, oilLookupId: e.target.value })}>
                <option value="">— {ar ? 'اختر نوع الزيت' : 'Select Oil Type'} —</option>
                {oilLookups.map(l => (
                  <option key={l.id} value={l.id}>{l.name}</option>
                ))}
              </select>
            </FormField>
            <FormField label={ar ? 'الكمية الحالية (لتر)' : 'Current Stock (L)'}>
              <Input type="number" min="0" step="0.1" placeholder="0"
                value={editing ? editing.currentStock : newOil.currentStock}
                onChange={e => editing ? setEditing({ ...editing, currentStock: e.target.value }) : setNewOil({ ...newOil, currentStock: e.target.value })} />
            </FormField>
            <FormField label={ar ? 'الحد الأدنى (لتر)' : 'Min Stock (L)'}>
              <Input type="number" min="0" step="0.1" placeholder="0"
                value={editing ? editing.minStock : newOil.minStock}
                onChange={e => editing ? setEditing({ ...editing, minStock: e.target.value }) : setNewOil({ ...newOil, minStock: e.target.value })} />
            </FormField>
          </div>

          <div className="form-group" style={{ marginTop: 12 }}>
            <label className="checkbox-label">
              <input type="checkbox" checked={editing ? editing.isActive !== false : newOil.isActive}
                onChange={e => editing ? setEditing({ ...editing, isActive: e.target.checked }) : setNewOil({ ...newOil, isActive: e.target.checked })} />
              <span>{ar ? 'الزيت نشط' : 'Oil is Active'}</span>
            </label>
          </div>

          <div className="form-section-header">{ar ? 'الموقع' : 'Location'}</div>
          <div className="form-row" style={{ gridTemplateColumns: '1fr 1fr 1fr', alignItems: 'flex-end' }}>
            <FormField label={ar ? 'الفرع' : 'Branch'}>
              <select className="form-select" value={editing ? String(editing.branchId || '') : newOil.branchId}
                onChange={e => editing ? setEditing({ ...editing, branchId: e.target.value, stationId: '', subStationId: '' }) : setNewOil({ ...newOil, branchId: e.target.value, stationId: '', subStationId: '' })}>
                <option value="">— {ar ? 'كل الفروع' : 'All Branches'} —</option>
                {branches.map(b => <option key={b.id} value={b.id}>{b.name}</option>)}
              </select>
            </FormField>
            <FormField label={ar ? 'المحطة الأساسية *' : 'Station *'}>
              <select className="form-select" value={editing ? String(editing.stationId || '') : newOil.stationId}
                onChange={e => editing ? setEditing({ ...editing, stationId: e.target.value, subStationId: '' }) : setNewOil({ ...newOil, stationId: e.target.value, subStationId: '' })}
                disabled={!formBranchId && formBranchId !== ''}>
                <option value="">— {ar ? 'اختر المحطة' : 'Select Station'} —</option>
                {filteredStations.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
              </select>
            </FormField>
            <FormField label={ar ? 'المنطقة (وحدة فرعية)' : 'Area (SubStation)'}>
              <select className="form-select" value={editing ? String(editing.subStationId || '') : newOil.subStationId}
                onChange={e => editing ? setEditing({ ...editing, subStationId: e.target.value }) : setNewOil({ ...newOil, subStationId: e.target.value })}
                disabled={!formStationId && formStationId !== ''}>
                <option value="">— {ar ? 'اختر المنطقة (اختياري)' : 'Select Area (Optional)'} —</option>
                {filteredSubStations.map(ss => (
                  <option key={ss.id} value={ss.id}>{ss.name}</option>
                ))}
              </select>
            </FormField>
          </div>
        </Modal>
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
                        <Button variant="warning" size="sm" icon={Edit2} onClick={() => startEdit(oil)}>
                          {ar ? 'تعديل' : 'Edit'}
                        </Button>
                        <Button variant="danger" size="sm" icon={Trash2} onClick={() => setDeleteConfirm(oil.id)}>
                          {ar ? 'حذف' : 'Delete'}
                        </Button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      <ConfirmModal
        open={!!deleteConfirm}
        title={ar ? 'تأكيد الحذف' : 'Confirm Delete'}
        message={ar ? 'هل أنت متأكد من حذف هذا الزيت؟' : 'Are you sure you want to delete this oil?'}
        confirmText={ar ? 'حذف' : 'Delete'}
        cancelText={ar ? 'إلغاء' : 'Cancel'}
        danger
        onConfirm={handleDelete}
        onCancel={() => setDeleteConfirm(null)}
        lang={lang}
      />
    </div>
  );
}

export default SetupOils;
