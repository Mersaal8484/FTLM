import React, { useState, useEffect, useCallback, useMemo } from 'react';
import { Plus, Database, Pencil, Trash2, Save, RefreshCw, Zap, Gauge, AlertCircle, Info } from 'lucide-react';
import client from '../api/client';
import ConfirmModal from '../components/ConfirmModal';
import { PageHeader, Button, Modal, FormField, Input } from '../components/ui';
import useCommercialMode from '../hooks/useCommercialMode';
import { isDailyTank } from '../utils/tankRoles';

const initialDailyTankForm = {
  name: '',
  code: '',
  generatorId: '',
  fuelLookupId: '',
  capacityLiters: '',
  diameterCm: '',
  cylinderHeightCm: '',
  coneHeightCm: '',
  multiplicationFactor: '1.0',
  minAlertLiters: '',
  deadStockLiters: '',
  actualStockLiters: '',
  notes: '',
  isActive: true,
};

const extractErrorMessage = (err, fallback) => {
  const data = err?.response?.data;
  if (!data) return err?.message || fallback;
  if (typeof data === 'string') return data;
  if (data.detail) return data.detail;
  if (data.message) return data.message;
  if (data.error) return data.error;
  if (Array.isArray(data.errors) && data.errors.length > 0) {
    const first = data.errors[0];
    return typeof first === 'string' ? first : first?.message || JSON.stringify(first);
  }
  if (data.errors && typeof data.errors === 'object') {
    const values = Object.values(data.errors);
    if (values.length > 0) {
      const first = values[0];
      if (Array.isArray(first) && first.length > 0) return first[0];
      if (typeof first === 'string') return first;
    }
  }
  if (data.title) return data.title;
  return fallback;
};

export default function DailyTanks({ lang, currentUser }) {
  const ar = lang === 'ar';
  const { isCommercialMode, modeResolved } = useCommercialMode();

  const [tanks, setTanks] = useState([]);
  const [generators, setGenerators] = useState([]);
  const [fuelTypes, setFuelTypes] = useState([]);
  const [subStations, setSubStations] = useState([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [warning, setWarning] = useState('');
  const [formError, setFormError] = useState('');
  const [showModal, setShowModal] = useState(false);
  const [editId, setEditId] = useState(null);
  const [form, setForm] = useState(initialDailyTankForm);
  const [deleteConfirm, setDeleteConfirm] = useState(null);
  const [search, setSearch] = useState('');

  const perms = currentUser?.permissions?.tanks || {};
  const canAdd = currentUser?.role === 'admin' || perms.create || perms.add;
  const canEdit = currentUser?.role === 'admin' || perms.edit;
  const canDelete = currentUser?.role === 'admin' || perms.delete;

  const fetchData = useCallback(async () => {
    setLoading(true);
    setError('');
    setWarning('');
    try {
      const ts = Date.now();
      const responses = await Promise.all([
        client.get('/api/fuel-tanks', { params: { _t: ts } }),
        client.get('/api/generators', { params: { _t: ts } }),
        client.get('/api/lookups/by-category/FuelType', { params: { _t: ts } }).catch(() => ({ data: [] })),
        ...(isCommercialMode ? [] : [client.get('/api/sub-stations', { params: { _t: ts } }).catch(() => ({ data: [] }))]),
      ]);
      const [tRes, gRes, ftRes, ssRes = { data: [] }] = responses;

      const allTanks = Array.isArray(tRes.data?.items) ? tRes.data.items : Array.isArray(tRes.data) ? tRes.data : [];
      const dailyTanks = allTanks.filter(isDailyTank);
      setTanks(dailyTanks);

      const gens = Array.isArray(gRes.data?.data) ? gRes.data.data : Array.isArray(gRes.data) ? gRes.data : [];
      setGenerators(gens);

      setFuelTypes(Array.isArray(ftRes.data) ? ftRes.data : []);
      setSubStations(Array.isArray(ssRes.data) ? ssRes.data.filter(s => s.isActive !== false) : []);
    } catch {
      setError(ar ? 'فشل تحميل بيانات الخزانات اليومية.' : 'Failed to load daily tanks data.');
    } finally {
      setLoading(false);
    }
  }, [ar, isCommercialMode]);

  useEffect(() => {
    if (modeResolved) fetchData();
  }, [fetchData, modeResolved]);

  const resetForm = () => {
    setForm(initialDailyTankForm);
    setEditId(null);
    setFormError('');
    setShowModal(false);
  };

  const openAdd = () => {
    resetForm();
    setShowModal(true);
  };

  const openEdit = (tank) => {
    setForm({
      name: tank.name || '',
      code: tank.code || '',
      generatorId: tank.generatorId != null ? String(tank.generatorId) : (tank.generator?.id != null ? String(tank.generator.id) : ''),
      fuelLookupId: tank.fuelLookupId ? String(tank.fuelLookupId) : '',
      capacityLiters: tank.capacityLiters ?? '',
      diameterCm: tank.diameterCm != null ? String(tank.diameterCm) : '',
      cylinderHeightCm: tank.cylinderHeightCm != null ? String(tank.cylinderHeightCm) : '',
      coneHeightCm: tank.coneHeightCm != null ? String(tank.coneHeightCm) : '',
      multiplicationFactor: tank.multiplicationFactor != null ? String(tank.multiplicationFactor) : '1.0',
      minAlertLiters: tank.minAlertLiters ?? '',
      deadStockLiters: tank.deadStockLiters ?? '',
      actualStockLiters: tank.actualStockLiters ?? '',
      notes: tank.notes || '',
      isActive: tank.isActive !== false,
    });
    setEditId(tank.id);
    setFormError('');
    setShowModal(true);
  };

  const fld = (k, v) => setForm(f => ({ ...f, [k]: v }));

  const validateForm = () => {
    if (!form.name.trim()) {
      setFormError(ar ? 'اسم الخزان اليومي مطلوب.' : 'Daily tank name is required.');
      return false;
    }
    if (!form.generatorId) {
      setFormError(ar ? 'يجب ربط الخزان اليومي بمولد محدد.' : 'Daily tank must be associated with a generator.');
      return false;
    }
    const cap = Number(form.capacityLiters);
    if (isNaN(cap) || cap <= 0) {
      setFormError(ar ? 'سعة الخزان يجب أن تكون أكبر من صفر.' : 'Tank capacity must be greater than zero.');
      return false;
    }
    setFormError('');
    return true;
  };

  const handleSave = async (e) => {
    e?.preventDefault();
    if (!validateForm()) return;

    setSaving(true);
    setFormError('');
    setWarning('');
    try {
      const fuelId = form.fuelLookupId ? Number(form.fuelLookupId) : (fuelTypes[0]?.id || 1);

      const dCm = form.diameterCm ? parseFloat(form.diameterCm) : null;
      const cylCm = form.cylinderHeightCm ? parseFloat(form.cylinderHeightCm) : null;
      const coneCm = form.coneHeightCm ? parseFloat(form.coneHeightCm) : null;
      const factor = form.multiplicationFactor ? parseFloat(form.multiplicationFactor) : 1.0;

      let savedId = editId;
      if (editId) {
        await client.put(`/api/fuel-tanks/${editId}`, {
          name: form.name.trim(),
          code: form.code.trim() || null,
          capacityLiters: parseFloat(form.capacityLiters),
          minAlertLiters: form.minAlertLiters ? parseFloat(form.minAlertLiters) : null,
          deadStockLiters: form.deadStockLiters ? parseFloat(form.deadStockLiters) : null,
          actualStockLiters: form.actualStockLiters ? parseFloat(form.actualStockLiters) : null,
          fuelLookupId: fuelId,
          generatorId: Number(form.generatorId),
          tankRole: 2, // TankRole.Daily
          shape: 0,    // TankShape.VerticalCylinderWithConicalBottom
          diameterCm: dCm,
          cylinderHeightCm: cylCm,
          coneHeightCm: coneCm,
          multiplicationFactor: factor,
          notes: form.notes || null,
          isActive: form.isActive,
        });
      } else {
        const autoCode = form.code.trim() || `DT-${Date.now().toString(36).toUpperCase().slice(-4)}`;
        const payload = {
          fuelLookupId: fuelId,
          name: form.name.trim(),
          code: autoCode,
          capacityLiters: parseFloat(form.capacityLiters),
          minAlertLiters: form.minAlertLiters ? parseFloat(form.minAlertLiters) : null,
          deadStockLiters: form.deadStockLiters ? parseFloat(form.deadStockLiters) : null,
          actualStockLiters: form.actualStockLiters ? parseFloat(form.actualStockLiters) : null,
          generatorId: Number(form.generatorId),
          tankRole: 2, // TankRole.Daily
          shape: 0,    // TankShape.VerticalCylinderWithConicalBottom
          diameterCm: dCm,
          cylinderHeightCm: cylCm,
          coneHeightCm: coneCm,
          multiplicationFactor: factor,
          notes: form.notes || null,
          isActive: form.isActive,
          hasMeter: false,
        };
        if (!isCommercialMode) {
          payload.subStationId = subStations[0]?.id || 1;
        }
        const createRes = await client.post('/api/fuel-tanks', payload);
        savedId = createRes.data?.id;
      }

      let calWarning = '';
      if (savedId && dCm && coneCm) {
        try {
          await client.put(`/api/fuel-tanks/${savedId}/calibration`, {
            diameterCm: dCm,
            cylinderHeightCm: cylCm ?? 0,
            coneHeightCm: coneCm,
            multiplicationFactor: factor,
            deadStockLitres: form.deadStockLiters ? parseFloat(form.deadStockLiters) : null,
            shape: 0,
          });
        } catch (calErr) {
          const reason = extractErrorMessage(calErr, ar ? 'فشل الاتصال' : 'Connection failed');
          calWarning = ar
            ? `تم حفظ الخزان ولكن فشلت المعايرة: ${reason}`
            : `Tank saved, calibration failed: ${reason}`;
        }
      }

      await fetchData();
      resetForm();
      if (calWarning) {
        setWarning(calWarning);
      }
    } catch (err) {
      setFormError(extractErrorMessage(err, ar ? 'فشل حفظ الخزان اليومي.' : 'Failed to save daily tank.'));
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async () => {
    if (!deleteConfirm) return;
    try {
      await client.delete(`/api/fuel-tanks/${deleteConfirm.id}`);
      setTanks(prev => prev.filter(t => t.id !== deleteConfirm.id));
      setDeleteConfirm(null);
    } catch (err) {
      setError(extractErrorMessage(err, ar ? 'فشل حذف الخزان اليومي.' : 'Failed to delete daily tank.'));
    }
  };

  const filtered = useMemo(() => {
    const q = search.toLowerCase();
    return tanks.filter(t =>
      !search ||
      t.name?.toLowerCase().includes(q) ||
      t.code?.toLowerCase().includes(q) ||
      t.generatorName?.toLowerCase().includes(q)
    );
  }, [tanks, search]);

  return (
    <div style={{ maxWidth: 1200, margin: '0 auto', padding: '24px 16px' }}>
      <PageHeader
        title={ar ? 'خزانات الوقود اليومية' : 'Daily Fuel Tanks'}
        subtitle={ar ? 'إدارة الخزانات اليومية الرأسية ذات القاعدة القمعية المرتبطة بالمولدات' : 'Manage vertical cylindrical daily tanks with conical bottom linked to generators'}
        actions={
          <div style={{ display: 'flex', gap: 8 }}>
            <Button variant="ghost" size="sm" icon={RefreshCw} onClick={fetchData} />
            {canAdd && <Button icon={Plus} onClick={openAdd}>{ar ? 'إضافة خزان يومي' : 'Add Daily Tank'}</Button>}
          </div>
        }
      />

      {/* Info Notice about Commercial Daily Tanks Geometry */}
      <div style={{
        display: 'flex', alignItems: 'center', gap: 10, padding: '12px 16px',
        borderRadius: 8, background: 'var(--primary-subtle, rgba(59,130,246,0.08))',
        border: '1px solid var(--primary-border, rgba(59,130,246,0.25))',
        color: 'var(--primary)', marginBottom: 20, fontSize: 13,
      }}>
        <Info size={18} style={{ flexShrink: 0 }} />
        <span>
          {ar
            ? 'تصميم الخزانات اليومية: أسطوانية رأسية ذات قاعدة قمعية، كل خزان مرتبط مباشرة بمولد خاص وتُحسب الاستهلاكات من قراءات الخزان اليومية الفعلية.'
            : 'Daily tank specification: Vertical cylinder with conical bottom, each linked directly to a specific generator.'}
        </span>
      </div>

      {warning && <div className="alert-inline alert-inline-warning" style={{ marginBottom: 16 }}>{warning}</div>}
      {error && <div className="alert-inline alert-inline-danger" style={{ marginBottom: 16 }}>{error}</div>}

      {/* Search Bar */}
      <div style={{ marginBottom: 20, maxWidth: 360 }}>
        <input
          type="text"
          className="form-control"
          placeholder={ar ? 'بحث بالاسم أو الكود أو المولد...' : 'Search by name, code or generator...'}
          value={search}
          onChange={e => setSearch(e.target.value)}
        />
      </div>

      {loading ? (
        <div className="spinner-wrap"><div className="ft-spinner" /></div>
      ) : filtered.length === 0 ? (
        <div className="empty-state">
          <Database size={48} />
          <h3>{ar ? 'لا توجد خزانات يومية' : 'No Daily Tanks'}</h3>
          <p>{ar ? 'قم بإضافة خزان وقود يومي وربطه بمولد للبدء' : 'Add a daily fuel tank and link it to a generator to begin'}</p>
        </div>
      ) : (
        <div className="grid-auto">
          {filtered.map(tank => {
            const gen = generators.find(g => g.id === tank.generatorId);
            const genName = tank.generatorName || gen?.name || (ar ? 'غير محدد' : 'Unassigned');
            const cap = Number(tank.capacityLiters) || 0;
            const actual = Number(tank.actualStockLiters) || 0;
            const fillPct = cap > 0 ? Math.min(100, Math.round((actual / cap) * 100)) : 0;

            return (
              <div key={tank.id} className="gen-card" style={{ padding: 18 }}>
                <div className="gen-card-header" style={{ marginBottom: 12 }}>
                  <div>
                    <div className="gen-name" style={{ fontSize: 16, fontWeight: 700 }}>{tank.name}</div>
                    <div style={{ fontSize: 12, color: 'var(--text-muted)' }}>{tank.code}</div>
                  </div>
                  <span className={`status-badge ${tank.isActive !== false ? 'status-active' : 'status-inactive'}`}>
                    {tank.isActive !== false ? (ar ? 'نشط' : 'Active') : (ar ? 'غير نشط' : 'Inactive')}
                  </span>
                </div>

                {/* Generator Link Badge */}
                <div style={{
                  display: 'flex', alignItems: 'center', gap: 6, padding: '6px 12px',
                  borderRadius: 6, background: 'var(--card-2)', border: '1px solid var(--border)',
                  marginBottom: 14, fontSize: 13, fontWeight: 600,
                }}>
                  <Zap size={14} style={{ color: 'var(--warning)' }} />
                  <span style={{ color: 'var(--text-muted)', fontSize: 11 }}>{ar ? 'المولد:' : 'Generator:'}</span>
                  <span style={{ color: 'var(--text)' }}>{genName}</span>
                </div>

                {/* Stock Stats */}
                <div className="gen-stats" style={{ marginBottom: 14 }}>
                  <div className="gen-stat-item">
                    <div className="gen-stat-label">{ar ? 'السعة الكلية' : 'Capacity'}</div>
                    <div className="gen-stat-value">{cap.toLocaleString('en-US')} L</div>
                  </div>
                  <div className="gen-stat-item">
                    <div className="gen-stat-label">{ar ? 'الرصيد الفعلي' : 'Actual Stock'}</div>
                    <div className="gen-stat-value" style={{ color: 'var(--primary)', fontWeight: 700 }}>
                      {actual.toLocaleString('en-US')} L
                    </div>
                  </div>
                  <div className="gen-stat-item">
                    <div className="gen-stat-label">{ar ? 'نسبة الامتلاء' : 'Fill Level'}</div>
                    <div className="gen-stat-value">{fillPct}%</div>
                  </div>
                </div>

                {/* Actions */}
                <div className="gen-card-actions" style={{ borderTop: '1px solid var(--border)', paddingTop: 12 }}>
                  {canEdit && (
                    <Button variant="warning" size="sm" icon={Pencil} onClick={() => openEdit(tank)}>
                      {ar ? 'تعديل' : 'Edit'}
                    </Button>
                  )}
                  {canDelete && (
                    <Button variant="danger" size="sm" icon={Trash2} onClick={() => setDeleteConfirm(tank)}>
                      {ar ? 'حذف' : 'Delete'}
                    </Button>
                  )}
                </div>
              </div>
            );
          })}
        </div>
      )}

      {/* Modal Add / Edit */}
      {showModal && (
        <Modal
          open={showModal}
          onClose={resetForm}
          title={editId ? (ar ? 'تعديل بيانات الخزان اليومي' : 'Edit Daily Tank') : (ar ? 'إضافة خزان وقود يومي' : 'Add Daily Fuel Tank')}
          maxWidth={540}
          footer={
            <>
              <Button variant="ghost" onClick={resetForm}>{ar ? 'إلغاء' : 'Cancel'}</Button>
              <Button variant="primary" onClick={handleSave} disabled={saving} icon={Save}>
                {saving ? (ar ? 'جاري الحفظ...' : 'Saving...') : (ar ? 'حفظ الخزان اليومي' : 'Save Daily Tank')}
              </Button>
            </>
          }
        >
          {formError && <div className="alert-inline alert-inline-danger" style={{ marginBottom: 14 }}>{formError}</div>}

          <FormField label={ar ? 'اسم الخزان اليومي *' : 'Tank Name *'} required>
            <Input value={form.name} onChange={e => fld('name', e.target.value)} placeholder={ar ? 'مثال: خزان المولد 1 اليومي' : 'e.g. Gen 1 Daily Tank'} />
          </FormField>

          <div className="form-row">
            <FormField label={ar ? 'كود الخزان' : 'Tank Code'}>
              <Input value={form.code} onChange={e => fld('code', e.target.value)} placeholder="DT-01" />
            </FormField>
            <FormField label={ar ? 'المولد المرتبط *' : 'Linked Generator *'} required>
              {generators.length === 0 ? (
                <div
                  className="generator-empty-state"
                  style={{
                    padding: '8px 12px',
                    borderRadius: 6,
                    background: 'var(--surface-2, rgba(255,255,255,0.04))',
                    border: '1px dashed var(--border)',
                    color: 'var(--text-muted, #888)',
                    fontSize: 13,
                  }}
                >
                  {ar
                    ? 'لا توجد مولدات معرفة في المحطة. يرجى إضافة مولد أولاً.'
                    : 'No generators defined in station. Please add a generator first.'}
                </div>
              ) : (
                <select className="form-select" value={form.generatorId} onChange={e => fld('generatorId', e.target.value)} required>
                  <option value="">— {ar ? 'اختر المولد' : 'Select Generator'} —</option>
                  {generators.map(g => (
                    <option key={g.id} value={g.id}>{g.name} {g.model ? `(${g.model})` : ''}</option>
                  ))}
                </select>
              )}
            </FormField>
          </div>

          <div className="form-row">
            <FormField label={ar ? 'نوع الوقود' : 'Fuel Type'}>
              <select className="form-select" value={form.fuelLookupId} onChange={e => fld('fuelLookupId', e.target.value)}>
                <option value="">— {ar ? 'اختر نوع الوقود' : 'Select Fuel Type'} —</option>
                {fuelTypes.map(ft => (
                  <option key={ft.id} value={ft.id}>{ft.name}</option>
                ))}
              </select>
            </FormField>
            <FormField label={ar ? 'السعة الكلية (لتر) *' : 'Capacity (Litres) *'} required>
              <Input type="number" step="1" value={form.capacityLiters} onChange={e => fld('capacityLiters', e.target.value)} placeholder="1000" />
            </FormField>
          </div>

          {/* Vertical Tank Calibration Fields (Reused from TankSetup) */}
          <div style={{
            margin: '12px 0', padding: 12, borderRadius: 8,
            background: 'var(--surface-2, rgba(255,255,255,0.03))', border: '1px solid var(--border)'
          }}>
            <div style={{ fontSize: 13, fontWeight: 600, marginBottom: 10, color: 'var(--primary)' }}>
              {ar ? 'أبعاد ومعايرة الخزان الرأسي (قاعدة قمعية)' : 'Vertical Conical Tank Calibration'}
            </div>
            <div className="form-row">
              <FormField label={ar ? 'القطر (سم)' : 'Diameter (cm)'}>
                <Input type="number" step="0.1" value={form.diameterCm} onChange={e => fld('diameterCm', e.target.value)} placeholder="200" />
              </FormField>
              <FormField label={ar ? 'ارتفاع الأسطوانة (سم)' : 'Cylinder Height (cm)'}>
                <Input type="number" step="0.1" value={form.cylinderHeightCm} onChange={e => fld('cylinderHeightCm', e.target.value)} placeholder="150" />
              </FormField>
            </div>
            <div className="form-row">
              <FormField label={ar ? 'ارتفاع القمع (سم)' : 'Cone Height (cm)'}>
                <Input type="number" step="0.1" value={form.coneHeightCm} onChange={e => fld('coneHeightCm', e.target.value)} placeholder="50" />
              </FormField>
              <FormField label={ar ? 'معامل الضرب' : 'Multiplication Factor'}>
                <Input type="number" step="0.0001" value={form.multiplicationFactor} onChange={e => fld('multiplicationFactor', e.target.value)} placeholder="1.0" />
              </FormField>
            </div>
          </div>

          <div className="form-row">
            <FormField label={ar ? 'الرصيد الفعلي الحالي (لتر)' : 'Current Stock (L)'}>
              <Input type="number" step="1" value={form.actualStockLiters} onChange={e => fld('actualStockLiters', e.target.value)} placeholder="0" />
            </FormField>
            <FormField label={ar ? 'المخزون الميت (لتر)' : 'Dead Stock (L)'}>
              <Input type="number" step="0.1" value={form.deadStockLiters} onChange={e => fld('deadStockLiters', e.target.value)} placeholder="0" />
            </FormField>
          </div>

          <div className="form-row">
            <FormField label={ar ? 'حد التنبيه الأدنى (لتر)' : 'Min Alert (L)'}>
              <Input type="number" step="1" value={form.minAlertLiters} onChange={e => fld('minAlertLiters', e.target.value)} placeholder="100" />
            </FormField>
          </div>

          <FormField label={ar ? 'ملاحظات' : 'Notes'}>
            <textarea
              className="form-control"
              rows={2}
              value={form.notes}
              onChange={e => fld('notes', e.target.value)}
              style={{ resize: 'vertical', fontFamily: 'inherit' }}
            />
          </FormField>

          <div className="form-group" style={{ marginTop: 8 }}>
            <label className="checkbox-label">
              <input type="checkbox" checked={form.isActive} onChange={e => fld('isActive', e.target.checked)} />
              <span>{ar ? 'الخزان نشط' : 'Tank is Active'}</span>
            </label>
          </div>
        </Modal>
      )}

      {/* Confirm Delete Modal */}
      <ConfirmModal
        open={!!deleteConfirm}
        title={ar ? 'حذف الخزان اليومي' : 'Delete Daily Tank'}
        message={ar ? `هل تريد بالتأكيد حذف الخزان اليومي "${deleteConfirm?.name}"؟` : `Delete daily tank "${deleteConfirm?.name}"?`}
        confirmText={ar ? 'نعم، حذف' : 'Yes, Delete'}
        cancelText={ar ? 'إلغاء' : 'Cancel'}
        danger
        onConfirm={handleDelete}
        onCancel={() => setDeleteConfirm(null)}
      />
    </div>
  );
}
