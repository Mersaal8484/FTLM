import React, { useState, useEffect, useMemo } from 'react';
import { Plus, Edit2, Trash2, X, Search, Check, PackageCheck } from 'lucide-react';
import client from '../api/client';
import ConfirmModal from '../components/ConfirmModal';
import Pagination from '../components/Pagination';
import { PageHeader, Button, Modal, FormField, Input } from '../components/ui';

const statusCfg = {
  draft:     { cls: 'badge-secondary', label: { ar: 'مسودة', en: 'Draft' } },
  completed: { cls: 'badge-success',  label: { ar: 'مكتمل', en: 'Completed' } },
  cancelled: { cls: 'badge-danger',   label: { ar: 'ملغى', en: 'Cancelled' } },
};

const fmt = (v, d = 2) => v != null ? Number(v).toLocaleString('en-US', { maximumFractionDigits: d }) : '—';

const initialForm = {
  stationId: '',
  supplyDate: '',
  supplierName: '',
  invoiceNumber: '',
  invoiceDate: '',
  totalAmount: '',
  notes: '',
  items: [{ sparePartId: '', quantity: '', unitPrice: '', serialNumber: '', notes: '' }],
};

function WarehouseSupply({ lang, currentUser }) {
  const ar = lang === 'ar';
  const [supplies, setSupplies] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [showModal, setShowModal] = useState(false);
  const [editId, setEditId] = useState(null);
  const [form, setForm] = useState(initialForm);
  const [saving, setSaving] = useState(false);
  const [confirmDelete, setConfirmDelete] = useState({ open: false, id: null });
  const [detailItem, setDetailItem] = useState(null);
  const [stations, setStations] = useState([]);
  const [parts, setParts] = useState([]);
  const [page, setPage] = useState(1);
  const PAGE_SIZE = 20;

  const perms = currentUser?.permissions?.warehouse || {};
  const canAdd = currentUser?.role === 'admin' || perms.add || perms.create;
  const canEdit = currentUser?.role === 'admin' || perms.edit;
  const canDelete = currentUser?.role === 'admin' || perms.delete;
  const canConfirm = currentUser?.role === 'admin' || perms.approve;

  const load = async () => {
    setLoading(true); setError(null);
    try {
      const [supRes, stRes, partRes] = await Promise.allSettled([
        client.get('/api/warehouse/supply', { params: { _t: Date.now() } }),
        client.get('/api/stations', { params: { _t: Date.now() } }),
        client.get('/api/spare-parts', { params: { _t: Date.now() } }),
      ]);
      if (supRes.status === 'fulfilled') setSupplies(supRes.value.data?.items || supRes.value.data || []);
      if (stRes.status === 'fulfilled') setStations(stRes.value.data || []);
      if (partRes.status === 'fulfilled') setParts(partRes.value.data?.items || partRes.value.data || []);
    } catch { setError(ar ? 'فشل تحميل البيانات' : 'Failed to load data'); }
    finally { setLoading(false); }
  };

  useEffect(() => { load(); }, []);

  const paginated = useMemo(() => {
    const start = (page - 1) * PAGE_SIZE;
    return supplies.slice(start, start + PAGE_SIZE);
  }, [supplies, page]);

  const totalAmount = useMemo(() => {
    return form.items.reduce((sum, it) => sum + (Number(it.quantity) || 0) * (Number(it.unitPrice) || 0), 0);
  }, [form.items]);

  const openAdd = () => { setForm(initialForm); setEditId(null); setShowModal(true); setError(null); };
  const openEdit = async (s) => {
    try {
      const res = await client.get(`/api/warehouse/supply/${s.id}`);
      const d = res.data;
      setForm({
        stationId: String(d.stationId || ''),
        supplyDate: (d.supplyDate || '').slice(0, 10),
        supplierName: d.supplierName || '',
        invoiceNumber: d.invoiceNumber || '',
        invoiceDate: (d.invoiceDate || '').slice(0, 10),
        totalAmount: d.totalAmount != null ? String(d.totalAmount) : '',
        notes: d.notes || '',
        items: d.items?.length ? d.items.map(it => ({
          sparePartId: String(it.sparePartId || it.partId || ''),
          quantity: it.quantity != null ? String(it.quantity) : '',
          unitPrice: it.unitPrice != null ? String(it.unitPrice) : '',
          serialNumber: it.serialNumber || '',
          notes: it.notes || '',
        })) : [{ sparePartId: '', quantity: '', unitPrice: '', serialNumber: '', notes: '' }],
      });
      setEditId(s.id);
      setShowModal(true);
    } catch {
      setForm({
        ...s,
        stationId: String(s.stationId || ''),
        supplyDate: (s.supplyDate || '').slice(0, 10),
        supplierName: s.supplierName || s.supplier || '',
        totalAmount: s.totalAmount != null ? String(s.totalAmount) : '',
        items: s.items || [{ sparePartId: '', quantity: '', unitPrice: '', serialNumber: '', notes: '' }],
      });
      setEditId(s.id);
      setShowModal(true);
    }
    setError(null);
  };

       const handleSave = async (e) => {
         e.preventDefault(); setSaving(true); setError(null);
         try {
           const payload = { ...form, totalAmount: totalAmount || 0 };
           if (editId) await client.put(`/api/warehouse/supply/${editId}`, payload);
           else await client.post('/api/warehouse/supply', payload);
           setShowModal(false); load();
    } catch (err) { setError(err.response?.data?.error || err.response?.data?.msg || (ar ? 'فشل الحفظ' : 'Save failed')); }
    finally { setSaving(false); }
  };

  const handleDelete = async () => {
    try {
      await client.delete(`/api/warehouse/supply/${confirmDelete.id}`);
      setConfirmDelete({ open: false, id: null }); load();
    } catch { setError(ar ? 'فشل الحذف' : 'Delete failed'); }
  };

  const confirmVoucher = async (id) => {
    try {
      await client.post(`/api/warehouse/supply/${id}/confirm`);
      load();
    } catch { setError(ar ? 'فشل تأكيد السند' : 'Confirm failed'); }
  };

  const viewDetail = async (s) => {
    try {
      const res = await client.get(`/api/warehouse/supply/${s.id}`);
      setDetailItem(res.data);
    } catch { setDetailItem(s); }
  };

  return (
    <div style={{ animation: 'fadeIn 0.4s ease' }}>
      <PageHeader
        title={
          <span style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
            <PackageCheck size={24} style={{ color: 'var(--primary)' }} />
            {ar ? 'سندات التوريد' : 'Warehouse Supply Vouchers'}
          </span>
        }
        subtitle={ar ? 'إدارة سندات توريد قطع الغيار' : 'Manage spare parts supply vouchers'}
        actions={canAdd ? (
          <Button icon={Plus} onClick={openAdd}>{ar ? 'سند جديد' : 'New Voucher'}</Button>
        ) : null}
      />

      {error && <div className="alert-inline alert-danger" style={{ marginBottom: 20 }}>{error}</div>}

      {loading ? (
        <div className="ft-spinner" style={{ width: 40, height: 40, margin: '40px auto' }} />
      ) : (
        <>
          <div className="data-table" style={{ overflowX: 'auto' }}>
            <table>
              <thead>
                <tr>
                  <th>{ar ? 'رقم السند' : 'Voucher #'}</th>
                  <th>{ar ? 'التاريخ' : 'Date'}</th>
                  <th>{ar ? 'المورد' : 'Supplier'}</th>
                  <th>{ar ? 'المبلغ' : 'Amount'}</th>
                  <th>{ar ? 'الموقع' : 'Station'}</th>
                  <th>{ar ? 'الحالة' : 'Status'}</th>
                  <th style={{ textAlign: 'center' }}>{ar ? 'إجراءات' : 'Actions'}</th>
                </tr>
              </thead>
              <tbody>
                {paginated.length === 0 && (
                  <tr><td colSpan={7} style={{ textAlign: 'center', padding: 40, opacity: 0.7 }}>{ar ? 'لا توجد بيانات' : 'No data'}</td></tr>
                )}
                  {paginated.map(s => {
                  const cfg = statusCfg[(s.statusName || '').toLowerCase()] || statusCfg.draft;
                  return (
                    <tr key={s.id}>
                      <td style={{ fontWeight: 700 }}>{s.supplyNumber || s.invoiceNumber}</td>
                      <td>{s.supplyDate}</td>
                      <td>{s.supplierName}</td>
                      <td>{s.totalAmount != null ? fmt(s.totalAmount, 2) : '—'}</td>
                      <td>{s.stationName}</td>
                      <td><span className={`badge ${cfg.cls}`}>{cfg.label[lang] || s.statusName}</span></td>
                      <td style={{ textAlign: 'center' }}>
                        <Button variant="ghost" size="sm" icon={Search} onClick={() => viewDetail(s)} title={ar ? 'عرض' : 'View'} />
                        {canEdit && s.statusName === 'draft' && <Button variant="ghost" size="sm" icon={Edit2} onClick={() => openEdit(s)} />}
                        {canConfirm && s.statusName === 'draft' && <Button variant="success" size="sm" icon={Check} onClick={() => confirmVoucher(s.id)}>{ar ? 'تأكيد' : 'Confirm'}</Button>}
                        {canDelete && s.statusName === 'draft' && <Button variant="ghost" size="sm" icon={Trash2} onClick={() => setConfirmDelete({ open: true, id: s.id })} />}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
          <Pagination page={page} setPage={setPage} total={supplies.length} pageSize={PAGE_SIZE} />
        </>
      )}

      {/* Add/Edit Modal */}
      {showModal && (
        <Modal
          open={showModal}
          onClose={() => setShowModal(false)}
          title={editId ? (ar ? 'تعديل سند' : 'Edit Voucher') : (ar ? 'سند توريد جديد' : 'New Supply Voucher')}
          maxWidth={700}
          footer={
            <Button variant="primary" onClick={handleSave} disabled={saving} style={{ width: '100%' }}>
              {saving ? <div className="ft-spinner" style={{ width: 20, height: 20, borderWidth: 2 }} /> : (ar ? 'حفظ' : 'Save')}
            </Button>
          }
        >
          {error && <div className="form-error" role="alert" style={{ marginBottom: 16 }}>{error}</div>}
          <form onSubmit={handleSave}>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
              <FormField label={ar ? 'الموقع' : 'Station'} required>
                <select className="form-select" required value={form.stationId} onChange={e => setForm({ ...form, stationId: e.target.value })}>
                  <option value="">{ar ? 'اختر الموقع' : 'Select station'}</option>
                  {stations.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
                </select>
              </FormField>
              <FormField label={ar ? 'تاريخ التوريد' : 'Supply Date'} required>
                <Input type="date" required value={form.supplyDate} onChange={e => setForm({ ...form, supplyDate: e.target.value })} />
              </FormField>
              <FormField label={ar ? 'تاريخ الفاتورة' : 'Invoice Date'}>
                <Input type="date" value={form.invoiceDate} onChange={e => setForm({ ...form, invoiceDate: e.target.value })} />
              </FormField>
              <FormField label={ar ? 'المورد' : 'Supplier'} required>
                <Input required value={form.supplierName} onChange={e => setForm({ ...form, supplierName: e.target.value })} />
              </FormField>
            </div>

            <div style={{ marginTop: 16 }}>
              <label className="form-label">{ar ? 'القطع' : 'Parts'}</label>
              {form.items.map((it, idx) => (
                <div key={idx} style={{ display: 'flex', gap: 8, marginBottom: 8, alignItems: 'center' }}>
                  <select className="form-select" required value={it.sparePartId} onChange={e => {
                    const items = [...form.items]; items[idx].sparePartId = e.target.value; setForm({ ...form, items });
                  }} style={{ flex: 2 }}>
                    <option value="">{ar ? 'اختر القطعة' : 'Select part'}</option>
                    {parts.map(p => <option key={p.id} value={p.id}>{p.name} ({p.partNumber})</option>)}
                  </select>
                  <Input type="number" placeholder={ar ? 'الكمية' : 'Qty'} required value={it.quantity} onChange={e => {
                    const items = [...form.items]; items[idx].quantity = e.target.value; setForm({ ...form, items });
                  }} style={{ flex: 1 }} />
                  <Input type="number" step="0.01" placeholder={ar ? 'السعر' : 'Price'} value={it.unitPrice} onChange={e => {
                    const items = [...form.items]; items[idx].unitPrice = e.target.value; setForm({ ...form, items });
                  }} style={{ flex: 1 }} />
                  <Input placeholder={ar ? 'الرقم التسلسلي' : 'Serial #'} value={it.serialNumber} onChange={e => {
                    const items = [...form.items]; items[idx].serialNumber = e.target.value; setForm({ ...form, items });
                  }} style={{ flex: 1 }} />
                  {form.items.length > 1 && (
                    <Button variant="ghost" size="sm" onClick={() => {
                      const items = form.items.filter((_, i) => i !== idx);
                      setForm({ ...form, items });
                    }}><Trash2 size={16} /></Button>
                  )}
                </div>
              ))}
              <Button variant="ghost" size="sm" onClick={() => setForm({ ...form, items: [...form.items, { sparePartId: '', quantity: '', unitPrice: '', serialNumber: '', notes: '' }] })} style={{ marginTop: 4 }}>
                <Plus size={14} /> {ar ? 'إضافة سطر' : 'Add row'}
              </Button>
            </div>

            <div style={{ marginTop: 12, textAlign: 'start', fontWeight: 700, fontSize: 'var(--text-lg)' }}>
              {ar ? 'الإجمالي:' : 'Total:'} {fmt(totalAmount, 2)}
            </div>

            <FormField label={ar ? 'ملاحظات' : 'Notes'}>
              <textarea className="form-control" rows={2} value={form.notes} onChange={e => setForm({ ...form, notes: e.target.value })} />
            </FormField>
          </form>
        </Modal>
      )}

      {/* Detail Modal */}
      {detailItem && (
        <Modal
          open={!!detailItem}
          onClose={() => setDetailItem(null)}
          title={
            <span>
              {ar ? 'تفاصيل السند' : 'Voucher Details'} — {detailItem.invoiceNumber || detailItem.supplyNumber}
            </span>
          }
          maxWidth={650}
        >
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12, marginBottom: 20 }}>
            <div><strong>{ar ? 'التاريخ' : 'Date'}:</strong> {detailItem.supplyDate}</div>
            <div><strong>{ar ? 'المورد' : 'Supplier'}:</strong> {detailItem.supplierName}</div>
            <div><strong>{ar ? 'المبلغ' : 'Amount'}:</strong> {detailItem.totalAmount != null ? fmt(detailItem.totalAmount, 2) : '—'}</div>
            <div><strong>{ar ? 'الموقع' : 'Station'}:</strong> {detailItem.stationName}</div>
          </div>
          <h4 style={{ marginBottom: 10 }}>{ar ? 'القطع' : 'Parts'}</h4>
          {detailItem.items?.length === 0 ? (
            <div className="empty-state">{ar ? 'لا توجد قطع' : 'No parts'}</div>
          ) : (
            <table className="data-table">
              <thead><tr><th>{ar ? 'القطعة' : 'Part'}</th><th>{ar ? 'الكمية' : 'Qty'}</th><th>{ar ? 'السعر' : 'Price'}</th><th>{ar ? 'الرقم التسلسلي' : 'Serial'}</th></tr></thead>
              <tbody>
                {detailItem.items?.map((it, i) => (
                  <tr key={i}><td>{it.sparePartName || it.partName}</td><td>{it.quantity}</td><td>{it.unitPrice}</td><td>{it.serialNumber}</td></tr>
                ))}
              </tbody>
            </table>
          )}
        </Modal>
      )}

      <ConfirmModal
        open={confirmDelete.open}
        title={ar ? 'تأكيد الحذف' : 'Confirm Delete'}
        message={ar ? 'هل أنت متأكد من حذف هذا السند؟' : 'Are you sure you want to delete this voucher?'}
        onConfirm={handleDelete}
        onCancel={() => setConfirmDelete({ open: false, id: null })}
        lang={lang}
      />
    </div>
  );
}

export default WarehouseSupply;
