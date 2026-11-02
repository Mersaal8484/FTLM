import React, { useState, useEffect, useMemo } from 'react';
import { Plus, Edit2, Trash2, Search, FileText, Wrench, ChevronDown, ShieldCheck, Check, Save, RefreshCw } from 'lucide-react';
import client from '../api/client';
import ConfirmModal from '../components/ConfirmModal';
import Pagination from '../components/Pagination';
import { PageHeader, Button, Modal, FormField, Input } from '../components/ui';

const priorityCfg = {
  low:      { cls: 'badge-secondary', label: { ar: 'عادي', en: 'Normal' } },
  normal:   { cls: 'badge-warning',  label: { ar: 'متوسط', en: 'Normal' } },
  high:     { cls: 'badge-primary',  label: { ar: 'مرتفع', en: 'High' } },
  critical: { cls: 'badge-danger',   label: { ar: 'حرج', en: 'Critical' } },
  emergency:{ cls: 'badge-danger',   label: { ar: 'طوارئ', en: 'Emergency' } },
};

const statusCfg = {
  draft:     { cls: 'badge-secondary', label: { ar: 'مسودة', en: 'Draft' } },
  submitted: { cls: 'badge-warning',  label: { ar: 'مقدم', en: 'Submitted' } },
  approved:  { cls: 'badge-success',  label: { ar: 'معتمد', en: 'Approved' } },
  in_progress:{ cls: 'badge-primary', label: { ar: 'قيد التنفيذ', en: 'In Progress' } },
  completed: { cls: 'badge-success',  label: { ar: 'مكتمل', en: 'Completed' } },
  rejected:  { cls: 'badge-danger',   label: { ar: 'مرفوض', en: 'Rejected' } },
  cancelled: { cls: 'badge-secondary', label: { ar: 'ملغى', en: 'Cancelled' } },
};

const today = () => new Date().toISOString().slice(0, 10);

const initialForm = {
  subStationId: '',
  generatorId: '',
  isGeneral: false,
  maintenanceTypeLookupId: '',
  departmentLookupId: '',
  priorityLookupId: '',
  permitDate: today(),
  problemDescription: '',
  problemCause: '',
  plannedStartDate: '',
  plannedEndDate: '',
  notes: '',
  tasks: [{ description: '', assignedTo: '', sortOrder: 1 }],
};

function WorkPermits({ lang, currentUser }) {
  const ar = lang === 'ar';
  const [permits, setPermits] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [showModal, setShowModal] = useState(false);
  const [editId, setEditId] = useState(null);
  const [form, setForm] = useState(initialForm);
  const [saving, setSaving] = useState(false);
  const [confirmDelete, setConfirmDelete] = useState({ open: false, id: null });
  const [detailItem, setDetailItem] = useState(null);
  const [generators, setGenerators] = useState([]);
  const [subStations, setSubStations] = useState([]);
  const [maintenanceTypes, setMaintenanceTypes] = useState([]);
  const [departments, setDepartments] = useState([]);
  const [priorities, setPriorities] = useState([]);
  const [lookupsLoading, setLookupsLoading] = useState(true);
  const [statusFilter, setStatusFilter] = useState('');
  const [priorityFilter, setPriorityFilter] = useState('');
  const [page, setPage] = useState(1);
  const PAGE_SIZE = 20;

  const perms = currentUser?.permissions?.maintenance || {};
  const canAdd = currentUser?.role === 'admin' || perms.add || perms.create;
  const canEdit = currentUser?.role === 'admin' || perms.edit;
  const canDelete = currentUser?.role === 'admin' || perms.delete;

  const kpis = useMemo(() => {
    const open = permits.filter(p => !['completed', 'cancelled', 'rejected'].includes(p.status)).length;
    const critical = permits.filter(p => p.priority === 'critical' || p.priority === 'emergency').length;
    const completed = permits.filter(p => p.status === 'completed').length;
    const approved = permits.filter(p => p.status === 'approved').length;
    return { open, critical, completed, approved };
  }, [permits]);

  const filtered = permits;
  const paginated = useMemo(() => {
    const start = (page - 1) * PAGE_SIZE;
    return filtered.slice(start, start + PAGE_SIZE);
  }, [filtered, page]);

  const loadLookups = async () => {
    setLookupsLoading(true);
    try {
      const [subRes, mtRes, depRes, priRes] = await Promise.allSettled([
        client.get('/api/sub-stations', { params: { _t: Date.now() } }),
        client.get('/api/lookups/by-category/MaintenanceType'),
        client.get('/api/lookups/by-category/MaintenanceDepartment'),
        client.get('/api/lookups/by-category/MaintenancePriority'),
      ]);
      if (subRes.status === 'fulfilled') setSubStations(subRes.value.data || []);
      if (mtRes.status === 'fulfilled') setMaintenanceTypes(mtRes.value.data || []);
      if (depRes.status === 'fulfilled') setDepartments(depRes.value.data || []);
      if (priRes.status === 'fulfilled') setPriorities(priRes.value.data || []);
    } catch { /* ignore lookup errors */ }
    finally { setLookupsLoading(false); }
  };

  const load = async () => {
    setLoading(true); setError(null);
    try {
      const params = { _t: Date.now() };
      if (statusFilter) params.status = statusFilter;
      if (priorityFilter) params.priority = priorityFilter;
      const [wpRes, genRes] = await Promise.allSettled([
        client.get('/api/work-permits', { params }),
        client.get('/api/generators', { params: { _t: Date.now() } }),
      ]);
      if (wpRes.status === 'fulfilled') setPermits(wpRes.value.data?.items || wpRes.value.data || []);
      if (genRes.status === 'fulfilled') setGenerators(genRes.value.data || []);
    } catch { setError(ar ? 'فشل تحميل البيانات' : 'Failed to load data'); }
    finally { setLoading(false); }
  };

  useEffect(() => { load(); }, [statusFilter, priorityFilter]); // eslint-disable-line
  useEffect(() => { loadLookups(); }, []); // eslint-disable-line

  const openAdd = () => { setForm(initialForm); setEditId(null); setShowModal(true); setError(null); };
  const openEdit = async (p) => {
    setLoading(true); setError(null);
    try {
      const res = await client.get(`/api/work-permits/${p.id}`);
      const d = res.data;
      setForm({
        subStationId: String(d.subStationId ?? ''),
        generatorId: d.generatorId != null ? String(d.generatorId) : '',
        isGeneral: !d.generatorId,
        maintenanceTypeLookupId: String(d.maintenanceTypeLookupId ?? ''),
        departmentLookupId: String(d.departmentLookupId ?? ''),
        priorityLookupId: String(d.priorityLookupId ?? ''),
        permitDate: d.permitDate ? new Date(d.permitDate).toISOString().slice(0, 10) : today(),
        problemDescription: d.problemDescription || '',
        problemCause: d.problemCause || '',
        plannedStartDate: d.plannedStartDate ? new Date(d.plannedStartDate).toISOString().slice(0, 10) : '',
        plannedEndDate: d.plannedEndDate ? new Date(d.plannedEndDate).toISOString().slice(0, 10) : '',
        notes: d.notes || '',
        tasks: (d.tasks && d.tasks.length > 0)
          ? d.tasks.map(t => ({ description: t.taskDescription || '', assignedTo: '', sortOrder: t.sortOrder || 1 }))
          : [{ description: '', assignedTo: '', sortOrder: 1 }],
      });
      setEditId(p.id);
      setShowModal(true);
    } catch { setError(ar ? 'فشل تحميل التصريح' : 'Failed to load permit'); }
    finally { setLoading(false); }
  };

  const mapFormToCreateDto = () => ({
    subStationId: Number(form.subStationId),
    generatorId: form.isGeneral ? null : (form.generatorId ? Number(form.generatorId) : null),
    maintenanceTypeLookupId: Number(form.maintenanceTypeLookupId),
    departmentLookupId: Number(form.departmentLookupId),
    priorityLookupId: Number(form.priorityLookupId),
    permitDate: form.permitDate ? new Date(form.permitDate).toISOString() : new Date().toISOString(),
    problemDescription: form.problemDescription,
    problemCause: form.problemCause || null,
    plannedStartDate: form.plannedStartDate ? new Date(form.plannedStartDate).toISOString() : null,
    plannedEndDate: form.plannedEndDate ? new Date(form.plannedEndDate).toISOString() : null,
    notes: form.notes || null,
    tasks: form.tasks.map((t, i) => ({ taskDescription: t.description, sortOrder: t.sortOrder || (i + 1) })),
  });

  const mapFormToUpdateDto = () => ({
    id: editId,
    generatorId: form.isGeneral ? null : (form.generatorId ? Number(form.generatorId) : null),
    maintenanceTypeLookupId: form.maintenanceTypeLookupId ? Number(form.maintenanceTypeLookupId) : null,
    departmentLookupId: form.departmentLookupId ? Number(form.departmentLookupId) : null,
    priorityLookupId: form.priorityLookupId ? Number(form.priorityLookupId) : null,
    permitDate: form.permitDate ? new Date(form.permitDate).toISOString() : null,
    problemDescription: form.problemDescription || null,
    problemCause: form.problemCause || null,
    plannedStartDate: form.plannedStartDate ? new Date(form.plannedStartDate).toISOString() : null,
    plannedEndDate: form.plannedEndDate ? new Date(form.plannedEndDate).toISOString() : null,
    notes: form.notes || null,
    tasks: form.tasks.map((t, i) => ({ taskDescription: t.description, sortOrder: t.sortOrder || (i + 1) })),
  });

  const handleSave = async (e) => {
    e.preventDefault(); setSaving(true); setError(null);
    try {
      const payload = editId ? mapFormToUpdateDto() : mapFormToCreateDto();
      if (editId) await client.put(`/api/work-permits/${editId}`, payload);
      else await client.post('/api/work-permits', payload);
      setShowModal(false); load();
    } catch (err) { setError(err.response?.data?.msg || (ar ? 'فشل الحفظ' : 'Save failed')); }
    finally { setSaving(false); }
  };

  const handleDelete = async () => {
    try {
      await client.delete(`/api/work-permits/${confirmDelete.id}`);
      setConfirmDelete({ open: false, id: null }); load();
    } catch { setError(ar ? 'فشل الحذف' : 'Delete failed'); }
  };

  const updateStatus = async (id, status) => {
    try {
      await client.post(`/api/work-permits/${id}/status`, { status });
      load();
    } catch { setError(ar ? 'فشل تحديث الحالة' : 'Status update failed'); }
  };

  const fld = (k, v) => setForm(f => ({ ...f, [k]: v }));

  const selectedPriorityName = priorities.find(p => String(p.id) === form.priorityLookupId)?.name || '';
  const priorityCfgKey = Object.keys(priorityCfg).find(k => priorityCfg[k].label.en === selectedPriorityName) || Object.keys(priorityCfg).find(k => priorityCfg[k].label.ar === selectedPriorityName) || 'normal';

  return (
    <div>
      <PageHeader
        title={
          <span style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
            <FileText size={24} style={{ color: 'var(--primary)' }} />
            {ar ? 'تصاريح العمل' : 'Work Permits'}
          </span>
        }
        subtitle={ar ? 'إدارة تصاريح عمل الصيانة' : 'Manage maintenance work permits'}
        actions={
          <div style={{ display: 'flex', gap: 8 }}>
            <Button variant="ghost" size="sm" icon={RefreshCw} onClick={load} />
            {canAdd && <Button icon={Plus} onClick={openAdd}>{ar ? 'تصريح جديد' : 'New Permit'}</Button>}
          </div>
        }
      />

      {error && <div className="alert-inline alert-inline-danger" style={{ marginBottom: 20 }}>{error}</div>}

      {/* KPI Cards */}
      <div className="kpi-grid" style={{ marginBottom: 28 }}>
        {[
          { label: ar ? 'مفتوحة' : 'Open', value: kpis.open, icon: <FileText size={22} />, colorClass: 'primary' },
          { label: ar ? 'حرجة/طوارئ' : 'Critical/Emergency', value: kpis.critical, icon: <Wrench size={22} />, colorClass: 'danger' },
          { label: ar ? 'مكتملة' : 'Completed', value: kpis.completed, icon: <Check size={22} />, colorClass: 'success' },
          { label: ar ? 'معتمدة' : 'Approved', value: kpis.approved, icon: <ShieldCheck size={22} />, colorClass: 'warning' },
        ].map((s, i) => (
          <div key={i} className="kpi-card" style={{ '--kpi-color': `var(--${s.colorClass})` }}>
            <div className={`kpi-icon ${s.colorClass}`}>{s.icon}</div>
            <div><div className="kpi-value">{s.value}</div><div className="kpi-label">{s.label}</div></div>
          </div>
        ))}
      </div>

      {/* Filters */}
      <div className="section-card" style={{ marginBottom: 20, padding: 16, display: 'flex', gap: 12, flexWrap: 'wrap', alignItems: 'flex-end' }}>
        <FormField label={ar ? 'الحالة' : 'Status'} style={{ minWidth: 160 }}>
          <select className="form-select" value={statusFilter} onChange={e => setStatusFilter(e.target.value)}>
            <option value="">{ar ? 'كل الحالات' : 'All Statuses'}</option>
            {Object.entries(statusCfg).map(([k, v]) => <option key={k} value={k}>{v.label[lang] || k}</option>)}
          </select>
        </FormField>
        <FormField label={ar ? 'الأولوية' : 'Priority'} style={{ minWidth: 160 }}>
          <select className="form-select" value={priorityFilter} onChange={e => setPriorityFilter(e.target.value)}>
            <option value="">{ar ? 'كل الأولويات' : 'All Priorities'}</option>
            {Object.entries(priorityCfg).map(([k, v]) => <option key={k} value={k}>{v.label[lang] || k}</option>)}
          </select>
        </FormField>
        <Button variant="ghost" size="sm" icon={RefreshCw} onClick={load} />
      </div>

      {/* Table */}
      {loading ? (
        <div className="ft-spinner" style={{ width: 40, height: 40, margin: '40px auto' }} />
      ) : (
        <>
          <div className="data-table" style={{ overflowX: 'auto' }}>
            <table>
              <thead>
                <tr>
                  <th>{ar ? 'رقم التصريح' : 'Permit #'}</th>
                  <th>{ar ? 'النوع' : 'Type'}</th>
                  <th>{ar ? 'المولد' : 'Generator'}</th>
                  <th>{ar ? 'الأولوية' : 'Priority'}</th>
                  <th>{ar ? 'الحالة' : 'Status'}</th>
                  <th>{ar ? 'التاريخ' : 'Date'}</th>
                  <th style={{ textAlign: 'center' }}>{ar ? 'إجراءات' : 'Actions'}</th>
                </tr>
              </thead>
              <tbody>
                {paginated.length === 0 && (
                  <tr><td colSpan={7} style={{ textAlign: 'center', padding: 40, opacity: 0.7 }}>{ar ? 'لا توجد بيانات' : 'No data'}</td></tr>
                )}
                {paginated.map(p => {
                  const pCfg = priorityCfg[p.priority] || priorityCfg.normal;
                  const sCfg = statusCfg[p.status] || statusCfg.draft;
                  return (
                    <tr key={p.id}>
                      <td style={{ fontWeight: 700 }}>{p.permitNumber}</td>
                      <td>{p.maintenanceTypeName}</td>
                      <td>{p.generatorName || '—'}</td>
                      <td><span className={`badge ${pCfg.cls}`}>{p.priorityName}</span></td>
                      <td><span className={`badge ${sCfg.cls}`}>{p.statusName}</span></td>
                      <td>{p.plannedStartDate}</td>
                      <td style={{ textAlign: 'center' }}>
                        <Button variant="ghost" size="sm" icon={Search} onClick={() => setDetailItem(p)} title={ar ? 'تفاصيل' : 'Details'} />
                        {canEdit && p.status === 'draft' && <Button variant="ghost" size="sm" icon={Edit2} onClick={() => openEdit(p)} />}
                        {canDelete && p.status === 'draft' && <Button variant="ghost" size="sm" icon={Trash2} onClick={() => setConfirmDelete({ open: true, id: p.id })} />}
                        {p.status === 'submitted' && canAdd && <Button variant="success" size="sm" onClick={() => updateStatus(p.id, 'approved')}>{ar ? 'اعتماد' : 'Approve'}</Button>}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
          <Pagination page={page} setPage={page => setPage(page)} total={filtered.length} pageSize={PAGE_SIZE} />
        </>
      )}

      {/* Add/Edit Modal */}
      {showModal && (
        <Modal
          open={showModal}
          onClose={() => setShowModal(false)}
          title={editId ? (ar ? 'تعديل تصريح' : 'Edit Permit') : (ar ? 'تصريح عمل جديد' : 'New Work Permit')}
          maxWidth={700}
          footer={
            <>
              <Button variant="ghost" onClick={() => setShowModal(false)}>{ar ? 'إلغاء' : 'Cancel'}</Button>
              <Button variant="primary" onClick={handleSave} disabled={saving || lookupsLoading} icon={Save}>
                {saving ? (ar ? 'جارٍ الحفظ...' : 'Saving...') : (ar ? 'حفظ' : 'Save')}
              </Button>
            </>
          }
        >
          {error && <div className="alert-inline alert-inline-danger" style={{ marginBottom: 16 }}>{error}</div>}
          <form onSubmit={handleSave}>
            <div className="form-row">
              <FormField label={ar ? 'الموقع' : 'Sub Station'} required>
                <select className="form-select" value={form.subStationId} onChange={e => fld('subStationId', e.target.value)} required disabled={!!editId}>
                  <option value="">{ar ? 'اختر الموقع' : 'Select sub-station'}</option>
                  {subStations.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
                </select>
              </FormField>
              <FormField label={ar ? 'المولد' : 'Generator'}>
                <select className="form-select" value={form.generatorId} onChange={e => fld('generatorId', e.target.value)} disabled={form.isGeneral || !!editId}>
                  <option value="">{ar ? 'اختر المولد' : 'Select generator'}</option>
                  {generators.map(g => <option key={g.id} value={g.id}>{g.name}</option>)}
                </select>
              </FormField>
            </div>
            <div className="form-row">
              <div className="form-group" style={{ marginTop: 8 }}>
                <label className="checkbox-label">
                  <input type="checkbox" checked={form.isGeneral} onChange={e => fld('isGeneral', e.target.checked)} disabled={!!editId} />
                  <span>{ar ? 'عام (بدون مولد محدد)' : 'General (no specific generator)'}</span>
                </label>
              </div>
              <FormField label={ar ? 'تاريخ التصريح' : 'Permit Date'} required>
                <Input type="date" value={form.permitDate} onChange={e => fld('permitDate', e.target.value)} required />
              </FormField>
            </div>
            <div className="form-row">
              <FormField label={ar ? 'نوع الصيانة' : 'Maintenance Type'} required>
                <select className="form-select" value={form.maintenanceTypeLookupId} onChange={e => fld('maintenanceTypeLookupId', e.target.value)} required>
                  <option value="">{ar ? 'اختر النوع' : 'Select type'}</option>
                  {maintenanceTypes.map(mt => <option key={mt.id} value={mt.id}>{mt.name}</option>)}
                </select>
              </FormField>
              <FormField label={ar ? 'القسم' : 'Department'} required>
                <select className="form-select" value={form.departmentLookupId} onChange={e => fld('departmentLookupId', e.target.value)} required>
                  <option value="">{ar ? 'اختر القسم' : 'Select department'}</option>
                  {departments.map(d => <option key={d.id} value={d.id}>{d.name}</option>)}
                </select>
              </FormField>
            </div>
            <div className="form-row">
              <FormField label={ar ? 'الأولوية' : 'Priority'} required>
                <select className="form-select" value={form.priorityLookupId} onChange={e => fld('priorityLookupId', e.target.value)} required>
                  <option value="">{ar ? 'اختر الأولوية' : 'Select priority'}</option>
                  {priorities.map(pr => <option key={pr.id} value={pr.id}>{pr.name}</option>)}
                </select>
              </FormField>
              <FormField label={ar ? 'تاريخ البداية المخطط' : 'Planned Start'}>
                <Input type="date" value={form.plannedStartDate} onChange={e => fld('plannedStartDate', e.target.value)} />
              </FormField>
            </div>
            <FormField label={ar ? 'تاريخ النهاية المخطط' : 'Planned End'}>
              <Input type="date" value={form.plannedEndDate} onChange={e => fld('plannedEndDate', e.target.value)} />
            </FormField>
            <div className="form-section-header">{ar ? 'الوصف' : 'Description'}</div>
            <FormField label={ar ? 'وصف المشكلة' : 'Problem Description'}>
              <textarea className="form-control" rows={3} value={form.problemDescription} onChange={e => fld('problemDescription', e.target.value)} style={{ resize: 'vertical', fontFamily: 'inherit' }} />
            </FormField>
            <FormField label={ar ? 'السبب الجذري' : 'Root Cause'}>
              <textarea className="form-control" rows={2} value={form.problemCause} onChange={e => fld('problemCause', e.target.value)} style={{ resize: 'vertical', fontFamily: 'inherit' }} />
            </FormField>

            <div className="form-section-header">{ar ? 'المهام' : 'Tasks'}</div>
            {form.tasks.map((t, i) => (
              <div key={i} className="form-row" style={{ marginBottom: 8, alignItems: 'center' }}>
                <div style={{ flex: 2 }}>
                  <Input placeholder={ar ? 'وصف المهمة' : 'Task description'} value={t.description} onChange={e => {
                    const tasks = [...form.tasks]; tasks[i].description = e.target.value; setForm({ ...form, tasks });
                  }} />
                </div>
                <div style={{ flex: 1 }}>
                  <Input placeholder={ar ? 'مسند إلى' : 'Assigned to'} value={t.assignedTo} onChange={e => {
                    const tasks = [...form.tasks]; tasks[i].assignedTo = e.target.value; setForm({ ...form, tasks });
                  }} />
                </div>
                {form.tasks.length > 1 && (
                  <Button variant="ghost" size="sm" icon={Trash2} onClick={() => {
                    const tasks = form.tasks.filter((_, idx) => idx !== i);
                    setForm({ ...form, tasks });
                  }} />
                )}
              </div>
            ))}
            <Button variant="ghost" size="sm" icon={Plus} onClick={() => setForm({ ...form, tasks: [...form.tasks, { description: '', assignedTo: '', sortOrder: form.tasks.length + 1 }] })} style={{ marginBottom: 12 }}>
              {ar ? 'إضافة مهمة' : 'Add task'}
            </Button>

            <div className="form-section-header">{ar ? 'إضافية' : 'Additional'}</div>
            <FormField label={ar ? 'ملاحظات' : 'Notes'}>
              <textarea className="form-control" rows={2} value={form.notes} onChange={e => fld('notes', e.target.value)} style={{ resize: 'vertical', fontFamily: 'inherit' }} />
            </FormField>
          </form>
        </Modal>
      )}

      {/* Detail Modal */}
      {detailItem && (
        <Modal
          open={!!detailItem}
          onClose={() => setDetailItem(null)}
          title={<>{ar ? 'تفاصيل التصريح' : 'Permit Details'}{detailItem ? ` — ${detailItem.permitNumber}` : ''}</>}
          maxWidth={650}
        >
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12, marginBottom: 20 }}>
            <div><strong>{ar ? 'النوع' : 'Type'}:</strong> {detailItem.maintenanceTypeName}</div>
            <div><strong>{ar ? 'المولد' : 'Generator'}:</strong> {detailItem.generatorName || '—'}</div>
            <div><strong>{ar ? 'الأولوية' : 'Priority'}:</strong> {detailItem.priorityName}</div>
            <div><strong>{ar ? 'الحالة' : 'Status'}:</strong> {detailItem.statusName}</div>
            <div><strong>{ar ? 'القسم' : 'Department'}:</strong> {detailItem.departmentName}</div>
            <div><strong>{ar ? 'التاريخ المخطط' : 'Planned'}:</strong> {detailItem.plannedStartDate} → {detailItem.plannedEndDate}</div>
          </div>
          <div style={{ marginBottom: 20 }}>
            <strong>{ar ? 'وصف المشكلة' : 'Problem'}:</strong> {detailItem.problemDescription}
          </div>
          <div style={{ marginBottom: 20 }}>
            <strong>{ar ? 'السبب الجذري' : 'Root Cause'}:</strong> {detailItem.problemCause}
          </div>
          <h4 style={{ marginBottom: 10 }}>{ar ? 'المهام' : 'Tasks'}</h4>
          {detailItem.tasks?.length === 0 ? (
            <div className="empty-state">{ar ? 'لا توجد مهام' : 'No tasks'}</div>
          ) : (
            <div className="data-table" style={{ overflowX: 'auto' }}>
              <table>
                <thead><tr><th>{ar ? 'المهمة' : 'Task'}</th><th>{ar ? 'مسند إلى' : 'Assigned'}</th><th>{ar ? 'الحالة' : 'Status'}</th></tr></thead>
                <tbody>
                  {detailItem.tasks?.map((t, i) => (
                    <tr key={i}><td>{t.taskDescription}</td><td>{t.assignedToName || '—'}</td><td>{t.isCompleted ? (ar ? 'مكتمل' : 'Done') : (ar ? 'قيد التنفيذ' : 'Pending')}</td></tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </Modal>
      )}

      <ConfirmModal
        open={confirmDelete.open}
        title={ar ? 'تأكيد الحذف' : 'Confirm Delete'}
        message={ar ? 'هل أنت متأكد من حذف هذا التصريح؟' : 'Are you sure you want to delete this permit?'}
        onConfirm={handleDelete}
        onCancel={() => setConfirmDelete({ open: false, id: null })}
        danger
        lang={lang}
      />
    </div>
  );
}

export default WorkPermits;
