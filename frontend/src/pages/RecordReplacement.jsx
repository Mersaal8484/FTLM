import React, { useState, useEffect, useRef } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { Wrench, ArrowLeft, ArrowRight, AlertTriangle, Check } from 'lucide-react';
import client from '../api/client';
import { Button, FormField, Input } from '../components/ui';

export default function RecordReplacement({ lang }) {
  const ar = lang === 'ar';
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();

  const [generators, setGenerators] = useState([]);
  const [serviceItems, setServiceItems] = useState([]);
  const [selectedGenId, setSelectedGenId] = useState(searchParams.get('generatorId') || '');
  const [selectedItemId, setSelectedItemId] = useState(searchParams.get('serviceItemId') || '');
  const [hoursAtService, setHoursAtService] = useState('');
  const [occurredAtUtc, setOccurredAtUtc] = useState(new Date().toISOString().slice(0, 16));
  const [workPermitId, setWorkPermitId] = useState('');
  const [earlyReason, setEarlyReason] = useState('');
  const [notes, setNotes] = useState('');
  const [selectedGenInfo, setSelectedGenInfo] = useState(null);
  const [submitting, setSubmitting] = useState(false);
  const [errorMsg, setErrorMsg] = useState('');
  const [successMsg, setSuccessMsg] = useState('');

  // Idempotency: generated once per submit attempt, reused on retry, renewed on edit after failure or on success
  const clientRequestIdRef = useRef(crypto.randomUUID());

  const handleInputChange = (setter) => (e) => {
    if (errorMsg) {
      clientRequestIdRef.current = crypto.randomUUID();
      setErrorMsg('');
    }
    setter(e.target.value);
  };

  useEffect(() => {
    const fetchData = async () => {
      try {
        const [genRes, itemRes] = await Promise.all([
          client.get('/api/generators'),
          client.get('/api/service-items'),
        ]);
        setGenerators(genRes.data?.items || genRes.data || []);
        setServiceItems(itemRes.data || []);
      } catch (e) {
        console.error(e);
      }
    };
    fetchData();
  }, []);

  useEffect(() => {
    if (selectedGenId) {
      const gen = generators.find(g => String(g.id) === String(selectedGenId));
      setSelectedGenInfo(gen || null);
    } else {
      setSelectedGenInfo(null);
    }
  }, [selectedGenId, generators]);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setErrorMsg('');
    setSuccessMsg('');

    if (!selectedGenId || !selectedItemId || !hoursAtService) {
      setErrorMsg(ar ? 'يرجى ملء جميع الحقول المطلوبة' : 'Please fill all required fields');
      return;
    }

    setSubmitting(true);
    try {
      await client.post('/api/service-replacements', {
        generatorId: parseInt(selectedGenId),
        serviceItemId: parseInt(selectedItemId),
        hoursAtService: parseFloat(hoursAtService),
        clientRequestId: clientRequestIdRef.current,
        occurredAtUtc: new Date(occurredAtUtc).toISOString(),
        workPermitId: workPermitId ? parseInt(workPermitId) : null,
        earlyReplacementReason: earlyReason.trim() || null,
        notes: notes.trim() || null,
      });

      clientRequestIdRef.current = crypto.randomUUID();
      setSuccessMsg(ar ? 'تم تسجيل الخدمة بنجاح!' : 'Service replacement recorded successfully!');
      setTimeout(() => {
        navigate('/maintenance/service');
      }, 1200);
    } catch (err) {
      setErrorMsg(err.response?.data?.error || (ar ? 'فشل تسجيل الخدمة' : 'Failed to record service'));
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div style={{ animation: 'fadeIn 0.3s ease', maxWidth: 640, margin: '0 auto', paddingBottom: 40 }}>
      <div style={{ marginBottom: 20 }}>
        <button
          className="btn btn-secondary btn-sm"
          onClick={() => navigate('/maintenance/service')}
          style={{ display: 'flex', alignItems: 'center', gap: 6, marginBottom: 12 }}
        >
          {ar ? <ArrowRight size={16} /> : <ArrowLeft size={16} />}
          {ar ? 'العودة لجداول الصيانة' : 'Back to Service Intervals'}
        </button>

        <h1 style={{ display: 'flex', alignItems: 'center', gap: 10, margin: 0, fontSize: '1.4rem' }}>
          <Wrench size={22} style={{ color: 'var(--primary)' }} />
          {ar ? 'تسجيل استبدال / خدمة صيانة' : 'Record Service Replacement'}
        </h1>
        <p style={{ opacity: 0.8, fontSize: '0.875rem', marginTop: 4 }}>
          {ar ? 'تسجيل استبدال بند صيانة للمولد يعيد تشغيل عداد فترات الصيانة لهذا البند' : 'Recording service restarts the maintenance clock for this item'}
        </p>
      </div>

      <div className="panel" style={{ padding: 24, borderRadius: 8, border: '1px solid var(--border-color, #e5e7eb)' }}>
        {errorMsg && (
          <div style={{ background: '#fee2e2', color: '#b91c1c', padding: '12px 16px', borderRadius: 6, marginBottom: 16, fontSize: '0.9rem' }}>
            {errorMsg}
          </div>
        )}
        {successMsg && (
          <div style={{ background: '#dcfce7', color: '#15803d', padding: '12px 16px', borderRadius: 6, marginBottom: 16, fontSize: '0.9rem', display: 'flex', alignItems: 'center', gap: 8 }}>
            <Check size={18} />
            {successMsg}
          </div>
        )}

        <form onSubmit={handleSubmit}>
          <FormField label={ar ? 'المولد (إلزامي)' : 'Generator (Required)'}>
            <select
              className="ft-input"
              required
              value={selectedGenId}
              onChange={handleInputChange(setSelectedGenId)}
              style={{ width: '100%' }}
            >
              <option value="">{ar ? '-- اختر المولد --' : '-- Select Generator --'}</option>
              {generators.map(g => (
                <option key={g.id} value={g.id}>
                  {g.name} {g.model ? `(${g.model})` : ''} - {g.subStationName || g.subStation?.name}
                </option>
              ))}
            </select>
          </FormField>

          <FormField label={ar ? 'بند الصيانة (إلزامي)' : 'Service Item (Required)'}>
            <select
              className="ft-input"
              required
              value={selectedItemId}
              onChange={handleInputChange(setSelectedItemId)}
              style={{ width: '100%' }}
            >
              <option value="">{ar ? '-- اختر بند الصيانة --' : '-- Select Service Item --'}</option>
              {serviceItems.map(i => (
                <option key={i.id} value={i.id}>
                  {ar ? i.nameAr : i.nameEn} ({i.code})
                </option>
              ))}
            </select>
          </FormField>

          <FormField label={ar ? 'ساعات التشغيل عند الخدمة (إلزامي)' : 'Running Hours at Service (Required)'}>
            <Input
              type="number"
              step="0.1"
              required
              placeholder="e.g. 5240"
              value={hoursAtService}
              onChange={handleInputChange(setHoursAtService)}
            />
          </FormField>

          <FormField label={ar ? 'سبب الاستبدال المبكر (إذا كان الاستبدال قبل اكتمال الفترة)' : 'Early Replacement Reason (If replacing before interval)'}>
            <Input
              type="text"
              placeholder={ar ? 'مطلوب في حال الاستبدال المبكر...' : 'Required if replaced early...'}
              value={earlyReason}
              onChange={handleInputChange(setEarlyReason)}
            />
          </FormField>

          <FormField label={ar ? 'تاريخ ووقت الخدمة' : 'Service Date & Time'}>
            <Input
              type="datetime-local"
              required
              value={occurredAtUtc}
              onChange={handleInputChange(setOccurredAtUtc)}
            />
          </FormField>

          <FormField label={ar ? 'رقم تصريح العمل (اختياري)' : 'Work Permit ID (Optional)'}>
            <Input
              type="number"
              placeholder="e.g. 1042"
              value={workPermitId}
              onChange={handleInputChange(setWorkPermitId)}
            />
          </FormField>

          <FormField label={ar ? 'ملاحظات (اختياري)' : 'Notes (Optional)'}>
            <textarea
              className="ft-input"
              rows="3"
              value={notes}
              onChange={handleInputChange(setNotes)}
              style={{ width: '100%' }}
            />
          </FormField>

          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 12, marginTop: 24 }}>
            <Button variant="secondary" type="button" onClick={() => navigate('/maintenance/service')}>
              {ar ? 'إلغاء' : 'Cancel'}
            </Button>
            <Button variant="primary" type="submit" disabled={submitting}>
              {submitting ? (ar ? 'جاري الحفظ...' : 'Saving...') : (ar ? 'حفظ السجل' : 'Save Record')}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}
