import React, { useState, useEffect, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import { Wrench, Clock, AlertTriangle, CheckCircle, Plus, History, Settings, ListPlus, X } from 'lucide-react';
import client from '../api/client';
import { Button, Modal, FormField, Input } from '../components/ui';

export default function GeneratorMaintenanceTable({ lang }) {
  const ar = lang === 'ar';
  const navigate = useNavigate();

  const [tables, setTables] = useState([]);
  const [loading, setLoading] = useState(true);
  const [subStations, setSubStations] = useState([]);
  const [selectedSubStation, setSelectedSubStation] = useState('');
  const [searchTerm, setSearchTerm] = useState('');

  // Client request ID for idempotency (reused on retry, renewed on edit after error or after success)
  const clientRequestIdRef = useRef(crypto.randomUUID());

  // Record replacement modal state
  const [modalOpen, setModalOpen] = useState(false);
  const [modalGen, setModalGen] = useState(null);
  const [modalItem, setModalItem] = useState(null);
  const [formHours, setFormHours] = useState('');
  const [formDate, setFormDate] = useState('');
  const [formPermit, setFormPermit] = useState('');
  const [formEarlyReason, setFormEarlyReason] = useState('');
  const [formNotes, setFormNotes] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [errorMsg, setErrorMsg] = useState('');

  // History modal state
  const [historyOpen, setHistoryOpen] = useState(false);
  const [historyItems, setHistoryItems] = useState([]);
  const [historyLoading, setHistoryLoading] = useState(false);
  const [historyGenName, setHistoryGenName] = useState('');
  const [historyItemName, setHistoryItemName] = useState('');
  const [voidModalOpen, setVoidModalOpen] = useState(false);
  const [voidRecordId, setVoidRecordId] = useState(null);
  const [voidReason, setVoidReason] = useState('');

  const loadData = async () => {
    setLoading(true);
    try {
      const url = selectedSubStation
        ? `/api/service-due/station?subStationId=${selectedSubStation}`
        : '/api/service-due/station';
      const res = await client.get(url);
      setTables(res.data || []);
    } catch (e) {
      console.error(e);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    const fetchSubStations = async () => {
      try {
        const res = await client.get('/api/substations');
        setSubStations(res.data?.items || res.data || []);
      } catch (e) {
        console.error(e);
      }
    };
    fetchSubStations();
  }, []);

  useEffect(() => {
    loadData();
  }, [selectedSubStation]);

  const handleInputChange = (setter) => (e) => {
    if (errorMsg) {
      clientRequestIdRef.current = crypto.randomUUID();
      setErrorMsg('');
    }
    setter(e.target.value);
  };

  const openRecordModal = (gen, item) => {
    clientRequestIdRef.current = crypto.randomUUID();
    setModalGen(gen);
    setModalItem(item);
    setFormHours(gen.currentOfficialHours != null ? String(gen.currentOfficialHours) : '');
    setFormDate(new Date().toISOString().slice(0, 16));
    setFormPermit('');
    setFormEarlyReason('');
    setFormNotes('');
    setErrorMsg('');
    setModalOpen(true);
  };

  const isEarlyReplacement = () => {
    if (!modalItem || !formHours || modalItem.lastServiceHours == null || !modalItem.intervalHours) return false;
    const delta = parseFloat(formHours) - modalItem.lastServiceHours;
    return delta < modalItem.intervalHours;
  };

  const handleSaveReplacement = async (e) => {
    e.preventDefault();
    if (!formHours || parseFloat(formHours) < 0) {
      setErrorMsg(ar ? 'يرجى إدخال ساعات تشغيل صحيحة' : 'Please enter valid operating hours');
      return;
    }
    if (isEarlyReplacement() && !formEarlyReason.trim()) {
      setErrorMsg(ar ? 'الاستبدال مبكر: سبب الاستبدال المبكر إلزامي' : 'Early replacement: reason is required');
      return;
    }

    setSubmitting(true);
    setErrorMsg('');
    try {
      await client.post('/api/service-replacements', {
        generatorId: modalGen.generatorId,
        serviceItemId: modalItem.serviceItemId,
        hoursAtService: parseFloat(formHours),
        clientRequestId: clientRequestIdRef.current,
        occurredAtUtc: new Date(formDate).toISOString(),
        workPermitId: formPermit ? parseInt(formPermit) : null,
        earlyReplacementReason: formEarlyReason.trim() || null,
        notes: formNotes.trim() || null,
      });
      clientRequestIdRef.current = crypto.randomUUID();
      setModalOpen(false);
      await loadData();
    } catch (err) {
      setErrorMsg(err.response?.data?.error || (ar ? 'فشل حفظ السجل' : 'Failed to save record'));
    } finally {
      setSubmitting(false);
    }
  };

  const openHistory = async (gen, item) => {
    setHistoryGenName(gen.generatorName);
    setHistoryItemName(ar ? item.serviceItemNameAr : item.serviceItemNameEn);
    setHistoryOpen(true);
    setHistoryLoading(true);
    try {
      const res = await client.get(`/api/service-replacements/history?generatorId=${gen.generatorId}&serviceItemId=${item.serviceItemId}`);
      setHistoryItems(res.data || []);
    } catch (e) {
      console.error(e);
    } finally {
      setHistoryLoading(false);
    }
  };

  const handleVoidRecord = async () => {
    if (!voidReason.trim()) return;
    try {
      await client.post(`/api/service-replacements/${voidRecordId}/void`, {
        reason: voidReason.trim(),
      });
      setVoidModalOpen(false);
      setVoidReason('');
      setVoidRecordId(null);
      setHistoryOpen(false);
      await loadData();
    } catch (err) {
      alert(err.response?.data?.error || (ar ? 'فشل إلغاء السجل' : 'Failed to void record'));
    }
  };

  const filteredTables = tables.filter(t => {
    if (!searchTerm) return true;
    const term = searchTerm.toLowerCase();
    return t.generatorName.toLowerCase().includes(term) ||
           (t.model && t.model.toLowerCase().includes(term)) ||
           t.subStationName.toLowerCase().includes(term);
  });

  const getStatusBadgeStyle = (status) => {
    switch (status) {
      case 4: // Overdue
        return { background: 'var(--danger-subtle)', color: 'var(--danger)', border: '1px solid color-mix(in srgb, var(--danger) 42%, transparent)' };
      case 3: // Due
        return { background: 'var(--warning-subtle)', color: 'var(--warning)', border: '1px solid color-mix(in srgb, var(--warning) 42%, transparent)' };
      case 2: // DueSoon
        return { background: 'var(--warning-subtle)', color: 'var(--warning)', border: '1px solid color-mix(in srgb, var(--warning) 28%, transparent)' };
      case 1: // Ok
        return { background: 'var(--success-subtle)', color: 'var(--success)', border: '1px solid color-mix(in srgb, var(--success) 38%, transparent)' };
      case 5: // NotConfigured
        return { background: 'var(--surface-2)', color: 'var(--text-muted)', border: '1px dashed var(--border-strong)' };
      default: // NotBaselined
        return { background: 'var(--surface-2)', color: 'var(--text-muted)', border: '1px solid var(--border)' };
    }
  };

  const getProgressColor = (status, fraction) => {
    if (status === 4 || fraction >= 1.0) return 'var(--danger)';
    if (status === 3 || status === 2 || fraction >= 0.85) return 'var(--warning)';
    if (status === 1) return 'var(--success)';
    return 'var(--text-muted)';
  };

  return (
    <div style={{ animation: 'fadeIn 0.3s ease', paddingBottom: 40 }}>
      {/* Page Header */}
      <div className="page-header" style={{ marginBottom: 20 }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', flexWrap: 'wrap', gap: 16 }}>
          <div>
            <h1 style={{ display: 'flex', alignItems: 'center', gap: 10, margin: 0, fontSize: '1.5rem' }}>
              <Wrench size={24} style={{ color: 'var(--primary)' }} />
              {ar ? 'جداول الصيانة وساعات التشغيل للمولدات' : 'Generator Service Intervals & Due Engine'}
            </h1>
            <p style={{ opacity: 0.8, fontSize: '0.9rem', margin: '4px 0 0 0' }}>
              {ar ? 'متابعة فترات صيانة المولدات الدورية المعتمدة على الساعات الرسمية وتنبيهات الاستحقاق' : 'Track hour-based periodic service intervals fed by official running hours'}
            </p>
          </div>
          <div style={{ display: 'flex', gap: 10, flexWrap: 'wrap' }}>
            <Button
              variant="secondary"
              onClick={() => navigate('/maintenance/service/baseline')}
              style={{ display: 'flex', alignItems: 'center', gap: 6 }}
            >
              <ListPlus size={16} />
              {ar ? 'تأسيس أولي جماعي' : 'Bulk Baseline'}
            </Button>
            <Button
              variant="secondary"
              onClick={() => navigate('/maintenance/service/definitions')}
              style={{ display: 'flex', alignItems: 'center', gap: 6 }}
            >
              <Settings size={16} />
              {ar ? 'تعريف الفترات' : 'Service Definitions'}
            </Button>
          </div>
        </div>
      </div>

      {/* Filter Bar */}
      <div style={{ display: 'flex', gap: 16, marginBottom: 20, flexWrap: 'wrap', alignItems: 'center' }}>
        <div style={{ minWidth: 220 }}>
          <select
            className="ft-input"
            value={selectedSubStation}
            onChange={(e) => setSelectedSubStation(e.target.value)}
            style={{ width: '100%', height: 38 }}
          >
            <option value="">{ar ? 'جميع المحطات الفرعية' : 'All Sub-Stations'}</option>
            {subStations.map(s => (
              <option key={s.id} value={s.id}>{s.name}</option>
            ))}
          </select>
        </div>
        <div style={{ flex: 1, minWidth: 240 }}>
          <input
            type="text"
            className="ft-input"
            placeholder={ar ? 'بحث باسم المولد أو الطراز...' : 'Search by generator name or model...'}
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            style={{ width: '100%', height: 38 }}
          />
        </div>
      </div>

      {/* Content */}
      {loading ? (
        <div className="ft-spinner" style={{ width: 40, height: 40, margin: '60px auto' }} />
      ) : filteredTables.length === 0 ? (
        <div className="panel" style={{ textAlign: 'center', padding: 40, color: 'var(--text-muted)' }}>
          {ar ? 'لا توجد مولدات مطابقة للبحث' : 'No generators found matching criteria'}
        </div>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 24 }}>
          {filteredTables.map(gen => (
            <div key={gen.generatorId} className="panel" style={{ padding: 20, borderRadius: 'var(--radius)', border: '1px solid var(--border)' }}>
              {/* Generator Header */}
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', borderBottom: '1px solid var(--border)', paddingBottom: 12, marginBottom: 16, flexWrap: 'wrap', gap: 10 }}>
                <div>
                  <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                    <h2 style={{ margin: 0, fontSize: '1.2rem', fontWeight: 600 }}>{gen.generatorName}</h2>
                    <span style={{ fontSize: '0.85rem', color: 'var(--text-muted)', background: 'var(--surface-2)', padding: '2px 8px', borderRadius: 'var(--radius-sm)' }}>
                      {gen.subStationName}
                    </span>
                    {gen.model && (
                      <span style={{ fontSize: '0.85rem', color: 'var(--text-muted)', border: '1px solid var(--border)', padding: '2px 8px', borderRadius: 'var(--radius-sm)' }}>
                        {gen.model}
                      </span>
                    )}
                  </div>
                  <div style={{ fontSize: '0.85rem', color: 'var(--text-muted)', marginTop: 4, display: 'flex', gap: 16 }}>
                    <span>
                      {ar ? 'الساعات الرسمية المعتمدة:' : 'Official Running Hours:'}{' '}
                      <strong style={{ color: 'var(--primary)' }}>
                        {gen.currentOfficialHours != null ? `${Number(gen.currentOfficialHours).toLocaleString('en-US')} h` : (ar ? 'لا توجد' : 'None')}
                      </strong>
                    </span>
                    {gen.lastClosingAtUtc && (
                      <span>
                        {ar ? 'آخر اعتماد:' : 'Last closing:'}{' '}
                        {new Date(gen.lastClosingAtUtc).toLocaleDateString('en-US')}
                      </span>
                    )}
                  </div>
                </div>

                {/* Due/Overdue counter pill */}
                <div>
                  {gen.dueOrOverdueCount > 0 ? (
                    <span style={{ background: 'var(--danger-subtle)', color: 'var(--danger)', border: '1px solid color-mix(in srgb, var(--danger) 38%, transparent)', padding: '6px 14px', borderRadius: 'var(--radius-full)', fontWeight: 600, fontSize: '0.85rem', display: 'flex', alignItems: 'center', gap: 6 }}>
                      <AlertTriangle size={16} />
                      {gen.dueOrOverdueCount} {ar ? 'بنود تستحق الخدمة' : 'items due/overdue'}
                    </span>
                  ) : (
                    <span style={{ background: 'var(--success-subtle)', color: 'var(--success)', border: '1px solid color-mix(in srgb, var(--success) 38%, transparent)', padding: '6px 14px', borderRadius: 'var(--radius-full)', fontWeight: 500, fontSize: '0.85rem', display: 'flex', alignItems: 'center', gap: 6 }}>
                      <CheckCircle size={16} />
                      {ar ? 'جميع البنود سليمة' : 'All items OK'}
                    </span>
                  )}
                </div>
              </div>

              {/* Items Table */}
              <div style={{ overflowX: 'auto' }}>
                <table className="table" style={{ width: '100%', fontSize: '0.9rem', textAlign: ar ? 'right' : 'left' }}>
                  <thead>
                    <tr style={{ background: 'var(--surface-2)', borderBottom: '2px solid var(--border)' }}>
                      <th style={{ padding: '10px 12px' }}>{ar ? 'بند الصيانة' : 'Service Item'}</th>
                      <th style={{ padding: '10px 12px' }}>{ar ? 'الفترة' : 'Interval'}</th>
                      <th style={{ padding: '10px 12px' }}>{ar ? 'آخر صيانة' : 'Last Service'}</th>
                      <th style={{ padding: '10px 12px' }}>{ar ? 'تستحق عند' : 'Due At'}</th>
                      <th style={{ padding: '10px 12px', minWidth: 160 }}>{ar ? 'مؤشر الاستحقاق' : 'Progress'}</th>
                      <th style={{ padding: '10px 12px' }}>{ar ? 'الحالة' : 'Status'}</th>
                      <th style={{ padding: '10px 12px', textAlign: 'center' }}>{ar ? 'إجراءات' : 'Actions'}</th>
                    </tr>
                  </thead>
                  <tbody>
                    {gen.items.map(item => {
                      const fraction = Math.min(Math.max(item.progressFraction, 0), 1.5);
                      const pct = Math.round(fraction * 100);
                      const barColor = getProgressColor(item.status, fraction);
                      const badgeStyle = getStatusBadgeStyle(item.status);

                      return (
                        <tr key={item.serviceItemId} style={{ borderBottom: '1px solid var(--border-light)' }}>
                          <td style={{ padding: '10px 12px', fontWeight: 600 }}>
                            {ar ? item.serviceItemNameAr : item.serviceItemNameEn}
                          </td>
                          <td style={{ padding: '10px 12px' }}>
                            {item.status === 5 ? (
                              <div style={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                                <span style={{ color: 'var(--text-muted)', fontSize: '0.8rem' }}>
                                  {!gen.model
                                    ? (ar ? 'المولد بدون طراز محدد' : 'Generator has no model set')
                                    : (ar ? 'غير مهيأ لهذا الطراز' : 'No definition configured for model')}
                                </span>
                                {item.definitionLink && (
                                  <a
                                    href={item.definitionLink}
                                    onClick={(e) => { e.preventDefault(); navigate(item.definitionLink); }}
                                    style={{ color: 'var(--primary)', fontSize: '0.75rem', textDecoration: 'underline', cursor: 'pointer' }}
                                  >
                                    {ar ? 'تعريف الفترة' : 'Set interval'}
                                  </a>
                                )}
                              </div>
                            ) : (
                              `${Number(item.intervalHours).toLocaleString('en-US')} h`
                            )}
                          </td>
                          <td style={{ padding: '10px 12px' }}>
                            {item.lastServiceHours != null ? `${Number(item.lastServiceHours).toLocaleString('en-US')} h` : '—'}
                          </td>
                          <td style={{ padding: '10px 12px' }}>
                            {item.dueAtHours != null ? `${Number(item.dueAtHours).toLocaleString('en-US')} h` : '—'}
                          </td>
                          <td style={{ padding: '10px 12px' }}>
                            <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                              <div style={{ flex: 1, background: 'var(--surface-3)', height: 8, borderRadius: 4, overflow: 'hidden' }}>
                                <div style={{ width: `${Math.min(pct, 100)}%`, background: barColor, height: '100%', borderRadius: 4 }} />
                              </div>
                              <span style={{ fontSize: '0.8rem', minWidth: 36, color: 'var(--text-muted)' }}>{pct}%</span>
                            </div>
                          </td>
                          <td style={{ padding: '10px 12px' }}>
                            <span style={{ ...badgeStyle, padding: '3px 8px', borderRadius: 4, fontSize: '0.8rem', fontWeight: 600 }}>
                              {ar ? item.labelAr : item.labelEn}
                            </span>
                          </td>
                          <td style={{ padding: '10px 12px', textAlign: 'center' }}>
                            <div style={{ display: 'flex', gap: 6, justifyContent: 'center' }}>
                              <button
                                className="btn btn-sm btn-primary"
                                onClick={() => openRecordModal(gen, item)}
                                title={ar ? 'تسجيل استبدال / خدمة' : 'Record replacement'}
                                style={{ padding: '4px 8px', fontSize: '0.8rem', display: 'flex', alignItems: 'center', gap: 4 }}
                              >
                                <Plus size={14} />
                                {ar ? 'تسجيل خدمة' : 'Record'}
                              </button>
                              <button
                                className="btn btn-sm btn-secondary"
                                onClick={() => openHistory(gen, item)}
                                title={ar ? 'سجل الصيانات' : 'Service history'}
                                style={{ padding: '4px 8px', fontSize: '0.8rem' }}
                              >
                                <History size={14} />
                              </button>
                            </div>
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Record Replacement Modal */}
      {modalOpen && modalGen && modalItem && (
        <Modal
          open={modalOpen}
          onClose={() => setModalOpen(false)}
          title={ar ? `تسجيل خدمة: ${modalItem.serviceItemNameAr} - ${modalGen.generatorName}` : `Record Service: ${modalItem.serviceItemNameEn} - ${modalGen.generatorName}`}
        >
          <form onSubmit={handleSaveReplacement}>
            {errorMsg && (
              <div style={{ background: '#fee2e2', color: '#b91c1c', padding: '10px 14px', borderRadius: 6, marginBottom: 16, fontSize: '0.875rem' }}>
                {errorMsg}
              </div>
            )}

            <FormField label={ar ? 'ساعات التشغيل عند الخدمة (إلزامي)' : 'Running Hours at Service (Required)'}>
              <Input
                type="number"
                step="0.1"
                required
                value={formHours}
                onChange={handleInputChange(setFormHours)}
                placeholder="e.g. 5240"
              />
            </FormField>

            {modalItem.lastServiceHours != null && (
              <div style={{ fontSize: '0.85rem', color: '#6b7280', marginBottom: 12 }}>
                {ar ? `ساعات آخر صيانة: ${modalItem.lastServiceHours} h | فترة الصيانة: ${modalItem.intervalHours} h` : `Last service: ${modalItem.lastServiceHours} h | Interval: ${modalItem.intervalHours} h`}
              </div>
            )}

            {/* Early replacement detection */}
            {isEarlyReplacement() && (
              <div style={{ background: '#fef3c7', border: '1px solid #fcd34d', color: '#92400e', padding: '10px 14px', borderRadius: 6, marginBottom: 16, fontSize: '0.875rem' }}>
                <div style={{ fontWeight: 600, display: 'flex', alignItems: 'center', gap: 6, marginBottom: 4 }}>
                  <AlertTriangle size={16} />
                  {ar ? 'استبدال مبكر تم رصده' : 'Early Replacement Detected'}
                </div>
                <div>
                  {ar ? 'الساعات المنقضية أقل من فترة الصيانة المحددة. سبب الاستبدال المبكر إلزامي:' : 'Hours since previous service are less than the interval. Reason is mandatory:'}
                </div>
                <div style={{ marginTop: 8 }}>
                  <input
                    type="text"
                    required
                    className="ft-input"
                    value={formEarlyReason}
                    onChange={handleInputChange(setFormEarlyReason)}
                    placeholder={ar ? 'مثال: تلف مبكر، تسريب، صيانة اضطرارية...' : 'e.g. premature wear, leak, emergency...'}
                    style={{ width: '100%', borderColor: '#d97706' }}
                  />
                </div>
              </div>
            )}

            <FormField label={ar ? 'تاريخ ووقت الخدمة' : 'Service Date & Time'}>
              <Input
                type="datetime-local"
                required
                value={formDate}
                onChange={handleInputChange(setFormDate)}
              />
            </FormField>

            <FormField label={ar ? 'رقم تصريح العمل (اختياري)' : 'Work Permit ID (Optional)'}>
              <Input
                type="number"
                value={formPermit}
                onChange={handleInputChange(setFormPermit)}
                placeholder="e.g. 1042"
              />
            </FormField>

            <FormField label={ar ? 'ملاحظات (اختياري)' : 'Notes (Optional)'}>
              <textarea
                className="ft-input"
                rows="2"
                value={formNotes}
                onChange={handleInputChange(setFormNotes)}
                style={{ width: '100%' }}
              />
            </FormField>

            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 10, marginTop: 20 }}>
              <Button variant="secondary" type="button" onClick={() => setModalOpen(false)}>
                {ar ? 'إلغاء' : 'Cancel'}
              </Button>
              <Button variant="primary" type="submit" disabled={submitting}>
                {submitting ? (ar ? 'جاري الحفظ...' : 'Saving...') : (ar ? 'حفظ السجل' : 'Save Record')}
              </Button>
            </div>
          </form>
        </Modal>
      )}

      {/* History Modal */}
      {historyOpen && (
        <Modal
          open={historyOpen}
          onClose={() => setHistoryOpen(false)}
          title={ar ? `سجل الخدمة: ${historyItemName} - ${historyGenName}` : `Service History: ${historyItemName} - ${historyGenName}`}
        >
          {historyLoading ? (
            <div className="ft-spinner" style={{ width: 30, height: 30, margin: '20px auto' }} />
          ) : historyItems.length === 0 ? (
            <div style={{ textAlign: 'center', padding: 20, color: '#6b7280' }}>
              {ar ? 'لا يوجد سجل سابق' : 'No service history recorded'}
            </div>
          ) : (
            <div style={{ overflowX: 'auto', maxHeight: 400 }}>
              <table className="table" style={{ width: '100%', fontSize: '0.85rem' }}>
                <thead>
                  <tr style={{ background: '#f9fafb' }}>
                    <th style={{ padding: '8px' }}>{ar ? 'النوع' : 'Kind'}</th>
                    <th style={{ padding: '8px' }}>{ar ? 'الساعات' : 'Hours'}</th>
                    <th style={{ padding: '8px' }}>{ar ? 'التاريخ' : 'Date'}</th>
                    <th style={{ padding: '8px' }}>{ar ? 'مبكر؟' : 'Early?'}</th>
                    <th style={{ padding: '8px' }}>{ar ? 'الحالة' : 'Status'}</th>
                    <th style={{ padding: '8px' }}>{ar ? 'إلغاء' : 'Void'}</th>
                  </tr>
                </thead>
                <tbody>
                  {historyItems.map(h => (
                    <tr key={h.id} style={{ opacity: h.voidedAtUtc ? 0.5 : 1 }}>
                      <td style={{ padding: '8px' }}>{h.kind === 2 ? (ar ? 'تأسيس' : 'Baseline') : (ar ? 'استبدال' : 'Replacement')}</td>
                      <td style={{ padding: '8px', fontWeight: 600 }}>{h.hoursAtService} h</td>
                      <td style={{ padding: '8px' }}>{new Date(h.occurredAtUtc).toLocaleDateString('en-US')}</td>
                      <td style={{ padding: '8px' }}>
                        {h.isEarly ? (
                          <span title={h.earlyReplacementReason} style={{ color: '#d97706', fontWeight: 600 }}>
                            {ar ? 'نعم' : 'Yes'}
                          </span>
                        ) : '—'}
                      </td>
                      <td style={{ padding: '8px' }}>
                        {h.voidedAtUtc ? (
                          <span style={{ color: '#ef4444', fontSize: '0.8rem' }} title={h.voidReason}>
                            {ar ? 'ملغي' : 'Voided'}
                          </span>
                        ) : (
                          <span style={{ color: '#22c55e' }}>{ar ? 'نشط' : 'Active'}</span>
                        )}
                      </td>
                      <td style={{ padding: '8px' }}>
                        {!h.voidedAtUtc && (
                          <button
                            className="btn btn-sm btn-danger"
                            onClick={() => {
                              setVoidRecordId(h.id);
                              setVoidModalOpen(true);
                            }}
                            style={{ padding: '2px 6px', fontSize: '0.75rem' }}
                          >
                            {ar ? 'إلغاء السجل' : 'Void'}
                          </button>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </Modal>
      )}

      {/* Void Confirmation Modal */}
      {voidModalOpen && (
        <Modal
          open={voidModalOpen}
          onClose={() => setVoidModalOpen(false)}
          title={ar ? 'إلغاء سجل خدمة' : 'Void Service Record'}
        >
          <div>
            <p style={{ fontSize: '0.9rem', color: '#374151', marginBottom: 12 }}>
              {ar ? 'إلغاء هذا السجل سيعيد حساب ساعات الاستحقاق للمولد. سبب الإلغاء إلزامي:' : 'Voiding this record will recalculate due hours. A reason is mandatory:'}
            </p>
            <textarea
              className="ft-input"
              rows="3"
              value={voidReason}
              onChange={(e) => setVoidReason(e.target.value)}
              placeholder={ar ? 'سبب الإلغاء...' : 'Reason for voiding...'}
              style={{ width: '100%', marginBottom: 16 }}
            />
            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 10 }}>
              <Button variant="secondary" onClick={() => setVoidModalOpen(false)}>
                {ar ? 'تراجع' : 'Cancel'}
              </Button>
              <Button variant="danger" disabled={!voidReason.trim()} onClick={handleVoidRecord}>
                {ar ? 'تأكيد الإلغاء' : 'Confirm Void'}
              </Button>
            </div>
          </div>
        </Modal>
      )}
    </div>
  );
}
