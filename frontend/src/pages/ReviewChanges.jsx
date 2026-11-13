// ORPHAN: Not imported by any route or component. Consider deleting this file.
import React, { useState, useEffect, useRef, useCallback } from 'react';
import client from '../api/client';
import { CheckCircle, X, FileText, Zap, PackageSearch, CheckSquare, Layout, AlertTriangle, Droplet } from 'lucide-react';
import ConfirmModal from '../components/ConfirmModal';

// ── Modal رفض مدمج ──────────────────────────────────────────────────────────
function RejectModal({ ar, onConfirm, onCancel }) {
  const [reason, setReason] = useState('');
  return (
    <div style={{
      position: 'fixed', inset: 0, zIndex: 9999,
      background: 'rgba(0,0,0,0.6)', backdropFilter: 'blur(4px)',
      display: 'flex', alignItems: 'center', justifyContent: 'center',
    }}>
      <div style={{
        background: 'var(--card)', border: '1px solid var(--border)',
        borderRadius: 12, padding: 28, width: 420, maxWidth: '90vw',
        boxShadow: '0 20px 60px rgba(0,0,0,0.4)',
      }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 16 }}>
          <AlertTriangle size={20} style={{ color: 'var(--danger)' }} />
          <h3 style={{ margin: 0, fontSize: 16, fontWeight: 700 }}>
            {ar ? 'سبب الرفض' : 'Rejection Reason'}
          </h3>
        </div>
        <textarea
          autoFocus
          rows={3}
          className="form-control"
          placeholder={ar ? 'اكتب سبب الرفض (اختياري)...' : 'Enter rejection reason (optional)...'}
          value={reason}
          onChange={e => setReason(e.target.value)}
          style={{ resize: 'vertical', fontFamily: 'inherit', marginBottom: 16 }}
        />
        <div style={{ display: 'flex', gap: 10, justifyContent: 'flex-end' }}>
          <button className="btn btn-ghost" onClick={onCancel}>
            {ar ? 'إلغاء' : 'Cancel'}
          </button>
          <button
            className="btn"
            style={{ background: 'var(--danger)', color: '#fff', border: 'none' }}
            onClick={() => onConfirm(reason)}
          >
            <X size={14} /> {ar ? 'تأكيد الرفض' : 'Confirm Reject'}
          </button>
        </div>
      </div>
    </div>
  );
}

// ── عرض التاريخ والوقت ────────────────────────────────────────────────────
function DateTimeCell({ date, ar }) {
  const d = new Date(date);
  const datePart = d.toLocaleDateString(ar ? 'ar-YE-u-nu-latn' : 'en-GB');
  const timePart = d.toLocaleTimeString(ar ? 'ar-YE-u-nu-latn' : 'en-GB', { hour: '2-digit', minute: '2-digit' });
  return (
    <div style={{ lineHeight: 1.5 }}>
      <div style={{ fontWeight: 600, fontSize: 13 }}>{datePart}</div>
      <div style={{ fontSize: 11, color: 'var(--text-muted)' }}>{timePart}</div>
    </div>
  );
}

// ── الصفحة الرئيسية ───────────────────────────────────────────────────────
function getEntityName(value) {
  if (!value) return '';
  if (typeof value === 'string') return value;
  return value.name || value.subStationName || value.stationName || value.id || '';
}

function SiteNameCell({ station, subStation }) {
  const stationName = getEntityName(station);
  const subStationName = getEntityName(subStation);
  const displayName = stationName && subStationName && stationName !== subStationName
    ? `${stationName} - ${subStationName}`
    : (stationName || subStationName || '—');

  return <span>{displayName}</span>;
}

function ReviewChanges({ lang }) {
  const ar = lang === 'ar';
  const [readings, setReadings] = useState([]);
  const [stationReadings, setStationReadings] = useState([]);
  const [inventories, setInventories] = useState([]);
  const [loading, setLoading] = useState(true);
  const [actionLoading, setActionLoading] = useState(null); // id العملية الجارية
  const [bulkLoading, setBulkLoading] = useState(false);
  const [toast, setToast] = useState(null);           // { type, msg }
  const [rejectModal, setRejectModal] = useState(null); // { type, id }
  const [confirmModal, setConfirmModal] = useState({ open: false, message: '', onConfirm: null });

  // ✅ الإصلاح الجوهري: useRef لتخزين fetchPending بشكل دائم
  // يمنع stale closure الذي كان يسبب عدم تحديث الصفحة بعد العمليات
  const fetchPendingRef = useRef(null);

  const fetchPending = useCallback(async (silent = false) => {
    if (!silent) setLoading(true);
    try {
      const [rRes, srRes, iRes] = await Promise.all([
        client.get('/api/readings?status=pending'),
        client.get('/api/substation-readings/pending'),
        client.get('/api/inventory'),
      ]);
      setReadings(rRes.data.readings || []);
      setStationReadings(srRes.data || []);
      setInventories((iRes.data || []).filter(i => i.status === 'submitted'));
    } catch {
      showToast('error', ar ? 'فشل جلب البيانات' : 'Failed to fetch data');
    } finally {
      if (!silent) setLoading(false);
    }
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  // تحديث الـ ref دائماً بأحدث نسخة
  fetchPendingRef.current = fetchPending;

  useEffect(() => {
    fetchPendingRef.current();
  }, []);

  // ── Toast تلقائي يختفي بعد 3 ثواني ──────────────────────────────────────
  const showToast = (type, msg) => {
    setToast({ type, msg });
    setTimeout(() => setToast(null), 3000);
  };

  // ── تنفيذ عملية مع تحديث فوري للقائمة ────────────────────────────────────
  const run = async (id, fn, successMsg) => {
    setActionLoading(id);
    try {
      await fn();
      showToast('success', successMsg);
      // ✅ تحديث فوري بدون loading كامل (silent refresh)
      await fetchPendingRef.current(true);
    } catch (err) {
      showToast('error', err.response?.data?.msg || (ar ? 'حدث خطأ' : 'Error occurred'));
    } finally {
      setActionLoading(null);
    }
  };

  // ── قراءات المولدات ───────────────────────────────────────────────────────
  const handleApproveReading = (id) =>
    run(id, () => client.post(`/api/readings/${id}/approve`), ar ? 'تم اعتماد القراءة ✓' : 'Reading approved ✓');

  const handleApproveAll = async () => {
    if (readings.length === 0) return;
    setConfirmModal({
      open: true,
      message: ar ? `اعتماد جميع القراءات (${readings.length})؟` : `Approve all ${readings.length}?`,
      onConfirm: async () => {
        setConfirmModal({ open: false, message: '', onConfirm: null });
        setBulkLoading(true);
        let ok = 0, fail = 0;
        for (const r of readings) {
          try { await client.post(`/api/readings/${r.id}/approve`); ok++; } catch { fail++; }
        }
        setBulkLoading(false);
        showToast(fail === 0 ? 'success' : 'error',
          ar ? `تم اعتماد ${ok} قراءة${fail > 0 ? ` وفشل ${fail}` : ''} ✓`
            : `Approved ${ok}${fail > 0 ? `, ${fail} failed` : ''} ✓`);
        await fetchPendingRef.current(true);
      },
    });
  };

  // ── قراءات المواقع ────────────────────────────────────────────────────────
  const handleApproveStationReading = (id) =>
    run(id, () => client.post(`/api/substation-readings/${id}/approve`), ar ? 'تم اعتماد قراءة الموقع ✓' : 'Site reading approved ✓');

  // ── تأكيد الرفض من Modal ──────────────────────────────────────────────────
  const handleConfirmReject = async (reason) => {
    const { type, id } = rejectModal;
    setRejectModal(null);
    const url = type === 'reading'
      ? `/api/readings/${id}/reject`
      : `/api/substation-readings/${id}/reject`;
    run(id, () => client.post(url, { reason }), ar ? 'تم رفض القراءة ✓' : 'Reading rejected ✓');
  };

  // ── الجرد ─────────────────────────────────────────────────────────────────
  const handleReviewInventory = async (id) => {
    run(id, async () => {
      const res = await client.get(`/api/inventory/${id}`);
      const inv = res.data;
      await client.put(`/api/inventory/${id}`, { ...inv, status: 'reviewed' });
    }, ar ? 'تم مراجعة الجرد ✓' : 'Inventory reviewed ✓');
  };

  // ─────────────────────────────────────────────────────────────────────────
  return (
    <div>
      {/* Reject Modal */}
      {rejectModal && (
        <RejectModal ar={ar} onConfirm={handleConfirmReject} onCancel={() => setRejectModal(null)} />
      )}

      {/* ✅ Toast يظهر في الزاوية ويختفي تلقائياً — بدون حاجة لـ refresh */}
      {toast && (
        <div style={{
          position: 'fixed', bottom: 24, left: '50%', transform: 'translateX(-50%)',
          zIndex: 8888, minWidth: 280, textAlign: 'center',
          background: toast.type === 'success' ? 'var(--success, #22c55e)' : 'var(--danger, #ef4444)',
          color: '#fff', borderRadius: 10, padding: '12px 24px',
          fontWeight: 600, fontSize: 14, boxShadow: '0 8px 30px rgba(0,0,0,0.3)',
          animation: 'fadeInUp 0.3s ease',
        }}>
          {toast.type === 'success' ? <CheckCircle size={16} style={{ marginInlineStart: 8, verticalAlign: 'middle' }} /> : <X size={16} style={{ marginInlineStart: 8, verticalAlign: 'middle' }} />}
          {toast.msg}
        </div>
      )}

      <div className="page-header">
        <div className="page-header-row">
          <div>
            <h1 style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
              <FileText size={22} style={{ color: 'var(--primary)' }} />
              {ar ? 'مراجعة التعديلات' : 'Review Changes'}
            </h1>
            <p>{ar ? 'اعتماد أو رفض القراءات اليومية وعمليات الجرد المعلقة' : 'Approve or reject pending readings and inventories'}</p>
          </div>
        </div>
      </div>

      {loading ? <div className="spinner-wrap"><div className="ft-spinner" /></div> : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 20 }}>

          {/* ── قراءات المولدات ── */}
          <div className="section-card" style={{ transform: 'none', animation: 'fadeInUp 0.4s ease' }}>
            <div className="section-card-header">
              <div className="section-card-title" style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                <Zap size={18} style={{ color: 'var(--warning)' }} />
                {ar ? 'قراءات المولدات المعلقة' : 'Pending Generator Readings'}
              </div>
              <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                <span className="badge badge-warning">{readings.length}</span>
                {readings.length > 0 && (
                  <button className="btn btn-sm"
                    style={{ background: 'var(--secondary)', color: '#fff', border: 'none', display: 'flex', alignItems: 'center', gap: 6 }}
                    onClick={handleApproveAll} disabled={bulkLoading}>
                    <CheckSquare size={14} />
                    {bulkLoading ? (ar ? 'جاري الاعتماد...' : 'Approving...') : (ar ? `اعتماد الكل (${readings.length})` : `Approve All (${readings.length})`)}
                  </button>
                )}
              </div>
            </div>
            {readings.length === 0 ? (
              <div className="empty-state" style={{ padding: 30 }}>
                <h3 style={{ fontSize: 14 }}>{ar ? 'لا توجد قراءات بانتظار المراجعة' : 'No pending readings'}</h3>
              </div>
            ) : (
              <div style={{ overflowX: 'auto' }}>
                <table className="data-table">
                  <thead>
                    <tr>
                      <th>{ar ? 'المولد' : 'Generator'}</th>
                      <th>{ar ? 'المحطة' : 'Station'}</th>
                      <th>{ar ? 'التاريخ والوقت' : 'Date & Time'}</th>
                      <th>{ar ? 'الطاقة المنتجة' : 'Energy Produced'}</th>
                      <th>{ar ? 'الوقود المستهلك' : 'Fuel Consumed'}</th>
                      <th>{ar ? 'الزيت المضاف' : 'Oil Added'}</th>
                      <th>{ar ? 'المشغل' : 'Operator'}</th>
                      <th>{ar ? 'إجراء' : 'Actions'}</th>
                    </tr>
                  </thead>
                  <tbody>
                    {readings.map(r => {
                      const energy = (r.currentPowerRecord || 0) - (r.prevPowerRecord || 0);
                      const isRunning = actionLoading === r.id;
                      return (
                        <tr key={r.id} style={{ opacity: isRunning ? 0.5 : 1, transition: 'opacity 0.2s' }}>
                          <td style={{ fontWeight: 600 }}>{r.generator?.name}</td>
                          <td><SiteNameCell station={r.station} subStation={r.generator?.subStation} /></td>
                          <td><DateTimeCell date={r.readingDate} ar={ar} /></td>
                          <td style={{ color: 'var(--primary)' }}>{energy.toFixed(0)} KWh</td>
                          <td style={{ color: 'var(--warning)' }}>{(r.fuelConsumedLiters || 0).toFixed(3)} L</td>
                          <td>
                            {r.oilAddedLiters > 0 ? (
                              <div style={{ display: 'flex', alignItems: 'center', gap: 5 }}>
                                <Droplet size={13} style={{ color: 'var(--secondary)' }} />
                                <span style={{ color: 'var(--secondary)', fontWeight: 600 }}>
                                  {(r.oilAddedLiters || 0).toFixed(3)} L
                                </span>
                                {r.oilTypeUsed?.name && (
                                  <span style={{ fontSize: 11, color: 'var(--text-muted)' }}>
                                    ({r.oilTypeUsed.name})
                                  </span>
                                )}
                              </div>
                            ) : (
                              <span style={{ color: 'var(--text-faint)', fontSize: 12 }}>—</span>
                            )}
                          </td>
                          <td>{r.createdBy?.name || '—'}</td>
                          <td>
                            <div style={{ display: 'flex', gap: 6 }}>
                              <button className="btn btn-sm"
                                style={{ background: 'var(--secondary)', color: '#fff', border: 'none' }}
                                disabled={isRunning}
                                onClick={() => handleApproveReading(r.id)}>
                                {isRunning ? <div className="ft-spinner" style={{ width: 12, height: 12, borderWidth: 2 }} /> : <CheckCircle size={13} />}
                                {ar ? ' اعتماد' : ' Approve'}
                              </button>
                              <button className="btn btn-sm btn-ghost"
                                style={{ color: 'var(--danger)' }}
                                disabled={isRunning}
                                onClick={() => setRejectModal({ type: 'reading', id: r.id })}>
                                <X size={13} /> {ar ? 'رفض' : 'Reject'}
                              </button>
                            </div>
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            )}
          </div>

          {/* ── قراءات المواقع ── */}
          <div className="section-card" style={{ transform: 'none', animation: 'fadeInUp 0.4s ease' }}>
            <div className="section-card-header">
              <div className="section-card-title" style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                <Layout size={18} style={{ color: 'var(--primary)' }} />
                {ar ? 'قراءات الطاقة المرسلة للمواقع المعلقة' : 'Pending Site Grid Readings'}
              </div>
              <span className="badge badge-warning">{stationReadings.length}</span>
            </div>
            {stationReadings.length === 0 ? (
              <div className="empty-state" style={{ padding: 30 }}>
                <h3 style={{ fontSize: 14 }}>{ar ? 'لا توجد قراءات مواقع بانتظار المراجعة' : 'No pending site readings'}</h3>
              </div>
            ) : (
              <div style={{ overflowX: 'auto' }}>
                <table className="data-table">
                  <thead>
                    <tr>
                      <th>{ar ? 'الموقع' : 'Site'}</th>
                      <th>{ar ? 'التاريخ والوقت' : 'Date & Time'}</th>
                      <th>{ar ? 'القراءة السابقة' : 'Prev Reading'}</th>
                      <th>{ar ? 'القراءة الحالية' : 'Current Reading'}</th>
                      <th>{ar ? 'المرسل للشبكة' : 'Sent to Grid'}</th>
                      <th>{ar ? 'المشغل' : 'Operator'}</th>
                      <th>{ar ? 'إجراء' : 'Actions'}</th>
                    </tr>
                  </thead>
                  <tbody>
                    {stationReadings.map(r => {
                      const isRunning = actionLoading === r.id;
                      return (
                        <tr key={r.id} style={{ opacity: isRunning ? 0.5 : 1, transition: 'opacity 0.2s' }}>
                          <td style={{ fontWeight: 600 }}><SiteNameCell station={r.subStationName} /></td>
                          <td><DateTimeCell date={r.readingDate} ar={ar} /></td>
                          <td style={{ color: 'var(--text-muted)' }}>{(r.prevExportedEnergyKWh || 0).toLocaleString('en-US')} KW</td>
                          <td style={{ color: 'var(--primary)', fontWeight: 700 }}>{(r.currentExportedEnergyKWh || 0).toLocaleString('en-US')} KW</td>
                          <td style={{ color: 'var(--secondary)' }}>{((r.currentExportedEnergyKWh || 0) - (r.prevExportedEnergyKWh || 0)).toFixed(0)} KWh</td>
                          <td>{r.createdBy || '—'}</td>
                          <td>
                            <div style={{ display: 'flex', gap: 6 }}>
                              <button className="btn btn-sm"
                                style={{ background: 'var(--secondary)', color: '#fff', border: 'none' }}
                                disabled={isRunning}
                                onClick={() => handleApproveStationReading(r.id)}>
                                {isRunning ? <div className="ft-spinner" style={{ width: 12, height: 12, borderWidth: 2 }} /> : <CheckCircle size={13} />}
                                {ar ? ' اعتماد' : ' Approve'}
                              </button>
                              <button className="btn btn-sm btn-ghost"
                                style={{ color: 'var(--danger)' }}
                                disabled={isRunning}
                                onClick={() => setRejectModal({ type: 'station', id: r.id })}>
                                <X size={13} /> {ar ? 'رفض' : 'Reject'}
                              </button>
                            </div>
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            )}
          </div>

          {/* ── الجرد الدوري ── */}
          <div className="section-card" style={{ transform: 'none', animation: 'fadeInUp 0.4s ease' }}>
            <div className="section-card-header">
              <div className="section-card-title" style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                <PackageSearch size={18} style={{ color: 'var(--primary)' }} />
                {ar ? 'الجرد الدوري المعلق' : 'Pending Periodic Inventories'}
              </div>
              <span className="badge badge-warning">{inventories.length}</span>
            </div>
            {inventories.length === 0 ? (
              <div className="empty-state" style={{ padding: 30 }}>
                <h3 style={{ fontSize: 14 }}>{ar ? 'لا يوجد جرد بانتظار المراجعة' : 'No pending inventories'}</h3>
              </div>
            ) : (
              <div style={{ overflowX: 'auto' }}>
                <table className="data-table">
                  <thead>
                    <tr>
                      <th>{ar ? 'المحطة' : 'Station'}</th>
                      <th>{ar ? 'التاريخ والوقت' : 'Date & Time'}</th>
                      <th>{ar ? 'فريق الجرد' : 'Team'}</th>
                      <th>{ar ? 'الفرق الكلي (وقود)' : 'Total Fuel Diff'}</th>
                      <th>{ar ? 'إجراء' : 'Actions'}</th>
                    </tr>
                  </thead>
                  <tbody>
                    {inventories.map(inv => {
                      const isRunning = actionLoading === inv.id;
                      return (
                        <tr key={inv.id} style={{ opacity: isRunning ? 0.5 : 1, transition: 'opacity 0.2s' }}>
                          <td style={{ fontWeight: 600 }}>{inv.station?.name}</td>
                          <td><DateTimeCell date={inv.inventoryDate} ar={ar} /></td>
                          <td>{inv.team || '—'}</td>
                          <td style={{ color: inv.totalFuelDifference < 0 ? 'var(--danger)' : 'var(--text)' }}>
                            {inv.totalFuelDifference} L
                          </td>
                          <td>
                            <button className="btn btn-sm"
                              style={{ background: 'var(--primary)', color: '#fff', border: 'none' }}
                              disabled={isRunning}
                              onClick={() => handleReviewInventory(inv.id)}>
                              {isRunning ? <div className="ft-spinner" style={{ width: 12, height: 12, borderWidth: 2 }} /> : <CheckCircle size={13} />}
                              {ar ? ' مراجعة ومطابقة' : ' Review & Match'}
                            </button>
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            )}
          </div>

        </div>
      )}

      <ConfirmModal
        open={confirmModal.open}
        title={ar ? 'تأكيد الاعتماد' : 'Confirm Approval'}
        message={confirmModal.message}
        confirmText={ar ? 'نعم، اعتمد' : 'Yes, Approve'}
        cancelText={ar ? 'إلغاء' : 'Cancel'}
        onConfirm={confirmModal.onConfirm}
        onCancel={() => setConfirmModal({ open: false, message: '', onConfirm: null })}
      />
    </div>
  );
}

export default ReviewChanges;
