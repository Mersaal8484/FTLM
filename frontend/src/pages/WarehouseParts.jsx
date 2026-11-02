import React, { useState, useEffect, useMemo } from 'react';
import { Plus, Edit2, Trash2, Search, Download, PackageSearch, AlertTriangle } from 'lucide-react';
import client from '../api/client';
import ConfirmModal from '../components/ConfirmModal';
import Pagination from '../components/Pagination';
import { PageHeader, Button, Modal, FormField, Input } from '../components/ui';

const initialForm = {
  partNumber: '',
  name: '',
  nameEn: '',
  partCategoryLookupId: '',
  unitLookupId: '',
  manufacturerPartNumber: '',
  barcode: '',
  unitPrice: '',
  minStockQuantity: '',
  replacementIntervalHours: '',
  isHoursTracked: false,
  replacementPriorityLookupId: '',
  description: '',
  notes: '',
  isActive: true,
};

function WarehouseParts({ lang, currentUser }) {
  const ar = lang === 'ar';
  const [parts, setParts] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [search, setSearch] = useState('');
  const [filterCat, setFilterCat] = useState('');
  const [showModal, setShowModal] = useState(false);
  const [editId, setEditId] = useState(null);
  const [form, setForm] = useState(initialForm);
  const [saving, setSaving] = useState(false);
  const [confirmDelete, setConfirmDelete] = useState({ open: false, id: null });
  const [detailItem, setDetailItem] = useState(null);
  const [page, setPage] = useState(1);
  const PAGE_SIZE = 20;
  const [categories, setCategories] = useState([]);
  const [units, setUnits] = useState([]);
  const [priorities, setPriorities] = useState([]);

  const perms = currentUser?.permissions?.warehouse || {};
  const canAdd = currentUser?.role === 'admin' || perms.add || perms.create;
  const canEdit = currentUser?.role === 'admin' || perms.edit;
  const canDelete = currentUser?.role === 'admin' || perms.delete;

  const loadLookups = async () => {
    try {
      const [catRes, unitRes, priRes] = await Promise.allSettled([
        client.get('/api/lookups/by-category/PartCategory'),
        client.get('/api/lookups/by-category/PartUnit'),
        client.get('/api/lookups/by-category/ReplacementPriority'),
      ]);
      if (catRes.status === 'fulfilled') setCategories(catRes.value.data || []);
      if (unitRes.status === 'fulfilled') setUnits(unitRes.value.data || []);
      if (priRes.status === 'fulfilled') setPriorities(priRes.value.data || []);
    } catch { /* ignore lookup errors */ }
  };

  const load = async () => {
    setLoading(true); setError(null);
    try {
      const params = { _t: Date.now() };
      if (search) params.search = search;
      if (filterCat) params.categoryId = filterCat;
      const res = await client.get('/api/spare-parts', { params });
      setParts(res.data?.items || res.data?.data || res.data || []);
    } catch { setError(ar ? 'فشل تحميل البيانات' : 'Failed to load data'); }
    finally { setLoading(false); }
  };

  useEffect(() => { loadLookups(); }, []);
  useEffect(() => { load(); }, [search, filterCat]);

  const filtered = useMemo(() => parts, [parts]);

  const paginated = useMemo(() => {
    const start = (page - 1) * PAGE_SIZE;
    return filtered.slice(start, start + PAGE_SIZE);
  }, [filtered, page]);

  useEffect(() => { setPage(1); }, [search, filterCat]);

  const openAdd = () => { setForm(initialForm); setEditId(null); setShowModal(true); setError(null); };
  const openEdit = async (p) => {
    try {
      const res = await client.get(`/api/spare-parts/${p.id}`);
      const d = res.data;
      setForm({
        partNumber: d.partNumber || p.partNumber || '',
        name: d.name || p.name || '',
        nameEn: d.nameEn || p.nameEn || '',
        partCategoryLookupId: d.partCategoryLookupId || '',
        unitLookupId: d.unitLookupId || '',
        manufacturerPartNumber: d.manufacturerPartNumber || '',
        barcode: d.barcode || '',
        unitPrice: d.unitPrice != null ? String(d.unitPrice) : '',
        minStockQuantity: d.minStockQuantity != null ? String(d.minStockQuantity) : '',
        replacementIntervalHours: d.replacementIntervalHours != null ? String(d.replacementIntervalHours) : '',
        isHoursTracked: d.isHoursTracked || false,
        replacementPriorityLookupId: d.replacementPriorityLookupId || '',
        description: d.description || '',
        notes: d.notes || '',
        isActive: d.isActive !== false,
      });
      setEditId(p.id);
      setShowModal(true);
    } catch {
      setForm({
        partNumber: p.partNumber || '',
        name: p.name || '',
        nameEn: p.nameEn || '',
        partCategoryLookupId: '',
        unitLookupId: '',
        manufacturerPartNumber: p.manufacturerPartNumber || '',
        barcode: p.barcode || '',
        unitPrice: p.unitPrice != null ? String(p.unitPrice) : '',
        minStockQuantity: p.minStockQuantity != null ? String(p.minStockQuantity) : '',
        replacementIntervalHours: p.replacementIntervalHours != null ? String(p.replacementIntervalHours) : '',
        isHoursTracked: p.isHoursTracked || false,
        replacementPriorityLookupId: '',
        description: p.description || '',
        notes: p.notes || '',
        isActive: p.isActive !== false,
      });
      setEditId(p.id);
      setShowModal(true);
    }
    setError(null);
  };

  const handleSave = async (e) => {
    e.preventDefault(); setSaving(true); setError(null);
    try {
      const payload = {
        name: form.name,
        nameEn: form.nameEn || null,
        partNumber: form.partNumber,
        manufacturerPartNumber: form.manufacturerPartNumber || null,
        barcode: form.barcode || null,
        partCategoryLookupId: Number(form.partCategoryLookupId),
        unitLookupId: Number(form.unitLookupId),
        description: form.description || null,
        unitPrice: form.unitPrice ? Number(form.unitPrice) : 0,
        minStockQuantity: form.minStockQuantity ? Number(form.minStockQuantity) : 0,
        replacementIntervalHours: form.replacementIntervalHours ? Number(form.replacementIntervalHours) : null,
        isHoursTracked: form.isHoursTracked,
        replacementPriorityLookupId: form.replacementPriorityLookupId ? Number(form.replacementPriorityLookupId) : null,
        notes: form.notes || null,
        isActive: form.isActive,
      };
      if (editId) await client.put(`/api/spare-parts/${editId}`, payload);
      else await client.post('/api/spare-parts', payload);
      setShowModal(false); load();
    } catch (err) { setError(err.response?.data?.error || err.response?.data?.msg || (ar ? 'فشل الحفظ' : 'Save failed')); }
    finally { setSaving(false); }
  };

  const handleDelete = async () => {
    try {
      await client.delete(`/api/spare-parts/${confirmDelete.id}`);
      setConfirmDelete({ open: false, id: null }); load();
    } catch { setError(ar ? 'فشل الحذف' : 'Delete failed'); }
  };

  const exportExcel = async () => {
    try {
      const res = await client.get('/api/spare-parts/export', { params: { search, categoryId: filterCat }, responseType: 'blob' });
      const url = window.URL.createObjectURL(new Blob([res.data]));
      const a = document.createElement('a'); a.href = url;
      a.download = ar ? 'قطع_غيار.csv' : 'spare-parts.csv';
      a.click(); window.URL.revokeObjectURL(url);
    } catch { setError(ar ? 'فشل التصدير' : 'Export failed'); }
  };

  const viewDetails = async (part) => {
    try {
      const res = await client.get(`/api/spare-parts/${part.id}/stock`);
      setDetailItem({ ...part, stock: res.data });
    } catch { setDetailItem({ ...part, stock: [] }); }
  };

  return (
    <div style={{ animation: 'fadeIn 0.4s ease' }}>
      <PageHeader
        title={
          <span style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
            <PackageSearch size={24} style={{ color: 'var(--primary)' }} />
            {ar ? 'كتالوج قطع الغيار' : 'Parts Catalog'}
          </span>
        }
        subtitle={ar ? 'إدارة كتالوج قطع الغيار والمواد' : 'Manage spare parts catalog and materials'}
        actions={
          <div style={{ display: 'flex', gap: 8, alignItems: 'center', flexWrap: 'wrap' }}>
            <Button variant="ghost" icon={Download} onClick={exportExcel}>{ar ? 'تصدير Excel' : 'Export Excel'}</Button>
            {canAdd && <Button icon={Plus} onClick={openAdd}>{ar ? 'إضافة قطعة' : 'Add Part'}</Button>}
          </div>
        }
      />

      {error && <div className="alert-inline alert-danger" style={{ marginBottom: 20 }}>{error}</div>}

      <div className="section-card" style={{ marginBottom: 20, padding: 16, display: 'flex', gap: 12, flexWrap: 'wrap', alignItems: 'center' }}>
        <FormField label={ar ? 'بحث برقم القطعة أو الاسم...' : 'Search by part number or name...'} style={{ flex: '1 1 240px' }}>
          <Input placeholder={ar ? 'بحث برقم القطعة أو الاسم...' : 'Search by part number or name...'}
            value={search} onChange={e => setSearch(e.target.value)} style={{ width: '100%' }} />
        </FormField>
        <FormField label={ar ? 'كل الفئات' : 'All Categories'} style={{ minWidth: 180 }}>
          <select className="form-select" value={filterCat} onChange={e => setFilterCat(e.target.value)} style={{ width: 180 }}>
            <option value="">{ar ? 'كل الفئات' : 'All Categories'}</option>
            {categories.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
          </select>
        </FormField>
      </div>

      {loading ? (
        <div className="ft-spinner" style={{ width: 40, height: 40, margin: '40px auto' }} />
      ) : (
        <>
          <div className="data-table" style={{ overflowX: 'auto' }}>
            <table>
              <thead>
                <tr>
                  <th>{ar ? 'رقم القطعة' : 'Part Number'}</th>
                  <th>{ar ? 'الاسم' : 'Name'}</th>
                  <th>{ar ? 'الفئة' : 'Category'}</th>
                  <th>{ar ? 'الوحدة' : 'Unit'}</th>
                  <th>{ar ? 'الحد الأدنى' : 'Min Stock'}</th>
                  <th>{ar ? 'الحالة' : 'Status'}</th>
                  <th style={{ textAlign: 'center' }}>{ar ? 'إجراءات' : 'Actions'}</th>
                </tr>
              </thead>
              <tbody>
                {paginated.length === 0 && (
                  <tr><td colSpan={7} style={{ textAlign: 'center', padding: 40, opacity: 0.7 }}>
                    {ar ? 'لا توجد بيانات' : 'No data found'}
                  </td></tr>
                )}
                {paginated.map(p => (
                  <tr key={p.id}>
                    <td style={{ fontWeight: 700 }}>{p.partNumber}</td>
                    <td>{p.name}</td>
                    <td>{p.partCategoryName || p.category || '—'}</td>
                    <td>{p.unitName || p.unit || '—'}</td>
                    <td>{p.minStockQuantity != null ? p.minStockQuantity : p.minStock}</td>
                    <td><span className={`badge ${p.isActive !== false ? 'badge-success' : 'badge-secondary'}`}>{p.isActive !== false ? (ar ? 'فعال' : 'Active') : (ar ? 'معطل' : 'Inactive')}</span></td>
                      <td style={{ textAlign: 'center' }}>
                        <Button variant="ghost" size="sm" icon={AlertTriangle} onClick={() => viewDetails(p)} title={ar ? 'عرض التفاصيل' : 'View details'} />
                        {canEdit && <Button variant="ghost" size="sm" icon={Edit2} onClick={() => openEdit(p)} />}
                        {canDelete && <Button variant="ghost" size="sm" icon={Trash2} onClick={() => setConfirmDelete({ open: true, id: p.id })} />}
                      </td>
                  </tr>
                ))}
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
          title={editId ? (ar ? 'تعديل قطعة' : 'Edit Part') : (ar ? 'إضافة قطعة' : 'Add Part')}
          maxWidth={600}
          footer={
            <div style={{ display: 'flex', gap: 10 }}>
              <Button variant="ghost" size="sm" onClick={() => setShowModal(false)} style={{ flex: 1 }}>{ar ? 'إلغاء' : 'Cancel'}</Button>
              <Button variant="primary" size="sm" onClick={handleSave} disabled={saving} style={{ flex: 2 }}>
                {saving ? <div className="ft-spinner" style={{ width: 14, height: 14 }} /> : (ar ? 'حفظ' : 'Save')}
              </Button>
            </div>
          }
        >
          {error && <div className="form-error" role="alert" style={{ marginBottom: 16 }}>{error}</div>}
          <form onSubmit={handleSave}>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 14 }}>
              <FormField label={ar ? 'رقم القطعة *' : 'Part Number *'} required>
                <Input required value={form.partNumber} onChange={e => setForm({ ...form, partNumber: e.target.value })} />
              </FormField>
              <FormField label={ar ? 'الاسم (عربي) *' : 'Name (Arabic) *'} required>
                <Input required value={form.name} onChange={e => setForm({ ...form, name: e.target.value })} />
              </FormField>
              <FormField label={ar ? 'الاسم (إنجليزي)' : 'Name (English)'}>
                <Input value={form.nameEn} onChange={e => setForm({ ...form, nameEn: e.target.value })} />
              </FormField>
              <FormField label={ar ? 'الفئة *' : 'Category *'} required>
                <select className="form-select" required value={form.partCategoryLookupId} onChange={e => setForm({ ...form, partCategoryLookupId: e.target.value })}>
                  <option value="">{ar ? 'اختر الفئة' : 'Select category'}</option>
                  {categories.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
                </select>
              </FormField>
              <FormField label={ar ? 'الوحدة *' : 'Unit *'} required>
                <select className="form-select" required value={form.unitLookupId} onChange={e => setForm({ ...form, unitLookupId: e.target.value })}>
                  <option value="">{ar ? 'اختر الوحدة' : 'Select unit'}</option>
                  {units.map(u => <option key={u.id} value={u.id}>{u.name}</option>)}
                </select>
              </FormField>
              <FormField label={ar ? 'الحد الأدنى للمخزون' : 'Min Stock'}>
                <Input type="number" value={form.minStockQuantity} onChange={e => setForm({ ...form, minStockQuantity: e.target.value })} />
              </FormField>
              <FormField label={ar ? 'السعر' : 'Unit Price'}>
                <Input type="number" step="0.01" value={form.unitPrice} onChange={e => setForm({ ...form, unitPrice: e.target.value })} />
              </FormField>
              <FormField label={ar ? 'فترة الاستبدال (ساعات)' : 'Replacement Interval (hours)'}>
                <Input type="number" value={form.replacementIntervalHours} onChange={e => setForm({ ...form, replacementIntervalHours: e.target.value })} />
              </FormField>
              <FormField label={ar ? 'أولوية الاستبدال' : 'Replacement Priority'}>
                <select className="form-select" value={form.replacementPriorityLookupId} onChange={e => setForm({ ...form, replacementPriorityLookupId: e.target.value })}>
                  <option value="">{ar ? 'بدون' : 'None'}</option>
                  {priorities.map(p => <option key={p.id} value={p.id}>{p.name}</option>)}
                </select>
              </FormField>
              <div style={{ gridColumn: '1 / -1' }}>
                <label className="form-check">
                  <input type="checkbox" checked={form.isHoursTracked} onChange={e => setForm({ ...form, isHoursTracked: e.target.checked })} />
                  <span>{ar ? 'تتبع بالساعات' : 'Hours Tracked'}</span>
                </label>
              </div>
              <FormField label={ar ? 'رقم القطعة (الصناعي)' : 'Manufacturer Part Number'}>
                <Input value={form.manufacturerPartNumber} onChange={e => setForm({ ...form, manufacturerPartNumber: e.target.value })} />
              </FormField>
              <FormField label={ar ? 'الباركود' : 'Barcode'}>
                <Input value={form.barcode} onChange={e => setForm({ ...form, barcode: e.target.value })} />
              </FormField>
              <FormField label={ar ? 'وصف' : 'Description'}>
                <textarea className="form-control" rows={2} value={form.description} onChange={e => setForm({ ...form, description: e.target.value })} style={{ resize: 'vertical' }} />
              </FormField>
              <FormField label={ar ? 'ملاحظات' : 'Notes'}>
                <textarea className="form-control" rows={2} value={form.notes} onChange={e => setForm({ ...form, notes: e.target.value })} style={{ resize: 'vertical' }} />
              </FormField>
              <div style={{ gridColumn: '1 / -1' }}>
                <label className="form-check">
                  <input type="checkbox" checked={form.isActive} onChange={e => setForm({ ...form, isActive: e.target.checked })} />
                  <span>{ar ? 'فعال' : 'Active'}</span>
                </label>
              </div>
            </div>
          </form>
        </Modal>
      )}

      {detailItem && (
        <Modal
          open={!!detailItem}
          onClose={() => setDetailItem(null)}
          title={
            <span>
              {ar ? 'تفاصيل القطعة' : 'Part Details'} — {detailItem.name}
            </span>
          }
          maxWidth={600}
        >
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12, marginBottom: 20 }}>
            <div><strong>{ar ? 'رقم القطعة' : 'Part Number'}:</strong> {detailItem.partNumber}</div>
            <div><strong>{ar ? 'الفئة' : 'Category'}:</strong> {detailItem.partCategoryName || detailItem.category || '—'}</div>
            <div><strong>{ar ? 'الوحدة' : 'Unit'}:</strong> {detailItem.unitName || detailItem.unit || '—'}</div>
            <div><strong>{ar ? 'الحد الأدنى' : 'Min Stock'}:</strong> {detailItem.minStockQuantity != null ? detailItem.minStockQuantity : detailItem.minStock}</div>
          </div>
          <h4 style={{ marginBottom: 10 }}>{ar ? 'المخزون الحالي حسب الموقع' : 'Current stock by location'}</h4>
          {detailItem.stock?.length === 0 ? (
            <div className="empty-state">{ar ? 'لا يوجد مخزون' : 'No stock'}</div>
          ) : (
            <table className="data-table">
              <thead><tr><th>{ar ? 'الموقع' : 'Station'}</th><th>{ar ? 'الكمية' : 'Qty'}</th></tr></thead>
              <tbody>
                {detailItem.stock?.map((s, i) => (
                  <tr key={i}><td>{s.stationName || s.subStationName || '—'}</td><td>{s.currentQuantity != null ? s.currentQuantity : s.quantity}</td></tr>
                ))}
              </tbody>
            </table>
          )}
        </Modal>
      )}

      <ConfirmModal
        open={confirmDelete.open}
        title={ar ? 'تأكيد الحذف' : 'Confirm Delete'}
        message={ar ? 'هل أنت متأكد من حذف هذه القطعة؟' : 'Are you sure you want to delete this part?'}
        onConfirm={handleDelete}
        onCancel={() => setConfirmDelete({ open: false, id: null })}
        lang={lang}
      />
    </div>
  );
}

export default WarehouseParts;
