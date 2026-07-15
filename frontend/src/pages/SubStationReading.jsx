import React, { useState, useEffect } from 'react';
import client from '../api/client';
import { useParams, useNavigate, useSearchParams } from 'react-router-dom';
import {
  AlertCircle, Clock, Activity, Eye, Layout, ChevronLeft, ArrowRight, Zap
} from 'lucide-react';

function SubStationReadingPage({ lang, currentUser: _currentUser }) {
  const ar = lang === 'ar';
  // Date field is editable by all roles — see AddReading.js for the same change.
  const canEditSchedule = true;
  const { stationId } = useParams();
  const [searchParams] = useSearchParams();
  const subStationId = searchParams.get('subStation') || '';
  const siteNameParam = searchParams.get('siteName') || '';
  const navigate = useNavigate();

  const [station, setStation] = useState(null);
  const [lastReading, setLastReading] = useState(null);
  const [gridMeter, setGridMeter] = useState(null);
  const [production, setProduction] = useState(0);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [status, setStatus] = useState(null);

  const sanitizeNumeric = (v, allowDecimal = true) => {
    let s = v.replace(/[^0-9.]/g, '');
    if (!allowDecimal) return s.replace(/\./g, '');
    const p = s.split('.');
    if (p.length > 2) s = p[0] + '.' + p.slice(1).join('');
    if (p.length === 2 && p[1].length > 2) s = p[0] + '.' + p[1].slice(0, 2);
    return s;
  };

  const [form, setForm] = useState({
    readingDate: new Date().toISOString().slice(0, 10),
    currentGridRecord: '',
    internalConsumption: '',
    notes: '',
  });

  useEffect(() => {
    const fetchData = async () => {
      setLoading(true);
      setStatus(null);

      try {
        const [stRes, lastRes, metersRes] = await Promise.all([
          client.get(`/api/stations/my/${stationId}`),
          client.get(`/api/substation-readings/last/${stationId}`, { params: subStationId ? { subStation: subStationId } : {} }),
          client.get('/api/meters/my'),
        ]);
        setStation(stRes.data);
        setLastReading(lastRes.data);

        // Find the active grid meter (عداد الطاقة المرسلة) linked to this station
        const activeGridMeter = metersRes.data.find(m =>
          (m.station?.id === stationId || m.station === stationId) &&
          (!subStationId || m.subStation?.id === subStationId || m.subStation === subStationId) &&
          m.meterType === 'عداد الطاقة المرسلة'
        );
        setGridMeter(activeGridMeter || null);
      } catch (err) {
        setStatus({ type: 'error', msg: ar ? 'فشل تحميل بيانات الموقع' : 'Failed to load site data' });
        setLoading(false);
        return;
      }

      try {
        const today = new Date().toISOString().split('T')[0];
        const prodRes = await client.get('/api/substation-readings/production-summary', {
          params: { stationId, date: today, ...(subStationId ? { subStation: subStationId } : {}) }
        });
        setProduction(prodRes.data.totalProduced || 0);
      } catch {
        setProduction(0);
      }

      setLoading(false);
    };

    fetchData();
  }, [stationId, subStationId, ar]);

  const hasMeter = !!gridMeter;

  const fld = (k, v) => setForm(f => ({ ...f, [k]: v }));

  // القراءة السابقة المعتمدة
  const prevGridVal = lastReading
    ? lastReading.currentExportedEnergyKWh
    : (gridMeter ? gridMeter.energyReading : (station?.lastGridRecord || 0));

  // حساب صافي الطاقة المرسلة = الفارق - الاستهلاك الداخلي
  const energySentDiff = form.currentGridRecord
    ? parseFloat(form.currentGridRecord) - prevGridVal
    : null;

  const internalConsumptionVal = parseFloat(form.internalConsumption) || 0;

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!subStationId) {
      setStatus({ type: 'error', msg: ar ? 'رقم الموقع مطلوب' : 'Site number is required' });
      return;
    }
    if (!gridMeter?.id) {
      setStatus({ type: 'error', msg: ar ? 'رقم العداد مطلوب — يجب ربط الموقع بعداد أولاً' : 'Meter number is required — link a meter to the site first' });
      return;
    }
    if (!form.currentGridRecord) {
      setStatus({ type: 'error', msg: ar ? 'قراءة العداد الحالية مطلوبة' : 'Current meter reading is required' });
      return;
    }
    if (parseFloat(form.currentGridRecord) < prevGridVal) {
      setStatus({ type: 'error', msg: ar ? `القراءة الحالية (${form.currentGridRecord}) يجب أن تكون أكبر من أو تساوي القراءة السابقة (${prevGridVal})` : `Current reading (${form.currentGridRecord}) must be ≥ previous reading (${prevGridVal})` });
      return;
    }
    setSaving(true); setStatus(null);
    try {
      await client.post('/api/substation-readings', {
        subStationId: subStationId ? Number(subStationId) : null,
        meterId: gridMeter?.id ?? null,
        readingDate: new Date(form.readingDate).toISOString(),
        prevExportedEnergyKWh: parseFloat(prevGridVal) || 0,
        currentExportedEnergyKWh: parseFloat(form.currentGridRecord),
        internalConsumption: parseFloat(form.internalConsumption) || 0,
        readingStatusLookupId: null,
        notes: form.notes,
      });
      setStatus({ type: 'success', msg: ar ? 'تم تسجيل قراءة الموقع بنجاح ✓' : 'Site reading recorded successfully ✓' });
      setTimeout(() => navigate('/my-generators'), 400);
    } catch (err) {
      setStatus({ type: 'error', msg: err.response?.data?.msg || (ar ? 'حدث خطأ' : 'Error occurred') });
    } finally { setSaving(false); }
  };

  const labelStyle = {
    display: 'block', fontSize: 11, fontWeight: 700,
    color: 'var(--text-muted)', marginBottom: 6,
    textTransform: 'uppercase', letterSpacing: '0.5px'
  };

  const getName = (value) => value?.name || value?.subStationName || value?.stationName || '';

  if (loading) return <div className="spinner-wrap"><div className="ft-spinner" /></div>;

  const parentStationName = getName(station) || stationId;
  const siteName = subStationId
    ? (siteNameParam || getName(gridMeter?.subStation) || getName(lastReading?.subStation) || subStationId)
    : parentStationName;
  const parentContext = subStationId && parentStationName && parentStationName !== siteName
    ? parentStationName
    : '';

  return (
    <div>
      <div className="page-header">
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 6 }}>
            <button className="btn btn-ghost btn-sm btn-icon" onClick={() => navigate('/my-generators')}>
              <ChevronLeft size={16} style={{ transform: ar ? 'rotate(180deg)' : 'none' }} />
            </button>
            <span style={{ fontSize: 12, color: 'var(--text-muted)' }}>{ar ? 'مواقعي' : 'My Sites'}</span>
            <ArrowRight size={12} style={{ color: 'var(--text-faint)', transform: ar ? 'rotate(180deg)' : 'none' }} />
            <span style={{ fontSize: 12, color: 'var(--text-muted)' }}>{siteName}</span>
            {parentContext && (
              <>
                <ArrowRight size={12} style={{ color: 'var(--text-faint)', transform: ar ? 'rotate(180deg)' : 'none' }} />
                <span style={{ fontSize: 12, color: 'var(--text-faint)' }}>{parentContext}</span>
              </>
            )}
          </div>
          <h1 style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
            <Layout size={22} style={{ color: 'var(--primary)' }} />
            {ar ? 'رصد الطاقة المرسلة للموقع' : 'Site Energy Monitoring'}
          </h1>
          <p>{ar ? `تسجيل قراءة عداد الموقع: ${siteName}${parentContext ? ` التابع للمحطة: ${parentContext}` : ''}` : `Recording site meter: ${siteName}${parentContext ? ` under station: ${parentContext}` : ''}`}</p>
        </div>
      </div>

      {status && (
        <div className={`alert ${status.type === 'success' ? 'alert-success' : 'alert-danger'}`} style={{ marginBottom: 20 }}>
          {status.msg}
        </div>
      )}

      {!hasMeter && (
        <div style={{
          background: 'rgba(220,53,69,0.1)',
          border: '1px solid var(--danger)',
          borderRadius: 8,
          padding: '12px 16px',
          marginBottom: 20,
        }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 6 }}>
            <AlertCircle size={16} style={{ color: 'var(--danger)' }} />
            <span style={{ fontSize: 14, fontWeight: 700, color: 'var(--danger)' }}>
              {ar ? 'لا يمكن إدخال قراءة' : 'Cannot Enter Reading'}
            </span>
          </div>
          <div style={{ fontSize: 12, color: 'var(--danger)', lineHeight: 1.6 }}>
            {ar
              ? 'هذا الموقع غير مرتبط بعداد نشط. يجب ربط الموقع بعداد أولاً قبل إدخال القراءة.'
              : 'This site is not linked to an active meter. Link the site to a meter before entering readings.'}
          </div>
        </div>
      )}

      <form onSubmit={handleSubmit}>
        <div className="form-2col" style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 20 }}>
          <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>

            {/* التاريخ */}
            <div className="glass-card">
              <div className="glass-card-header">
                <div className="glass-card-title" style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                  <Clock size={16} style={{ color: 'var(--primary)' }} />
                  {ar ? 'تاريخ القراءة' : 'Reading Date'}
                </div>
                {!canEditSchedule && (
                  <span className="badge badge-muted">{ar ? 'تلقائي' : 'Auto'}</span>
                )}
              </div>
              <div className="glass-card-body">
                <div className="form-group">
                  <label style={labelStyle}>{ar ? 'التاريخ *' : 'Date *'}</label>
                  <input className="form-control" type="date"
                    value={form.readingDate} onChange={e => fld('readingDate', e.target.value)}
                    disabled={!canEditSchedule} required />
                </div>
              </div>
            </div>

            {/* عداد الموقع */}
            <div className="glass-card">
              <div className="glass-card-header">
                <div className="glass-card-title" style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                  <Eye size={16} style={{ color: 'var(--warning)' }} />
                  {ar ? 'قراءة عداد الرصد (الشبكة)' : 'Grid Meter Reading'}
                </div>
              </div>
              <div className="glass-card-body">

                {/* رقم الموقع ورقم العداد */}
                {hasMeter && (
                  <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 10, marginBottom: 12 }}>
                    <div style={{ background: 'var(--card-2)', borderRadius: 8, padding: '10px 14px' }}>
                      <div style={{ ...labelStyle, marginBottom: 4 }}>{ar ? 'رقم الموقع' : 'Site Number'}</div>
                      <div style={{ fontSize: 14, fontWeight: 700 }}>{subStationId || '—'}</div>
                    </div>
                    <div style={{ background: 'var(--card-2)', borderRadius: 8, padding: '10px 14px' }}>
                      <div style={{ ...labelStyle, marginBottom: 4 }}>{ar ? 'رقم العداد' : 'Meter Number'}</div>
                      <div style={{ fontSize: 14, fontWeight: 700 }}>{gridMeter?.meterName || gridMeter?.name || '—'}</div>
                    </div>
                  </div>
                )}

                {/* القراءة السابقة للطاقة المرسلة */}
                {(lastReading || station || gridMeter) && (
                  <div className="form-group">
                    <label style={labelStyle}>
                      {ar
                        ? `القراءة السابقة للطاقة المرسلة (KWh)${gridMeter ? ` - عداد: ${gridMeter.meterName}` : ''}`
                        : `Previous Sent Energy Reading (KWh)${gridMeter ? ` - Meter: ${gridMeter.meterName}` : ''}`}
                    </label>
                    <div style={{
                      padding: '10px 14px',
                      background: 'var(--card-2)',
                      borderRadius: 8,
                      border: '1px solid var(--border)',
                      fontSize: 16,
                      fontWeight: 700,
                      color: 'var(--text-muted)',
                      display: 'flex',
                      alignItems: 'center',
                      gap: 8,
                    }}>
                      <Activity size={14} style={{ color: 'var(--primary)', opacity: 0.7 }} />
                      {lastReading
                        ? (lastReading.currentExportedEnergyKWh?.toLocaleString() ?? '—')
                        : (gridMeter ? (gridMeter.energyReading?.toLocaleString() ?? '0') : (station?.lastGridRecord?.toLocaleString() ?? '0'))}
                      <span style={{ fontSize: 11, fontWeight: 400, color: 'var(--text-faint)' }}>KWh</span>
                      <span style={{ fontSize: 10, marginRight: 'auto', color: 'var(--text-faint)' }}>
                        {lastReading
                          ? new Date(lastReading.readingDate).toLocaleDateString(ar ? 'ar' : 'en')
                          : (gridMeter ? (ar ? 'قراءة العداد الأولية' : 'Initial Meter Reading') : (ar ? 'القراءة الأولية للموقع' : 'Initial Site Reading'))}
                      </span>
                    </div>
                  </div>
                )}

                {/* القراءة الحالية */}
                <div className="form-group">
                  <label style={labelStyle}>{ar ? 'الطاقة المرسلة للشبكة (KWh) *' : 'Sent Energy to Grid (KWh) *'}</label>
                  <input
                    className="form-control"
                    type="number"
                    step="0.1"
                    min={prevGridVal}
                    value={form.currentGridRecord}
                    onChange={e => fld('currentGridRecord', sanitizeNumeric(e.target.value))}
                    disabled={!hasMeter}
                    required
                    style={
                      form.currentGridRecord && parseFloat(form.currentGridRecord) < prevGridVal
                        ? { borderColor: 'var(--danger)', boxShadow: '0 0 0 2px hsla(0,80%,50%,0.2)' }
                        : {}
                    }
                  />
                  {form.currentGridRecord && parseFloat(form.currentGridRecord) < prevGridVal && (
                    <div style={{ fontSize: 11, color: 'var(--danger)', marginTop: 5, display: 'flex', alignItems: 'center', gap: 4 }}>
                      <AlertCircle size={11} />
                      {ar
                        ? `يجب أن تكون أكبر من أو تساوي القراءة السابقة (${prevGridVal} KWh)`
                        : `Must be ≥ previous reading (${prevGridVal} KWh)`}
                    </div>
                  )}
                </div>

                {/* ✅ حقل الاستهلاك الداخلي الجديد */}
                <div className="form-group">
                  <label style={labelStyle}>
                    <span style={{ display: 'flex', alignItems: 'center', gap: 5 }}>
                      <Zap size={11} style={{ color: 'var(--warning)' }} />
                      {ar ? 'الاستهلاك الداخلي (KWh)' : 'Internal Consumption (KWh)'}
                    </span>
                  </label>
                  <input
                    className="form-control"
                    type="number"
                    step="0.1"
                    min={0}
                    placeholder={ar ? 'أدخل الاستهلاك الداخلي...' : 'Enter internal consumption...'}
                    value={form.internalConsumption}
                    onChange={e => fld('internalConsumption', sanitizeNumeric(e.target.value))}
                    disabled={!hasMeter}
                  />
                  <div style={{ fontSize: 10, color: 'var(--text-faint)', marginTop: 4 }}>
                    {ar
                      ? 'الطاقة المستهلكة داخلياً في الموقع (اختياري)'
                      : 'Energy consumed internally at the site (optional)'}
                  </div>
                </div>

                <div className="form-group" style={{ margin: 0 }}>
                  <label style={labelStyle}>{ar ? 'ملاحظات' : 'Notes'}</label>
                  <textarea className="form-control" rows={3} value={form.notes} onChange={e => fld('notes', e.target.value)}
                    disabled={!hasMeter} />
                </div>
              </div>
            </div>
          </div>

          {/* الملخص */}
          <div>
            <div className="glass-card" style={{ position: 'sticky', top: 20 }}>
              <div className="glass-card-header">
                <div className="glass-card-title">{ar ? 'ملخص إنتاج الموقع (24 ساعة)' : 'Site Production Summary (24h)'}</div>
              </div>
              <div className="glass-card-body" style={{ display: 'flex', flexDirection: 'column', gap: 15 }}>

                <div style={{ background: 'var(--primary-glow)', padding: 15, borderRadius: 10, border: '1px solid hsla(210,100%,60%,0.1)' }}>
                  <div style={labelStyle}>{ar ? 'إجمالي إنتاج المولدات' : 'Total Gen Production'}</div>
                  <div style={{ fontSize: 28, fontWeight: 800, color: 'var(--primary)' }}>
                    {production.toLocaleString()} <span style={{ fontSize: 14 }}>KWh</span>
                  </div>
                  <div style={{ fontSize: 10, color: 'var(--text-muted)', marginTop: 4 }}>
                    {ar ? '(مجموع القراءات اليومية لجميع المولدات بالموقع)' : '(Sum of all daily gen readings in this site)'}
                  </div>
                </div>

                <div style={{ background: 'var(--card-2)', padding: 12, borderRadius: 8 }}>
                  <div style={labelStyle}>{ar ? 'قراءة العداد الحالية' : 'Current Meter Reading'}</div>
                  <div style={{ fontSize: 18, fontWeight: 700, color: 'var(--text-muted)' }}>
                    {form.currentGridRecord || '—'} <span style={{ fontSize: 11 }}>KWh</span>
                  </div>
                </div>

                {/* الفارق = الطاقة المرسلة الإجمالية */}
                {energySentDiff !== null && energySentDiff >= 0 && (
                  <div style={{ background: 'var(--primary-glow)', padding: 12, borderRadius: 8, border: '1px solid hsla(210,100%,60%,0.15)' }}>
                    <div style={labelStyle}>{ar ? 'الطاقة المرسلة الإجمالية (الحالية − السابقة)' : 'Total Sent Energy (Current − Previous)'}</div>
                    <div style={{ fontSize: 22, fontWeight: 800, color: 'var(--secondary)' }}>
                      {energySentDiff.toLocaleString()}
                      <span style={{ fontSize: 11, fontWeight: 400, marginRight: 4 }}>KWh</span>
                    </div>
                    <div style={{ fontSize: 10, color: 'var(--text-muted)', marginTop: 3 }}>
                      {form.currentGridRecord} − {prevGridVal} = {energySentDiff.toLocaleString()} KWh
                    </div>
                  </div>
                )}

                {/* ✅ بطاقة الاستهلاك الداخلي في الملخص */}
                {internalConsumptionVal > 0 && (
                  <div style={{ background: 'hsla(35,100%,50%,0.08)', padding: 12, borderRadius: 8, border: '1px solid hsla(35,100%,50%,0.15)' }}>
                    <div style={labelStyle}>
                      <span style={{ display: 'flex', alignItems: 'center', gap: 5 }}>
                        <Zap size={10} style={{ color: 'var(--warning)' }} />
                        {ar ? 'الاستهلاك الداخلي' : 'Internal Consumption'}
                      </span>
                    </div>
                    <div style={{ fontSize: 20, fontWeight: 800, color: 'var(--warning)' }}>
                      {internalConsumptionVal.toLocaleString()}
                      <span style={{ fontSize: 11, fontWeight: 400, marginRight: 4 }}>KWh</span>
                    </div>
                  </div>
                )}


                <button
                  type="submit"
                  className="btn btn-primary"
                  disabled={saving || !hasMeter || !form.currentGridRecord || parseFloat(form.currentGridRecord) < prevGridVal}
                  style={{ width: '100%', padding: 15, marginTop: 10 }}
                >
                  {saving
                    ? (ar ? 'جاري الحفظ...' : 'Saving...')
                    : (ar ? 'حفظ قراءة الموقع' : 'Save Site Reading')}
                </button>
              </div>
            </div>
          </div>
        </div>
      </form>
    </div>
  );
}

export default SubStationReadingPage;
