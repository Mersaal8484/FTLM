import React, { useState, useEffect } from 'react';
import client from '../api/client';
import { useParams, useNavigate } from 'react-router-dom';
import { Droplet, ArrowRight, CheckCircle, AlertCircle, ClipboardList, ChevronLeft } from 'lucide-react';

function AddOilToGenerator({ lang }) {
  const ar = lang === 'ar';
  const { generatorId } = useParams();
  const navigate = useNavigate();

  const [oils, setOils] = useState([]);
  const [generator, setGenerator] = useState(null);
  const [transactions, setTransactions] = useState([]);
  const [selectedOil, setSelectedOil] = useState('');
  const [quantity, setQuantity] = useState('');
  const [notes, setNotes] = useState('');

  const sanitizeNumeric = (v, allowDecimal = true) => {
    let s = v.replace(/[^0-9.]/g, '');
    if (!allowDecimal) return s.replace(/\./g, '');
    const p = s.split('.');
    if (p.length > 2) s = p[0] + '.' + p.slice(1).join('');
    if (p.length === 2 && p[1].length > 2) s = p[0] + '.' + p[1].slice(0, 2);
    return s;
  };

  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [status, setStatus] = useState(null); // { type: 'success'|'error', msg }

  useEffect(() => {
    const fetchData = async () => {
      setLoading(true);
      try {
        // ✅ جلب بيانات المولد أولاً للحصول على الموقع
        const [genRes, txRes] = await Promise.all([
          client.get(`/api/generators/my/${generatorId}`),
          client.get(`/api/oils/transactions/generator/${generatorId}`),
        ]);
        const gen = genRes.data;
        setGenerator(gen);
        setTransactions(txRes.data);

        // ✅ جلب الزيوت الخاصة بموقع المولد فقط
        const stationId = gen?.stationId;
        const oilsRes = await client.get('/api/oils', {
          params: stationId ? { stationId } : {}
        });
        setOils(oilsRes.data);
      } catch {
        setStatus({ type: 'error', msg: ar ? 'فشل تحميل البيانات' : 'Failed to load data' });
      } finally {
        setLoading(false);
      }
    };
    fetchData();
  }, [generatorId, ar]);

  const selectedOilData = oils.find(o => String(o.id) === String(selectedOil));

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!selectedOil) { setStatus({ type: 'error', msg: ar ? 'يرجى اختيار نوع الزيت' : 'Please select an oil type' }); return; }
    if (!quantity || parseFloat(quantity) <= 0) { setStatus({ type: 'error', msg: ar ? 'يرجى إدخال كمية صحيحة' : 'Please enter a valid quantity' }); return; }

    setSaving(true); setStatus(null);
    try {
      await client.post('/api/oils/add-to-generator', {
        generatorId: parseInt(generatorId, 10),
        oilId: parseInt(selectedOil, 10),
        quantity: parseFloat(quantity),
        notes,
      });
      setStatus({ type: 'success', msg: ar ? 'تم إضافة الزيت بنجاح ✓' : 'Oil added successfully ✓' });
      setQuantity('');
      setNotes('');
      setSelectedOil('');
      // ✅ تحديث الزيوت مع الفلترة بالموقع
      const stationId = generator?.stationId;
      const [oilsRes, txRes] = await Promise.all([
        client.get('/api/oils', { params: stationId ? { stationId } : {} }),
        client.get(`/api/oils/transactions/generator/${generatorId}`),
      ]);
      setOils(oilsRes.data);
      setTransactions(txRes.data);
    } catch (err) {
      setStatus({ type: 'error', msg: err.response?.data?.error || (ar ? 'حدث خطأ أثناء الإضافة' : 'Error occurred') });
    } finally {
      setSaving(false);
    }
  };

  const formatDate = (d) => {
    if (!d) return '—';
    return new Date(d).toLocaleDateString(ar ? 'ar-YE' : 'en-US', {
      year: 'numeric', month: 'short', day: 'numeric',
    });
  };

  return (
    <div>
      {/* Page Header */}
      <div className="page-header">
        <div className="page-header-row">
          <div>
            <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 6 }}>
              <button
                className="btn btn-ghost btn-sm btn-icon"
                onClick={() => navigate('/my-generators')}
                style={{ padding: '5px 8px' }}
              >
                <ChevronLeft size={16} style={{ transform: ar ? 'rotate(180deg)' : 'none' }} />
              </button>
              <span style={{ fontSize: 12, color: 'var(--text-muted)' }}>
                {ar ? 'المولدات' : 'Generators'}
              </span>
              <ArrowRight size={12} style={{ color: 'var(--text-faint)', transform: ar ? 'rotate(180deg)' : 'none' }} />
              <span style={{ fontSize: 12, color: 'var(--text-muted)' }}>
                {ar ? 'إضافة زيت' : 'Add Oil'}
              </span>
            </div>
            <h1 style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
              <Droplet size={22} style={{ color: 'var(--warning)' }} />
              {ar ? 'إضافة زيت للمولد' : 'Add Oil to Generator'}
            </h1>
            {generator && (
              <p style={{ marginTop: 4 }}>
                <span style={{ color: 'var(--primary)', fontWeight: 600 }}>{generator.name}</span>
                {generator.location && <span style={{ color: 'var(--text-muted)' }}> — {generator.location}</span>}
              </p>
            )}
          </div>
        </div>
      </div>

      {loading ? (
        <div className="spinner-wrap"><div className="ft-spinner" /></div>
      ) : (
        <div className="form-2col" style={{ display: 'grid', gridTemplateColumns: '1fr 1.2fr', gap: 20 }}>

          {/* ── Left: Form ── */}
          <div>
            <div className="glass-card">
              <div className="glass-card-header">
                <div>
                  <div className="glass-card-title">{ar ? 'سجل إضافة الزيت' : 'Oil Addition Form'}</div>
                  <div className="glass-card-sub">{ar ? 'يرجى ملء البيانات بدقة' : 'Please fill in details carefully'}</div>
                </div>
              </div>
              <div className="glass-card-body">
                {/* Status Alert */}
                {status && (
                  <div className={`alert ${status.type === 'success' ? 'alert-success' : 'alert-danger'}`} style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                    {status.type === 'success'
                      ? <CheckCircle size={15} />
                      : <AlertCircle size={15} />
                    }
                    {status.msg}
                  </div>
                )}

                <form onSubmit={handleSubmit}>
                  {/* Oil Type */}
                  <div className="form-group">
                    <label className="form-label">{ar ? 'نوع الزيت *' : 'Oil Type *'}</label>
                    <select
                      className="form-select"
                      value={selectedOil}
                      onChange={e => setSelectedOil(e.target.value)}
                      required
                    >
                      <option value="">{ar ? '— اختر نوع الزيت —' : '— Select Oil Type —'}</option>
                      {oils.map(oil => (
                        <option key={oil.id || oil._id} value={oil.id || oil._id} disabled={oil.currentStock <= 0}>
                          {oil.name} ({ar ? 'المتبقي:' : 'Available:'} {oil.currentStock} {ar ? 'لتر' : 'L'})
                          {oil.currentStock <= oil.minStock ? (ar ? ' ⚠ منخفض' : ' ⚠ Low') : ''}
                        </option>
                      ))}
                    </select>
                  </div>

                  {/* Selected Oil Info */}
                  {selectedOilData && (
                    <div style={{
                      background: 'var(--card-2)', borderRadius: 10, padding: '12px 14px',
                      marginBottom: 16, display: 'flex', justifyContent: 'space-between',
                      border: '1px solid var(--border)',
                    }}>
                      <div>
                        <div style={{ fontSize: 11, color: 'var(--text-muted)', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.5px' }}>
                          {ar ? 'المخزون المتاح' : 'Available Stock'}
                        </div>
                        <div style={{ fontSize: 20, fontWeight: 800, color: selectedOilData.currentStock <= selectedOilData.minStock ? 'var(--danger)' : 'var(--secondary)' }}>
                          {selectedOilData.currentStock}
                          <span style={{ fontSize: 12, fontWeight: 400, marginRight: 4, color: 'var(--text-muted)' }}>
                            {ar ? 'لتر' : 'L'}
                          </span>
                        </div>
                      </div>
                      <div>
                        <div style={{ fontSize: 11, color: 'var(--text-muted)', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.5px' }}>
                          {ar ? 'الحد الأدنى' : 'Min Stock'}
                        </div>
                        <div style={{ fontSize: 20, fontWeight: 800, color: 'var(--text-muted)' }}>
                          {selectedOilData.minStock}
                          <span style={{ fontSize: 12, fontWeight: 400, marginRight: 4 }}>
                            {ar ? 'لتر' : 'L'}
                          </span>
                        </div>
                      </div>
                    </div>
                  )}

                  {/* Quantity */}
                  <div className="form-group">
                    <label className="form-label">{ar ? 'الكمية المضافة (لتر) *' : 'Quantity Added (L) *'}</label>
                    <input
                      className="form-control"
                      type="number" step="0.1" min="0.1"
                      placeholder={ar ? 'أدخل الكمية...' : 'Enter quantity...'}
                      value={quantity}
                      onChange={e => setQuantity(sanitizeNumeric(e.target.value))}
                      required
                    />
                    {selectedOilData && quantity && parseFloat(quantity) > selectedOilData.currentStock && (
                      <div style={{ fontSize: 11, color: 'var(--danger)', marginTop: 4, display: 'flex', alignItems: 'center', gap: 4 }}>
                        <AlertCircle size={11} />
                        {ar ? 'الكمية تتجاوز المخزون المتاح' : 'Quantity exceeds available stock'}
                      </div>
                    )}
                  </div>

                  {/* Notes */}
                  <div className="form-group">
                    <label className="form-label">{ar ? 'ملاحظات' : 'Notes'}</label>
                    <textarea
                      className="form-control"
                      rows={3}
                      placeholder={ar ? 'أي ملاحظات إضافية...' : 'Any additional notes...'}
                      value={notes}
                      onChange={e => setNotes(e.target.value)}
                      style={{ resize: 'vertical', fontFamily: 'inherit' }}
                    />
                  </div>

                  {/* Submit */}
                  <button
                    className="btn btn-primary"
                    type="submit"
                    disabled={saving || !selectedOil || !quantity}
                    style={{ width: '100%', padding: '11px', fontSize: 14 }}
                  >
                    {saving ? (
                      <><span className="ft-spinner" style={{ width: 14, height: 14 }} /> {ar ? 'جاري الحفظ...' : 'Saving...'}</>
                    ) : (
                      <><Droplet size={16} /> {ar ? 'تأكيد إضافة الزيت' : 'Confirm Oil Addition'}</>
                    )}
                  </button>
                </form>
              </div>
            </div>
          </div>

          {/* ── Right: History ── */}
          <div>
            <div className="glass-card">
              <div className="glass-card-header">
                <div>
                  <div className="glass-card-title" style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                    <ClipboardList size={16} style={{ color: 'var(--primary)' }} />
                    {ar ? 'سجل إضافات الزيت' : 'Oil Addition History'}
                  </div>
                  <div className="glass-card-sub">
                    {transactions.length} {ar ? 'عملية مسجلة' : 'records'}
                  </div>
                </div>
              </div>

              {transactions.length === 0 ? (
                <div className="empty-state" style={{ padding: '40px 20px' }}>
                  <ClipboardList size={40} />
                  <h3 style={{ fontSize: 14 }}>{ar ? 'لا توجد عمليات سابقة' : 'No history yet'}</h3>
                </div>
              ) : (
                <div style={{ overflowX: 'auto' }}>
                  <table className="ft-table">
                    <thead>
                      <tr>
                        <th>{ar ? 'التاريخ' : 'Date'}</th>
                        <th>{ar ? 'نوع الزيت' : 'Oil Type'}</th>
                        <th>{ar ? 'الكمية' : 'Qty (L)'}</th>
                        <th>{ar ? 'ملاحظات' : 'Notes'}</th>
                      </tr>
                    </thead>
                    <tbody>
                      {transactions.map(tx => (
                        <tr key={tx.id}>
                          <td style={{ color: 'var(--text-muted)', fontSize: 12, whiteSpace: 'nowrap' }}>
                            {formatDate(tx.transactionDate)}
                          </td>
                          <td>
                            <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
                              <Droplet size={12} style={{ color: 'var(--warning)', flexShrink: 0 }} />
                              <span style={{ fontWeight: 500 }}>{tx.oilName || '—'}</span>
                            </div>
                          </td>
                          <td>
                            <span style={{ fontWeight: 700, color: 'var(--warning)' }}>{tx.quantityLiters}</span>
                            <span style={{ fontSize: 11, color: 'var(--text-muted)', marginRight: 3 }}>{ar ? 'لتر' : 'L'}</span>
                          </td>
                          <td style={{ color: 'var(--text-muted)', fontSize: 12 }}>
                            {tx.reference || '—'}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          </div>

        </div>
      )}
    </div>
  );
}

export default AddOilToGenerator;
