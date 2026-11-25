import React, { useState, useEffect, useCallback, useMemo } from 'react';
import { Plus, Database, Pencil, Trash2, Save, RefreshCw, Zap, Gauge, AlertCircle, Info } from 'lucide-react';
import client from '../api/client';
import ConfirmModal from '../components/ConfirmModal';
import { PageHeader, Button, Modal, FormField, Input } from '../components/ui';
import useCommercialMode from '../hooks/useCommercialMode';
import { isDailyTank } from '../utils/tankRoles';
import TankVolumeField from '../components/TankVolumeField';
import TankCalibrationSection from '../components/TankCalibrationSection';
import {
  TANK_CALIBRATION_MODE,
  resolveEnteredLitres,
  validateTankStockInputs,
} from '../utils/tankCalibration';

const initialDailyTankForm = {
  name: '',
  code: '',
  generatorId: '',
  fuelLookupId: '',
  capacityLiters: '',
  capacityHeightCm: '',
  capacityUnit: 'litres',
  calibrationMode: TANK_CALIBRATION_MODE.GEOMETRY,
  litresPerCm: '',
  diameterCm: '',
  cylinderHeightCm: '',
  coneHeightCm: '',
  multiplicationFactor: '1.0',
  minAlertLiters: '',
  minAlertHeightCm: '',
  minAlertUnit: 'litres',
  deadStockLiters: '',
  deadStockHeightCm: '',
  deadStockUnit: 'litres',
  actualStockLiters: '',
  actualStockHeightCm: '',
  actualStockUnit: 'litres',
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
      capacityHeightCm: tank.capacityHeightCm ?? '',
      capacityUnit: tank.capacityInputUnit === 'cm' || tank.capacityHeightCm != null ? 'cm' : 'litres',
      calibrationMode: tank.calibrationMode ?? TANK_CALIBRATION_MODE.GEOMETRY,
      litresPerCm: tank.litresPerCm != null ? String(tank.litresPerCm) : '',
      diameterCm: tank.diameterCm != null ? String(tank.diameterCm) : '',
      cylinderHeightCm: tank.cylinderHeightCm != null ? String(tank.cylinderHeightCm) : '',
      coneHeightCm: tank.coneHeightCm != null ? String(tank.coneHeightCm) : '',
      multiplicationFactor: tank.multiplicationFactor != null ? String(tank.multiplicationFactor) : '1.0',
      minAlertLiters: tank.minAlertLiters ?? '',
      minAlertHeightCm: tank.minAlertHeightCm ?? '',
      minAlertUnit: tank.minAlertInputUnit === 'cm' || tank.minAlertHeightCm != null ? 'cm' : 'litres',
      deadStockLiters: tank.deadStockLiters ?? '',
      deadStockHeightCm: tank.deadStockLevelCm ?? '',
      deadStockUnit: tank.deadStockInputUnit === 'cm' || tank.deadStockLevelCm != null ? 'cm' : 'litres',
      actualStockLiters: tank.actualStockLiters ?? '',
      actualStockHeightCm: tank.actualStockHeightCm ?? '',
      actualStockUnit: tank.actualStockInputUnit === 'cm' || tank.actualStockHeightCm != null ? 'cm' : 'litres',
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
    const msg = validateTankStockInputs({
      ar,
      config: form,
      fields: [
        {
          key: 'capacity',
          label: ar ? 'السعة الكلية' : 'Capacity',
          unit: form.capacityUnit,
          heightValue: form.capacityHeightCm,
          resolvedLitres: resolveEnteredLitres(form, form.capacityUnit, form.capacityLiters, form.capacityHeightCm),
        },
        {
          key: 'minAlert',
          label: ar ? 'حد التنبيه الأدنى' : 'Min Alert',
          unit: form.minAlertUnit,
          heightValue: form.minAlertHeightCm,
          resolvedLitres: resolveEnteredLitres(form, form.minAlertUnit, form.minAlertLiters, form.minAlertHeightCm),
        },
        {
          key: 'deadStock',
          label: ar ? 'المخزون الميت' : 'Dead Stock',
          unit: form.deadStockUnit,
          heightValue: form.deadStockHeightCm,
          resolvedLitres: resolveEnteredLitres(form, form.deadStockUnit, form.deadStockLiters, form.deadStockHeightCm),
        },
        {
          key: 'actualStock',
          label: ar ? 'الرصيد الفعلي' : 'Current Stock',
          unit: form.actualStockUnit,
          heightValue: form.actualStockHeightCm,
          resolvedLitres: resolveEnteredLitres(form, form.actualStockUnit, form.actualStockLiters, form.actualStockHeightCm),
        },
      ],
    });
    if (msg) {
      setFormError(msg);
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

      const calibrationMode = Number(form.calibrationMode ?? TANK_CALIBRATION_MODE.GEOMETRY);
      const litresPerCm = calibrationMode === TANK_CALIBRATION_MODE.LITRES_PER_CM && form.litresPerCm !== '' ? parseFloat(form.litresPerCm) : null;
      const dCm = form.diameterCm ? parseFloat(form.diameterCm) : null;
      const cylCm = form.cylinderHeightCm ? parseFloat(form.cylinderHeightCm) : null;
      const coneCm = form.coneHeightCm ? parseFloat(form.coneHeightCm) : null;
      const factor = form.multiplicationFactor ? parseFloat(form.multiplicationFactor) : 1.0;

      const commonFields = {
        fuelLookupId: fuelId,
        generatorId: form.generatorId ? Number(form.generatorId) : null,
        tankRole: 2, // TankRole.Daily
        shape: 0,    // TankShape.VerticalCylinderWithConicalBottom
        calibrationMode,
        litresPerCm,
        diameterCm: dCm,
        cylinderHeightCm: cylCm,
        coneHeightCm: coneCm,
        multiplicationFactor: factor,
        capacityLiters: form.capacityUnit === 'cm' ? 0 : parseFloat(form.capacityLiters),
        capacityHeightCm: form.capacityUnit === 'cm' && form.capacityHeightCm !== '' ? parseFloat(form.capacityHeightCm) : null,
        minAlertLiters: form.minAlertUnit === 'cm' || !form.minAlertLiters ? null : parseFloat(form.minAlertLiters),
        minAlertHeightCm: form.minAlertUnit === 'cm' && form.minAlertHeightCm !== '' ? parseFloat(form.minAlertHeightCm) : null,
        deadStockLiters: form.deadStockUnit === 'cm' || !form.deadStockLiters ? null : parseFloat(form.deadStockLiters),
        deadStockHeightCm: form.deadStockUnit === 'cm' && form.deadStockHeightCm !== '' ? parseFloat(form.deadStockHeightCm) : null,
        actualStockLiters: form.actualStockUnit === 'cm' || !form.actualStockLiters ? null : parseFloat(form.actualStockLiters),
        actualStockHeightCm: form.actualStockUnit === 'cm' && form.actualStockHeightCm !== '' ? parseFloat(form.actualStockHeightCm) : null,
        notes: form.notes || null,
        isActive: form.isActive,
      };

      let savedId = editId;
      if (editId) {
        await client.put(`/api/fuel-tanks/${editId}`, {
          ...commonFields,
          name: form.name.trim(),
          code: form.code.trim() || null,
        });
      } else {
        const autoCode = form.code.trim() || `DT-${Date.now().toString(36).toUpperCase().slice(-4)}`;
        const payload = {
          ...commonFields,
          name: form.name.trim(),
          code: autoCode,
          hasMeter: false,
        };
        if (!isCommercialMode) {
          payload.subStationId = subStations[0]?.id || 1;
        }
        const createRes = await client.post('/api/fuel-tanks', payload);
        savedId = createRes.data?.id;
      }

      let calWarning = '';
      if (savedId && calibrationMode === TANK_CALIBRATION_MODE.GEOMETRY && dCm && coneCm) {
        try {
          await client.put(`/api/fuel-tanks/${savedId}/calibration`, {
            diameterCm: dCm,
            cylinderHeightCm: cylCm ?? 0,
            coneHeightCm: coneCm,
            multiplicationFactor: factor,
            ...(form.deadStockUnit === 'cm' && form.deadStockHeightCm !== ''
              ? { deadStockLevelCm: parseFloat(form.deadStockHeightCm) }
              : { deadStockLitres: form.deadStockLiters ? parseFloat(form.deadStockLiters) : null }),
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
            const genName = tank.generatorName || gen?.name || (ar ? '\u063a\u064a\u0631 \u0645\u0631\u062a\u0628\u0637' : 'Unlinked');
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
            <FormField label={ar ? '\u0627\u0644\u0645\u0648\u0644\u062f \u0627\u0644\u0645\u0631\u062a\u0628\u0637 (\u0627\u062e\u062a\u064a\u0627\u0631\u064a)' : 'Linked Generator (Optional)'}>
              <select className="form-select" value={form.generatorId} onChange={e => fld('generatorId', e.target.value)}>
                <option value="">— {ar ? '\u0628\u062f\u0648\u0646 (\u063a\u064a\u0631 \u0645\u0631\u062a\u0628\u0637)' : 'None (Unlinked)'} —</option>
                {generators
                  .filter(g => editId ? String(g.id) === String(form.generatorId) || !tanks.some(t => Number(t.generatorId) === Number(g.id)) : !tanks.some(t => Number(t.generatorId) === Number(g.id)))
                  .map(g => (
                    <option key={g.id} value={g.id}>{g.name} {g.model ? `(${g.model})` : ''}</option>
                  ))}
              </select>
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
            <TankVolumeField
              ar={ar}
              label={ar ? 'السعة الكلية' : 'Capacity'}
              unit={form.capacityUnit}
              onUnitChange={(u) => fld('capacityUnit', u)}
              litresValue={form.capacityLiters}
              onLitresChange={(v) => fld('capacityLiters', v)}
              heightValue={form.capacityHeightCm}
              onHeightChange={(v) => fld('capacityHeightCm', v)}
              calibrationConfig={form}
              placeholder="1000"
              required
            />
          </div>

          <TankCalibrationSection
            ar={ar}
            value={form}
            onChange={(next) => setForm(f => ({ ...f, ...next }))}
            showGeometry
          />

          <div className="form-row">
            <TankVolumeField
              ar={ar}
              label={ar ? 'الرصيد الفعلي الحالي' : 'Current Stock'}
              unit={form.actualStockUnit}
              onUnitChange={(u) => fld('actualStockUnit', u)}
              litresValue={form.actualStockLiters}
              onLitresChange={(v) => fld('actualStockLiters', v)}
              heightValue={form.actualStockHeightCm}
              onHeightChange={(v) => fld('actualStockHeightCm', v)}
              calibrationConfig={form}
              placeholder="0"
            />
            <TankVolumeField
              ar={ar}
              label={ar ? 'المخزون الميت' : 'Dead Stock'}
              unit={form.deadStockUnit}
              onUnitChange={(u) => fld('deadStockUnit', u)}
              litresValue={form.deadStockLiters}
              onLitresChange={(v) => fld('deadStockLiters', v)}
              heightValue={form.deadStockHeightCm}
              onHeightChange={(v) => fld('deadStockHeightCm', v)}
              calibrationConfig={form}
              placeholder="0"
            />
          </div>

          <div className="form-row">
            <TankVolumeField
              ar={ar}
              label={ar ? 'حد التنبيه الأدنى' : 'Min Alert'}
              unit={form.minAlertUnit}
              onUnitChange={(u) => fld('minAlertUnit', u)}
              litresValue={form.minAlertLiters}
              onLitresChange={(v) => fld('minAlertLiters', v)}
              heightValue={form.minAlertHeightCm}
              onHeightChange={(v) => fld('minAlertHeightCm', v)}
              calibrationConfig={form}
              placeholder="100"
            />
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
