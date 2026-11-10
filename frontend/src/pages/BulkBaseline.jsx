import React, { useState, useEffect, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import { ListPlus, ArrowLeft, ArrowRight, Check } from 'lucide-react';
import client from '../api/client';
import { Button, FormField, Input } from '../components/ui';

export default function BulkBaseline({ lang }) {
  const ar = lang === 'ar';
  const navigate = useNavigate();

  const [generators, setGenerators] = useState([]);
  const [serviceItems, setServiceItems] = useState([]);
  const [selectedGenId, setSelectedGenId] = useState('');
  const [commonHours, setCommonHours] = useState('');
  const [commonDate, setCommonDate] = useState(new Date().toISOString().slice(0, 16));
  const [itemRows, setItemRows] = useState({});
  const [submitting, setSubmitting] = useState(false);
  const [errorMsg, setErrorMsg] = useState('');
  const [successMsg, setSuccessMsg] = useState('');

  // Idempotency: generated once per submit attempt, reused on retry, renewed on edit after failure or on success
  const clientRequestIdRef = useRef(crypto.randomUUID());

  const resetErrorAndRenewKey = () => {
    if (errorMsg) {
      clientRequestIdRef.current = crypto.randomUUID();
      setErrorMsg('');
    }
  };

  useEffect(() => {
    const fetchData = async () => {
      try {
        const [genRes, itemRes] = await Promise.all([
          client.get('/api/generators'),
          client.get('/api/service-items'),
        ]);
        setGenerators(genRes.data?.items || genRes.data || []);
        const items = itemRes.data || [];
        setServiceItems(items);

        // Pre-fill item rows
        const initial = {};
        items.forEach(i => {
          initial[i.id] = { selected: true, hours: '', date: new Date().toISOString().slice(0, 16), notes: '' };
        });
        setItemRows(initial);
      } catch (e) {
        console.error(e);
      }
    };
    fetchData();
  }, []);

  const handleApplyCommonHours = () => {
    if (!commonHours) return;
    setItemRows(prev => {
      const next = { ...prev };
      Object.keys(next).forEach(k => {
        next[k] = { ...next[k], hours: commonHours, date: commonDate };
      });
      return next;
    });
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setErrorMsg('');
    setSuccessMsg('');

    if (!selectedGenId) {
      setErrorMsg(ar ? 'يرجى اختيار المولد' : 'Please select a generator');
      return;
    }

    const rowsToSubmit = [];
    for (const item of serviceItems) {
      const row = itemRows[item.id];
      if (row && row.selected && row.hours !== '') {
        rowsToSubmit.push({
          serviceItemId: item.id,
          hoursAtService: parseFloat(row.hours),
          occurredAtUtc: new Date(row.date || commonDate).toISOString(),
          notes: row.notes?.trim() || 'Initial bulk baseline',
        });
      }
    }

    if (rowsToSubmit.length === 0) {
      setErrorMsg(ar ? 'يرجى تحديد بند واحد على الأقل وإدخال ساعاته' : 'Please select at least one item with hours');
      return;
    }

    setSubmitting(true);
    try {
      await client.post('/api/service-replacements/bulk-baseline', {
        generatorId: parseInt(selectedGenId),
        items: rowsToSubmit,
        clientRequestId: clientRequestIdRef.current,
      });

      clientRequestIdRef.current = crypto.randomUUID();
      setSuccessMsg(ar ? 'تم تأسيس ساعات البنود بنجاح!' : 'Bulk baseline saved successfully!');
      setTimeout(() => {
        navigate('/maintenance/service');
      }, 1200);
    } catch (err) {
      setErrorMsg(err.response?.data?.error || (ar ? 'فشل حفظ التأسيس الجماعي' : 'Failed to save bulk baseline'));
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div style={{ animation: 'fadeIn 0.3s ease', maxWidth: 860, margin: '0 auto', paddingBottom: 40 }}>
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
          <ListPlus size={22} style={{ color: 'var(--primary)' }} />
          {ar ? 'تأسيس أولي جماعي لساعات بنود الصيانة' : 'Bulk Service Baseline Entry'}
        </h1>
        <p style={{ opacity: 0.8, fontSize: '0.875rem', marginTop: 4 }}>
          {ar ? 'إدخال ساعات الأساس لجميع بنود الصيانة للمولد في خطوة واحدة لبدء حساب فترات الصيانة' : 'Set initial baseline running hours for all service items at once'}
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
          <FormField label={ar ? 'المولد المستهدف (إلزامي)' : 'Target Generator (Required)'}>
            <select
              className="ft-input"
              required
              value={selectedGenId}
              onChange={(e) => setSelectedGenId(e.target.value)}
              style={{ width: '100%', height: 38 }}
            >
              <option value="">{ar ? '-- اختر المولد --' : '-- Select Generator --'}</option>
              {generators.map(g => (
                <option key={g.id} value={g.id}>
                  {g.name} {g.model ? `(${g.model})` : ''} - {g.subStationName || g.subStation?.name}
                </option>
              ))}
            </select>
          </FormField>

          {/* Quick Apply Card */}
          <div style={{ background: '#f9fafb', border: '1px solid #e5e7eb', borderRadius: 6, padding: 14, marginBottom: 20 }}>
            <div style={{ fontSize: '0.85rem', fontWeight: 600, color: '#374151', marginBottom: 8 }}>
              {ar ? 'تطبيق ساعات موحدة على جميع البنود:' : 'Apply same hours to all items:'}
            </div>
            <div style={{ display: 'flex', gap: 12, alignItems: 'center', flexWrap: 'wrap' }}>
              <input
                type="number"
                step="0.1"
                placeholder={ar ? 'ساعات التشغيل (مثال: 5000)' : 'Operating hours (e.g. 5000)'}
                className="ft-input"
                value={commonHours}
                onChange={(e) => setCommonHours(e.target.value)}
                style={{ width: 220 }}
              />
              <input
                type="datetime-local"
                className="ft-input"
                value={commonDate}
                onChange={(e) => setCommonDate(e.target.value)}
                style={{ width: 200 }}
              />
              <button
                type="button"
                className="btn btn-secondary btn-sm"
                onClick={handleApplyCommonHours}
              >
                {ar ? 'تطبيق على الكل' : 'Apply to All'}
              </button>
            </div>
          </div>

          {/* Items Table */}
          <div style={{ overflowX: 'auto', marginBottom: 24 }}>
            <table className="table" style={{ width: '100%', fontSize: '0.9rem', textAlign: ar ? 'right' : 'left' }}>
              <thead>
                <tr style={{ background: '#f3f4f6', borderBottom: '2px solid #e5e7eb' }}>
                  <th style={{ padding: '8px 10px', width: 40 }}>{ar ? 'تضمين' : 'Include'}</th>
                  <th style={{ padding: '8px 10px' }}>{ar ? 'بند الصيانة' : 'Service Item'}</th>
                  <th style={{ padding: '8px 10px' }}>{ar ? 'الفترة المقترحة' : 'Suggested Interval'}</th>
                  <th style={{ padding: '8px 10px', minWidth: 150 }}>{ar ? 'ساعات الأساس (h)' : 'Baseline Hours (h)'}</th>
                  <th style={{ padding: '8px 10px', minWidth: 180 }}>{ar ? 'التاريخ والوقت' : 'Date & Time'}</th>
                </tr>
              </thead>
              <tbody>
                {serviceItems.map(item => {
                  const row = itemRows[item.id] || { selected: true, hours: '', date: commonDate };
                  return (
                    <tr key={item.id} style={{ borderBottom: '1px solid #f3f4f6' }}>
                      <td style={{ padding: '8px 10px', textAlign: 'center' }}>
                        <input
                          type="checkbox"
                          checked={row.selected}
                          onChange={(e) => {
                            const val = e.target.checked;
                            setItemRows(prev => ({
                              ...prev,
                              [item.id]: { ...prev[item.id], selected: val },
                            }));
                          }}
                        />
                      </td>
                      <td style={{ padding: '8px 10px', fontWeight: 600 }}>
                        {ar ? item.nameAr : item.nameEn}
                        <div style={{ fontSize: '0.75rem', color: '#6b7280' }}>{item.code}</div>
                      </td>
                      <td style={{ padding: '8px 10px' }}>
                        {item.suggestedIntervalHours != null ? `${item.suggestedIntervalHours} h` : '—'}
                      </td>
                      <td style={{ padding: '8px 10px' }}>
                        <input
                          type="number"
                          step="0.1"
                          placeholder="e.g. 5000"
                          className="ft-input"
                          disabled={!row.selected}
                          value={row.hours}
                          onChange={(e) => {
                            const val = e.target.value;
                            setItemRows(prev => ({
                              ...prev,
                              [item.id]: { ...prev[item.id], hours: val },
                            }));
                          }}
                          style={{ width: '100%', height: 34 }}
                        />
                      </td>
                      <td style={{ padding: '8px 10px' }}>
                        <input
                          type="datetime-local"
                          className="ft-input"
                          disabled={!row.selected}
                          value={row.date}
                          onChange={(e) => {
                            const val = e.target.value;
                            setItemRows(prev => ({
                              ...prev,
                              [item.id]: { ...prev[item.id], date: val },
                            }));
                          }}
                          style={{ width: '100%', height: 34 }}
                        />
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>

          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 12 }}>
            <Button variant="secondary" type="button" onClick={() => navigate('/maintenance/service')}>
              {ar ? 'إلغاء' : 'Cancel'}
            </Button>
            <Button variant="primary" type="submit" disabled={submitting}>
              {submitting ? (ar ? 'جاري الحفظ...' : 'Saving...') : (ar ? 'حفظ التأسيس' : 'Save Baseline')}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}
