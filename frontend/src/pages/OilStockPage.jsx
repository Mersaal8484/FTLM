import React, { useState, useCallback, useMemo, useEffect } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { Droplet, AlertTriangle, RefreshCw, ChevronDown, ChevronRight, MapPin } from 'lucide-react';
import { useOilStockSummary, stockSummaryKeys } from '../api/stockSummaryApi';
import connection from '../socket';

const fmt = (v, d = 0) =>
  v != null ? Number(v).toLocaleString('en-US', { maximumFractionDigits: d }) : '—';

const alertCfg = {
  danger:  { color: '#ef4444', bg: '#ef444415', label: { ar: 'خطر',   en: 'Danger'  } },
  warning: { color: 'var(--warning)', bg: 'var(--warning)15', label: { ar: 'تحذير', en: 'Warning' } },
  normal:  { color: '#10b981', bg: '#10b98115', label: { ar: 'طبيعي', en: 'Normal'  } },
};

function AlertBadge({ level, ar }) {
  const cfg = alertCfg[level] || alertCfg.normal;
  return (
    <span style={{ background: cfg.bg, color: cfg.color, padding: '3px 10px', borderRadius: 99, fontSize: 11, fontWeight: 700 }}>
      {ar ? cfg.label.ar : cfg.label.en}
    </span>
  );
}

function OilStockPage({ lang }) {
  const ar = lang === 'ar';
  const queryClient = useQueryClient();

  const [monthStr, setMonthStr] = useState(() => new Date().toISOString().slice(0, 7));
  const dateFrom = useMemo(() => `${monthStr}-01`, [monthStr]);
  const dateTo = useMemo(() => {
    const [y, m] = monthStr.split('-');
    const lastDay = new Date(y, m, 0).getDate();
    return `${monthStr}-${lastDay}`;
  }, [monthStr]);

  const [openBranches, setOpenBranches] = useState({});
  const [openStations, setOpenStations] = useState({});

  const oilQuery = useOilStockSummary(dateFrom, dateTo);
  const summaryData = oilQuery.data || [];
  const summaryLoading = oilQuery.isLoading;

  const refresh = useCallback(() => {
    queryClient.invalidateQueries({ queryKey: stockSummaryKeys.oil(dateFrom, dateTo) });
  }, [queryClient, dateFrom, dateTo]);

  useEffect(() => { refresh(); }, [refresh]);

  useEffect(() => {
    const events = ['OilTransactionCreated', 'OilTransactionUpdated', 'OilTransactionApproved', 'OilTransactionRejected'];
    const handler = () => { refresh(); };
    events.forEach(e => connection.on(e, handler));
    return () => events.forEach(e => connection.off(e, handler));
  }, [refresh]);

  const kpi = useMemo(() => {
    const danger  = summaryData.filter(s => s.alertLevel === 'danger').length;
    const warning = summaryData.filter(s => s.alertLevel === 'warning').length;
    const totalRemaining = summaryData.reduce((a, s) => a + (s.remainingStock || 0), 0);
    return { danger, warning, totalRemaining, sites: summaryData.length };
  }, [summaryData]);

  const grouped = useMemo(() => {
    const tree = {};
    summaryData.forEach(row => {
      const brKey = row.branchId || 0;
      const stKey = row.stationId || 0;
      const ssKey = row.subStationId || 0;
      if (!tree[brKey]) tree[brKey] = { id: brKey, name: row.branchName || '—', stations: {} };
      if (!tree[brKey].stations[stKey]) tree[brKey].stations[stKey] = { id: stKey, name: row.stationName || '—', rows: {} };
      if (!tree[brKey].stations[stKey].rows[ssKey]) tree[brKey].stations[stKey].rows[ssKey] = [];
      tree[brKey].stations[stKey].rows[ssKey].push(row);
    });
    return Object.values(tree);
  }, [summaryData]);

  useEffect(() => {
    if (grouped.length > 0 && Object.keys(openBranches).length === 0) {
      setOpenBranches({ [grouped[0].id]: true });
    }
  }, [grouped]); // eslint-disable-line

  const toggleBranch  = id => setOpenBranches(p => ({ ...p, [id]: !p[id] }));
  const toggleStation = id => setOpenStations(p => ({ ...p, [id]: !p[id] }));

  const renderRow = (s, idx) => {
    const lookupName = s.oilLookupName || '';
    const remaining = s.remainingStock || 0;
    const alertLevel = s.alertLevel || 'normal';
    const minStock = s.totalMinAlert || s.minStock || 0;
    const fillPct = minStock > 0 ? Math.min(100, (remaining / (minStock * 3)) * 100) : null;
    const barColor = alertLevel === 'danger' ? '#ef4444' : alertLevel === 'warning' ? 'var(--warning)' : '#10b981';

    return (
      <tr key={idx} style={{ borderBottom: '1px solid var(--border)' }}>
        <td style={{ textAlign: 'center', fontWeight: 700 }}>{lookupName || '—'}</td>
        <td style={{ textAlign: 'center', color: 'var(--warning)' }}>{fmt(minStock, 0)}</td>
        <td style={{ textAlign: 'center' }}>{fmt(s.baseStock, 0)}</td>
        <td style={{ textAlign: 'center', color: 'var(--secondary)', fontWeight: 700 }}>{fmt(s.inbound, 0)}</td>
        <td style={{ textAlign: 'center', color: 'var(--danger)' }}>{fmt(s.transferOut, 0)}</td>
        <td style={{ textAlign: 'center', color: '#10b981' }}>{fmt(s.transferIn, 0)}</td>
        <td style={{ textAlign: 'center', color: 'var(--danger)' }}>{fmt(s.loss, 0)}</td>
        <td style={{ textAlign: 'center', color: 'var(--warning)' }}>{fmt(s.consumed, 0)}</td>
        <td style={{ textAlign: 'center', fontWeight: 800, fontSize: 14 }}>
          <div>{fmt(remaining, 0)}</div>
          {fillPct != null && (
            <div style={{ marginTop: 4, height: 5, background: 'var(--border)', borderRadius: 99, overflow: 'hidden' }}>
              <div style={{ height: '100%', width: `${fillPct}%`, borderRadius: 99, background: barColor, transition: 'width 0.5s ease' }} />
            </div>
          )}
        </td>
        <td style={{ textAlign: 'center' }}><AlertBadge level={alertLevel} ar={ar} /></td>
      </tr>
    );
  };

  return (
    <div style={{ animation: 'fadeIn 0.4s ease' }}>
      <div className="page-header" style={{ borderBottom: '1px solid var(--border)', paddingBottom: 20, marginBottom: 30 }}>
        <div className="page-header-row">
          <div>
            <h1 style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
              <Droplet size={24} style={{ color: 'var(--warning)' }} />
              {ar ? 'مخزون الزيوت' : 'Oil Stock Inventory'}
            </h1>
            <p style={{ opacity: 0.8, fontSize: '0.9rem' }}>
              {ar ? 'عرض مخزون الزيوت الحالي لجميع المواقع' : 'Current oil stock across all locations'}
            </p>
          </div>
          <div style={{ display: 'flex', gap: 8, alignItems: 'center', flexWrap: 'wrap' }}>
            <input type="month" className="form-control" style={{ width: 180 }} value={monthStr}
              onChange={e => { setMonthStr(e.target.value); setTimeout(refresh, 50); }} />
            <button className="btn btn-ghost" style={{ display: 'flex', alignItems: 'center', gap: 6 }} onClick={refresh}>
              <RefreshCw size={15} /> {ar ? 'تحديث' : 'Refresh'}
            </button>
          </div>
        </div>
      </div>

      {/* KPI Cards */}
      <div className="kpi-grid" style={{ marginBottom: 28 }}>
        <div className="kpi-card" style={{ '--kpi-color': 'var(--primary)', '--kpi-bg': 'var(--primary-glow)' }}>
          <div className="kpi-icon"><MapPin size={22} /></div>
          <div><div className="kpi-value">{kpi.sites}</div><div className="kpi-label">{ar ? 'المواقع' : 'Locations'}</div></div>
        </div>
        <div className="kpi-card" style={{ '--kpi-color': 'var(--warning)', '--kpi-bg': 'var(--warning-glow)' }}>
          <div className="kpi-icon"><Droplet size={22} /></div>
          <div><div className="kpi-value">{fmt(kpi.totalRemaining, 0)}</div><div className="kpi-label">{ar ? 'إجمالي المخزون (L)' : 'Total Stock (L)'}</div></div>
        </div>
        <div className="kpi-card" style={{ '--kpi-color': 'var(--danger)', '--kpi-bg': 'var(--danger-glow)' }}>
          <div className="kpi-icon"><AlertTriangle size={22} /></div>
          <div><div className="kpi-value">{kpi.danger}</div><div className="kpi-label">{ar ? 'تنبيهات خطر' : 'Danger Alerts'}</div></div>
        </div>
        <div className="kpi-card" style={{ '--kpi-color': 'var(--warning)', '--kpi-bg': 'var(--warning-glow)' }}>
          <div className="kpi-icon"><AlertTriangle size={22} /></div>
          <div><div className="kpi-value">{kpi.warning}</div><div className="kpi-label">{ar ? 'تحذيرات' : 'Warnings'}</div></div>
        </div>
      </div>

      {/* Tree */}
      <div className="section-card" style={{ marginBottom: 0 }}>
        {summaryLoading ? (
          <div style={{ padding: '60px 0', textAlign: 'center' }}><div className="ft-spinner" style={{ width: 36, height: 36, margin: '0 auto' }} /></div>
        ) : summaryData.length === 0 ? (
          <div style={{ padding: '60px 0', textAlign: 'center', color: 'var(--text-muted)' }}>
            <Droplet size={40} style={{ opacity: 0.3, marginBottom: 12 }} />
            <p>{ar ? 'لا توجد بيانات للفترة المحددة' : 'No data for selected period'}</p>
          </div>
        ) : grouped.map(branch => (
          <div key={branch.id} style={{ marginBottom: 10 }}>
            <button onClick={() => toggleBranch(branch.id)}
              style={{ width: '100%', display: 'flex', alignItems: 'center', gap: 10, padding: '12px 16px', background: 'var(--warning)', color: '#fff', border: 'none', borderRadius: 10, cursor: 'pointer', fontWeight: 700, fontSize: 14, textAlign: 'start' }}>
              {openBranches[branch.id] ? <ChevronDown size={16} /> : <ChevronRight size={16} />}
              <span style={{ flex: 1 }}>{branch.name}</span>
              <span style={{ background: 'rgba(255,255,255,0.2)', borderRadius: 99, padding: '2px 10px', fontSize: 12 }}>
                {Object.values(branch.stations).reduce((a, st) => a + Object.values(st.rows).reduce((b, r) => b + r.length, 0), 0)} {ar ? 'نوع' : 'types'}
              </span>
            </button>

            {openBranches[branch.id] && Object.values(branch.stations).map(station => (
              <div key={station.id} style={{ marginTop: 4, marginInlineStart: 16 }}>
                <button onClick={() => toggleStation(station.id)}
                  style={{ width: '100%', display: 'flex', alignItems: 'center', gap: 10, padding: '10px 14px', background: 'var(--card-2)', border: '1px solid var(--border)', borderRadius: 8, cursor: 'pointer', fontWeight: 600, fontSize: 13, textAlign: 'start', color: 'var(--text)' }}>
                  {openStations[station.id] ? <ChevronDown size={14} style={{ color: 'var(--warning)' }} /> : <ChevronRight size={14} style={{ color: 'var(--warning)' }} />}
                  <span style={{ flex: 1 }}>{ar ? 'محطة: ' : 'Station: '}{station.name}</span>
                  <span style={{ background: 'var(--warning-glow)', color: 'var(--warning)', borderRadius: 99, padding: '2px 8px', fontSize: 11, fontWeight: 700 }}>
                    {Object.values(station.rows).reduce((a, r) => a + r.length, 0)}
                  </span>
                </button>

                {openStations[station.id] && Object.entries(station.rows).map(([ssId, rows]) => {
                  const ssName = rows[0]?.subStationName || (ar ? 'موقع غير محدد' : 'Unknown site');
                  const hasAlerts = rows.some(r => r.alertLevel === 'danger' || r.alertLevel === 'warning');
                  return (
                    <div key={ssId} style={{ marginTop: 4, marginInlineStart: 16, marginBottom: 8 }}>
                      <div style={{ display: 'flex', alignItems: 'center', gap: 8, padding: '8px 12px', background: 'var(--card)', border: `1px solid ${hasAlerts ? 'var(--warning)40' : 'var(--border)'}`, borderRadius: 6, marginBottom: 4, fontWeight: 600, fontSize: 12, color: 'var(--text-muted)' }}>
                        <MapPin size={13} style={{ color: hasAlerts ? 'var(--warning)' : 'var(--text-muted)' }} />
                        <span>{ssName}</span>
                        {hasAlerts && <span style={{ marginInlineStart: 'auto', color: 'var(--warning)', fontSize: 11, fontWeight: 700 }}>⚠ {ar ? 'تنبيه مخزون' : 'Stock Alert'}</span>}
                      </div>
                      <div style={{ overflowX: 'auto', marginInlineStart: 8 }}>
                        <table className="data-table" style={{ borderTop: 'none', fontSize: 12, minWidth: 740 }}>
                          <thead>
                            <tr style={{ background: 'var(--card-2)' }}>
                              <th style={{ textAlign: 'center' }}>{ar ? 'نوع الزيت' : 'Oil Type'}</th>
                              <th style={{ textAlign: 'center', color: 'var(--warning)' }}>{ar ? 'الحد الأدنى (L)' : 'Min Stock (L)'}</th>
                              <th style={{ textAlign: 'center' }}>{ar ? 'الرصيد الافتتاحي' : 'Base Stock'}</th>
                              <th style={{ textAlign: 'center', color: 'var(--secondary)' }}>{ar ? 'وارد' : 'Inbound'}</th>
                              <th style={{ textAlign: 'center', color: 'var(--danger)' }}>{ar ? 'نقل مرسل' : 'Sent'}</th>
                              <th style={{ textAlign: 'center', color: '#10b981' }}>{ar ? 'نقل مستلم' : 'Received'}</th>
                              <th style={{ textAlign: 'center', color: 'var(--danger)' }}>{ar ? 'فاقد' : 'Loss'}</th>
                              <th style={{ textAlign: 'center', color: 'var(--warning)' }}>{ar ? 'مستهلك' : 'Consumed'}</th>
                              <th style={{ textAlign: 'center', fontWeight: 800 }}>{ar ? 'المخزون الفعلي (L)' : 'Actual Stock (L)'}</th>
                              <th style={{ textAlign: 'center' }}>{ar ? 'التنبيه' : 'Alert'}</th>
                            </tr>
                          </thead>
                          <tbody>{rows.map((s, idx) => renderRow(s, idx))}</tbody>
                        </table>
                      </div>
                    </div>
                  );
                })}
              </div>
            ))}
          </div>
        ))}
      </div>
    </div>
  );
}

export default OilStockPage;
