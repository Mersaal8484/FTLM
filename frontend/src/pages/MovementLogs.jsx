import React, { useState, useEffect, useMemo, useCallback } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import client from '../api/client';
import connection from '../socket';
import { getMovementId, getApiErrorMessage } from './FuelMovements.helpers';
import {
  ShieldCheck, ShieldAlert, MapPin, LayoutGrid, Truck, RefreshCw, Clock
} from 'lucide-react';
import ConfirmModal from '../components/ConfirmModal';
import Pagination from '../components/Pagination';
import usePermissions from '../permissions/usePermissions';
import useToastStore from '../store/toastStore';

const statusConfig = {
  pending:  { label: 'بانتظار الموافقة', color: 'var(--warning)', icon: Clock },
  approved: { label: 'تم الاعتماد',      color: 'var(--success)', icon: ShieldCheck },
  rejected: { label: 'مرفوض',            color: 'var(--danger)', icon: ShieldAlert },
};

const getStatus = (m) => {
  if (!m) return 'pending';
  if (m.isApproved) return 'approved';
  if (m.approvedAt) return 'rejected';
  return 'pending';
};

const renderMovementDetails = (m) => {
  const type = (m?.transactionTypeLookupName || '').toLowerCase();
  const ref = m?.reference || m?.invoiceNumber;
  if (type.includes('توريد') || type.includes('supply') || type.includes('وارد')) {
    return m.supplierCompany || ref || '—';
  }
  if (type.includes('نقل') || type.includes('transfer')) {
    const src = m.sourceSubStationName || '—';
    const dst = m.destinationSubStationName || '—';
    return `${src} → ${dst}`;
  }
  if (type.includes('فاقد') || type.includes('loss')) {
    return m.correctionReason || m.notes || '—';
  }
  return ref || m?.notes || '—';
};

const fmtDate = (value, withTime = false) => {
  if (!value) return '—';
  const d = new Date(value);
  if (Number.isNaN(d.getTime())) return '—';
  return withTime
    ? d.toLocaleString('ar-YE')
    : d.toLocaleDateString('ar-YE');
};

export default function MovementLogs({ lang }) {
  const ar = lang === 'ar';
  const { can } = usePermissions();
  const canApprove = can('fuel_movements', 'approve') || can('readings', 'approve') || can('inventory', 'approve');
  const queryClient = useQueryClient();

  const [movements, setMovements] = useState([]);
  const [oilMovements, setOilMovements] = useState([]);
  const [loading, setLoading] = useState(true);
  const [actionLoading, setActionLoading] = useState(null);
  const [rejectModal, setRejectModal] = useState(null);
  const [confirmModal, setConfirmModal] = useState({ open: false, id: null, type: 'fuel' });
  const [fetchError, setFetchError] = useState(null);
  const showToast = useToastStore((s) => s.showToast);

  const PAGE_SIZE = 20;
  const [page, setPage] = useState(1);

  const fetchAll = useCallback(async () => {
    setLoading(true);
    setFetchError(null);
    try {
      const [fuelRes, oilRes] = await Promise.allSettled([
        client.get('/api/fuel-movements'),
        client.get('/api/oil-transactions', { params: { page: 1, pageSize: 1000 } }),
      ]);
      if (fuelRes.status === 'fulfilled') setMovements(fuelRes.value.data?.items || []);
      if (oilRes.status === 'fulfilled') setOilMovements(oilRes.value.data?.data || oilRes.value.data?.items || oilRes.value.data || []);
    } catch (err) {
      const msg = getApiErrorMessage(err);
      setFetchError(msg);
      showToast(msg, 'error');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { fetchAll(); }, [lang, fetchAll]);

  useEffect(() => {
    const refreshEvents = [
      'FuelMovementCreated', 'FuelMovementUpdated', 'FuelMovementSubmitted', 'FuelMovementApproved', 'FuelMovementRejected', 'FuelMovementDeleted',
      'OilTransactionCreated', 'OilTransactionUpdated', 'OilTransactionSubmitted', 'OilTransactionApproved', 'OilTransactionRejected', 'OilTransactionDeleted',
    ];
    const handler = () => fetchAll();
    refreshEvents.forEach(evt => connection.on(evt, handler));
    return () => { refreshEvents.forEach(evt => connection.off(evt, handler)); };
  }, [fetchAll]);

  useEffect(() => {
    const id = setInterval(() => { fetchAll(); }, 60000);
    return () => clearInterval(id);
  }, [fetchAll]);

  const allMovements = useMemo(() => {
    const fuel = movements.map(m => ({ ...m, movementType: 'fuel' }));
    const oil = oilMovements.map(m => ({ ...m, movementType: 'oil' }));
    return [...fuel, ...oil].sort((a, b) => new Date(b.transactionDate) - new Date(a.transactionDate));
  }, [movements, oilMovements]);

  // ── Grouping: Station → SubStation → Movements ───────────────────────────
  const groupedByStation = useMemo(() => {
    const stations = {};

    const ensureStation = (name) => {
      const key = name || (ar ? 'محطات غير مصنفة' : 'Unassigned Stations');
      if (!stations[key]) stations[key] = {};
      return stations[key];
    };

    const ensureSubStation = (stationObj, name) => {
      const key = name || (ar ? 'مواقع غير مصنفة' : 'Unassigned Sites');
      if (!stationObj[key]) stationObj[key] = [];
      return stationObj[key];
    };

    allMovements.forEach((m) => {
      const stationName = m.stationName || m.station?.name || (ar ? 'محطات غير مصنفة' : 'Unassigned Stations');
      const subStationName = m.subStationName || (ar ? 'مواقع غير مصنفة' : 'Unassigned Sites');
      const stationObj = ensureStation(stationName);
      const list = ensureSubStation(stationObj, subStationName);
      list.push(m);
    });

    return stations;
  }, [allMovements, ar]);

  const totalItems = allMovements.length;
  const totalPages = Math.max(1, Math.ceil(totalItems / PAGE_SIZE));
  const currentPage = page;
  const paginatedMovements = useMemo(() => {
    const start = (currentPage - 1) * PAGE_SIZE;
    return allMovements.slice(start, start + PAGE_SIZE);
  }, [allMovements, currentPage]);

  const run = async (id, fn, successMsg) => {
    setActionLoading(id);
    try {
      await fn();
      await fetchAll();
      queryClient.invalidateQueries({ queryKey: ['fuel-movements'] });
      queryClient.invalidateQueries({ queryKey: ['oil-transactions'] });
      if (successMsg) showToast(successMsg, 'success');
    } catch (err) {
      showToast(getApiErrorMessage(err), 'error');
    } finally {
      setActionLoading(null);
    }
  };

  const handleApprove = (id, type = 'fuel') => {
    const url = type === 'oil'
      ? `/api/oil-transactions/${id}/approve`
      : `/api/fuel-movements/${id}/approve`;
    run(id, () => client.post(url), ar ? 'تم الاعتماد ✓' : 'Approved ✓');
  };

  const handleReject = (reason) => {
    const { id, type } = rejectModal;
    setRejectModal(null);
    const url = type === 'oil'
      ? `/api/oil-transactions/${id}/reject`
      : `/api/fuel-movements/${id}/reject`;
    run(id, () => client.post(url, { reason }), ar ? 'تم الرفض ✓' : 'Rejected ✓');
  };

  return (
    <div>
      {rejectModal && (
        <div style={{ position: 'fixed', inset: 0, zIndex: 9999, background: 'rgba(0,0,0,0.6)', backdropFilter: 'blur(4px)', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
          <div style={{ background: 'var(--card)', border: '1px solid var(--border)', borderRadius: 12, padding: 28, width: 420, maxWidth: '90vw', boxShadow: '0 20px 60px rgba(0,0,0,0.4)' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 16 }}>
              <ShieldAlert size={20} style={{ color: 'var(--danger)' }} />
              <h3 style={{ margin: 0, fontSize: 16, fontWeight: 700 }}>{ar ? 'سبب الرفض' : 'Rejection Reason'}</h3>
            </div>
            <textarea autoFocus rows={3} className="form-control" placeholder={ar ? 'اكتب سبب الرفض (اختياري)...' : 'Enter rejection reason (optional)...'} onChange={e => e.target.value} style={{ resize: 'vertical', fontFamily: 'inherit', marginBottom: 16 }} />
            <div style={{ display: 'flex', gap: 10, justifyContent: 'flex-end' }}>
              <button className="btn btn-ghost" onClick={() => setRejectModal(null)}>{ar ? 'إلغاء' : 'Cancel'}</button>
              <button className="btn" style={{ background: 'var(--danger)', color: '#fff', border: 'none' }} onClick={handleReject}>
                <ShieldAlert size={14} /> {ar ? 'تأكيد الرفض' : 'Confirm Reject'}
              </button>
            </div>
          </div>
        </div>
      )}

      {confirmModal.open && (
        <ConfirmModal
          open
          title={ar ? 'تأكيد الاعتماد' : 'Confirm Approval'}
          message={ar ? 'هل أنت متأكد من اعتماد هذه الحركة؟' : 'Are you sure you want to approve this movement?'}
          onConfirm={async () => {
            handleApprove(confirmModal.id, confirmModal.type);
            setConfirmModal({ open: false, id: null, type: 'fuel' });
          }}
          onCancel={() => setConfirmModal({ open: false, id: null, type: 'fuel' })}
        />
      )}

      <div className="page-header">
        <div className="page-header-row">
          <div>
            <h1 style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
              <Truck size={22} style={{ color: 'var(--primary)' }} />
              {ar ? 'سجل الحركات العام' : 'Movement Logs'}
            </h1>
            <p>{ar ? 'سجل شامل لجميع حركات الوقود والزيوت مرتب حسب الموقع' : 'Complete log of all fuel and oil movements grouped by site'}</p>
          </div>
          <button className="btn btn-sm" onClick={fetchAll}>
            <RefreshCw size={14} /> {ar ? 'تحديث' : 'Refresh'}
          </button>
        </div>
      </div>

      {fetchError ? (
        <div className="empty-state">
          <AlertTriangle size={48} style={{ color: 'var(--danger)' }} />
          <h3>{fetchError}</h3>
          <button className="btn btn-sm" onClick={fetchAll}>{ar ? 'إعادة المحاولة' : 'Retry'}</button>
        </div>
      ) : loading ? <div className="spinner-wrap"><div className="ft-spinner" /></div> : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 20 }}>
          {Object.entries(groupedByStation).map(([stationName, subStations]) => {
            const allStationMoves = Object.values(subStations).flat();
            return (
              <div key={stationName} className="station-group" style={{ marginBottom: 40 }}>
                <div className="station-group-header" style={{
                  display: 'flex', alignItems: 'center', gap: 15,
                  padding: '18px 20px', background: 'var(--bg-card)',
                  border: '1px solid var(--border)', borderBottom: '2px solid #84cc16',
                  borderRadius: 12, marginBottom: 20, boxShadow: '0 4px 15px rgba(0,0,0,0.1)'
                }}>
                  <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', width: 42, height: 42, borderRadius: 10, background: '#84cc1615', color: '#84cc16' }}>
                    <MapPin size={22} />
                  </div>
                  <div>
                    <h3 style={{ margin: 0, fontSize: 18, fontWeight: 'bold' }}>{stationName}</h3>
                    <div style={{ fontSize: 12, color: 'var(--text-muted)', marginTop: 4 }}>{ar ? 'المحطة' : 'Station'}</div>
                  </div>
                  <span style={{ marginInlineStart: 'auto', padding: '6px 14px', background: 'var(--primary)', color: '#fff', borderRadius: 20, fontSize: 13, fontWeight: 'bold' }}>
                    {allStationMoves.length} {ar ? 'حركات' : 'Movements'}
                  </span>
                </div>

                <div style={{ display: 'flex', flexDirection: 'column', gap: 24 }}>
                  {Object.entries(subStations).map(([subStationName, siteMoves]) => (
                    <div key={subStationName}>
                      <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 10, padding: '0 4px' }}>
                        <div style={{ width: 8, height: 8, borderRadius: '50%', background: '#84cc16' }} />
                        <h4 style={{ margin: 0, fontSize: 15, fontWeight: 700 }}>{subStationName}</h4>
                        <span className="badge badge-muted">{siteMoves.length} {ar ? 'حركات' : 'Movements'}</span>
                      </div>

                      <div className="section-card" style={{ padding: 0 }}>
                        <div className="data-table-wrap">
                          <table className="data-table">
                            <thead>
                              <tr>
                                <th style={{ textAlign: 'center', width: 120 }}>{ar ? 'التاريخ' : 'Date'}</th>
                                <th style={{ textAlign: 'center' }}>{ar ? 'نوع الحركة' : 'Type'}</th>
                                <th style={{ textAlign: 'center' }}>{ar ? 'نوع المادة' : 'Material'}</th>
                                <th style={{ textAlign: 'center' }}>{ar ? 'الكمية' : 'Qty (L)'}</th>
                                <th style={{ textAlign: 'center' }}>{ar ? 'المرجع / التفاصيل' : 'Ref / Details'}</th>
                                <th style={{ textAlign: 'center' }}>{ar ? 'الحالة' : 'Status'}</th>
                                <th style={{ textAlign: 'center' }}>{ar ? 'إجراء' : 'Actions'}</th>
                              </tr>
                            </thead>
                            <tbody>
                              {siteMoves.length === 0 ? (
                                <tr><td colSpan={7} style={{ textAlign: 'center', padding: 40, color: 'var(--text-faint)' }}>
                                  {ar ? 'لا توجد سجلات' : 'No records'}
                                </td></tr>
                              ) : siteMoves.map(m => {
                                const movId = getMovementId(m);
                                const st = getStatus(m);
                                const stCfg = statusConfig[st];
                                const isOil = m.movementType === 'oil';
                                return (
                                  <tr key={movId}>
                                    <td style={{ whiteSpace: 'nowrap', fontSize: 11, fontWeight: 500, textAlign: 'center' }}>
                                      {fmtDate(m.transactionDate)}
                                    </td>
                                    <td style={{ textAlign: 'center' }}>
                                      <span className="badge" style={{ background: 'var(--warning)15', color: 'var(--warning)', border: '1px solid var(--warning)30', padding: '4px 8px', fontWeight: 700 }}>
                                        {m.transactionTypeLookupName || (isOil ? (ar ? 'زيت' : 'Oil') : (ar ? 'وقود' : 'Fuel'))}
                                      </span>
                                    </td>
                                    <td style={{ textAlign: 'center', fontWeight: 600 }}>
                                      {isOil ? (m.oilLookupName || m.oilTypeName || '—') : (m.fuelLookupName || '—')}
                                    </td>
                                    <td style={{ fontWeight: 800, fontSize: 14, textAlign: 'center' }}>
                                      {m.quantityLiters !== null && m.quantityLiters !== undefined
                                        ? m.quantityLiters.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })
                                        : '—'}
                                      <span style={{ fontSize: 10, fontWeight: 400, marginInlineStart: 3 }}>L</span>
                                    </td>
                                    <td style={{ textAlign: 'center', maxWidth: 220 }}>{renderMovementDetails(m)}</td>
                                    <td style={{ textAlign: 'center' }}>
                                      <span className="badge" style={{ background: `${stCfg.color}15`, color: stCfg.color, padding: '4px 8px', fontSize: 11 }}>
                                        {React.createElement(stCfg.icon, { size: 10, style: { marginInlineEnd: 4 } })}
                                        {stCfg.label}
                                      </span>
                                    </td>
                                    <td style={{ textAlign: 'center' }}>
                                      {st === 'pending' && canApprove && (
                                        <div style={{ display: 'flex', gap: 6, justifyContent: 'center' }}>
                                          <button className="btn btn-sm btn-ghost" style={{ color: 'var(--secondary)' }} onClick={() => handleApprove(movId, isOil ? 'oil' : 'fuel')}>
                                            <ShieldCheck size={13} /> {ar ? 'اعتماد' : 'Approve'}
                                          </button>
                                          <button className="btn btn-sm btn-ghost" style={{ color: 'var(--danger)' }} onClick={() => setRejectModal({ id: movId, type: isOil ? 'oil' : 'fuel' })}>
                                            <ShieldAlert size={13} /> {ar ? 'رفض' : 'Reject'}
                                          </button>
                                        </div>
                                      )}
                                    </td>
                                  </tr>
                                );
                              })}
                            </tbody>
                          </table>
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
