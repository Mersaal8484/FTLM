import React, { useState, useEffect, useMemo } from 'react';
import { Plus, Edit2, Trash2, X, Search, Check, PackageCheck } from 'lucide-react';
import client from '../api/client';
import ConfirmModal from '../components/ConfirmModal';
import Pagination from '../components/Pagination';
import { PageHeader, Button, Modal, FormField, Input } from '../components/ui';

const statusCfg = {
  pending:   { cls: 'badge-warning', label: { ar: 'قيد الانتظار', en: 'Pending' } },
  approved:  { cls: 'badge-success', label: { ar: 'معتمد', en: 'Approved' } },
  partial:   { cls: 'badge-warning', label: { ar: 'جزئي', en: 'Partial' } },
  rejected:  { cls: 'badge-danger',  label: { ar: 'مرفوض', en: 'Rejected' } },
  executed:  { cls: 'badge-primary', label: { ar: 'منفذ', en: 'Executed' } },
  cancelled: { cls: 'badge-secondary', label: { ar: 'ملغى', en: 'Cancelled' } },
};

const initialForm = {
  subStationId: '',
  stationId: '',
  workPermitId: '',
  purpose: '',
  notes: '',
  items: [{ sparePartId: '', requestedQuantity: '', notes: '' }],
};

function WarehouseDispatch({ lang, currentUser }) {
  const ar = lang === 'ar';
  const [dispatches, setDispatches] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [activeTab, setActiveTab] = useState('incoming');
  const [showModal, setShowModal] = useState(false);
  const [editId, setEditId] = useState(null);
  const [form, setForm] = useState(initialForm);
  const [saving, setSaving] = useState(false);
  const [confirmDelete, setConfirmDelete] = useState({ open: false, id: null });
  const [detailItem, setDetailItem] = useState(null);
  const [subStations, setSubStations] = useState([]);
  const [parts, setParts] = useState([]);
  const [workPermits, setWorkPermits] = useState([]);
  const [approvalModal, setApprovalModal] = useState({ open: false, id: null, items: [] });
  const [page, setPage] = useState(1);
  const PAGE_SIZE = 20;

  const perms = currentUser?.permissions?.warehouse || {};
  const canAdd = currentUser?.role === 'admin' || perms.add || perms.create;
  const canApprove = currentUser?.role === 'admin' || perms.approve;
  const canExecute = currentUser?.role === 'admin' || perms.execute;
  const canDelete = currentUser?.role === 'admin' || perms.delete;

  const load = async () => {
    setLoading(true); setError(null);
    try {
      const [disRes, subRes, partRes, wpRes] = await Promise.allSettled([
        client.get('/api/warehouse/dispatch', { params: { _t: Date.now() } }),
        client.get('/api/sub-stations', { params: { _t: Date.now() } }),
        client.get('/api/spare-parts', { params: { _t: Date.now() } }),
        client.get('/api/work-permits', { params: { _t: Date.now() } }),
      ]);
      if (disRes.status === 'fulfilled') setDispatches(disRes.value.data?.items || disRes.value.data || []);
      if (subRes.status === 'fulfilled') setSubStations(subRes.value.data?.items || subRes.value.data || []);
      if (partRes.status === 'fulfilled') setParts(partRes.value.data?.items || partRes.value.data || []);
      if (wpRes.status === 'fulfilled') setWorkPermits(wpRes.value.data?.items || wpRes.value.data || []);
    } catch { setError(ar ? 'فشل تحميل البيانات' : 'Failed to load data'); }
    finally { setLoading(false); }
  };

  useEffect(() => { load(); }, []);

  const selectedSubStation = useMemo(
    () => subStations.find(s => s.id === Number(form.subStationId)),
    [subStations, form.subStationId]
  );

  useEffect(() => {
    if (selectedSubStation && !editId) {
      setForm(f => ({ ...f, stationId: String(selectedSubStation.stationId) }));
    }
  }, [selectedSubStation, editId]);

  const filtered = useMemo(() => {
    if (activeTab === 'incoming') return dispatches;
    return dispatches.filter(d => d.requestedById === currentUser?.id);
  }, [dispatches, activeTab, currentUser]);

  const paginated = useMemo(() => {
    const start = (page - 1) * PAGE_SIZE;
    return filtered.slice(start, start + PAGE_SIZE);
  }, [filtered, page]);

  const openAdd = () => {
    setForm(initialForm);
    setEditId(null);
    setShowModal(true);
    setError(null);
  };

  const openEdit = (d) => {
    setForm({
      subStationId: String(d.subStationId),
      stationId: String(d.stationId),
      workPermitId: d.workPermitId ? String(d.workPermitId) : '',
      purpose: d.purpose || '',
      notes: d.notes || '',
      items: [{ sparePartId: '', requestedQuantity: '', notes: '' }],
    });
    setEditId(d.id);
    setShowModal(true);
    setError(null);
  };

  const handleSave = async (e) => {
    e.preventDefault();
    setSaving(true);
    setError(null);
    try {
      const payload = {
        subStationId: Number(form.subStationId),
        stationId: Number(form.stationId),
        workPermitId: form.workPermitId ? Number(form.workPermitId) : null,
        purpose: form.purpose || null,
        notes: form.notes || null,
        items: form.items
          .filter(it => it.sparePartId && it.requestedQuantity)
          .map(it => ({
            sparePartId: Number(it.sparePartId),
            requestedQuantity: Number(it.requestedQuantity),
            notes: it.notes || null,
          })),
      };
      if (editId) await client.put(`/api/warehouse/dispatch/${editId}`, payload);
      else await client.post('/api/warehouse/dispatch', payload);
      setShowModal(false);
      load();
    } catch (err) { setError(err.response?.data?.error || err.response?.data?.msg || (ar ? 'فشل الحفظ' : 'Save failed')); }
    finally { setSaving(false); }
  };

  const handleDelete = async () => {
    try {
      await client.delete(`/api/warehouse/dispatch/${confirmDelete.id}`);
      setConfirmDelete({ open: false, id: null });
      load();
    } catch { setError(ar ? 'فشل الحذف' : 'Delete failed'); }
  };

  const approveAll = async (id) => {
    try {
      const items = approvalModal.items.map(it => ({
        dispatchRequestItemId: it.id,
        approvedQuantity: Number(it.quantity),
        isApproved: true,
      }));
      await client.post(`/api/warehouse/dispatch/${id}/approve`, { items, notes: null });
      setApprovalModal({ open: false, id: null, items: [] });
      load();
    } catch { setError(ar ? 'فشل الاعتماد' : 'Approve failed'); }
  };

  const partialApprove = async (id) => {
    try {
      const items = approvalModal.items
        .filter(it => it.approvedQty && Number(it.approvedQty) > 0)
        .map(it => ({
          dispatchRequestItemId: it.id,
          approvedQuantity: Number(it.approvedQty),
          isApproved: true,
        }));
      await client.post(`/api/warehouse/dispatch/${id}/approve`, { items, notes: null });
      setApprovalModal({ open: false, id: null, items: [] });
      load();
    } catch { setError(ar ? 'فشل الاعتماد الجزئي' : 'Partial approve failed'); }
  };

  const rejectDispatch = async (id) => {
    try {
      await client.post(`/api/warehouse/dispatch/${id}/reject`, { reason: '' });
      load();
    } catch { setError(ar ? 'فشل الرفض' : 'Reject failed'); }
  };

  const executeDispatch = async (id) => {
    try {
      await client.post(`/api/warehouse/dispatch/${id}/issue`);
      load();
    } catch { setError(ar ? 'فشل التنفيذ' : 'Execute failed'); }
  };

  const openApproval = async (d) => {
    try {
      const res = await client.get(`/api/warehouse/dispatch/${d.id}/approve`);
      const raw = res.data || [];
      const items = raw.map(it => ({ ...it, approvedQty: String(it.requestedQuantity) }));
      setApprovalModal({ open: true, id: d.id, items });
    } catch { setError(ar ? 'فشل تحميل بيانات الاعتماد' : 'Failed to load approval data'); }
  };

  return (
    <div style={{ animation: 'fadeIn 0.4s ease' }}>
      <PageHeader
        title={
          <span style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
            <PackageCheck size={24} style={{ color: 'var(--primary)' }} />
            {ar ? 'طلبات الصرف' : 'Warehouse Dispatch Requests'}
          </span>
        }
        subtitle={ar ? 'إدارة طلبات صرف قطع الغيار' : 'Manage parts dispatch requests'}
        actions={canAdd ? (
          <Button icon={Plus} onClick={openAdd}>{ar ? 'طلب جديد' : 'New Request'}</Button>
        ) : null}
      />

      {error && <div className="alert-inline alert-danger" style={{ marginBottom: 20 }}>{error}</div>}

      <div style={{ display: 'flex', gap: 8, marginBottom: 20 }}>
        <button className={`btn ${activeTab === 'incoming' ? 'btn-primary' : 'btn-ghost'}`} onClick={() => setActiveTab('incoming')}>{ar ? 'الواردة' : 'Incoming'}</button>
        <button className={`btn ${activeTab === 'outgoing' ? 'btn-primary' : 'btn-ghost'}`} onClick={() => setActiveTab('outgoing')}>{ar ? 'الصادرة' : 'My Requests'}</button>
      </div>

      {loading ? (
        <div className="ft-spinner" style={{ width: 40, height: 40, margin: '40px auto' }} />
      ) : (
        <>
          <div className="data-table" style={{ overflowX: 'auto' }}>
            <table>
              <thead>
                <tr>
                  <th>{ar ? 'الموقع' : 'Station'}</th>
                  <th>{ar ? 'تصريح العمل' : 'Work Permit'}</th>
                  <th>{ar ? 'التاريخ' : 'Date'}</th>
                  <th>{ar ? 'الحالة' : 'Status'}</th>
                  <th style={{ textAlign: 'center' }}>{ar ? 'إجراءات' : 'Actions'}</th>
                </tr>
              </thead>
              <tbody>
                {paginated.length === 0 && (
                  <tr><td colSpan={5} style={{ textAlign: 'center', padding: 40, opacity: 0.7 }}>{ar ? 'لا توجد بيانات' : 'No data'}</td></tr>
                )}
                {paginated.map(d => {
                  const cfg = statusCfg[d.status] || statusCfg.pending;
                  return (
                    <tr key={d.id}>
                      <td style={{ fontWeight: 700 }}>{d.subStationName || d.stationName}</td>
                      <td>{d.workPermitNumber || '—'}</td>
                      <td>{d.createdAt}</td>
                      <td><span className={`badge ${cfg.cls}`}>{cfg.label[lang] || d.status}</span></td>
                      <td style={{ textAlign: 'center' }}>
                        {activeTab === 'incoming' && canApprove && d.status === 'pending' && (
                          <Button variant="primary" size="sm" icon={Check} onClick={() => openApproval(d)}>{ar ? 'اعتماد' : 'Approve'}</Button>
                        )}
                        {d.status === 'approved' && canExecute && (
                          <Button variant="success" size="sm" icon={PackageCheck} onClick={() => executeDispatch(d.id)}>{ar ? 'تنفيذ' : 'Execute'}</Button>
                        )}
                        {canAdd && (d.status === 'pending' || d.status === 'rejected') && (
                          <Button variant="ghost" size="sm" icon={Edit2} onClick={() => openEdit(d)} />
                        )}
                        {canDelete && (d.status === 'pending' || d.status === 'rejected') && (
                          <Button variant="ghost" size="sm" icon={Trash2} onClick={() => setConfirmDelete({ open: true, id: d.id })} />
                        )}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
          <Pagination page={page} setPage={setPage} total={filtered.length} pageSize={PAGE_SIZE} />
        </>
      )}

      {showModal && (
        <Modal
          open={showModal}
          onClose={() => setShowModal(false)}
          title={editId ? (ar ? 'تعديل طلب الصرف' : 'Edit Dispatch Request') : (ar ? 'طلب صرف جديد' : 'New Dispatch Request')}
          maxWidth={600}
          footer={
            <Button variant="primary" onClick={handleSave} disabled={saving} style={{ width: '100%' }}>
              {saving ? <div className="ft-spinner" style={{ width: 20, height: 20, borderWidth: 2 }} /> : (ar ? 'إرسال' : 'Submit')}
            </Button>
          }
        >
          {error && <div className="form-error" role="alert" style={{ marginBottom: 16 }}>{error}</div>}
          <form onSubmit={handleSave}>
            <FormField label={ar ? 'الموقع الفرعي' : 'Sub Station'} required>
              <select className="form-control" required value={form.subStationId} onChange={e => setForm({ ...form, subStationId: e.target.value })}>
                <option value="">{ar ? 'اختر الموقع' : 'Select station'}</option>
                {subStations.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
              </select>
            </FormField>
            <FormField label={ar ? 'الغرض' : 'Purpose'}>
              <Input value={form.purpose} onChange={e => setForm({ ...form, purpose: e.target.value })} placeholder={ar ? 'مثال: صيانة دورية' : 'e.g. routine maintenance'} />
            </FormField>
            <FormField label={ar ? 'تصريح العمل (اختياري)' : 'Work Permit (optional)'}>
              <select className="form-control" value={form.workPermitId} onChange={e => setForm({ ...form, workPermitId: e.target.value })}>
                <option value="">{ar ? 'بدون' : 'None'}</option>
                {workPermits.map(w => <option key={w.id} value={w.id}>{w.permitNumber} — {w.problemDescription}</option>)}
              </select>
            </FormField>
            <div style={{ marginTop: 12 }}>
              <label className="form-label">{ar ? 'القطع' : 'Parts'}</label>
              {form.items.map((it, idx) => (
                <div key={idx} style={{ display: 'flex', gap: 8, marginBottom: 8, alignItems: 'center' }}>
                  <select className="form-control" required value={it.sparePartId} onChange={e => {
                    const items = [...form.items]; items[idx].sparePartId = e.target.value; setForm({ ...form, items });
                  }} style={{ flex: 2 }}>
                    <option value="">{ar ? 'اختر القطعة' : 'Select part'}</option>
                    {parts.map(p => <option key={p.id} value={p.id}>{p.name}</option>)}
                  </select>
                  <Input type="number" placeholder={ar ? 'الكمية' : 'Qty'} required value={it.requestedQuantity} onChange={e => {
                    const items = [...form.items]; items[idx].requestedQuantity = e.target.value; setForm({ ...form, items });
                  }} style={{ flex: 1 }} />
                  {form.items.length > 1 && (
                    <Button variant="ghost" size="sm" onClick={() => {
                      const items = form.items.filter((_, i) => i !== idx);
                      setForm({ ...form, items });
                    }}><Trash2 size={16} /></Button>
                  )}
                </div>
              ))}
              <Button variant="ghost" size="sm" onClick={() => setForm({ ...form, items: [...form.items, { sparePartId: '', requestedQuantity: '', notes: '' }] })}>
                <Plus size={14} /> {ar ? 'إضافة سطر' : 'Add row'}
              </Button>
            </div>
            <FormField label={ar ? 'ملاحظات' : 'Notes'}>
              <textarea className="form-control" rows={2} value={form.notes} onChange={e => setForm({ ...form, notes: e.target.value })} />
            </FormField>
          </form>
        </Modal>
      )}

      {approvalModal.open && (
        <Modal
          open={approvalModal.open}
          onClose={() => setApprovalModal({ open: false, id: null, items: [] })}
          title={ar ? 'اعتماد طلب الصرف' : 'Approve Dispatch Request'}
          maxWidth={650}
          footer={
            <div style={{ display: 'flex', gap: 8, marginTop: 16, justifyContent: 'flex-end' }}>
              <Button variant="success" onClick={() => approveAll(approvalModal.id)}>{ar ? 'اعتماد الكل' : 'Approve All'}</Button>
              <Button variant="primary" onClick={() => partialApprove(approvalModal.id)}>{ar ? 'اعتماد جزئي' : 'Partial Approve'}</Button>
              <Button variant="danger" onClick={() => rejectDispatch(approvalModal.id)}>{ar ? 'رفض' : 'Reject'}</Button>
            </div>
          }
        >
          <table className="data-table">
            <thead><tr><th>{ar ? 'القطعة' : 'Part'}</th><th>{ar ? 'المطلوب' : 'Requested'}</th><th>{ar ? 'المتاح' : 'Available'}</th><th>{ar ? 'الموافقة' : 'Approved Qty'}</th></tr></thead>
            <tbody>
              {approvalModal.items.map((it, i) => (
                <tr key={i}>
                  <td>{it.partName}</td>
                  <td>{it.requestedQuantity}</td>
                  <td>{it.availableQty}</td>
                  <td>
                    <Input type="number" defaultValue={it.requestedQuantity} max={it.availableQty}
                      onChange={e => {
                        const items = [...approvalModal.items];
                        items[i].approvedQty = e.target.value;
                        setApprovalModal({ ...approvalModal, items });
                      }} />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </Modal>
      )}

      <ConfirmModal
        open={confirmDelete.open}
        title={ar ? 'تأكيد الحذف' : 'Confirm Delete'}
        message={ar ? 'هل أنت متأكد من حذف هذا الطلب؟' : 'Are you sure you want to delete this request?'}
        onConfirm={handleDelete}
        onCancel={() => setConfirmDelete({ open: false, id: null })}
        lang={lang}
      />
    </div>
  );
}

export default WarehouseDispatch;
