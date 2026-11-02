import React, { useState, useEffect, useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import client from '../api/client';
import {
  Package, Filter, Clock, ShieldCheck, ShieldAlert, LayoutGrid,
  ArrowRightLeft, AlertCircle, Plus, X, MapPin
} from 'lucide-react';
import ConfirmModal from '../components/ConfirmModal';
import Pagination from '../components/Pagination';
import { PageHeader, Button, Modal, FormField, Input } from '../components/ui';
import useToastStore from '../store/toastStore';

// typeLabels and statusConfig are now built lazily from i18n inside the component.
// Static fallback icons map (icons don't change with language).
const TAB_ICONS = {
  summary:    LayoutGrid,
  supply:     Package,
  transfer:   ArrowRightLeft,
  adjustment: Filter,
  loss:       AlertCircle,
};
const TAB_COLORS = {
  summary:    'var(--secondary)',
  supply:     'var(--secondary)',
  transfer:   'var(--primary)',
  adjustment: 'var(--text-muted)',
  loss:       'var(--danger)',
};
const STATUS_ICONS  = { pending: Clock, approved: ShieldCheck, rejected: ShieldAlert };
const STATUS_COLORS = { pending: 'var(--warning)', approved: 'var(--success)', rejected: 'var(--danger)' };

/** استخراج حالة الحركة من الـ DTO الجديد */
const getStatus = (m) => {
  if (!m) return 'pending';
  if (m.isApproved) return 'approved';
  if (m.approvedAt) return 'rejected';   // أُعيد isApproved=false بعد موافقة سابقة = رفض
  return 'pending';
};

/** استخراج معرف الحركة */
export const getMovementId = (movement) => movement?.id;

/** بناء خيار label لـ Select الزيت */
const oilOptionLabel = (o) => {
  const name  = o.oilLookupName || o.name || '';
  const sub   = o.subStationName || '';
  const stat  = o.stationName    || '';
  if (sub && stat && sub !== stat) return `${name} — ${sub} — ${stat}`;
  if (sub)  return `${name} — ${sub}`;
  if (stat) return `${name} — ${stat}`;
  return name;
};

/** بناء label لـ SubStation في القائمة */
const subStationOptionLabel = (site, stations) => {
  const stationName = site.stationName
    || (stations || []).find(s => s.id === site.stationId)?.name
    || '';
  return stationName ? `${stationName} ← ${site.name}` : site.name;
};

export const getApiErrorMessage = (err) => (
  err?.response?.data?.msg
  || err?.response?.data?.errors?.[0]?.msg
  || err?.response?.data?.title
  || 'حدث خطأ'
);

/** تفاصيل موجزة للحركة (المرجع / المورد) */
const renderMovementDetails = (m) => {
  const type = (m?.transactionTypeLookupName || '').toLowerCase();
  if (type.includes('توريد') || type.includes('supply') || type.includes('وارد')) {
    return m.supplierCompany || m.reference || '—';
  }
  if (type.includes('نقل') || type.includes('transfer')) {
    return m.destinationSubStationName || m.reference || '—';
  }
  if (type.includes('فاقد') || type.includes('loss')) {
    return m.correctionReason || m.notes || '—';
  }
  return m?.reference || m?.notes || '—';
};

// ─────────────────────────────────────────────────────────────────────────────

function FuelMovements({ lang, currentUser }) {
  const ar = lang === 'ar';
  const { t, i18n } = useTranslation();

  useEffect(() => {
    if (lang && i18n && i18n.language !== lang) {
      i18n.changeLanguage(lang);
    }
  }, [lang, i18n]);

  // i18n-driven config (reconstructed on lang change via useMemo)
  const typeLabels = useMemo(() => ({
    summary:    { label: t('movements.tabs.summary'),    color: TAB_COLORS.summary,    icon: TAB_ICONS.summary },
    supply:     { label: t('movements.tabs.supply'),     color: TAB_COLORS.supply,     icon: TAB_ICONS.supply },
    transfer:   { label: t('movements.tabs.transfer'),   color: TAB_COLORS.transfer,   icon: TAB_ICONS.transfer },
    adjustment: { label: t('movements.tabs.adjustment'), color: TAB_COLORS.adjustment, icon: TAB_ICONS.adjustment },
    loss:       { label: t('movements.tabs.loss'),       color: TAB_COLORS.loss,       icon: TAB_ICONS.loss },
  }), [t]);

  const statusConfig = useMemo(() => ({
    pending:  { label: t('movements.status.pending'),  color: STATUS_COLORS.pending,  icon: STATUS_ICONS.pending },
    approved: { label: t('movements.status.approved'), color: STATUS_COLORS.approved, icon: STATUS_ICONS.approved },
    rejected: { label: t('movements.status.rejected'), color: STATUS_COLORS.rejected, icon: STATUS_ICONS.rejected },
  }), [t]);

  const perms = currentUser?.permissions?.fuel_movements || currentUser?.permissions?.movements || {};
  const canAdd     = currentUser?.role === 'admin' || perms.create || perms.add;
  const canApprove = currentUser?.role === 'admin' || perms.approve;

  const [movements,    setMovements]    = useState([]);
  const [tanks,        setTanks]        = useState([]);
  const [oils,         setOils]         = useState([]);
  const [stations,     setStations]     = useState([]);
  const [subStations,  setSubStations]  = useState([]);
  const [transferDests, setTransferDests] = useState([]);   // SubStation list for transfer target
  const [activeTab,    setActiveTab]    = useState('summary');
  const [summaryData,  setSummaryData]  = useState([]);
  const [showForm,     setShowForm]     = useState(false);
  const [editId,       setEditId]       = useState(null);
  const [saving,       setSaving]       = useState(false);
  const [status, setStatus] = useState(null);
  const showToast = useToastStore((s) => s.showToast);
  const [rejectReason, setRejectReason] = useState('');
  const [showRejectModal, setShowRejectModal] = useState(null);
  const [confirmModal, setConfirmModal] = useState({ open: false, id: null });
  const [selectedSupplySubStation, setSelectedSupplySubStation] = useState('');

  const TAB_PAGE_SIZE = 20;
  const [tabPage, setTabPage] = useState({});

  const [form, setForm] = useState(initialForm());

  function initialForm() {
    return {
      type: 'supply', itemType: 'fuel', tank: '', oil: '',
      targetTank: '', targetSubStation: '', subStation: '',
      quantityLiters: '', pricePerLiter: '', supplierName: '',
      driverName: '', driverId: '', truckNumber: '', trailerNumber: '',
      receiptQuantity: '', supplyNumber: '', receiptNumber: '',
      receiptDate: '', supplyDate: '', calibrationReading: '', observedReading: '',
      issueOrderNumber: '', issueOrderDate: '',
      reason: '', notes: '', movementDate: new Date().toISOString().slice(0, 16),
    };
  }

  // ── Fetch all required data ──────────────────────────────────────────────
  const fetchAll = async () => {
    try {
      const [movRes, tankRes, oilRes, statRes, subStatRes, destRes] = await Promise.allSettled([
        client.get('/api/fuel-movements'),
        client.get('/api/fuel-tanks'),
        client.get('/api/oils'),
        client.get('/api/stations'),
        client.get('/api/sub-stations'),
        client.get('/api/fuel-movements/transfer-destinations'),
      ]);

      if (movRes.status  === 'fulfilled') setMovements(movRes.value.data?.items || []);
      if (tankRes.status === 'fulfilled') setTanks(tankRes.value.data || []);
      if (oilRes.status  === 'fulfilled') setOils(oilRes.value.data  || []);
      if (statRes.status === 'fulfilled') setStations(statRes.value.data || []);
      if (subStatRes.status === 'fulfilled') setSubStations((subStatRes.value.data || []).filter(s => s.isActive !== false));
      if (destRes.status === 'fulfilled') {
        // Backend returns List<SubStationListDto> directly
        const dest = destRes.value.data;
        setTransferDests(Array.isArray(dest) ? dest : (dest?.data || []));
      }
    } catch { /* silently fail — each Promise.allSettled handles its own */ }
  };

  useEffect(() => { fetchAll(); }, [lang]);

  const fld = (k, v) => setForm(f => ({ ...f, [k]: v }));

  // ── Tank selection ────────────────────────────────────────────────────────
  const handleTankSelect = (tankId, isTarget = false) => {
    const list = isTarget ? tanks : tanks;  // both lists are the same tank list for now
    const t = list.find(x => String(x.id) === String(tankId));
    if (!t) return;
    if (isTarget) {
      setForm(f => ({ ...f, targetTank: tankId, targetSubStation: String(t.subStationId) }));
    } else {
      setForm(f => ({ ...f, tank: tankId, subStation: String(t.subStationId) }));
    }
  };

  // ── Oil selection ─────────────────────────────────────────────────────────
  const handleOilSelect = (oilId) => {
    const o = oils.find(x => String(x.id) === String(oilId));
    setForm(f => ({ ...f, oil: oilId, subStation: o ? String(o.subStationId || o.stationId || '') : f.subStation }));
  };

  // ── SubStation selection for supply form ─────────────────────────────────
  const handleSupplySiteSelect = (siteId) => {
    setSelectedSupplySubStation(siteId);
    setForm(f => ({ ...f, subStation: siteId, tank: '' }));
  };

  // ── Build payload matching CreateFuelMovementDto ──────────────────────────
  const buildPayload = (formData) => {
    const num = v => (v === '' || v === null || v === undefined ? null : Number(v));
    const str = v => (v === '' || v === null || v === undefined ? null : String(v));

    // Date resolution
    let txDate = formData.movementDate;
    if (activeTab === 'supply')   txDate = formData.supplyDate   || formData.movementDate;
    if (activeTab === 'transfer') txDate = formData.issueOrderDate || formData.movementDate;

    // SubStation from selected tank or directly selected
    let subStationId = num(formData.subStation) || num(formData.targetSubStation);
    let fuelLookupId = null;

    const selectedTank = tanks.find(t => String(t.id) === String(formData.tank));
    if (selectedTank) {
      subStationId = subStationId || selectedTank.subStationId;
      fuelLookupId = selectedTank.fuelLookupId || null;
    }

    return {
      subStationId:          subStationId,
      fuelLookupId:          fuelLookupId || 1,    // fallback — user must select tank with fuel type
      transactionTypeLookupId: null,
      transactionDate:       txDate ? new Date(txDate).toISOString() : new Date().toISOString(),
      quantityLiters:        num(formData.quantityLiters),
      reference:             str(formData.supplyNumber || formData.issueOrderNumber),
      supplierCompany:       str(formData.supplierName),
      driverName:            str(formData.driverName),
      driverIdCard:          str(formData.driverId),
      truckNumber:           str(formData.truckNumber),
      invoiceNumber:         str(formData.receiptNumber),
      invoiceDate:           formData.receiptDate ? new Date(formData.receiptDate).toISOString() : null,
      invoiceQuantity:       num(formData.receiptQuantity),
      warehouseReceiptNumber: str(formData.receiptNumber),
      warehouseReceiptDate:  formData.receiptDate ? new Date(formData.receiptDate).toISOString() : null,
      warehouseQuantity:     num(formData.receiptQuantity),
      meterId:               null,
      prevMeterVisualReading:     num(formData.observedReading),
      currentMeterVisualReading:  num(formData.observedReading),
      prevMeterStandardReading:   num(formData.calibrationReading),
      currentMeterStandardReading: num(formData.calibrationReading),
      sourceSubStationId:    num(formData.subStation),
      destinationSubStationId: num(formData.targetSubStation),
      correctedFromId:       null,
      correctionReason:      str(formData.reason),
      notes:                 str(formData.notes),
    };
  };

  // ── Submit form ───────────────────────────────────────────────────────────
  const handleSubmit = async (e) => {
    e.preventDefault();
    setSaving(true); setStatus(null);

    // ── Client-side capacity validation (R5) ──
    if (activeTab === 'supply' && form.tank && form.itemType !== 'oil') {
      const selectedTank = tanks.find(t => String(t.id) === String(form.tank));
      if (selectedTank) {
        const capacity = Number(selectedTank.capacityLiters) || 0;
        const current = Number(selectedTank.actualStockLiters) || 0;
        const qty = Number(form.quantityLiters) || 0;
        if (qty > 0 && capacity > 0 && (current + qty) > capacity) {
          setStatus({
            type: 'error',
            msg: ar
              ? `الكمية المطلوبة (${qty.toLocaleString()} L) ستتجاوز سعة الخزان (${capacity.toLocaleString()} L). المخزون الحالي: ${current.toLocaleString()} L`
              : `Requested quantity (${qty.toLocaleString()} L) exceeds tank capacity (${capacity.toLocaleString()} L). Current stock: ${current.toLocaleString()} L`
          });
          setSaving(false);
          return;
        }
      }
    }

    try {
      const payload = buildPayload(form);
      if (editId) {
        await client.put(`/api/fuel-movements/${editId}`, payload);
      } else {
        await client.post('/api/fuel-movements', payload);
      }
      setStatus({ type: 'success', msg: ar ? 'تم الحفظ بانتظار موافقة المشرف ✓' : 'Saved! Awaiting supervisor approval ✓' });
      setShowForm(false); setEditId(null);
      setForm(initialForm());
      setSelectedSupplySubStation('');
      fetchAll();
    } catch (err) {
      setStatus({ type: 'error', msg: getApiErrorMessage(err) });
    } finally { setSaving(false); }
  };

  // ── Approve ────────────────────────────────────────────────────────────────
  const handleApprove = (id) => setConfirmModal({ open: true, id });

  const confirmApprove = async () => {
    const id = confirmModal.id;
    setConfirmModal({ open: false, id: null });
    try {
      await client.post(`/api/fuel-movements/${id}/approve`);
      fetchAll();
      showToast(ar ? 'تم الاعتماد ✓' : 'Approved ✓', 'success');
    } catch (err) { showToast(getApiErrorMessage(err), 'error'); }
  };

  // ── Reject ─────────────────────────────────────────────────────────────────
  const handleReject = async () => {
    if (!rejectReason) return showToast(ar ? 'يرجى إدخال سبب الرفض' : 'Enter rejection reason', 'error');
    try {
      await client.post(`/api/fuel-movements/${showRejectModal}/reject`);
      setShowRejectModal(null); setRejectReason('');
      fetchAll();
      showToast(ar ? 'تم الرفض ✓' : 'Rejected ✓', 'success');
    } catch (err) { showToast(getApiErrorMessage(err), 'error'); }
  };

  // ── Grouping by sub-station name ──────────────────────────────────────────
  const groupedMovements = useMemo(() => {
    const groups = {};
    movements.forEach(m => {
      const key = m.subStationName || (ar ? 'غير محدد' : 'Unassigned');
      if (!groups[key]) groups[key] = [];
      groups[key].push(m);
    });
    return groups;
  }, [movements, ar]);

  // ── Tab pagination — all tabs show all movements (no backend type field) ──
  const tabMovements = useMemo(
    () => activeTab === 'summary' ? [] : movements,
    [movements, activeTab]
  );
  const tabTotal      = tabMovements.length;
  const tabTotalPages = Math.max(1, Math.ceil(tabTotal / TAB_PAGE_SIZE));
  const currentTabPage = tabPage[activeTab] || 1;
  const paginatedTabMovements = useMemo(
    () => tabMovements.slice((currentTabPage - 1) * TAB_PAGE_SIZE, currentTabPage * TAB_PAGE_SIZE),
    [tabMovements, currentTabPage]
  );

  // ── Date formatting ────────────────────────────────────────────────────────
  const fmtDate = (value, withTime = false) => {
    if (!value) return '—';
    const d = new Date(value);
    if (Number.isNaN(d.getTime())) return '—';
    return withTime
      ? d.toLocaleString(ar ? 'ar-YE' : 'en-US')
      : d.toLocaleDateString(ar ? 'ar-YE' : 'en-US');
  };

  // ── Tanks for selected supply sub-station ─────────────────────────────────
  const tanksForSupplySite = useMemo(
    () => (selectedSupplySubStation
      ? tanks.filter(t => String(t.subStationId) === String(selectedSupplySubStation))
      : tanks),
    [tanks, selectedSupplySubStation]
  );

  // ─────────────────────────────────────────────────────────────────────────
  // RENDER
  // ─────────────────────────────────────────────────────────────────────────
  return (
    <div className="fuel-movements-page" style={{ animation: 'fadeIn 0.5s ease' }}>
      {/* ── Header (unified PageHeader component) ── */}
      <PageHeader
        title={t('movements.pageTitle')}
        subtitle={t('movements.pageSubtitle')}
        actions={activeTab !== 'summary' && canAdd ? (
          <Button icon={Plus} onClick={() => { setShowForm(true); setEditId(null); setForm(initialForm()); setSelectedSupplySubStation(''); }}>
            {ar ? 'حركة جديدة' : 'New Movement'}
          </Button>
        ) : null}
      />

      {/* ── Tabs (labels now come from i18n) ── */}
      <div style={{ display: 'flex', justifyContent: 'center', marginBottom: 30 }}>
        <div className="ft-tabs">
          {Object.entries(typeLabels).filter(([k]) => k !== 'adjustment').map(([k, cfg]) => (
            <button key={k} className={`ft-tab ${activeTab === k ? 'active' : ''}`}
              onClick={() => {
                setActiveTab(k);
                setTabPage(p => ({ ...p, [k]: 1 }));
                if (!showForm) fld('type', k);
                fld('itemType', 'fuel');
                fld('oil', ''); fld('tank', '');
                setSelectedSupplySubStation('');
              }}>
              <cfg.icon size={16} /> {cfg.label}
            </button>
          ))}
        </div>
      </div>

      {/* ── Alert ── */}
      {status && (
        <div className={`alert ${status.type === 'success' ? 'alert-success' : 'alert-danger'}`} style={{ marginBottom: 16 }}>
          {status.msg}
        </div>
      )}

      {/* ═══════════════════════════════ SUMMARY TAB ═══════════════════════════ */}
      {activeTab === 'summary' && (
        <div className="section-card" style={{ marginBottom: 30 }}>
          <div className="section-card-header">
            <div className="section-card-title">{ar ? 'ملخص مخزون المواقع' : 'Site Inventory Summary'}</div>
            <button className="btn btn-sm btn-ghost" onClick={fetchAll}>{ar ? 'تحديث البيانات' : 'Refresh'}</button>
          </div>
          <div style={{ overflowX: 'auto' }}>
            <table className="data-table" style={{ borderTop: 'none' }}>
              <thead>
                <tr style={{ background: 'hsla(220,40%,60%,0.05)' }}>
                  <th style={{ textAlign: 'center' }}>{ar ? 'اسم الموقع' : 'Site'}</th>
                  <th style={{ textAlign: 'center' }}>{ar ? 'سعة الخزان' : 'Capacity'}</th>
                  <th style={{ textAlign: 'center' }}>{ar ? 'الحد الأدنى' : 'Min Level'}</th>
                  <th style={{ textAlign: 'center' }}>{ar ? 'الوقود الوارد' : 'Inbound'}</th>
                  <th style={{ textAlign: 'center' }}>{ar ? 'الوقود المنقول' : 'Transferred'}</th>
                  <th style={{ textAlign: 'center' }}>{ar ? 'الوقود المفقود' : 'Lost'}</th>
                  <th style={{ textAlign: 'center' }}>{ar ? 'الوقود المستهلك' : 'Consumed'}</th>
                  <th style={{ background: '#84cc1615', color: '#84cc16', textAlign: 'center' }}>{ar ? 'المخزون المتبقي' : 'Remaining'}</th>
                </tr>
              </thead>
              <tbody>
                {summaryData.length === 0 ? (
                  <tr><td colSpan={8} style={{ textAlign: 'center', padding: 40, color: 'var(--text-faint)' }}>
                    {ar ? 'لا توجد بيانات' : 'No data available'}
                  </td></tr>
                ) : summaryData.map((s, idx) => (
                  <tr key={idx}>
                    <td style={{ fontWeight: 700, color: 'var(--secondary)', textAlign: 'center' }}>{s.siteLabel || s.stationName || s.subStationName || '—'}</td>
                    <td style={{ textAlign: 'center' }}>{s.capacity?.toLocaleString() ?? '—'}</td>
                    <td style={{ textAlign: 'center' }}>{s.minLevel?.toLocaleString() ?? '—'}</td>
                    <td style={{ color: 'var(--secondary)', textAlign: 'center' }}>{s.supply?.toLocaleString() ?? '—'}</td>
                    <td style={{ color: s.transfer >= 0 ? 'var(--secondary)' : 'var(--danger)', textAlign: 'center' }}>{s.transfer?.toLocaleString() ?? '—'}</td>
                    <td style={{ color: 'var(--danger)', textAlign: 'center' }}>{s.loss?.toLocaleString() ?? '—'}</td>
                    <td style={{ color: 'var(--warning)', textAlign: 'center' }}>{s.consumption?.toLocaleString() ?? '—'}</td>
                    <td style={{ fontWeight: 800, textAlign: 'center' }}>{s.remaining?.toLocaleString() ?? '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* ═══════════════════ GROUPED MOVEMENT LOG (Summary tab only) ═══════════ */}
      {!showForm && activeTab === 'summary' && (
        <div className="grouped-container" style={{ animation: 'fadeInUp 0.4s ease' }}>
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 20 }}>
            <h2 style={{ fontSize: 20, fontWeight: 'bold' }}>
              {ar ? 'سجل الحركات العام (مبوب حسب المواقع)' : 'Movement Logs (Grouped by Site)'}
            </h2>
            <div className="badge badge-muted">{movements.length} {ar ? 'حركة إجمالية' : 'total'}</div>
          </div>

          {Object.entries(groupedMovements).map(([siteName, siteMoves]) => (
            <div key={siteName} className="station-group" style={{ marginBottom: 40 }}>
              <div className="station-group-header" style={{
                display: 'flex', alignItems: 'center', gap: 15,
                padding: '15px 20px', background: 'var(--bg-card)',
                border: '1px solid var(--border)', borderBottom: '2px solid #84cc16',
                borderRadius: 12, marginBottom: 20, boxShadow: '0 4px 15px rgba(0,0,0,0.1)'
              }}>
                <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', width: 42, height: 42, borderRadius: 10, background: '#84cc1615', color: '#84cc16' }}>
                  <MapPin size={22} />
                </div>
                <div>
                  <h3 style={{ margin: 0, fontSize: 18, fontWeight: 'bold' }}>{siteName}</h3>
                  <div style={{ fontSize: 12, color: 'var(--text-muted)', marginTop: 4 }}>{ar ? 'الحركات المتعلقة بهذا الموقع' : 'Movements for this location'}</div>
                </div>
                <span style={{ marginInlineStart: 'auto', padding: '6px 14px', background: 'var(--primary)', color: '#fff', borderRadius: 20, fontSize: 13, fontWeight: 'bold' }}>
                  {siteMoves.length} {ar ? 'حركات' : 'Movements'}
                </span>
              </div>

              <div className="section-card" style={{ padding: 0 }}>
                <div style={{ overflowX: 'auto' }}>
                  <table className="data-table">
                    <thead>
                      <tr>
                        <th style={{ textAlign: 'center', width: 120 }}>{ar ? 'التاريخ' : 'Date'}</th>
                        <th style={{ textAlign: 'center' }}>{ar ? 'نوع الحركة' : 'Type'}</th>
                        <th style={{ textAlign: 'center' }}>{ar ? 'نوع الوقود' : 'Fuel'}</th>
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
                        const movId  = getMovementId(m);
                        const st     = getStatus(m);
                        const stCfg  = statusConfig[st];
                        return (
                          <tr key={movId}>
                            <td style={{ whiteSpace: 'nowrap', fontSize: 11, fontWeight: 500, textAlign: 'center' }}>
                              {fmtDate(m.transactionDate)}
                            </td>
                            <td style={{ textAlign: 'center' }}>
                              <span className="badge" style={{ background: 'var(--warning)15', color: 'var(--warning)', border: '1px solid var(--warning)30', padding: '4px 8px', fontWeight: 700 }}>
                                {m.transactionTypeLookupName || (ar ? 'وقود' : 'Fuel')}
                              </span>
                            </td>
                            <td style={{ textAlign: 'center', fontWeight: 600 }}>
                              {m.fuelLookupName || '—'}
                            </td>
                            <td style={{ fontWeight: 800, fontSize: 14, textAlign: 'center' }}>
                              {m.quantityLiters !== null && m.quantityLiters !== undefined
                                ? m.quantityLiters.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })
                                : '—'}
                              <span style={{ fontSize: 10, fontWeight: 400, marginInlineStart: 3 }}>L</span>
                            </td>
                            <td style={{ textAlign: 'center', maxWidth: 220 }}>
                              {renderMovementDetails(m)}
                            </td>
                            <td style={{ textAlign: 'center' }}>
                              <span className="badge" style={{ background: `${stCfg.color}15`, color: stCfg.color, padding: '4px 8px', fontSize: 11 }}>
                                {React.createElement(stCfg.icon, { size: 10, style: { marginInlineEnd: 4 } })}
                                {stCfg.label}
                              </span>
                            </td>
                            <td style={{ textAlign: 'center' }}>
                              {st === 'pending' && canApprove && (
                                <div style={{ display: 'flex', gap: 6, justifyContent: 'center' }}>
                                  <button className="btn btn-sm btn-ghost" style={{ color: 'var(--secondary)' }} onClick={() => handleApprove(movId)}>
                                    <ShieldCheck size={13} /> {ar ? 'اعتماد' : 'Approve'}
                                  </button>
                                  <button className="btn btn-sm btn-ghost" style={{ color: 'var(--danger)' }} onClick={() => setShowRejectModal(movId)}>
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
      )}

      {/* ═══════════════════════════ TAB MOVEMENT TABLE ═════════════════════ */}
      {!showForm && activeTab !== 'summary' && (<>
        <div className="section-card" style={{ padding: 0, animation: 'fadeInUp 0.4s ease' }}>
          <div className="section-card-header">
            <div>
              <div className="section-card-title">{typeLabels[activeTab]?.label}</div>
              <div style={{ fontSize: 12, color: 'var(--text-muted)', marginTop: 4 }}>
                {ar ? 'جميع حركات الوقود المسجلة' : 'All registered fuel movements'}
              </div>
            </div>
            <div className="badge badge-muted">{tabTotal} {ar ? 'حركة' : 'records'}</div>
          </div>

          <div style={{ overflowX: 'auto' }}>
            <table className="data-table">
              <thead>
                <tr>
                  <th style={{ textAlign: 'center', minWidth: 110 }}>{ar ? 'تاريخ الحركة' : 'Date'}</th>
                  <th style={{ textAlign: 'center', minWidth: 110 }}>{ar ? 'الموقع' : 'Site'}</th>
                  <th style={{ textAlign: 'center', minWidth: 110 }}>{ar ? 'نوع الحركة' : 'Type'}</th>
                  <th style={{ textAlign: 'center', minWidth: 110 }}>{ar ? 'نوع الوقود' : 'Fuel'}</th>
                  <th style={{ textAlign: 'center', minWidth: 110 }}>{ar ? 'الكمية' : 'Qty (L)'}</th>
                  <th style={{ textAlign: 'center', minWidth: 200 }}>{ar ? 'المرجع / التفاصيل' : 'Ref / Details'}</th>
                  <th style={{ textAlign: 'center', minWidth: 160 }}>{ar ? 'ملاحظات' : 'Notes'}</th>
                  <th style={{ textAlign: 'center', minWidth: 100 }}>{ar ? 'الحالة' : 'Status'}</th>
                  <th style={{ textAlign: 'center', minWidth: 130 }}>{ar ? 'اعتمد بواسطة' : 'Approved By'}</th>
                  <th style={{ textAlign: 'center', minWidth: 130 }}>{ar ? 'تاريخ الاعتماد' : 'Approved At'}</th>
                  <th style={{ textAlign: 'center', minWidth: 160 }}>{ar ? 'الإجراءات' : 'Actions'}</th>
                </tr>
              </thead>
              <tbody>
                {paginatedTabMovements.length === 0 ? (
                  <tr>
                    <td colSpan={11} style={{ textAlign: 'center', padding: 40, color: 'var(--text-faint)' }}>
                      {ar ? 'لا توجد حركات بعد' : 'No records yet'}
                    </td>
                  </tr>
                ) : paginatedTabMovements.map(m => {
                  const movId = getMovementId(m);
                  const st    = getStatus(m);
                  const cfg   = statusConfig[st];
                  return (
                    <tr key={movId}>
                      <td style={{ textAlign: 'center', whiteSpace: 'nowrap', fontSize: 12 }}>
                        <div style={{ fontWeight: 700 }}>{fmtDate(m.transactionDate)}</div>
                      </td>
                      <td style={{ textAlign: 'center', fontWeight: 600 }}>
                        {m.subStationName || '—'}
                      </td>
                      <td style={{ textAlign: 'center' }}>
                        <span className="badge" style={{ background: 'var(--warning)15', color: 'var(--warning)' }}>
                          {m.transactionTypeLookupName || (ar ? 'وقود' : 'Fuel')}
                        </span>
                      </td>
                      <td style={{ textAlign: 'center', fontWeight: 600 }}>
                        {m.fuelLookupName || '—'}
                      </td>
                      <td className="positive-cell" style={{ textAlign: 'center', fontWeight: 800 }}>
                        {(m.quantityLiters || 0).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                        <span style={{ fontSize: 10, marginInlineStart: 4 }}>L</span>
                      </td>
                      <td style={{ textAlign: 'center', whiteSpace: 'normal', maxWidth: 280 }}>
                        {renderMovementDetails(m)}
                      </td>
                      <td style={{ textAlign: 'center', whiteSpace: 'normal', maxWidth: 200 }}>
                        {m.notes || '—'}
                      </td>
                      <td style={{ textAlign: 'center' }}>
                        <span className="badge" style={{ background: `${cfg.color}15`, color: cfg.color }}>
                          {React.createElement(cfg.icon, { size: 10, style: { marginInlineEnd: 4 } })}
                          {cfg.label}
                        </span>
                      </td>
                      <td style={{ textAlign: 'center', fontWeight: 600 }}>
                        {m.approvedByName || '—'}
                      </td>
                      <td style={{ textAlign: 'center', fontSize: 12 }}>
                        {fmtDate(m.approvedAt)}
                      </td>
                      <td style={{ textAlign: 'center' }}>
                        {st === 'pending' && canApprove ? (
                          <div style={{ display: 'flex', gap: 6, justifyContent: 'center' }}>
                            <button className="btn btn-sm btn-ghost" style={{ color: 'var(--secondary)' }} onClick={() => handleApprove(movId)}>
                              <ShieldCheck size={13} /> {ar ? 'اعتماد' : 'Approve'}
                            </button>
                            <button className="btn btn-sm btn-ghost" style={{ color: 'var(--danger)' }} onClick={() => setShowRejectModal(movId)}>
                              <ShieldAlert size={13} /> {ar ? 'رفض' : 'Reject'}
                            </button>
                          </div>
                        ) : (
                          <span style={{ color: 'var(--text-muted)', fontSize: 12 }}>—</span>
                        )}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </div>

        <Pagination
          page={currentTabPage}
          totalPages={tabTotalPages}
          total={tabTotal}
          pageSize={TAB_PAGE_SIZE}
          lang={lang}
          onPageChange={(p) => setTabPage(prev => ({ ...prev, [activeTab]: p }))}
        />
      </>)}

      {/* ═══════════════════════════════ FORM ═══════════════════════════════ */}
      {showForm && activeTab !== 'summary' && (
        <div className="section-card" style={{ marginBottom: 30, animation: 'fadeInUp 0.3s ease' }}>
          <div className="section-card-header">
            <div className="section-card-title">
              {editId
                ? (ar ? 'تعديل حركة' : 'Edit Movement')
                : (ar ? `تسجيل ${typeLabels[activeTab].label}` : `New ${activeTab}`)}
            </div>
            <button className="btn btn-ghost btn-sm btn-icon" onClick={() => setShowForm(false)}>
              <X size={16} />
            </button>
          </div>
          <div className="section-card-body">
            <form onSubmit={handleSubmit}>

              {/* ── Supply Form ── */}
              {activeTab === 'supply' && (
                <>
                  <div className="form-row" style={{ marginBottom: 20 }}>
                    <div className="form-group">
                      <label className="form-label">{ar ? 'نوع المادة *' : 'Item Type *'}</label>
                      <div className="btn-group">
                        <button type="button"
                          className={`btn btn-sm ${form.itemType !== 'oil' ? 'btn-primary' : 'btn-ghost'}`}
                          onClick={() => { fld('itemType', 'fuel'); fld('oil', ''); }}>
                          {ar ? 'وقود' : 'Fuel'}
                        </button>
                        <button type="button"
                          className={`btn btn-sm ${form.itemType === 'oil' ? 'btn-primary' : 'btn-ghost'}`}
                          onClick={() => { fld('itemType', 'oil'); fld('tank', ''); setSelectedSupplySubStation(''); }}>
                          {ar ? 'زيوت' : 'Oils'}
                        </button>
                      </div>
                    </div>
                  </div>

                  {/* Fuel supply fields */}
                  {form.itemType !== 'oil' && (
                    <>
                      <div className="form-section-header">{ar ? 'بيانات التوريد الأساسية' : 'Primary Supply Data'}</div>
                      <div className="form-row">
                        <FormField label={ar ? 'الموقع / الفرع *' : 'Site / Branch *'} required>
                          <select className="form-select" value={selectedSupplySubStation}
                            onChange={e => handleSupplySiteSelect(e.target.value)} required>
                            <option value="">— {ar ? 'اختر الموقع' : 'Select Site'} —</option>
                            {subStations.map(site => (
                              <option key={site.id} value={site.id}>
                                {subStationOptionLabel(site, stations)}
                              </option>
                            ))}
                          </select>
                        </FormField>
                        <FormField label={ar ? 'الخزان المستلم *' : 'Receiving Tank *'} required>
                          <select className="form-select" value={form.tank}
                            onChange={e => handleTankSelect(e.target.value)} required>
                            <option value="">— {ar ? 'اختر الخزان' : 'Select Tank'} —</option>
                            {tanksForSupplySite.map(t => (
                              <option key={t.id} value={t.id}>
                                {t.name} ({t.fuelName || '—'})
                              </option>
                            ))}
                          </select>
                        </FormField>
                      </div>
                      {/* ── Tank Capacity Info ── */}
                      {form.tank && (() => {
                        const selectedTank = tanksForSupplySite.find(t => String(t.id) === String(form.tank));
                        if (!selectedTank) return null;
                        const capacity = Number(selectedTank.capacityLiters) || 0;
                        const current = Number(selectedTank.actualStockLiters) || 0;
                        const fillPct = capacity > 0 ? Math.min((current / capacity) * 100, 100) : 0;
                        const fillColor = fillPct > 90 ? '#ef4444' : fillPct > 70 ? 'var(--warning)' : '#10b981';
                        const qty = Number(form.quantityLiters) || 0;
                        const wouldExceed = qty > 0 && capacity > 0 && (current + qty) > capacity;
                        return (
                          <div style={{
                            padding: '12px 16px', borderRadius: 10, marginBottom: 20,
                            background: wouldExceed ? '#ef444415' : 'var(--glass-bg)',
                            border: `1px solid ${wouldExceed ? '#ef444440' : 'var(--border)'}`,
                          }}>
                            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 8 }}>
                              <span style={{ fontSize: 13, fontWeight: 700, color: 'var(--text)' }}>
                                {ar ? 'سعة الخزان' : 'Tank Capacity'}
                              </span>
                              <span style={{ fontSize: 12, fontWeight: 600, color: fillColor }}>
                                {current.toLocaleString()} / {capacity.toLocaleString()} L
                              </span>
                            </div>
                            <div style={{ height: 6, borderRadius: 3, background: 'var(--border)', overflow: 'hidden' }}>
                              <div style={{ height: '100%', width: `${fillPct}%`, background: fillColor, borderRadius: 3, transition: 'width 0.3s' }} />
                            </div>
                            {wouldExceed && (
                              <div style={{ marginTop: 8, fontSize: 12, color: '#ef4444', fontWeight: 600 }}>
                                <AlertCircle size={12} style={{ marginInlineEnd: 4, verticalAlign: 'middle' }} />
                                {ar
                                  ? `الكمية المطلوبة (${qty.toLocaleString()} L) ستتجاوز السعة. المتاح: ${capacity.toLocaleString()} L`
                                  : `Requested qty (${qty.toLocaleString()} L) exceeds capacity. Available: ${capacity.toLocaleString()} L`}
                              </div>
                            )}
                          </div>
                        );
                      })()}
                      <div className="form-row">
                        <FormField label={ar ? 'الجهة الموردة' : 'Supplier'}>
                          <Input value={form.supplierName} onChange={e => fld('supplierName', e.target.value)} />
                        </FormField>
                        <FormField label={ar ? 'رقم التوريد' : 'Supply No.'}>
                          <Input value={form.supplyNumber} onChange={e => fld('supplyNumber', e.target.value)} />
                        </FormField>
                      </div>
                      <div className="form-row">
                        <FormField label={ar ? 'الكمية الموردة (لتر) *' : 'Qty (L) *'} required>
                          <Input type="number" min="0.001" step="0.001"
                            value={form.quantityLiters} onChange={e => fld('quantityLiters', e.target.value)} required />
                        </FormField>
                        <FormField label={ar ? 'تاريخ التوريد' : 'Supply Date'}>
                          <Input type="date" value={form.supplyDate} onChange={e => fld('supplyDate', e.target.value)} />
                        </FormField>
                      </div>

                      <div className="form-section-header">{ar ? 'بيانات الناقل والإيصال' : 'Truck & Receipt'}</div>
                      <div className="form-row">
                        <FormField label={ar ? 'اسم السواق' : 'Driver Name'}>
                          <Input value={form.driverName} onChange={e => fld('driverName', e.target.value)} />
                        </FormField>
                        <FormField label={ar ? 'البطاقة الشخصية' : 'Driver ID'}>
                          <Input value={form.driverId} onChange={e => fld('driverId', e.target.value)} />
                        </FormField>
                      </div>
                      <div className="form-row">
                        <FormField label={ar ? 'رقم القاطرة' : 'Truck No.'}>
                          <Input value={form.truckNumber} onChange={e => fld('truckNumber', e.target.value)} />
                        </FormField>
                        <FormField label={ar ? 'رقم المقطورة' : 'Trailer No.'}>
                          <Input value={form.trailerNumber} onChange={e => fld('trailerNumber', e.target.value)} />
                        </FormField>
                      </div>
                      <div className="form-row">
                        <FormField label={ar ? 'رقم الإيصال' : 'Receipt No.'}>
                          <Input value={form.receiptNumber} onChange={e => fld('receiptNumber', e.target.value)} />
                        </FormField>
                        <FormField label={ar ? 'الكمية في الإيصال' : 'Receipt Qty'}>
                          <Input type="number" value={form.receiptQuantity} onChange={e => fld('receiptQuantity', e.target.value)} />
                        </FormField>
                      </div>
                      <div className="form-row">
                        <FormField label={ar ? 'تاريخ الإيصال' : 'Receipt Date'}>
                          <Input type="date" value={form.receiptDate} onChange={e => fld('receiptDate', e.target.value)} />
                        </FormField>
                      </div>

                      <div className="form-section-header">{ar ? 'قراءات العدادات' : 'Meter Readings'}</div>
                      <div className="form-row">
                        <FormField label={ar ? 'العداد العياري' : 'Calibration Reading'}>
                          <Input type="number" value={form.calibrationReading} onChange={e => fld('calibrationReading', e.target.value)} />
                        </FormField>
                        <FormField label={ar ? 'العداد المشاهد' : 'Observed Reading'}>
                          <Input type="number" value={form.observedReading} onChange={e => fld('observedReading', e.target.value)} />
                        </FormField>
                      </div>
                    </>
                  )}

                  {/* Oil supply fields */}
                  {form.itemType === 'oil' && (
                    <>
                       <div className="form-section-header">{ar ? 'بيانات توريد الزيت' : 'Oil Supply Data'}</div>
                       <div className="form-row">
                         <FormField label={ar ? 'نوع الزيت *' : 'Oil Type *'} required>
                           <select className="form-select" value={form.oil}
                             onChange={e => handleOilSelect(e.target.value)} required>
                             <option value="">— {ar ? 'اختر نوع الزيت' : 'Select oil'} —</option>
                             {oils.map(o => (
                               <option key={o.id} value={o.id}>{oilOptionLabel(o)}</option>
                             ))}
                           </select>
                         </FormField>
                         <FormField label={ar ? 'الكمية الموردة (لتر) *' : 'Qty (L) *'} required>
                           <Input type="number" min="0.001" step="0.001"
                             value={form.quantityLiters} onChange={e => fld('quantityLiters', e.target.value)} required />
                         </FormField>
                       </div>
                       <div className="form-row">
                         <FormField label={ar ? 'الجهة الموردة' : 'Supplier'}>
                           <Input value={form.supplierName} onChange={e => fld('supplierName', e.target.value)} />
                         </FormField>
                         <FormField label={ar ? 'تاريخ التوريد' : 'Supply Date'}>
                           <Input type="date" value={form.supplyDate} onChange={e => fld('supplyDate', e.target.value)} />
                         </FormField>
                       </div>
                    </>
                  )}
                </>
              )}

              {/* ── Transfer Form ── */}
              {activeTab === 'transfer' && (
                <>
                   <div className="form-section-header">{ar ? 'أمر نقل وقود' : 'Fuel Transfer Order'}</div>
                   <div className="form-row">
                     <FormField label={ar ? 'من خزان *' : 'From Tank *'} required>
                       <select className="form-select" value={form.tank}
                         onChange={e => handleTankSelect(e.target.value)} required>
                         <option value="">— {ar ? 'اختر المصدر' : 'Select Source'} —</option>
                         {tanks.map(t => (
                           <option key={t.id} value={t.id}>
                             {t.name} ({t.fuelName || '—'}) — {t.subStationName || '—'}
                           </option>
                         ))}
                       </select>
                     </FormField>
                     <FormField label={ar ? 'إلى موقع *' : 'To Site *'} required>
                       <select className="form-select" value={form.targetSubStation}
                         onChange={e => setForm(f => ({ ...f, targetSubStation: e.target.value }))} required>
                         <option value="">— {ar ? 'اختر الوجهة' : 'Select Destination'} —</option>
                         {transferDests
                           .filter(s => String(s.id) !== String(form.subStation))
                           .map(s => (
                             <option key={s.id} value={s.id}>
                               {subStationOptionLabel(s, stations)}
                             </option>
                           ))}
                       </select>
                     </FormField>
                   </div>
                   <div className="form-row">
                     <FormField label={ar ? 'رقم أمر الصرف *' : 'Issue Order No. *'} required>
                       <Input value={form.issueOrderNumber}
                         onChange={e => fld('issueOrderNumber', e.target.value)} required />
                     </FormField>
                     <FormField label={ar ? 'تاريخ أمر الصرف *' : 'Issue Order Date *'} required>
                       <Input type="date" value={form.issueOrderDate}
                         onChange={e => fld('issueOrderDate', e.target.value)} required />
                     </FormField>
                   </div>
                   <FormField label={ar ? 'الكمية المنقولة (لتر) *' : 'Transfer Qty (L) *'} required>
                     <Input type="number" min="0.001" step="0.001"
                       value={form.quantityLiters} onChange={e => fld('quantityLiters', e.target.value)} required />
                   </FormField>
                </>
              )}

              {/* ── Loss / Adjustment Form ── */}
              {(activeTab === 'loss' || activeTab === 'adjustment') && (
                <>
                   <div className="form-row">
                     <FormField label={ar ? 'الخزان *' : 'Tank *'} required>
                       <select className="form-select" value={form.tank}
                         onChange={e => handleTankSelect(e.target.value)} required>
                         <option value="">— {ar ? 'اختر الخزان' : 'Select Tank'} —</option>
                         {tanks.map(t => (
                           <option key={t.id} value={t.id}>
                             {t.name} — {t.subStationName || '—'}
                           </option>
                         ))}
                       </select>
                     </FormField>
                     <FormField label={ar ? 'الكمية (لتر) *' : 'Quantity (L) *'} required>
                       <Input type="number" min="0.001" step="0.001"
                         value={form.quantityLiters} onChange={e => fld('quantityLiters', e.target.value)} required />
                     </FormField>
                     <FormField label={ar ? 'تاريخ الحدث *' : 'Date *'} required>
                       <Input type="date"
                         value={form.movementDate?.slice(0, 10) || ''}
                         onChange={e => fld('movementDate', e.target.value)} required />
                     </FormField>
                   </div>
                   <FormField label={ar ? 'السبب / التفاصيل *' : 'Reason *'} required>
                     <textarea className="form-control" value={form.reason}
                       onChange={e => fld('reason', e.target.value)} required rows={2} />
                   </FormField>
                </>
              )}

              {/* ── Notes ── */}
              <FormField label={ar ? 'ملاحظات إضافية' : 'Extra Notes'}>
                <textarea className="form-control" value={form.notes}
                  onChange={e => fld('notes', e.target.value)} rows={2} />
              </FormField>

              <div style={{ display: 'flex', gap: 10, justifyContent: 'flex-end', marginTop: 20 }}>
                <button type="button" className="btn btn-ghost" onClick={() => setShowForm(false)}>
                  {ar ? 'إلغاء' : 'Cancel'}
                </button>
                <button type="submit" className="btn btn-primary" disabled={saving}>
                  {saving ? '...' : (editId
                    ? (ar ? 'تحديث وإعادة إرسال' : 'Update & Resubmit')
                    : (ar ? 'حفظ وإرسال للموافقة' : 'Save & Submit'))}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* ── Reject Modal ── */}
      {showRejectModal && (
        <Modal
          open={!!showRejectModal}
          onClose={() => setShowRejectModal(null)}
          title={ar ? 'سبب الرفض' : 'Rejection Reason'}
          maxWidth={400}
          footer={
            <>
              <Button variant="ghost" onClick={() => setShowRejectModal(null)}>
                {ar ? 'إلغاء' : 'Cancel'}
              </Button>
              <Button variant="danger" onClick={handleReject}>
                {ar ? 'تأكيد الرفض' : 'Confirm Reject'}
              </Button>
            </>
          }
        >
          <textarea className="form-control" rows={3} value={rejectReason}
            onChange={e => setRejectReason(e.target.value)}
            placeholder={ar ? 'لماذا تم رفض هذه الحركة؟' : 'Why reject?'} />
        </Modal>
      )}

      {/* ── Approve Confirmation ── */}
      <ConfirmModal
        open={confirmModal.open}
        title={ar ? 'اعتماد الحركة' : 'Approve Movement'}
        message={ar ? 'هل أنت متأكد من اعتماد هذه الحركة وتحديث المخزون؟' : 'Approve this movement and update stock levels?'}
        confirmText={ar ? 'نعم، اعتمد' : 'Yes, Approve'}
        cancelText={ar ? 'إلغاء' : 'Cancel'}
        onConfirm={confirmApprove}
        onCancel={() => setConfirmModal({ open: false, id: null })}
      />
    </div>
  );
}

export default FuelMovements;
