import React, { useState, useEffect, useMemo } from 'react';
import { Plus, Trash2, X, Wrench } from 'lucide-react';
import client from '../api/client';
import ConfirmModal from '../components/ConfirmModal';
import Pagination from '../components/Pagination';
import { PageHeader, Button, Modal, FormField, Input } from '../components/ui';

const indicatorCfg = {
  green:  { cls: 'badge-success',  label: { ar: 'جيد', en: 'Good' } },
  yellow: { cls: 'badge-warning',  label: { ar: 'قرب الاستبدال', en: 'Near Replacement' } },
  red:    { cls: 'badge-danger',   label: { ar: 'متأخر', en: 'Overdue' } },
};

const fmt = (v, d = 1) => v != null ? Number(v).toLocaleString('en-US', { maximumFractionDigits: d }) : '—';

const initialForm = {
  generatorId: '',
  sparePartId: '',
  installationDate: '',
  installationHours: '',
  dispatchRequestId: '',
  workPermitId: '',
  notes: '',
};

function InstalledParts({ lang, currentUser }) {
  const ar = lang === 'ar';
  const [parts, setParts] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [showModal, setShowModal] = useState(false);
  const [removeModal, setRemoveModal] = useState({ open: false, id: null, data: null });
  const [form, setForm] = useState(initialForm);
  const [saving, setSaving] = useState(false);
  const [detailItem, setDetailItem] = useState(null);
  const [generators, setGenerators] = useState([]);
  const [spareParts, setSpareParts] = useState([]);
  const [workPermits, setWorkPermits] = useState([]);
  const [dispatchRequests, setDispatchRequests] = useState([]);
  const [stationFilter, setStationFilter] = useState('');
  const [activeTab, setActiveTab] = useState('all');
  const [page, setPage] = useState(1);
  const PAGE_SIZE = 20;

  const perms = currentUser?.permissions?.maintenance || {};
  const canAdd = currentUser?.role === 'admin' || perms.add || perms.create;
  const canRemove = currentUser?.role === 'admin' || perms.remove || perms.delete;

  const load = async () => {
    setLoading(true); setError(null);
    try {
      const params = { _t: Date.now() };
      if (stationFilter) params.generatorId = stationFilter;
      const [partRes, genRes, spRes, wpRes, drRes] = await Promise.allSettled([
        client.get('/api/installed-parts', { params }),
        client.get('/api/generators', { params: { _t: Date.now() } }),
        client.get('/api/spare-parts', { params: { _t: Date.now() } }),
        client.get('/api/work-permits', { params: { _t: Date.now() } }),
        client.get('/api/warehouse/dispatch', { params: { _t: Date.now() } }),
      ]);
      if (partRes.status === 'fulfilled') setParts(partRes.value.data?.items || partRes.value.data || []);
      if (genRes.status === 'fulfilled') setGenerators(genRes.value.data || []);
      if (spRes.status === 'fulfilled') setSpareParts(spRes.value.data?.items || spRes.value.data || []);
      if (wpRes.status === 'fulfilled') setWorkPermits(wpRes.value.data?.items || wpRes.value.data || []);
      if (drRes.status === 'fulfilled') setDispatchRequests(drRes.value.data?.items || drRes.value.data || []);
    } catch { setError(ar ? 'فشل تحميل البيانات' : 'Failed to load data'); }
    finally { setLoading(false); }
  };

  useEffect(() => { load(); }, [stationFilter]);

  const filtered = useMemo(() => {
    if (activeTab === 'red') return parts.filter(p => p.isOverdue);
    if (activeTab === 'yellow') return parts.filter(p => !p.isOverdue && p.hoursUntilReplacement != null && p.hoursUntilReplacement <= (p.replacementIntervalHours || 0) * 0.2);
    return parts;
  }, [parts, activeTab]);

  const paginated = useMemo(() => {
    const start = (page - 1) * PAGE_SIZE;
    return filtered.slice(start, start + PAGE_SIZE);
  }, [filtered, page]);

  useEffect(() => { setPage(1); }, [stationFilter, activeTab]);

  const getIndicator = (part) => {
    const remaining = part.hoursUntilReplacement;
    const interval = part.replacementIntervalHours || 0;
    if (remaining == null) return 'green';
    if (remaining <= 0) return 'red';
    if (remaining <= interval * 0.1) return 'red';
    if (remaining <= interval * 0.2) return 'yellow';
    return 'green';
  };

  const withIndicator = useMemo(() => {
    return paginated.map(p => ({ ...p, indicator: getIndicator(p) }));
  }, [paginated]);

  const openAdd = () => { setForm(initialForm); setShowModal(true); setError(null); };

  const handleSave = async (e) => {
    e.preventDefault(); setSaving(true); setError(null);
    try {
      const payload = {
        generatorId: Number(form.generatorId),
        sparePartId: Number(form.sparePartId),
        installationDate: form.installationDate,
        installationHours: Number(form.installationHours),
        replacementIntervalHours: null,
        workPermitId: form.workPermitId ? Number(form.workPermitId) : null,
         dispatchRequestItemId: form.dispatchRequestId ? Number(form.dispatchRequestId) : null,
        notes: form.notes || null,
      };
      await client.post('/api/installed-parts/install', payload);
      setShowModal(false); load();
    } catch (err) { setError(err.response?.data?.error || err.response?.data?.msg || (ar ? 'فشل الحفظ' : 'Save failed')); }
    finally { setSaving(false); }
  };

  const handleRemove = async () => {
    try {
      await client.post(`/api/installed-parts/${removeModal.id}/remove`, {
        removedAtHours: Number(removeModal.data.hoursAtRemoval) || 0,
        earlyReplacementReason: removeModal.data.earlyReason || null,
        notes: removeModal.data.notes || null,
      });
      setRemoveModal({ open: false, id: null, data: null }); load();
    } catch { setError(ar ? 'فشل الإزالة' : 'Remove failed'); }
  };

  return (
    <div style={{ animation: 'fadeIn 0.4s ease' }}>
      <PageHeader
        title={
          <span style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
            <Wrench size={24} style={{ color: 'var(--primary)' }} />
            {ar ? 'القطع المركبة على المولدات' : 'Installed Parts on Generators'}
          </span>
        }
        subtitle={ar ? 'تتبع القطع المركبة وحالة الاستبدال' : 'Track installed parts and replacement status'}
        actions={canAdd ? (
          <Button icon={Plus} onClick={openAdd}>{ar ? 'تركيب قطعة' : 'Install Part'}</Button>
        ) : null}
      />

      {error && <div className="alert-inline alert-danger" style={{ marginBottom: 20 }}>{error}</div>}

      <div style={{ display: 'flex', gap: 8, marginBottom: 20 }}>
        <button className={`btn ${activeTab === 'all' ? 'btn-primary' : 'btn-ghost'}`} onClick={() => setActiveTab('all')}>{ar ? 'الكل' : 'All'}</button>
        <button className={`btn ${activeTab === 'red' ? 'btn-primary' : 'btn-ghost'}`} onClick={() => setActiveTab('red')}>{ar ? 'متأخرة' : 'Overdue'}</button>
        <button className={`btn ${activeTab === 'yellow' ? 'btn-primary' : 'btn-ghost'}`} onClick={() => setActiveTab('yellow')}>{ar ? 'قرب الاستبدال' : 'Near Replacement'}</button>
      </div>

      <div className="section-card" style={{ marginBottom: 20, padding: 16, display: 'flex', gap: 12, flexWrap: 'wrap', alignItems: 'center' }}>
        <select className="form-control" value={stationFilter} onChange={e => setStationFilter(e.target.value)} style={{ width: 220 }}>
          <option value="">{ar ? 'كل المولدات' : 'All Generators'}</option>
          {generators.map(g => <option key={g.id} value={g.id}>{g.name}</option>)}
        </select>
      </div>

      {loading ? (
        <div className="ft-spinner" style={{ width: 40, height: 40, margin: '40px auto' }} />
      ) : (
        <>
          <div className="data-table" style={{ overflowX: 'auto' }}>
            <table>
              <thead>
                <tr>
                  <th>{ar ? 'القطعة' : 'Part'}</th>
                  <th>{ar ? 'المولد' : 'Generator'}</th>
                  <th>{ar ? 'تاريخ التركيب' : 'Install Date'}</th>
                  <th>{ar ? 'ساعات التركيب' : 'Install Hours'}</th>
                  <th>{ar ? 'الساعات المتبقية' : 'Remaining Hours'}</th>
                  <th>{ar ? 'الحالة' : 'Status'}</th>
                  <th style={{ textAlign: 'center' }}>{ar ? 'إجراء' : 'Action'}</th>
                </tr>
              </thead>
              <tbody>
                {withIndicator.length === 0 && (
                  <tr><td colSpan={7} style={{ textAlign: 'center', padding: 40, opacity: 0.7 }}>{ar ? 'لا توجد بيانات' : 'No data'}</td></tr>
                )}
                {withIndicator.map(p => {
                  const cfg = indicatorCfg[p.indicator] || indicatorCfg.green;
                  return (
                    <tr key={p.id} style={{ background: p.indicator === 'red' ? 'rgba(239,68,68,0.08)' : p.indicator === 'yellow' ? 'rgba(234,179,8,0.08)' : 'transparent' }}>
                      <td style={{ fontWeight: 700 }}>{p.sparePartName}</td>
                      <td>{p.generatorName}</td>
                      <td>{p.installationDate}</td>
                      <td>{p.installationHours}</td>
                      <td style={{ color: p.indicator === 'red' ? 'var(--danger)' : p.indicator === 'yellow' ? 'var(--warning)' : 'inherit', fontWeight: 700 }}>{p.hoursUntilReplacement != null ? fmt(p.hoursUntilReplacement, 1) : '—'}</td>
                      <td><span className={`badge ${cfg.cls}`}>{cfg.label[lang] || p.indicator}</span></td>
                      <td style={{ textAlign: 'center' }}>
                        {canRemove && (
                          <button className="btn btn-ghost" onClick={() => setRemoveModal({ open: true, id: p.id, data: { hoursAtRemoval: '', earlyReason: '', notes: '' } })}><Trash2 size={16} /></button>
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

      {/* Install Modal */}
      {showModal && (
        <Modal
          open={showModal}
          onClose={() => setShowModal(false)}
          title={ar ? 'تركيب قطعة جديدة' : 'Install New Part'}
          maxWidth={520}
          footer={
            <Button variant="primary" onClick={handleSave} disabled={saving} style={{ width: '100%' }}>
              {saving ? <div className="ft-spinner" style={{ width: 20, height: 20, borderWidth: 2 }} /> : (ar ? 'تركيب' : 'Install')}
            </Button>
          }
        >
          {error && <div className="form-error" role="alert" style={{ marginBottom: 16 }}>{error}</div>}
          <form onSubmit={handleSave}>
            <FormField label={ar ? 'المولد' : 'Generator'} required>
              <select className="form-control" required value={form.generatorId} onChange={e => setForm({ ...form, generatorId: e.target.value })}>
                <option value="">{ar ? 'اختر المولد' : 'Select generator'}</option>
                {generators.map(g => <option key={g.id} value={g.id}>{g.name}</option>)}
              </select>
            </FormField>
            <FormField label={ar ? 'القطعة' : 'Part'} required>
              <select className="form-control" required value={form.sparePartId} onChange={e => setForm({ ...form, sparePartId: e.target.value })}>
                <option value="">{ar ? 'اختر القطعة' : 'Select part'}</option>
                {spareParts.map(p => <option key={p.id} value={p.id}>{p.name}</option>)}
              </select>
            </FormField>
            <FormField label={ar ? 'تاريخ التركيب' : 'Installation Date'} required>
              <Input type="date" required value={form.installationDate} onChange={e => setForm({ ...form, installationDate: e.target.value })} />
            </FormField>
            <FormField label={ar ? 'ساعات المولد الحالية' : 'Current Generator Hours'}>
              <Input type="number" value={form.installationHours} onChange={e => setForm({ ...form, installationHours: e.target.value })} />
            </FormField>
            <FormField label={ar ? 'طلب صرف (اختياري)' : 'Dispatch Request (optional)'}>
              <select className="form-control" value={form.dispatchRequestId} onChange={e => setForm({ ...form, dispatchRequestId: e.target.value })}>
                <option value="">{ar ? 'بدون' : 'None'}</option>
                {dispatchRequests.map(d => <option key={d.id} value={d.id}>{d.requestNumber || d.id}</option>)}
              </select>
            </FormField>
            <FormField label={ar ? 'تصريح عمل (اختياري)' : 'Work Permit (optional)'}>
              <select className="form-control" value={form.workPermitId} onChange={e => setForm({ ...form, workPermitId: e.target.value })}>
                <option value="">{ar ? 'بدون' : 'None'}</option>
                {workPermits.map(w => <option key={w.id} value={w.id}>{w.permitNumber} — {w.problemDescription}</option>)}
              </select>
            </FormField>
            <FormField label={ar ? 'ملاحظات' : 'Notes'}>
              <textarea className="form-control" rows={2} value={form.notes} onChange={e => setForm({ ...form, notes: e.target.value })} />
            </FormField>
          </form>
        </Modal>
      )}

      {/* Remove Modal */}
      {removeModal.open && (
        <Modal
          open={removeModal.open}
          onClose={() => setRemoveModal({ open: false, id: null, data: null })}
          title={ar ? 'إزالة قطعة' : 'Remove Part'}
          maxWidth={420}
          footer={
            <Button variant="primary" onClick={handleRemove} style={{ width: '100%' }}>
              {ar ? 'تأكيد الإزالة' : 'Confirm Removal'}
            </Button>
          }
        >
          <FormField label={ar ? 'ساعات المولد عند الإزالة' : 'Generator Hours at Removal'} required>
            <Input type="number" required
              value={removeModal.data?.hoursAtRemoval || ''}
              onChange={e => setRemoveModal({ ...removeModal, data: { ...removeModal.data, hoursAtRemoval: e.target.value } })} />
          </FormField>
          <FormField label={ar ? 'سبب الاستبدال المبكر (إجباري إذا قبل الوقت)' : 'Early Replacement Reason (mandatory if early)'}>
            <textarea className="form-control" rows={2}
              value={removeModal.data?.earlyReason || ''}
              onChange={e => setRemoveModal({ ...removeModal, data: { ...removeModal.data, earlyReason: e.target.value } })} />
          </FormField>
          <FormField label={ar ? 'ملاحظات' : 'Notes'}>
            <textarea className="form-control" rows={2}
              value={removeModal.data?.notes || ''}
              onChange={e => setRemoveModal({ ...removeModal, data: { ...removeModal.data, notes: e.target.value } })} />
          </FormField>
        </Modal>
      )}

      <ConfirmModal
        open={false}
        title={ar ? 'تأكيد الحذف' : 'Confirm Delete'}
        message={ar ? 'هل أنت متأكد من حذف هذا السجل؟' : 'Are you sure you want to delete this record?'}
        onConfirm={() => {}}
        onCancel={() => {}}
        lang={lang}
      />
    </div>
  );
}

export default InstalledParts;
