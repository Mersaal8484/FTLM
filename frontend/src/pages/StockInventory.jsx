import React, { useState, useCallback, useMemo } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { Database, Droplet, AlertTriangle, RefreshCw, ChevronDown, ChevronRight, MapPin, Package } from 'lucide-react';
import { useFuelStockSummary, useOilStockSummary, stockSummaryKeys } from '../api/stockSummaryApi';
import connection from '../socket';
import { useEffect } from 'react';

/* ─────────────────────────────────────────────────────── helpers ── */
const fmt = (v, d = 1) =>
  v != null ? Number(v).toLocaleString('ar-EG-u-nu-latn', { maximumFractionDigits: d }) : '—';

const alertCfg = {
  danger:  { color: 'var(--danger)', bg: 'var(--danger-subtle)', label: { ar: 'خطر',    en: 'Danger'  } },
  warning: { color: 'var(--warning)', bg: 'var(--warning-subtle)', label: { ar: 'تحذير',  en: 'Warning' } },
  normal:  { color: 'var(--success)', bg: 'var(--success-subtle)', label: { ar: 'طبيعي',  en: 'Normal'  } },
};

function AlertBadge({ level, ar }) {
  const cfg = alertCfg[level] || alertCfg.normal;
  return (
    <span style={{
      background: cfg.bg, color: cfg.color,
      padding: '3px 10px', borderRadius: 99,
      fontSize: 11, fontWeight: 700,
    }}>
      {ar ? cfg.label.ar : cfg.label.en}
    </span>
  );
}

/* ─────────────────────────────────────────────── StockInventory ── */
function StockInventory({ lang }) {
  const ar = lang === 'ar';
  const queryClient = useQueryClient();

  /* ── تواريخ الفلتر (افتراضي: الشهر الحالي) ─────────────────── */
  const [dateFrom, setDateFrom] = useState(() => {
    const d = new Date(); d.setDate(1);
    return d.toISOString().slice(0, 10);
  });
  const [dateTo, setDateTo] = useState(() => new Date().toISOString().slice(0, 10));

  const periodLabel = useMemo(() => {
    const from = new Date(dateFrom + 'T00:00:00');
    const to = new Date(dateTo + 'T00:00:00');
    const fromDay = from.getDate();
    const toDay = to.getDate();
    const daysInMonth = new Date(from.getFullYear(), from.getMonth() + 1, 0).getDate();
    if (fromDay === 1 && toDay === 15) return ar ? 'النصف الأول' : 'First Half';
    if (fromDay === 16 && toDay === daysInMonth) return ar ? 'النصف الثاني' : 'Second Half';
    return ar ? 'فترة مخصصة' : 'Custom Range';
  }, [dateFrom, dateTo, ar]);

  /* ── التبويب الفرعي ──────────────────────────────────────────── */
  const [subTab, setSubTab] = useState('fuel');   // 'fuel' | 'oil'

  /* ── حالة فتح/إغلاق الشجرة ──────────────────────────────────── */
  const [openBranches, setOpenBranches] = useState({});
  const [openStations, setOpenStations] = useState({});

  /* ── Queries ─────────────────────────────────────────────────── */
  const fuelQuery = useFuelStockSummary(dateFrom, dateTo);
  const oilQuery  = useOilStockSummary(dateFrom, dateTo);

  const summaryData    = subTab === 'fuel' ? (fuelQuery.data    || []) : (oilQuery.data    || []);
  const summaryLoading = subTab === 'fuel' ?  fuelQuery.isLoading      :  oilQuery.isLoading;

  const refresh = useCallback(() => {
    queryClient.invalidateQueries({ queryKey: stockSummaryKeys.fuel(dateFrom, dateTo) });
    queryClient.invalidateQueries({ queryKey: stockSummaryKeys.oil(dateFrom, dateTo) });
  }, [queryClient, dateFrom, dateTo]);

  /* ── تحديث تلقائي عبر WebSocket ──────────────────────────────── */
  useEffect(() => {
    const events = [
      'FuelMovementCreated', 'FuelMovementUpdated', 'FuelMovementApproved',
      'FuelMovementRejected', 'FuelMovementDeleted',
      'OilTransactionCreated', 'OilTransactionUpdated', 'OilTransactionApproved',
      'OilTransactionRejected', 'OilTransactionDeleted',
      'ReadingCreated', 'ReadingApproved',
    ];
    const handler = () => { refresh(); };
    events.forEach(e => connection.on(e, handler));
    return () => events.forEach(e => connection.off(e, handler));
  }, [refresh]);

  /* ── إحصائيات KPI ────────────────────────────────────────────── */
  const kpi = useMemo(() => {
    const isFuel = subTab === 'fuel';
    const danger  = summaryData.filter(s => s.alertLevel === 'danger').length;
    const warning = summaryData.filter(s => s.alertLevel === 'warning').length;
    const totalCap = isFuel ? summaryData.reduce((a, s) => a + (s.totalCapacity || 0), 0) : 0;
    const totalRemaining = summaryData.reduce((a, s) => a + (s.remainingStock || 0), 0);
    const totalAvailable = summaryData.reduce((a, s) => a + (s.availableStock || 0), 0);

    return { danger, warning, totalCap, totalRemaining, totalAvailable, sites: summaryData.length };
  }, [summaryData, subTab]);

  /* ── تجميع حسب Branch → Station → SubStation ──────────────── */
  const grouped = useMemo(() => {
    const tree = {};
    summaryData.forEach(row => {
      const brKey = row.branchId   || 0;
      const stKey = row.stationId  || 0;
      const ssKey = row.subStationId || 0;
      if (!tree[brKey]) tree[brKey] = { id: brKey, name: row.branchName  || '—', stations: {} };
      if (!tree[brKey].stations[stKey]) tree[brKey].stations[stKey] = { id: stKey, name: row.stationName || '—', rows: {} };
      if (!tree[brKey].stations[stKey].rows[ssKey]) tree[brKey].stations[stKey].rows[ssKey] = [];
      tree[brKey].stations[stKey].rows[ssKey].push(row);
    });
    return Object.values(tree);
  }, [summaryData]);

  /* ── فتح أول عنصر تلقائياً ──────────────────────────────────── */
  useEffect(() => {
    if (grouped.length > 0 && Object.keys(openBranches).length === 0) {
      setOpenBranches({ [grouped[0].id]: true });
    }
  }, [grouped]); // eslint-disable-line react-hooks/exhaustive-deps

  const toggleBranch  = (id) => setOpenBranches(p => ({ ...p, [id]: !p[id] }));
  const toggleStation = (id) => setOpenStations(p => ({ ...p, [id]: !p[id] }));

  /* ── عرض صف واحد من الجدول ──────────────────────────────────── */
  const renderRow = (s, idx) => {
    const isFuel = subTab === 'fuel';
    const lookupName = isFuel ? (s.fuelLookupName || '') : (s.oilLookupName || '');
    const remaining = s.remainingStock || 0;
    const available = s.availableStock || (isFuel ? Math.max(0, remaining - (s.totalDeadStock || 0)) : null);
    const fillPct   = isFuel && s.totalCapacity > 0 ? Math.min(100, (remaining / s.totalCapacity) * 100) : null;
    const alertLevel = s.alertLevel || 'normal';

    return (
      <tr key={idx} style={{ borderBottom: '1px solid var(--border)' }}>
        {/* النوع */}
        <td style={{ textAlign: 'center', fontWeight: 700 }}>{lookupName || '—'}</td>

        {/* السعة + الحد الأدنى + الميت — وقود فقط */}
        {isFuel && <>
          <td style={{ textAlign: 'center', color: 'var(--text-muted)' }}>{fmt(s.totalCapacity, 0)}</td>
          <td style={{ textAlign: 'center', color: 'var(--warning)' }}>{fmt(s.totalMinAlert, 0)}</td>
          <td style={{ textAlign: 'center', color: 'var(--text-muted)' }}>{fmt(s.totalDeadStock, 0)}</td>
        </>}

        {/* الرصيد الافتتاحي */}
        <td style={{ textAlign: 'center' }}>{fmt(s.baseStock, 0)}</td>

        {/* الوارد */}
        <td style={{ textAlign: 'center', color: 'var(--secondary)', fontWeight: 700 }}>
          {fmt(s.inbound, 0)}
        </td>

        {/* نقل مرسل */}
        <td style={{ textAlign: 'center', color: 'var(--danger)' }}>
          {fmt(s.transferOut, 0)}
        </td>

        {/* نقل مستلم */}
        <td style={{ textAlign: 'center', color: 'var(--success)' }}>
          {fmt(s.transferIn, 0)}
        </td>

        {/* الفاقد */}
        <td style={{ textAlign: 'center', color: 'var(--danger)' }}>{fmt(s.loss, 0)}</td>

        {/* المستهلك */}
        <td style={{ textAlign: 'center', color: 'var(--warning)' }}>{fmt(s.consumed, 0)}</td>

        {/* المخزون الفعلي */}
        <td style={{ textAlign: 'center', fontWeight: 800, fontSize: 14 }}>
          <div>{fmt(remaining, 0)}</div>
          {fillPct != null && (
            <div style={{ marginTop: 4, height: 5, background: 'var(--border)', borderRadius: 99, overflow: 'hidden' }}>
              <div style={{
                height: '100%', width: `${fillPct}%`, borderRadius: 99,
                background: alertLevel === 'danger' ? 'var(--danger)' : alertLevel === 'warning' ? 'var(--warning)' : 'var(--success)',
                transition: 'width 0.5s ease',
              }} />
            </div>
          )}
        </td>

        {/* المخزون المتاح */}
        {isFuel && (
          <td style={{ textAlign: 'center', fontWeight: 700, color: 'var(--primary)' }}>
            {fmt(available, 0)}
          </td>
        )}

        {/* التنبيه */}
        <td style={{ textAlign: 'center' }}>
          <AlertBadge level={alertLevel} ar={ar} />
        </td>
      </tr>
    );
  };

  /* ══════════════════════════════════════════════════ RENDER ═══ */
  return (
    <div style={{ animation: 'fadeIn 0.4s ease' }}>

      {/* ── رأس الصفحة ──────────────────────────────────────────── */}
      <div className="page-header" style={{ borderBottom: '1px solid var(--border)', paddingBottom: 20, marginBottom: 30 }}>
        <div className="page-header-row">
          <div>
            <h1 style={{ display: 'flex', alignItems: 'center', gap: 10, fontSize: '1.8rem', letterSpacing: '-0.5px' }}>
              <Database size={24} style={{ color: 'var(--primary)' }} />
              {ar ? 'مخزون الوقود والزيوت' : 'Fuel & Oil Inventory'}
            </h1>
            <p style={{ opacity: 0.8, fontSize: '0.95rem' }}>
              {ar ? 'عرض المخزون الفعلي والمتاح لجميع المواقع' : 'View actual and available stock across all sites'}
            </p>
          </div>

          {/* فلتر التاريخ + تحديث */}
          <div style={{ display: 'flex', gap: 8, alignItems: 'center', flexWrap: 'wrap' }}>
            <span style={{
              background: 'var(--primary-glow)', color: 'var(--primary)',
              padding: '4px 12px', borderRadius: 99, fontSize: 12, fontWeight: 700
            }}>{periodLabel}</span>
            <input type="date" className="form-control" style={{ width: 150, padding: '8px 10px', fontSize: 13 }}
              value={dateFrom} onChange={e => { setDateFrom(e.target.value); setTimeout(refresh, 50); }} />
            <span style={{ color: 'var(--text-muted)' }}>—</span>
            <input type="date" className="form-control" style={{ width: 150, padding: '8px 10px', fontSize: 13 }}
              value={dateTo}   onChange={e => { setDateTo(e.target.value);   setTimeout(refresh, 50); }} />
            <button className="btn btn-ghost" style={{ display: 'flex', alignItems: 'center', gap: 6 }} onClick={refresh}>
              <RefreshCw size={15} />
              {ar ? 'تحديث' : 'Refresh'}
            </button>
          </div>
        </div>
      </div>

      {/* ── KPI Cards ────────────────────────────────────────────── */}
      <div className="kpi-grid" style={{ marginBottom: 28 }}>
        <div className="kpi-card" style={{ '--kpi-color': 'var(--primary)', '--kpi-bg': 'var(--primary-glow)' }}>
          <div className="kpi-icon"><MapPin size={22} /></div>
          <div><div className="kpi-value">{kpi.sites}</div><div className="kpi-label">{ar ? 'عدد المواقع' : 'Locations'}</div></div>
        </div>
        {subTab === 'fuel' && (
          <div className="kpi-card" style={{ '--kpi-color': 'var(--secondary)', '--kpi-bg': 'rgba(99,102,241,0.12)' }}>
            <div className="kpi-icon" style={{ color: 'var(--secondary)' }}><Package size={22} /></div>
            <div><div className="kpi-value">{fmt(kpi.totalCap, 0)}</div><div className="kpi-label">{ar ? 'إجمالي السعة (L)' : 'Total Capacity (L)'}</div></div>
          </div>
        )}
        <div className="kpi-card" style={{ '--kpi-color': 'var(--success)', '--kpi-bg': 'var(--success-subtle)' }}>
          <div className="kpi-icon" style={{ color: 'var(--success)' }}><Database size={22} /></div>
          <div><div className="kpi-value">{fmt(kpi.totalRemaining, 0)}</div><div className="kpi-label">{ar ? 'المخزون الفعلي (L)' : 'Actual Stock (L)'}</div></div>
        </div>
        {subTab === 'fuel' && (
          <div className="kpi-card" style={{ '--kpi-color': 'var(--primary)', '--kpi-bg': 'var(--primary-glow)' }}>
            <div className="kpi-icon"><Droplet size={22} /></div>
            <div><div className="kpi-value">{fmt(kpi.totalAvailable, 0)}</div><div className="kpi-label">{ar ? 'المخزون المتاح (L)' : 'Available Stock (L)'}</div></div>
          </div>
        )}
        <div className="kpi-card" style={{ '--kpi-color': 'var(--danger)', '--kpi-bg': 'var(--danger-glow)' }}>
          <div className="kpi-icon"><AlertTriangle size={22} /></div>
          <div><div className="kpi-value">{kpi.danger}</div><div className="kpi-label">{ar ? 'تنبيهات خطر' : 'Danger Alerts'}</div></div>
        </div>
        <div className="kpi-card" style={{ '--kpi-color': 'var(--warning)', '--kpi-bg': 'var(--warning-glow)' }}>
          <div className="kpi-icon"><AlertTriangle size={22} /></div>
          <div><div className="kpi-value">{kpi.warning}</div><div className="kpi-label">{ar ? 'تنبيهات تحذيرية' : 'Warnings'}</div></div>
        </div>
      </div>

      {/* ── تبويب وقود / زيوت ────────────────────────────────────── */}
      <div className="section-card" style={{ marginBottom: 0 }}>
        <div className="section-card-header" style={{ borderBottom: '1px solid var(--border)', paddingBottom: 16, marginBottom: 20 }}>
          <div style={{ display: 'flex', gap: 8 }}>
            <button
              className={`btn btn-sm ${subTab === 'fuel' ? 'btn-primary' : 'btn-ghost'}`}
              onClick={() => setSubTab('fuel')}
              style={{ display: 'flex', alignItems: 'center', gap: 6 }}
            >
              <Database size={14} /> {ar ? 'مخزون الوقود' : 'Fuel Stock'}
            </button>
            <button
              className={`btn btn-sm ${subTab === 'oil' ? 'btn-primary' : 'btn-ghost'}`}
              onClick={() => setSubTab('oil')}
              style={{ display: 'flex', alignItems: 'center', gap: 6 }}
            >
              <Droplet size={14} /> {ar ? 'مخزون الزيوت' : 'Oil Stock'}
            </button>
          </div>
        </div>

        {/* ── الشجرة الهرمية ────────────────────────────────────── */}
        {summaryLoading ? (
          <div style={{ padding: '60px 0', textAlign: 'center' }}>
            <div className="ft-spinner" style={{ width: 36, height: 36, margin: '0 auto' }} />
          </div>
        ) : summaryData.length === 0 ? (
          <div style={{ padding: '60px 0', textAlign: 'center', color: 'var(--text-muted)' }}>
            <Database size={40} style={{ opacity: 0.3, marginBottom: 12 }} />
            <p>{ar ? 'لا توجد بيانات للفترة المحددة' : 'No data for selected period'}</p>
          </div>
        ) : (
          grouped.map(branch => (
            <div key={branch.id} style={{ marginBottom: 10 }}>

              {/* ── مستوى الفرع ─────────────────────────────────── */}
              <button
                onClick={() => toggleBranch(branch.id)}
                style={{
                  width: '100%', display: 'flex', alignItems: 'center', gap: 10,
                  padding: '12px 16px', background: 'var(--primary)', color: 'var(--text)',
                  border: 'none', borderRadius: 10, cursor: 'pointer',
                  fontWeight: 700, fontSize: 14, textAlign: 'start',
                }}
              >
                {openBranches[branch.id] ? <ChevronDown size={16} /> : <ChevronRight size={16} />}
                <span style={{ flex: 1 }}>{branch.name}</span>
                <span style={{ background: 'rgba(255,255,255,0.2)', borderRadius: 99, padding: '2px 10px', fontSize: 12 }}>
                  {Object.values(branch.stations).reduce((a, st) => a + Object.values(st.rows).reduce((b, r) => b + r.length, 0), 0)} {ar ? 'خزان' : 'tanks'}
                </span>
              </button>

              {openBranches[branch.id] && Object.values(branch.stations).map(station => (
                <div key={station.id} style={{ marginTop: 4, marginInlineStart: 16 }}>

                  {/* ── مستوى المحطة ─────────────────────────── */}
                  <button
                    onClick={() => toggleStation(station.id)}
                    style={{
                      width: '100%', display: 'flex', alignItems: 'center', gap: 10,
                      padding: '10px 14px', background: 'var(--card-2)',
                      border: '1px solid var(--border)', borderRadius: 8,
                      cursor: 'pointer', fontWeight: 600, fontSize: 13,
                      textAlign: 'start', color: 'var(--text)',
                    }}
                  >
                    {openStations[station.id] ? <ChevronDown size={14} style={{ color: 'var(--primary)' }} /> : <ChevronRight size={14} style={{ color: 'var(--primary)' }} />}
                    <span style={{ flex: 1 }}>{ar ? 'محطة: ' : 'Station: '}{station.name}</span>
                    <span style={{ background: 'var(--primary-glow)', color: 'var(--primary)', borderRadius: 99, padding: '2px 8px', fontSize: 11, fontWeight: 700 }}>
                      {Object.values(station.rows).reduce((a, r) => a + r.length, 0)}
                    </span>
                  </button>

                  {openStations[station.id] && Object.entries(station.rows).map(([ssId, rows]) => {
                    const ssName = rows[0]?.subStationName || (ar ? 'موقع غير محدد' : 'Unknown site');
                    const hasAlerts = rows.some(r => r.alertLevel === 'danger' || r.alertLevel === 'warning');
                    const isFuel = subTab === 'fuel';
                    return (
                      <div key={ssId} style={{ marginTop: 4, marginInlineStart: 16, marginBottom: 8 }}>

                        {/* ── اسم الموقع (SubStation) ────────── */}
                        <div style={{
                          display: 'flex', alignItems: 'center', gap: 8,
                          padding: '8px 12px', background: 'var(--card)',
                          border: `1px solid ${hasAlerts ? 'var(--warning)40' : 'var(--border)'}`,
                          borderRadius: 6, marginBottom: 4, fontWeight: 600, fontSize: 12,
                          color: 'var(--text-muted)',
                        }}>
                          <MapPin size={13} style={{ color: hasAlerts ? 'var(--warning)' : 'var(--text-muted)' }} />
                          <span>{ssName}</span>
                          {hasAlerts && (
                            <span style={{ marginInlineStart: 'auto', color: 'var(--warning)', fontSize: 11, fontWeight: 700 }}>
                              ⚠ {ar ? 'تنبيه مخزون' : 'Stock Alert'}
                            </span>
                          )}
                        </div>

                        {/* ── جدول البيانات ──────────────────── */}
                        <div style={{ overflowX: 'auto', marginInlineStart: 8 }}>
                          <table className="data-table" style={{ borderTop: 'none', fontSize: 12, minWidth: isFuel ? 960 : 740 }}>
                            <thead>
                              <tr style={{ background: 'var(--card-2)' }}>
                                <th style={{ textAlign: 'center' }}>{ar ? 'نوع الوقود' : 'Type'}</th>
                                {isFuel && <>
                                  <th style={{ textAlign: 'center' }}>{ar ? 'السعة (L)' : 'Capacity (L)'}</th>
                                  <th style={{ textAlign: 'center', color: 'var(--warning)' }}>{ar ? 'الحد الأدنى (L)' : 'Min Alert (L)'}</th>
                                  <th style={{ textAlign: 'center' }}>{ar ? 'النقطة الميتة (L)' : 'Dead Stock (L)'}</th>
                                </>}
                                <th style={{ textAlign: 'center' }}>{ar ? 'الرصيد الافتتاحي' : 'Base Stock'}</th>
                                <th style={{ textAlign: 'center', color: 'var(--secondary)' }}>{ar ? 'وارد' : 'Inbound'}</th>
                                <th style={{ textAlign: 'center', color: 'var(--danger)' }}>{ar ? 'نقل مرسل' : 'Sent'}</th>
                                {isFuel && <th style={{ textAlign: 'center', color: 'var(--success)' }}>{ar ? 'نقل مستلم' : 'Received'}</th>}
                                <th style={{ textAlign: 'center', color: 'var(--danger)' }}>{ar ? 'فاقد' : 'Loss'}</th>
                                <th style={{ textAlign: 'center', color: 'var(--warning)' }}>{ar ? 'مستهلك' : 'Consumed'}</th>
                                <th style={{ textAlign: 'center', fontWeight: 800 }}>{ar ? 'المخزون الفعلي (L)' : 'Actual Stock (L)'}</th>
                                {isFuel && <th style={{ textAlign: 'center', color: 'var(--primary)' }}>{ar ? 'المخزون المتاح (L)' : 'Available (L)'}</th>}
                                <th style={{ textAlign: 'center' }}>{ar ? 'التنبيه' : 'Alert'}</th>
                              </tr>
                            </thead>
                            <tbody>
                              {rows.map((s, idx) => renderRow(s, idx))}
                            </tbody>
                          </table>
                        </div>
                      </div>
                    );
                  })}
                </div>
              ))}
            </div>
          ))
        )}
      </div>
    </div>
  );
}

export default StockInventory;
