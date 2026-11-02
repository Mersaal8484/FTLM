import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Zap, ChevronLeft, Save, AlertCircle, CheckCircle,
  Clock, Activity, Droplet, Layout, RefreshCw
} from 'lucide-react';
import client from '../api/client';
import { PageHeader, Button, FormField, Input } from '../components/ui';

function AddReading({ lang, currentUser }) {
  const ar = lang === 'ar';
  const canEditSchedule = true;
  const getCurrentShiftType = () => {
    const hour = new Date().getHours();
    return hour >= 6 && hour < 18 ? 'day' : 'night';
  };
  const assignedShiftType = currentUser?.assignedShiftType === 'dual'
    ? getCurrentShiftType()
    : (currentUser?.assignedShiftType || 'day');
  const assignedShiftDuration = currentUser?.assignedShiftDuration || (assignedShiftType === 'full' ? 24 : 12);
  const { generatorId } = useParams();
  const navigate = useNavigate();

  const [generator, setGenerator] = useState(null);
  const [lastReading, setLastReading] = useState(null);
  const [controller, setController] = useState(null);
  const [oils, setOils] = useState([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [status, setStatus] = useState(null);

  const [form, setForm] = useState({
    readingDate: new Date().toISOString().slice(0, 16),
    shiftDuration: assignedShiftDuration,
    shiftType: assignedShiftType,
    currentEnergyKWh: '',
    currentRunningHours: '',
    maxLoadKw: '',
    minLoadKw: '',
    oilTypeUsed: '',
    oilAddedLiters: '',
    notes: '',
  });

  useEffect(() => {
    const fetchData = async () => {
      setLoading(true);
      try {
        const [genRes, lastRes] = await Promise.all([
          client.get(`/api/generators/my/${generatorId}`),
          client.get(`/api/generators/my/${generatorId}/last-reading`),
        ]);
        const gen = genRes.data;
        setGenerator(gen);
        setLastReading(lastRes.data);

        const stationId = gen?.stationId;
        const oilsRes = await client.get('/api/oils', { params: stationId ? { stationId } : {} });
        setOils(oilsRes.data || []);

        try {
          const controllersRes = await client.get('/api/controllers');
          const active = (controllersRes.data || []).find(c => c.generatorId === gen.id);
          setController(active || null);
        } catch {
          setController(null);
        }

        if (gen?.assignedOilTypes?.length > 0) {
          setForm(f => ({ ...f, oilTypeUsed: gen.assignedOilTypes[0].id }));
        }
      } catch (err) {
        setStatus({ type: 'error', msg: ar ? 'فشل تحميل بيانات المولد' : 'Failed to load generator data' });
      } finally { setLoading(false); }
    };
    fetchData();
  }, [generatorId, ar]);

  const sanitizeNumeric = (v, allowDecimal = true) => {
    let s = v.replace(/[^0-9.]/g, '');
    if (!allowDecimal) return s.replace(/\./g, '');
    const p = s.split('.');
    if (p.length > 2) s = p[0] + '.' + p.slice(1).join('');
    if (p.length === 2 && p[1].length > 2) s = p[0] + '.' + p[1].slice(0, 2);
    return s;
  };
  const fld = (k, v) => setForm(f => ({ ...f, [k]: v }));

  const hasController = !!controller;

  const prevPower = hasController
    ? (lastReading?.currentEnergyKWh ?? generator?.lastPowerRecord ?? 0)
    : 0;
  const prevHours = hasController
    ? (lastReading?.currentRunningHours ?? generator?.lastHoursRecord ?? 0)
    : 0;

  const calcEnergy = form.currentEnergyKWh
    ? Math.max(0, parseFloat(form.currentEnergyKWh) - prevPower).toFixed(3)
    : '—';
  const calcHours = form.currentRunningHours
    ? Math.max(0, parseFloat(form.currentRunningHours) - prevHours).toFixed(3)
    : '—';

  const consumptionRate = generator?.fuelConsumptionRatePerHour || generator?.fuelConsumption || 0;

  const calcFuel = calcEnergy !== '—'
    ? (parseFloat(calcEnergy) * consumptionRate).toFixed(3)
    : '—';

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!form.currentEnergyKWh || !form.currentRunningHours) {
      setStatus({ type: 'error', msg: ar ? 'قراءة العداد وساعات التشغيل مطلوبة' : 'Power record and operating hours are required' });
      return;
    }
    setSaving(true); setStatus(null);
    try {
      await client.post('/api/readings', {
        generatorId: parseInt(generatorId, 10),
        readingDate: new Date(form.readingDate).toISOString(),
        prevEnergyKWh: prevPower,
        currentEnergyKWh: parseFloat(form.currentEnergyKWh),
        prevRunningHours: prevHours,
        currentRunningHours: parseFloat(form.currentRunningHours),
        fuelLookupId: generator?.fuelLookupId || generator?.fuelLookup?.id || 0,
        standardFuelRate: generator?.standardFuelRate || generator?.fuelConsumptionRatePerHour || 0,
        oilLookupId: form.oilTypeUsed ? parseInt(form.oilTypeUsed, 10) : (generator?.assignedOilTypes?.[0]?.id || generator?.oilLookupId || 0),
        oilUsedLiters: form.oilAddedLiters ? parseFloat(form.oilAddedLiters) : 0,
        notes: form.notes,
      });
      setStatus({ type: 'success', msg: ar ? 'تم إرسال القراءة بنجاح وهي بانتظار الموافقة ✓' : 'Reading submitted successfully, pending approval ✓' });
      setTimeout(() => navigate('/my-generators'), 400);
    } catch (err) {
      const detail = err.response?.data?.detail || err.response?.data?.msg || err.response?.data?.title;
      setStatus({ type: 'error', msg: detail || (ar ? 'حدث خطأ' : 'Error occurred') });
    } finally { setSaving(false); }
  };

  const labelStyle = { display: 'block', fontSize: 11, fontWeight: 700, color: 'var(--text-muted)', marginBottom: 6, textTransform: 'uppercase', letterSpacing: '0.5px' };

  const prevBox = (label, value, unit = '') => (
    <div className="glass-card" style={{ padding: '10px 14px', marginBottom: 12, background: 'var(--card-2)' }}>
      <div style={{ ...labelStyle, marginBottom: 4 }}>{label}</div>
      <div style={{ fontSize: 18, fontWeight: 700, color: 'var(--text-faint)' }}>
        {value !== null && value !== undefined ? value.toLocaleString() : '—'}
        {unit && <span style={{ fontSize: 12, fontWeight: 400, marginInlineStart: 4 }}>{unit}</span>}
      </div>
      <div style={{ fontSize: 10, color: 'var(--text-faint)', marginTop: 2 }}>
        {ar ? '(القراءة السابقة — للمشاهدة فقط)' : '(Previous reading — read only)'}
      </div>
    </div>
  );

  const calcBox = (label, value, unit, color = 'var(--primary)') => (
    <div className="glass-card" style={{ padding: '10px 14px', background: 'var(--card-2)' }}>
      <div style={{ ...labelStyle, marginBottom: 4 }}>{label}</div>
      <div style={{ fontSize: 22, fontWeight: 800, color }}>
        {value}
        {value !== '—' && <span style={{ fontSize: 12, fontWeight: 400, marginInlineStart: 4, color: 'var(--text-muted)' }}>{unit}</span>}
      </div>
    </div>
  );

  if (loading) return <div className="spinner-wrap"><div className="ft-spinner" /></div>;

  return (
    <div>
      <PageHeader
        title={
          <span style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
            <Button variant="ghost" size="sm" icon={ChevronLeft} onClick={() => navigate('/my-generators')} style={{ transform: ar ? 'rotate(180deg)' : 'none' }} />
            <Activity size={22} style={{ color: 'var(--primary)' }} />
            {ar ? 'إدخال قراءة جديدة' : 'New Reading Entry'}
          </span>
        }
        subtitle={
          <span>
            <span style={{ color: 'var(--primary)', fontWeight: 600 }}>{generator?.name}</span>
            <span style={{ color: 'var(--text-muted)' }}> — {generator?.stationName}</span>
          </span>
        }
        actions={
          <Button variant="ghost" size="sm" icon={RefreshCw} onClick={() => navigate('/my-generators')} />
        }
      />

      {status && (
        <div className={`alert ${status.type === 'success' ? 'alert-success' : 'alert-danger'}`}
          style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 20 }}>
          {status.type === 'success' ? <CheckCircle size={15} /> : <AlertCircle size={15} />}
          {status.msg}
        </div>
      )}

      <form onSubmit={handleSubmit}>
        <div className="form-2col" style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 20 }}>

          {/* ── الجانب الأيمن: النموذج ── */}
          <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>

            {/* ── لوحة المحطة/الموقع المرتبط بالمولد ── */}
            <div className="section-card" style={{ border: '1px solid var(--border-light)' }}>
              <div className="section-card-header">
                <div className="section-card-title" style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                  <Layout size={16} style={{ color: 'var(--primary)' }} />
                  {ar ? 'المحطة/الموقع المرتبط' : 'Linked Station / Site'}
                </div>
              </div>
              <div className="section-card-body">
                <div style={{ fontSize: 12, color: 'var(--text-muted)', marginBottom: 10 }}>
                  <span style={{ fontWeight: 700 }}>{generator?.subStationName}</span>
                  {generator?.stationName ? <span> — {generator.stationName}</span> : null}
                </div>

                <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 10, padding: '8px 12px', borderRadius: 8, background: controller ? 'var(--card-2)' : 'rgba(220,53,69,0.1)', border: controller ? '1px solid var(--border-light)' : '1px solid var(--danger)' }}>
                  <Layout size={14} style={{ color: controller ? 'var(--primary)' : 'var(--danger)' }} />
                  <div>
                    <div style={{ ...labelStyle, marginBottom: 2 }}>{ar ? 'شاشة التحكم النشطة' : 'Active Control Panel'}</div>
                    <div style={{ fontSize: 14, fontWeight: 700, color: controller ? 'var(--text)' : 'var(--danger)' }}>
                      {controller ? (controller.name || controller.code || `#${controller.id}`) : (ar ? 'لا توجد شاشة تحكم مرتبطة' : 'No control panel linked')}
                    </div>
                  </div>
                </div>
                {!hasController && (
                  <div className="glass-card" style={{ marginBottom: 12, borderColor: 'var(--danger)', padding: '12px 16px' }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 6 }}>
                      <AlertCircle size={16} style={{ color: 'var(--danger)' }} />
                      <span style={{ fontSize: 14, fontWeight: 700, color: 'var(--danger)' }}>
                        {ar ? 'لا يمكن إدخال قراءة' : 'Cannot Enter Reading'}
                      </span>
                    </div>
                    <div style={{ fontSize: 12, color: 'var(--danger)', lineHeight: 1.6 }}>
                      {ar
                        ? 'هذا المولد غير مرتبط بشاشة تحكم نشطة. يجب ربط المولد بشاشة تحكم أولاً قبل إدخال القراءة.'
                        : 'This generator is not linked to an active control panel. Link the generator to a control panel before entering readings.'}
                    </div>
                  </div>
                )}

                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 10 }}>
                  <div className="glass-card" style={{ padding: '10px 14px', background: 'var(--card-2)' }}>
                    <div style={{ ...labelStyle, marginBottom: 4 }}>{ar ? 'نوع الوقود المستخدم' : 'Fuel Type Used'}</div>
                    <div style={{ fontSize: 16, fontWeight: 700 }}>
                      {generator?.fuelLookupName || '—'}
                    </div>
                  </div>
                </div>
              </div>
            </div>

            {/* الوردية */}
            <div className="section-card">
              <div className="section-card-header">
                <div className="section-card-title" style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                  <Clock size={16} style={{ color: 'var(--primary)' }} />
                  {ar ? 'التاريخ والوردية' : 'Date & Shift'}
                </div>
              </div>
              <div className="section-card-body">
                <div className="form-row">
                  <FormField label={ar ? 'التاريخ والوقت *' : 'Date & Time *'}>
                    <Input type="datetime-local" value={form.readingDate} onChange={e => fld('readingDate', e.target.value)} disabled={!canEditSchedule} required />
                  </FormField>
                  <FormField label={ar ? 'مدة الوردية' : 'Shift Duration'}>
                    <select className="form-select" value={form.shiftDuration} onChange={e => fld('shiftDuration', e.target.value)}
                      disabled={!canEditSchedule}>
                      <option value={6}>{ar ? '6 ساعات' : '6 Hours'}</option>
                      <option value={12}>{ar ? '12 ساعة' : '12 Hours'}</option>
                      <option value={24}>{ar ? '24 ساعة' : '24 Hours'}</option>
                    </select>
                  </FormField>
                </div>
                <div className="form-group" style={{ marginTop: 8 }}>
                  <label style={labelStyle}>{ar ? 'نوع الوردية' : 'Shift Type'}</label>
                  <div style={{ display: 'flex', gap: 8 }}>
                    {[['day', ar ? '☀️ صباحية' : '☀️ Day'], ['night', ar ? '🌙 مسائية' : '🌙 Night'], ['full', ar ? '🔄 كاملة 24h' : '🔄 Full 24h']].map(([v, l]) => (
                      <Button key={v} variant={form.shiftType === v ? 'primary' : 'ghost'} size="sm" onClick={() => fld('shiftType', v)} disabled={!canEditSchedule} style={{ flex: 1 }}>
                        {l}
                      </Button>
                    ))}
                  </div>
                </div>
              </div>
            </div>

            {/* عداد الطاقة */}
            <div className="section-card">
              <div className="section-card-header">
                <div className="section-card-title" style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                  <Zap size={16} style={{ color: 'var(--warning)' }} />
                  {ar ? 'عداد الطاقة المنتجة (KW/KVA)' : 'Power Meter (KW/KVA)'}
                </div>
              </div>
              <div className="section-card-body">
                {prevBox(ar ? 'القراءة السابقة' : 'Previous Reading', prevPower, 'KWh')}
                <FormField
                  label={ar ? 'القراءة الحالية *' : 'Current Reading *'}
                  error={form.currentEnergyKWh && parseFloat(form.currentEnergyKWh) < prevPower ? (ar ? 'القراءة أقل من السابقة' : 'Reading is less than previous') : null}
                >
                  <Input type="number" step="0.01" min={prevPower} placeholder={`≥ ${prevPower}`}
                    value={form.currentEnergyKWh} onChange={e => fld('currentEnergyKWh', sanitizeNumeric(e.target.value))}
                    disabled={!hasController} required />
                </FormField>
              </div>
            </div>

            {/* عداد الساعات */}
            <div className="section-card">
              <div className="section-card-header">
                <div className="section-card-title" style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                  <Clock size={16} style={{ color: 'var(--secondary)' }} />
                  {ar ? 'عداد ساعات التشغيل' : 'Operating Hours Meter'}
                </div>
              </div>
              <div className="section-card-body">
                {prevBox(ar ? 'الساعات السابقة' : 'Previous Hours', prevHours, ar ? 'ساعة' : 'h')}
                <div className="form-row" style={{ margin: 0 }}>
                  <FormField label={ar ? 'الساعات الحالية *' : 'Current Hours *'}>
                    <Input type="number" step="0.1" min={prevHours} placeholder={`≥ ${prevHours}`}
                      value={form.currentRunningHours} onChange={e => fld('currentRunningHours', sanitizeNumeric(e.target.value))}
                      disabled={!hasController} required />
                  </FormField>
                </div>
              </div>
            </div>

            {/* الأحمال */}
            <div className="section-card">
              <div className="section-card-header">
                <div className="section-card-title" style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                  <Activity size={16} style={{ color: 'var(--primary)' }} />
                  {ar ? 'الأحمال (KW)' : 'Load (KW)'}
                </div>
              </div>
              <div className="section-card-body">
                <div className="form-row" style={{ margin: 0 }}>
                  <FormField label={ar ? 'أقصى الأحمال KW' : 'Max Load KW'}>
                    <Input type="number" step="0.1" value={form.maxLoadKw} onChange={e => fld('maxLoadKw', sanitizeNumeric(e.target.value))} disabled={!hasController} />
                  </FormField>
                  <FormField label={ar ? 'أدنى الأحمال KW' : 'Min Load KW'}>
                    <Input type="number" step="0.1" value={form.minLoadKw} onChange={e => fld('minLoadKw', sanitizeNumeric(e.target.value))} disabled={!hasController} />
                  </FormField>
                </div>
              </div>
            </div>

            {/* الزيت */}
            <div className="section-card">
              <div className="section-card-header">
                <div className="section-card-title" style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                  <Droplet size={16} style={{ color: 'var(--warning)' }} />
                  {ar ? 'الزيت المضاف' : 'Oil Added'}
                </div>
              </div>
              <div className="section-card-body">
                <div className="form-row">
                  <FormField label={ar ? 'نوع الزيت المستخدم' : 'Oil Type Used'}>
                    <select className="form-select" value={form.oilTypeUsed} onChange={e => fld('oilTypeUsed', e.target.value)} disabled={!hasController}>
                      <option value="">{ar ? '— لا يوجد —' : '— None —'}</option>
                      {oils.map(o => (
                        <option key={o.id} value={o.id}>
                          {o.name} ({ar ? 'متبقي:' : 'Avail:'} {o.currentStock} {ar ? 'لتر' : 'L'})
                        </option>
                      ))}
                    </select>
                  </FormField>
                  <FormField label={ar ? 'الكمية المضافة (لتر)' : 'Quantity Added (L)'}>
                    <Input type="number" step="0.1" min="0" value={form.oilAddedLiters} onChange={e => fld('oilAddedLiters', sanitizeNumeric(e.target.value))} disabled={!hasController} />
                  </FormField>
                </div>
                <FormField label={ar ? 'ملاحظات' : 'Notes'}>
                  <textarea className="form-control" rows={2} value={form.notes} onChange={e => fld('notes', e.target.value)} disabled={!hasController} style={{ resize: 'vertical', fontFamily: 'inherit' }} />
                </FormField>
              </div>
            </div>
          </div>

          {/* ── الجانب الأيسر: ملخص مباشر ── */}
          <div>
            <div className="section-card" style={{ position: 'sticky', top: 20 }}>
              <div className="section-card-header">
                <div className="section-card-title">{ar ? 'الملخص المحسوب تلقائياً' : 'Auto-Calculated Summary'}</div>
                <span className="live-badge">
                  <span className="status-dot online" />
                  {ar ? 'مباشر' : 'Live'}
                </span>
              </div>
              <div className="section-card-body" style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
                {calcBox(ar ? 'ساعات التشغيل' : 'Operating Hours', calcHours, ar ? 'ساعة' : 'h', 'var(--secondary)')}
                {calcBox(ar ? 'الطاقة المنتجة' : 'Energy Produced', calcEnergy, 'KWh', 'var(--primary)')}
                {calcBox(ar ? 'الوقود المستهلك (تقديري)' : 'Fuel Consumed (est.)', calcFuel, ar ? 'لتر' : 'L', 'var(--warning)')}

                {/* معلومات المولد */}
                <div style={{ marginTop: 8, paddingTop: 12, borderTop: '1px solid var(--border)' }}>
                  <div style={{ fontSize: 11, color: 'var(--text-faint)', fontWeight: 600, marginBottom: 8, textTransform: 'uppercase', letterSpacing: '0.5px' }}>
                    {ar ? 'بيانات المولد' : 'Generator Info'}
                  </div>
                  {[
                    [ar ? 'معدل الاستهلاك' : 'Consumption Rate', `${consumptionRate} L/kWh`],
                    [ar ? 'القدرة الاسمية' : 'Rated Capacity', `${generator?.actualPowerKW || 0} KW`],
                    [ar ? 'نوع المولد' : 'Gen Type', generator?.genType || '—'],
                  ].map(([label, val]) => (
                    <div key={label} style={{ display: 'flex', justifyContent: 'space-between', padding: '5px 0', fontSize: 13 }}>
                      <span style={{ color: 'var(--text-muted)' }}>{label}</span>
                      <span style={{ fontWeight: 600 }}>{val}</span>
                    </div>
                  ))}
                </div>

                <Button variant="primary" type="submit" disabled={saving || !hasController || !form.currentEnergyKWh || !form.currentRunningHours} style={{ width: '100%', padding: 12, marginTop: 8 }}>
                  {saving
                    ? <><span className="ft-spinner" style={{ width: 14, height: 14 }} /> {ar ? 'جاري الإرسال...' : 'Submitting...'}</>
                    : <><Save size={16} /> {ar ? 'إرسال القراءة للموافقة' : 'Submit for Approval'}</>}
                </Button>
              </div>
            </div>
          </div>
        </div>
      </form>
    </div>
  );
}

export default AddReading;
