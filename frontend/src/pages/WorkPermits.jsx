import React, { useState, useEffect, useMemo } from 'react';
import { Plus, Edit2, Trash2, Search, FileText, Wrench, ChevronDown, ShieldCheck, Check, Save, RefreshCw, Printer, XCircle } from 'lucide-react';
import client from '../api/client';
import ConfirmModal from '../components/ConfirmModal';
import Pagination from '../components/Pagination';
import { PageHeader, Button, Modal, FormField, Input } from '../components/ui';
import useCommercialMode from '../hooks/useCommercialMode';
import { groupSitesByStation } from '../utils/stationSites';

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
  active:    { cls: 'badge-primary', label: { ar: 'نشط', en: 'Active' } },
  completed: { cls: 'badge-success',  label: { ar: 'مكتمل', en: 'Completed' } },
  rejected:  { cls: 'badge-danger',   label: { ar: 'مرفوض', en: 'Rejected' } },
  cancelled: { cls: 'badge-secondary', label: { ar: 'ملغى', en: 'Cancelled' } },
};

const today = () => new Date().toISOString().slice(0, 10);

function responseList(data) {
  if (Array.isArray(data)) return data;
  if (Array.isArray(data?.items)) return data.items;
  if (Array.isArray(data?.data)) return data.data;
  throw new Error('Unexpected list response');
}

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

const PAGE_SIZE = 20;

export function WorkPermits({ lang = 'ar', currentUser = {} }) {
  const ar = lang === 'ar';
  const { isCommercialMode, modeResolved } = useCommercialMode();

  const [permits, setPermits] = useState([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState(null);

  const [subStations, setSubStations] = useState([]);
  const [generators, setGenerators] = useState([]);
  const [maintenanceTypes, setMaintenanceTypes] = useState([]);
  const [departments, setDepartments] = useState([]);
  const [priorities, setPriorities] = useState([]);
  const [statuses, setStatuses] = useState([]);
  const [lookupsLoading, setLookupsLoading] = useState(false);

  const [statusFilter, setStatusFilter] = useState('');
  const [priorityFilter, setPriorityFilter] = useState('');

  const [showModal, setShowModal] = useState(false);
  const [editId, setEditId] = useState(null);
  const [form, setForm] = useState(initialForm);

  const [detailItem, setDetailItem] = useState(null);
  const [sheetData, setSheetData] = useState(null);
  const [rejectModal, setRejectModal] = useState({ open: false, id: null, reason: '' });
  const [confirmDelete, setConfirmDelete] = useState({ open: false, id: null });

  const role = currentUser?.role || '';
  const perms = currentUser?.permissions?.maintenance || {};
  const isAdmin = role === 'admin' || role === 'systemowner';
  const isManager = isAdmin || role === 'manager' || role === 'operationsmanager';
  const canAdd = isAdmin || isManager || perms.create || perms.add;
  const canEdit = isAdmin || isManager || perms.update || perms.edit;
  const canDelete = isAdmin || perms.delete;
  const canApprove = isAdmin || isManager || perms.approve || perms.manage;

  const kpis = useMemo(() => {
    let open = 0, critical = 0, completed = 0, approved = 0;
    for (const p of permits) {
      if (p.status !== 'completed' && p.status !== 'cancelled') open++;
      if (p.priority === 'critical' || p.priority === 'emergency') critical++;
      if (p.status === 'completed') completed++;
      if (p.status === 'approved') approved++;
    }
    return { open, critical, completed, approved };
  }, [permits]);

  const paginated = permits;
  const stationGroups = useMemo(() => groupSitesByStation(subStations), [subStations]);
  const selectedStation = stationGroups.find(group => group.sites.some(site => String(site.id) === form.subStationId));

  const loadLookups = async () => {
    setLookupsLoading(true);
    try {
      const [subRes, mtRes, depRes, priRes, statusRes] = await Promise.allSettled([
        isCommercialMode ? Promise.resolve({ data: [] }) : client.get('/api/sub-stations', { params: { _t: Date.now() } }),
        client.get('/api/lookups/by-category/MaintenanceType'),
        client.get('/api/lookups/by-category/MaintenanceDepartment'),
        client.get('/api/lookups/by-category/MaintenancePriority'),
        client.get('/api/lookups/by-category/MaintenanceStatus'),
      ]);
      if (!isCommercialMode && subRes.status === 'fulfilled') setSubStations(responseList(subRes.value.data));
      if (mtRes.status === 'fulfilled') setMaintenanceTypes(responseList(mtRes.value.data));
      if (depRes.status === 'fulfilled') setDepartments(responseList(depRes.value.data));
      if (priRes.status === 'fulfilled') setPriorities(responseList(priRes.value.data));
      if (statusRes.status === 'fulfilled') setStatuses(responseList(statusRes.value.data));
      if ([subRes, mtRes, depRes, priRes, statusRes].some(result => result.status === 'rejected'))
        setError(ar ? 'تعذر تحميل بعض بيانات النموذج. حاول مجدداً.' : 'Some form data could not be loaded. Please try again.');
    } catch { setError(ar ? 'تعذر تحميل بيانات النموذج' : 'Failed to load form data'); }
    finally { setLookupsLoading(false); }
  };

  const load = async () => {
    setLoading(true); setError(null);
    try {
      const params = { _t: Date.now(), page, pageSize: PAGE_SIZE };
      if (statusFilter) params.status = statusFilter;
      if (priorityFilter) params.priority = priorityFilter;
      const [wpRes, genRes] = await Promise.allSettled([
        client.get('/api/work-permits', { params }),
        client.get('/api/generators', { params: { _t: Date.now() } }),
      ]);
      if (wpRes.status === 'fulfilled') {
        const data = wpRes.value.data;
        const items = responseList(data).map(p => ({ ...p, status: p.status?.toLowerCase(), priority: p.priority?.toLowerCase() }));
        setPermits(items);
        setTotal(data?.totalCount ?? items.length);
      }
      if (genRes.status === 'fulfilled') {
        const items = responseList(genRes.value.data);
        setGenerators(items);
      }
      if ([wpRes, genRes].some(result => result.status === 'rejected'))
        setError(ar ? 'تعذر تحميل بعض البيانات. حاول مجدداً.' : 'Some data could not be loaded. Please try again.');
    } catch { setError(ar ? 'فشل تحميل البيانات' : 'Failed to load data'); }
    finally { setLoading(false); }
  };

  useEffect(() => { if (modeResolved) load(); }, [statusFilter, priorityFilter, page, isCommercialMode, modeResolved]); // eslint-disable-line
  useEffect(() => { if (modeResolved) loadLookups(); }, [isCommercialMode, modeResolved]); // eslint-disable-line

  const openDetail = async (p) => {
    try { const res = await client.get(`/api/work-permits/${p.id}`); setDetailItem(res.data); }
    catch { setError(ar ? 'فشل تحميل التصريح' : 'Failed to load permit'); }
  };

  const openSheet = async (id) => {
    try {
      const res = await client.get(`/api/work-permits/${id}/sheet`);
      setSheetData(res.data);
    } catch {
      setError(ar ? 'فشل تحميل سند التصريح' : 'Failed to load permit sheet');
    }
  };

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

  const mapFormToCreateDto = () => {
    const dto = {
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
    };
    if (!isCommercialMode && form.subStationId) {
      dto.subStationId = Number(form.subStationId);
    }
    return dto;
  };

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
    } catch (err) { setError(err.response?.data?.error || err.response?.data?.msg || (ar ? 'فشل الحفظ' : 'Save failed')); }
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
      const lookup = statuses.find(s => s.code?.toLowerCase() === status);
      if (!lookup) throw new Error('Status not found');
      await client.post(`/api/work-permits/${id}/status`, { statusLookupId: lookup.id });
      load();
    } catch (err) { setError(err.response?.data?.error || (ar ? 'فشل تحديث الحالة' : 'Status update failed')); }
  };

  const handleReject = async () => {
    if (!rejectModal.reason?.trim()) return;
    try {
      await client.post(`/api/work-permits/${rejectModal.id}/reject`, { reason: rejectModal.reason });
      setRejectModal({ open: false, id: null, reason: '' });
      load();
    } catch (err) {
      setError(err.response?.data?.error || (ar ? 'فشل رفض التصريح' : 'Failed to reject permit'));
    }
  };

  const fld = (k, v) => setForm(f => ({ ...f, [k]: v }));

  return (
    <div>
      <PageHeader
        title={
          <span style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
            <FileText size={24} style={{ color: 'var(--primary)' }} />
            {ar ? 'تصاريح العمل' : 'Work Permits'}
          </span>
        }
        subtitle={ar ? 'إدارة تصاريح عمل الصيانة — سير العمل وسندات التصاريح' : 'Manage maintenance work permits — workflow and permit sheets'}
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
          <select className="form-select" value={statusFilter} onChange={e => { setPage(1); setStatusFilter(e.target.value); }}>
            <option value="">{ar ? 'كل الحالات' : 'All Statuses'}</option>
            {statuses.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
          </select>
        </FormField>
        <FormField label={ar ? 'الأولوية' : 'Priority'} style={{ minWidth: 160 }}>
          <select className="form-select" value={priorityFilter} onChange={e => { setPage(1); setPriorityFilter(e.target.value); }}>
            <option value="">{ar ? 'كل الأولويات' : 'All Priorities'}</option>
            {priorities.map(p => <option key={p.id} value={p.id}>{p.name}</option>)}
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
                        <div style={{ display: 'flex', gap: 4, justifyContent: 'center', alignItems: 'center' }}>
                          <Button variant="ghost" size="sm" icon={Search} onClick={() => openDetail(p)} title={ar ? 'تفاصيل' : 'Details'} />
                          <Button variant="ghost" size="sm" icon={Printer} onClick={() => openSheet(p.id)} title={ar ? 'سند التصريح' : 'Permit Sheet'} />
                          {canEdit && p.status === 'draft' && <Button variant="ghost" size="sm" icon={Edit2} onClick={() => openEdit(p)} />}
                          {canDelete && p.status === 'draft' && <Button variant="ghost" size="sm" icon={Trash2} onClick={() => setConfirmDelete({ open: true, id: p.id })} />}
                          {canEdit && p.status === 'draft' && <Button variant="primary" size="sm" onClick={() => updateStatus(p.id, 'submitted')}>{ar ? 'تقديم' : 'Submit'}</Button>}
                          {p.status === 'submitted' && canApprove && (
                            <>
                              <Button variant="success" size="sm" onClick={() => updateStatus(p.id, 'approved')}>{ar ? 'اعتماد' : 'Approve'}</Button>
                              <Button variant="danger" size="sm" onClick={() => setRejectModal({ open: true, id: p.id, reason: '' })}>{ar ? 'رفض' : 'Reject'}</Button>
                            </>
                          )}
                          {p.status === 'approved' && canEdit && <Button variant="primary" size="sm" onClick={() => updateStatus(p.id, 'active')}>{ar ? 'بدء العمل' : 'Start Work'}</Button>}
                          {p.status === 'active' && canEdit && <Button variant="success" size="sm" onClick={() => updateStatus(p.id, 'completed')}>{ar ? 'إكمال' : 'Complete'}</Button>}
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
          <Pagination page={page} onPageChange={setPage} totalPages={Math.ceil(total / PAGE_SIZE)} total={total} pageSize={PAGE_SIZE} lang={lang} />
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
              {!isCommercialMode && (
                <FormField label={ar ? 'المحطة' : 'Station'} required>
                  <select className="form-select" value={selectedStation?.id || ''} onChange={e => {
                    const group = stationGroups.find(item => String(item.id) === e.target.value);
                    setForm(f => ({ ...f, subStationId: group ? String(group.sites[0].id) : '', generatorId: '' }));
                  }} required disabled={!!editId}>
                    <option value="">{ar ? 'اختر المحطة' : 'Select station'}</option>
                    {stationGroups.map(group => <option key={group.id} value={group.id}>{group.name || (ar ? 'محطة' : 'Station')}</option>)}
                  </select>
                </FormField>
              )}
              <FormField label={ar ? 'المولد' : 'Generator'}>
                <select className="form-select" value={form.generatorId} onChange={e => {
                  const generator = generators.find(item => String(item.id) === e.target.value);
                  setForm(f => ({ ...f, generatorId: e.target.value, subStationId: !isCommercialMode && generator?.subStationId ? String(generator.subStationId) : f.subStationId }));
                }} disabled={form.isGeneral || !!editId}>
                  <option value="">{ar ? 'اختر المولد' : 'Select generator'}</option>
                  {generators.filter(g => !selectedStation || selectedStation.sites.some(site => String(site.id) === String(g.subStationId))).map(g => <option key={g.id} value={g.id}>{g.name}</option>)}
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
          footer={
            <div style={{ display: 'flex', justifyContent: 'space-between', width: '100%' }}>
              <Button variant="ghost" size="sm" icon={Printer} onClick={() => openSheet(detailItem.id)}>
                {ar ? 'سند التصريح' : 'Permit Sheet'}
              </Button>
              <Button variant="ghost" onClick={() => setDetailItem(null)}>{ar ? 'إغلاق' : 'Close'}</Button>
            </div>
          }
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

      {/* Sheet / Printable Modal */}
      {sheetData && (
        <Modal
          open={!!sheetData}
          onClose={() => setSheetData(null)}
          title={<>{ar ? 'سند تصريح العمل' : 'Work Permit Sheet'} — {sheetData.permitNumber}</>}
          maxWidth={800}
          footer={
            <div style={{ display: 'flex', justifyContent: 'space-between', width: '100%' }}>
              <Button variant="primary" icon={Printer} onClick={() => window.print()}>
                {ar ? 'طباعة' : 'Print'}
              </Button>
              <Button variant="ghost" onClick={() => setSheetData(null)}>{ar ? 'إغلاق' : 'Close'}</Button>
            </div>
          }
        >
          <div className="permit-sheet-content" style={{ padding: '8px 4px' }}>
            {/* Header info */}
            <div style={{ borderBottom: '2px solid var(--border)', paddingBottom: 12, marginBottom: 16 }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <h3 style={{ margin: 0 }}>{sheetData.stationName}</h3>
                <span style={{ fontSize: 18, fontWeight: 700, color: 'var(--primary)' }}>{sheetData.permitNumber}</span>
              </div>
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))', gap: 8, marginTop: 12, fontSize: 14 }}>
                <div><strong>{ar ? 'تاريخ التصريح:' : 'Permit Date:'}</strong> {new Date(sheetData.permitDate).toLocaleDateString()}</div>
                <div><strong>{ar ? 'نوع الصيانة:' : 'Maintenance Type:'}</strong> {sheetData.maintenanceTypeName}</div>
                <div><strong>{ar ? 'الأولوية:' : 'Priority:'}</strong> {sheetData.priorityName}</div>
                <div><strong>{ar ? 'الحالة:' : 'Status:'}</strong> {sheetData.statusName}</div>
                <div><strong>{ar ? 'المولد:' : 'Generator:'}</strong> {sheetData.generatorName || (ar ? 'صيانة عامة' : 'General Maintenance')}</div>
                <div><strong>{ar ? 'القسم:' : 'Department:'}</strong> {sheetData.departmentName}</div>
              </div>
            </div>

            {/* Description */}
            <div style={{ marginBottom: 16 }}>
              <div style={{ fontWeight: 600, marginBottom: 4 }}>{ar ? 'وصف المشكلة والسبب:' : 'Problem Description & Cause:'}</div>
              <div style={{ background: 'var(--bg-muted, #f8f9fa)', padding: 10, borderRadius: 6, fontSize: 14 }}>
                <div>{sheetData.problemDescription}</div>
                {sheetData.problemCause && <div style={{ marginTop: 4, opacity: 0.85 }}><strong>{ar ? 'السبب:' : 'Cause:'}</strong> {sheetData.problemCause}</div>}
              </div>
            </div>

            {/* Roles and workflow actors */}
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: 12, marginBottom: 16, background: 'var(--bg-card)', border: '1px solid var(--border)', padding: 12, borderRadius: 6 }}>
              <div>
                <div style={{ fontSize: 12, opacity: 0.7 }}>{ar ? 'مقدم الطلب (المشغل)' : 'Requester (Operator)'}</div>
                <div style={{ fontWeight: 600 }}>{sheetData.requesterName}</div>
              </div>
              <div>
                <div style={{ fontSize: 12, opacity: 0.7 }}>{ar ? 'المعتمد (مدير العمليات)' : 'Approver (Operations Manager)'}</div>
                <div style={{ fontWeight: 600 }}>{sheetData.approverName || '—'}</div>
                {sheetData.approvedAt && <div style={{ fontSize: 11, opacity: 0.7 }}>{new Date(sheetData.approvedAt).toLocaleDateString()}</div>}
              </div>
              <div>
                <div style={{ fontSize: 12, opacity: 0.7 }}>{ar ? 'المسند إليه (فريق الصيانة)' : 'Assigned To (Maintenance)'}</div>
                <div style={{ fontWeight: 600 }}>{sheetData.assignedToName || '—'}</div>
              </div>
            </div>

            {/* Tasks section */}
            <div style={{ marginBottom: 16 }}>
              <div style={{ fontWeight: 600, marginBottom: 6 }}>{ar ? 'المهام المنجزة' : 'Tasks'}</div>
              {sheetData.tasks?.length === 0 ? (
                <div style={{ opacity: 0.6, fontSize: 13 }}>{ar ? 'لا توجد مهام مسجلة' : 'No tasks recorded'}</div>
              ) : (
                <table style={{ width: '100%', fontSize: 13, borderCollapse: 'collapse' }}>
                  <thead>
                    <tr style={{ borderBottom: '1px solid var(--border)' }}>
                      <th style={{ textAlign: ar ? 'right' : 'left', padding: '6px 8px' }}>{ar ? 'المهمة' : 'Task'}</th>
                      <th style={{ textAlign: 'center', padding: '6px 8px', width: 100 }}>{ar ? 'الحالة' : 'Status'}</th>
                    </tr>
                  </thead>
                  <tbody>
                    {sheetData.tasks.map((t) => (
                      <tr key={t.id} style={{ borderBottom: '1px solid var(--border)' }}>
                        <td style={{ padding: '6px 8px' }}>{t.taskDescription}</td>
                        <td style={{ textAlign: 'center', padding: '6px 8px' }}>
                          {t.isCompleted ? (ar ? 'مكتمل' : 'Completed') : (ar ? 'قيد التنفيذ' : 'Pending')}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </div>

            {/* Parts Dispatched */}
            <div style={{ marginBottom: 16 }}>
              <div style={{ fontWeight: 600, marginBottom: 6 }}>{ar ? 'القطع المصروفة' : 'Parts Dispatched'}</div>
              {sheetData.dispatchedParts?.length === 0 ? (
                <div style={{ opacity: 0.6, fontSize: 13 }}>{ar ? 'لا توجد قطع مصروفة' : 'No parts dispatched'}</div>
              ) : (
                <table style={{ width: '100%', fontSize: 13, borderCollapse: 'collapse' }}>
                  <thead>
                    <tr style={{ borderBottom: '1px solid var(--border)' }}>
                      <th style={{ textAlign: ar ? 'right' : 'left', padding: '6px 8px' }}>{ar ? 'القطعة' : 'Part'}</th>
                      <th style={{ textAlign: 'center', padding: '6px 8px', width: 90 }}>{ar ? 'الكمية المطلوبة' : 'Requested'}</th>
                      <th style={{ textAlign: 'center', padding: '6px 8px', width: 90 }}>{ar ? 'الكمية المعتمدة' : 'Approved'}</th>
                    </tr>
                  </thead>
                  <tbody>
                    {sheetData.dispatchedParts.map((p, idx) => (
                      <tr key={idx} style={{ borderBottom: '1px solid var(--border)' }}>
                        <td style={{ padding: '6px 8px' }}>{p.partName} ({p.partNumber})</td>
                        <td style={{ textAlign: 'center', padding: '6px 8px' }}>{p.quantityRequested} {p.unitOfMeasure || ''}</td>
                        <td style={{ textAlign: 'center', padding: '6px 8px' }}>{p.quantityApproved ?? '—'} {p.unitOfMeasure || ''}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </div>

            {/* Replacements Recorded & Oil Consumed (C6.2 Link) */}
            <div style={{ marginBottom: 16 }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 6 }}>
                <div style={{ fontWeight: 600 }}>{ar ? 'استبدالات الصيانة واستهلاك الزيت' : 'Service Replacements & Oil Consumption'}</div>
                {sheetData.totalOilConsumedLitres > 0 && (
                  <span style={{ fontSize: 13, fontWeight: 700, color: 'var(--primary)' }}>
                    {ar ? `إجمالي الزيت: ${sheetData.totalOilConsumedLitres} لتر` : `Total Oil: ${sheetData.totalOilConsumedLitres} L`}
                  </span>
                )}
              </div>
              {sheetData.replacements?.length === 0 ? (
                <div style={{ opacity: 0.6, fontSize: 13 }}>{ar ? 'لا توجد استبدالات مسجلة' : 'No replacements recorded'}</div>
              ) : (
                <table style={{ width: '100%', fontSize: 13, borderCollapse: 'collapse' }}>
                  <thead>
                    <tr style={{ borderBottom: '1px solid var(--border)' }}>
                      <th style={{ textAlign: ar ? 'right' : 'left', padding: '6px 8px' }}>{ar ? 'البند' : 'Item'}</th>
                      <th style={{ textAlign: 'center', padding: '6px 8px', width: 100 }}>{ar ? 'ساعات التشغيل' : 'Hours'}</th>
                      <th style={{ textAlign: 'center', padding: '6px 8px', width: 90 }}>{ar ? 'النوع' : 'Kind'}</th>
                      <th style={{ textAlign: 'center', padding: '6px 8px', width: 120 }}>{ar ? 'الزيت المستهلك' : 'Oil Consumed'}</th>
                    </tr>
                  </thead>
                  <tbody>
                    {sheetData.replacements.map((r) => (
                      <tr key={r.id} style={{ borderBottom: '1px solid var(--border)' }}>
                        <td style={{ padding: '6px 8px' }}>{r.serviceItemName}</td>
                        <td style={{ textAlign: 'center', padding: '6px 8px' }}>{r.hoursAtService}</td>
                        <td style={{ textAlign: 'center', padding: '6px 8px' }}>{r.kind}</td>
                        <td style={{ textAlign: 'center', padding: '6px 8px' }}>
                          {r.oilConsumedLitres > 0 ? `${r.oilConsumedLitres} L ${r.oilTypeName ? `(${r.oilTypeName})` : ''}` : '—'}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </div>
          </div>
        </Modal>
      )}

      {/* Reject Modal */}
      {rejectModal.open && (
        <Modal
          open={rejectModal.open}
          onClose={() => setRejectModal({ open: false, id: null, reason: '' })}
          title={ar ? 'رفض تصريح العمل' : 'Reject Work Permit'}
          maxWidth={500}
          footer={
            <>
              <Button variant="ghost" onClick={() => setRejectModal({ open: false, id: null, reason: '' })}>{ar ? 'إلغاء' : 'Cancel'}</Button>
              <Button variant="danger" onClick={handleReject} disabled={!rejectModal.reason?.trim()}>
                {ar ? 'تأكيد الرفض' : 'Confirm Rejection'}
              </Button>
            </>
          }
        >
          <FormField label={ar ? 'سبب الرفض' : 'Rejection Reason'} required>
            <textarea
              className="form-control"
              rows={3}
              value={rejectModal.reason}
              onChange={e => setRejectModal(m => ({ ...m, reason: e.target.value }))}
              placeholder={ar ? 'أدخل سبب الرفض بالتفصيل...' : 'Enter rejection reason in detail...'}
              style={{ resize: 'vertical', fontFamily: 'inherit' }}
            />
          </FormField>
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
