import React, { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Zap, Fuel, Activity, RefreshCw,
  BatteryCharging, Globe, Droplets, LayoutGrid
} from 'lucide-react';
import { Line } from 'react-chartjs-2';
import {
  Chart as ChartJS, CategoryScale, LinearScale, PointElement,
  LineElement, Title, Tooltip, Legend, Filler
} from 'chart.js';
import client from '../api/client';
import connection from '../socket';
import SmartInsightsPanel from '../analytics/insights/SmartInsightsPanel';
import { buildDashboardInsightModel } from '../analytics/insights/insightEngine';
import { buildDashboardForecasts } from '../analytics/predictions/forecastEngine';
import { splitServerInsights } from '../analytics/insights/serverInsightAdapter';
import useDashboardVisibility from '../permissions/useDashboardVisibility';
import HierarchicalFilter from '../components/common/HierarchicalFilter';

ChartJS.register(CategoryScale, LinearScale, PointElement, LineElement, Title, Tooltip, Legend, Filler);

const stationColors = [
  '#3b82f6', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6', '#ec4899', '#06b6d4', '#f97316'
];

function formatLocalDate(date = new Date()) {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, '0');
  const d = String(date.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

function parseLocalDate(value) {
  const [y, m, d] = String(value || '').split('-').map(Number);
  if (!y || !m || !d) return new Date();
  return new Date(y, m - 1, d);
}

const chartOptions = {
  responsive: true,
  maintainAspectRatio: false,
  tension: 0.4,
  plugins: {
    legend: {
      display: true,
      position: 'top',
      labels: { color: 'hsl(215,20%,70%)', usePointStyle: true, pointStyle: 'circle', font: { size: 11 } }
    },
    tooltip: {
      backgroundColor: 'rgba(15, 23, 42, 0.9)',
      borderColor: 'rgba(255, 255, 255, 0.1)',
      borderWidth: 1,
      padding: 15,
      cornerRadius: 12,
      titleFont: { size: 14, weight: 'bold' },
      bodyFont: { size: 13 }
    },
  },
  scales: {
    x: { grid: { display: false }, ticks: { color: 'hsl(215,20%,55%)', font: { size: 10 } } },
    y: { grid: { color: 'rgba(255,255,255,0.03)' }, ticks: { color: 'hsl(215,20%,55%)', font: { size: 10 } } },
  },
};

function GlowCard({ icon: Icon, label, value, color, bg }) {
  return (
    <div className="glow-card" style={{ '--glow-color': color }}>
      <div className="glow-card-inner">
        <div className="glow-icon" style={{ background: bg, color: color, '--icon-color': color }}>
          <Icon size={24} />
        </div>
        <div className="glow-content">
          <div className="glow-label">{label}</div>
          <div className="glow-value">{value}</div>
        </div>
      </div>
      <div className="glow-bg" style={{ background: color }}></div>
    </div>
  );
}

function Dashboard({ lang }) {
  const ar = lang === 'ar';

  // ── Dashboard Visibility (role-based) ──────────────────────────────
  const visibility = useDashboardVisibility();

  const [filter, setFilter] = useState({ branchId: null, stationId: null, subStationId: null, dateFrom: null, dateTo: null });
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [serverInsights, setServerInsights] = useState([]);
  const [fetchError, setFetchError] = useState('');

  const fetchData = useCallback(async () => {
    setLoading(true);
    setFetchError('');
    try {
      const params = new URLSearchParams();
      if (filter.branchId) params.set('branchId', filter.branchId);
      if (filter.stationId) params.set('stationId', filter.stationId);
      if (filter.subStationId) params.set('subStationId', filter.subStationId);
      if (filter.dateFrom) params.set('dateFrom', filter.dateFrom);
      if (filter.dateTo) params.set('dateTo', filter.dateTo);
      const dashRes = await client.get(`/api/reports/dashboard-stats?${params.toString()}`);
      setData(dashRes.data);
    } catch (err) {
      console.error(err);
      setFetchError(ar ? 'فشل جلب بيانات لوحة التحكم. البيانات لم تُحذف؛ تحقق من اتصال السيرفر ثم حدّث الصفحة.' : 'Failed to load dashboard data. Data was not deleted; check the server connection and refresh.');
    } finally {
      setLoading(false);
    }
  }, [ar, filter]);

  useEffect(() => {
    fetchData();
    const interval = setInterval(fetchData, 60000);
    return () => clearInterval(interval);
  }, [fetchData]);

  const fetchServerInsights = useCallback(async ({ refresh = false } = {}) => {
    try {
      const params = { days: 90 };
      if (filter.subStationId) params.subStationId = filter.subStationId;
      if (refresh) params.refresh = 'true';
      const res = await client.get('/api/analytics/insights', { params });
      setServerInsights(Array.isArray(res.data?.insights) ? res.data.insights : []);
    } catch (err) {
      console.warn('Analytics insights unavailable', err?.message || err);
      setServerInsights([]);
    }
  }, [filter.subStationId]);

  useEffect(() => {
    fetchServerInsights();
  }, [fetchServerInsights]);

  const refreshDashboard = useCallback(() => {
    fetchData();
    fetchServerInsights({ refresh: true });
  }, [fetchData, fetchServerInsights]);

  // Real-time socket listeners — refresh dashboard data when relevant backend events fire
  useEffect(() => {
    const refreshEvents = [
      'ReadingCreated', 'ReadingApproved', 'ReadingRejected',
      'FuelMovementCreated', 'FuelMovementApproved',
      'InventoryCreated', 'InventoryApproved',
      'GeneratorCreated', 'GeneratorDeleted',
      'StationCreated', 'StationUpdated', 'StationDeleted',
      'SubStationCreated', 'SubStationUpdated', 'SubStationDeleted',
      'FuelTankUpdated',
    ];
    const handler = () => refreshDashboard();
    refreshEvents.forEach(evt => connection.on(evt, handler));
    return () => { refreshEvents.forEach(evt => connection.off(evt, handler)); };
  }, [refreshDashboard]);

  const handleInsightFeedback = useCallback(async (insightId, verdict) => {
    try {
      await client.post(`/api/analytics/insights/${insightId}/feedback`, { verdict });
      setServerInsights((current) => current.map((item) => (
        item.id === insightId
          ? { ...item, feedback: [{ verdict, createdAt: new Date().toISOString() }, ...(item.feedback || [])] }
          : item
      )));
    } catch (err) {
      console.warn('Insight feedback failed', err?.message || err);
    }
  }, []);

  const stats = useMemo(() => data?.stats || { energyProducedToday: 0, energySentToday: 0, fuelConsumedToday: 0, oilConsumedToday: 0, remainingFuel: 0, remainingOil: 0, totalGenerators: 0 }, [data]);
  const comps = useMemo(() => data?.comparisons || [], [data]);
  const trends = useMemo(() => data?.trends || { trendDaily: [], trendStation: [] }, [data]);

  const kpis = useMemo(() => [
    { icon: BatteryCharging, label: ar ? 'الطاقة المنتجة (يوم)' : 'Produced (Day)',      value: `${(stats.energyProducedToday || 0).toLocaleString()} kWh`,                                              color: '#3b82f6', bg: '#3b82f615', visibilityKey: 'energy_daily' },
    { icon: Globe,           label: ar ? 'المرسلة للشبكة (يوم)' : 'Sent to Grid (Day)', value: `${(stats.energySentToday  || 0).toLocaleString()} kWh`,                                              color: '#10b981', bg: '#10b98115', visibilityKey: 'grid_export_daily' },
    { icon: BatteryCharging, label: ar ? 'الطاقة المنتجة (15 يوم)' : 'Produced (15d)',      value: `${(stats.energyProduced15d || 0).toLocaleString()} kWh`,                                              color: '#2563eb', bg: '#2563eb15', visibilityKey: 'energy_15d' },
    { icon: BatteryCharging, label: ar ? 'الطاقة المنتجة (30 يوم)' : 'Produced (30d)',      value: `${(stats.energyProduced30d || stats.energyProduced15d || 0).toLocaleString()} kWh`,            color: '#0ea5e9', bg: '#0ea5e915', visibilityKey: 'energy_30d' },
    { icon: Globe,           label: ar ? 'المرسلة للشبكة (30 يوم)' : 'Sent to Grid (30d)', value: `${(stats.energySent30d  || stats.energySent15d || 0).toLocaleString()} kWh`,                    color: '#14b8a6', bg: '#14b8a615', visibilityKey: 'grid_export_30d' },
    { icon: Fuel,            label: ar ? 'الوقود المستهلك (يوم)' : 'Fuel Consumed (Day)',  value: `${(stats.fuelConsumedToday || 0).toLocaleString()} L`,                                             color: '#f59e0b', bg: '#f59e0b15', visibilityKey: 'fuel_daily' },
    { icon: Fuel,            label: ar ? 'الوقود المستهلك (15 يوم)' : 'Fuel Consumed (15d)',  value: `${(stats.fuelConsumed15d  || 0).toLocaleString()} L`,                                             color: '#d97706', bg: '#d9770615', visibilityKey: 'fuel_15d' },
    { icon: Droplets,        label: ar ? 'الزييت المستهلك (يوم)' : 'Oil Consumed (Day)',   value: `${(stats.oilConsumedToday || 0).toLocaleString()} L`,                                             color: '#ef4444', bg: '#ef444415', visibilityKey: 'oil_consumed' },
    { icon: LayoutGrid,      label: ar ? 'المخزون المتبقي وقود' : 'Remaining Fuel',     value: `${(stats.remainingFuel   || 0).toLocaleString()} L`,                                             color: '#8b5cf6', bg: '#8b5cf615', visibilityKey: 'fuel_stock' },
    { icon: Droplets,        label: ar ? 'المخزون المتبقي زيوت' : 'Remaining Oil',      value: `${(stats.remainingOil    || 0).toLocaleString()} L`,                                             color: '#ec4899', bg: '#ec489915', visibilityKey: 'oil_stock' },
    { icon: Zap,             label: ar ? 'إجمالي المولدات' : 'Total Generators',    value: stats.totalGenerators || 0,                                                                         color: '#06b6d4', bg: '#06b6d415', visibilityKey: 'total_generators' },
    { icon: Activity,        label: ar ? 'الكفاءة' : 'Efficiency',           value: Number.isFinite(Number(stats.efficiency)) ? `${Number(stats.efficiency).toFixed(2)}%` : '—',   color: '#6366f1', bg: '#6366f115', visibilityKey: 'efficiency' },
  ], [ar, stats]);

  // Filter KPI cards according to role visibility
  const visibleKpis = useMemo(
    () => kpis.filter(k => visibility.kpi[k.visibilityKey] !== false),
    [kpis, visibility]
  );

  const smartInsights = useMemo(() => {
    return buildDashboardInsightModel({ stats, trends, comparisons: comps, lang });
  }, [stats, trends, comps, lang]);

  const smartForecasts = useMemo(() => {
    return buildDashboardForecasts({ stats, trends, lang });
  }, [stats, trends, lang]);

  const persistedSmart = useMemo(() => {
    return splitServerInsights(serverInsights, lang);
  }, [serverInsights, lang]);

  const visibleSmartInsights = useMemo(() => (
    [...smartInsights.insights, ...persistedSmart.insights]
  ), [persistedSmart.insights, smartInsights.insights]);

  const visibleSmartForecasts = useMemo(() => (
    [...smartForecasts.forecasts, ...persistedSmart.forecasts]
  ), [persistedSmart.forecasts, smartForecasts.forecasts]);

  // Build Trend Datasets
  const last7Days = [...Array(7)].map((_, i) => {
    const d = parseLocalDate(data?.period?.date || filter.dateFrom);
    d.setDate(d.getDate() - (6 - i));
    return formatLocalDate(d);
  });

  const buildTrendData = (trendArray, valueKey) => {
    const datasets = [];
    const validTrend = (trendArray || []).filter(t => t && t.station);
    const siteIds = [...new Set(validTrend.map(t => t.station))];

    siteIds.forEach((sid, idx) => {
      const sample = validTrend.find(t => t.station === sid) || {};
      const label = sample.name || sid;

      const siteColor = stationColors[idx % stationColors.length];
      const dataPoints = last7Days.map(date => {
        const found = validTrend.find(t => t.date === date && t.station === sid);
        return found ? found[valueKey] : 0;
      });

      datasets.push({
        label,
        data: dataPoints,
        borderColor: siteColor,
        backgroundColor: `${siteColor}10`,
        fill: true,
        pointRadius: 3,
        pointHoverRadius: 6,
        borderWidth: 3,
      });
    });
    return { labels: last7Days.map(d => d.split('-').slice(1).join('/')), datasets };
  };

  const energyTrend = buildTrendData(trends.trendStation, 'sent');
  const producedTrend = buildTrendData(trends.trendDaily, 'produced');

  if (!data && loading) return <div className="spinner-wrap"><div className="ft-spinner" /></div>;

  if (!data && fetchError) {
    return (
      <div className="pro-dashboard">
        <div className="glass-card" style={{ padding: 30, textAlign: 'center' }}>
          <h3 style={{ color: 'var(--danger)', marginBottom: 10 }}>{ar ? 'تعذر عرض لوحة التحكم' : 'Dashboard unavailable'}</h3>
          <p style={{ color: 'var(--text-muted)', marginBottom: 18 }}>{fetchError}</p>
          <button className="btn" onClick={fetchData}>
            <RefreshCw size={16} /> {ar ? 'إعادة المحاولة' : 'Retry'}
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="pro-dashboard">
      <div className="pro-header">
        <div className="header-info">
          <h1 className="glowing-text">{ar ? 'نظام مراقبة الطاقة والوقود الذكي' : 'Smart Energy & Fuel Monitor'}</h1>
          <p>{ar ? 'لوحة تحكم لمؤشرات الأداء اللحظية' : 'Extraordinary Dashboard for Real-time KPIs'}</p>
        </div>

        <div className="header-controls">
          <HierarchicalFilter onFilterChange={setFilter} lang={lang} showDateRange={true} />
          <button className="pro-refresh" onClick={refreshDashboard}>
            <RefreshCw size={18} className={loading ? 'spin' : ''} />
          </button>
        </div>
      </div>

      {/* KPI Grid — only show if there are visible cards */}
      {visibleKpis.length > 0 && (
        <div className="pro-kpi-grid">
          {visibleKpis.map((k, i) => <GlowCard key={i} {...k} />)}
        </div>
      )}

      {/* Smart Insights Panel — hidden when role has no permission */}
      {visibility.smart_insights && (
        <SmartInsightsPanel
          lang={lang}
          insights={visibleSmartInsights}
          forecasts={visibleSmartForecasts}
          onFeedback={handleInsightFeedback}
        />
      )}

      <div className="pro-charts-grid">
        <div className="pro-chart-card">
          <div className="chart-header">
            <h3>{ar ? 'منحنى الطاقة المرسلة للشبكة (7 أيام)' : 'Energy Sent Curve (7 Days)'}</h3>
          </div>
          <div className="chart-wrap">
            <Line data={energyTrend} options={chartOptions} />
          </div>
        </div>

        <div className="pro-chart-card">
          <div className="chart-header">
            <h3>{ar ? 'منحنى الطاقة المنتجة (7 أيام)' : 'Produced Energy Curve (7 Days)'}</h3>
          </div>
          <div className="chart-wrap">
            <Line data={producedTrend} options={chartOptions} />
          </div>
        </div>
      </div>

      <div className="pro-stations-bar">
        <div className="pro-chart-card">
          <div className="chart-header">
            <h3>{ar ? 'توزيع القدرة المتاحة حسب الموقع/الفرع' : 'Available Capacity Distribution by Site'}</h3>
          </div>
          <div className="stations-scroll">
            {comps.map((c, i) => {
              const label = c.name || c.id;
              return (
                <div key={i} className="station-stat-row">
                  <div className="station-info">
                    <span className="s-name">{label}</span>
                    <span className="s-val">{c.energySent.toLocaleString()} kWh</span>
                  </div>
                  <div className="s-bar-bg">
                    <div className="s-bar-fill" style={{ width: `${(c.energySent / Math.max(...comps.map(x => x.energySent), 1)) * 100}%`, background: stationColors[i % stationColors.length] }}></div>
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      </div>

      <style>{`
        .pro-dashboard { animation: fadeIn 0.6s ease; padding: 20px; }
        .pro-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 40px; }
        .glowing-text { font-size: 2.2rem; font-weight: 900; background: linear-gradient(90deg, #fff, #3b82f6); -webkit-background-clip: text; -webkit-text-fill-color: transparent; text-shadow: 0 0 30px rgba(59, 130, 246, 0.3); margin-bottom: 5px; }
        .header-info p { color: var(--text-muted); font-size: 1rem; opacity: 0.8; }
        
        .header-controls { display: flex; gap: 15px; }
        .modern-select { display: flex; align-items: center; gap: 10px; background: rgba(255,255,255,0.03); backdrop-filter: blur(10px); padding: 10px 20px; border-radius: 15px; border: 1px solid rgba(255,255,255,0.05); }
        .modern-select select { background: transparent; border: none; color: #fff; font-weight: 600; outline: none; cursor: pointer; }
        .modern-select input { background: transparent; border: none; color: #fff; font-weight: 600; outline: none; cursor: pointer; color-scheme: dark; min-width: 130px; }
        .modern-select select option { background: #fff; color: #000; }
        .pro-refresh { background: rgba(59, 130, 246, 0.1); border: 1px solid rgba(59, 130, 246, 0.2); color: #3b82f6; width: 45px; height: 45px; border-radius: 15px; cursor: pointer; display: flex; align-items: center; justify-content: center; transition: all 0.3s; flex-shrink: 0; }
        .pro-refresh:hover { background: #3b82f6; color: #fff; transform: rotate(180deg); }

        .pro-kpi-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(280px, 1fr)); gap: 25px; margin-bottom: 40px; }
        .glow-card { position: relative; border-radius: 24px; overflow: hidden; transition: all 0.4s cubic-bezier(0.175, 0.885, 0.32, 1.275); cursor: pointer; }
        .glow-card:hover { transform: translateY(-10px) scale(1.02); }
        .glow-card-inner { position: relative; z-index: 2; background: var(--card); backdrop-filter: blur(20px); padding: 25px; border: 1px solid var(--border); border-radius: 24px; display: flex; align-items: center; gap: 20px; height: 100%; }
        .glow-icon { width: 60px; height: 60px; border-radius: 18px; display: flex; align-items: center; justify-content: center; box-shadow: 0 10px 20px rgba(0,0,0,0.2); flex-shrink: 0; }
        .theme-light .glow-icon { background: color-mix(in srgb, var(--icon-color, #3b82f6), transparent 75%) !important; box-shadow: 0 4px 14px rgba(0,0,0,0.10); }
        .glow-label { color: var(--text-muted); font-size: 0.9rem; font-weight: 500; margin-bottom: 5px; }
        .glow-value { font-size: 1.6rem; font-weight: 900; color: var(--text); letter-spacing: -0.5px; }
        .glow-bg { position: absolute; top: -50%; left: -50%; width: 200%; height: 200%; opacity: 0.07; filter: blur(60px); z-index: 1; transition: opacity 0.3s; }
        .glow-card:hover .glow-bg { opacity: 0.18; }
        .theme-light .glow-card-inner { border-color: var(--border); box-shadow: 0 2px 12px rgba(0,0,0,0.08); background: var(--card); }
        .theme-light .glow-bg { opacity: 0.04; }
        .theme-light .glow-card:hover .glow-bg { opacity: 0.09; }

        .smart-insights-panel { margin: -14px 0 30px; background: linear-gradient(160deg, rgba(13,20,38,0.92) 0%, rgba(8,12,26,0.97) 100%); backdrop-filter: blur(20px); border: 1px solid rgba(148,163,184,0.14); border-top: 1px solid rgba(148,163,184,0.22); border-radius: 18px; padding: 13px 15px; box-shadow: inset 0 1px 0 rgba(255,255,255,0.05), 0 20px 50px rgba(0,0,0,0.28); }
        .smart-insights-header { display: flex; align-items: center; justify-content: space-between; gap: 16px; margin-bottom: 11px; padding-bottom: 9px; border-bottom: 1px solid rgba(148,163,184,0.07); }
        .smart-insights-header h2 { font-size: 1.02rem; font-weight: 900; margin: 0 0 2px; color: #f1f5f9; letter-spacing: -0.01em; }
        .smart-insights-header p { margin: 0; color: #6e84a0; font-size: 0.74rem; line-height: 1.4; }
        .smart-status-pill { display: inline-flex; align-items: center; gap: 6px; padding: 5px 10px; border-radius: 999px; font-weight: 900; font-size: 0.70rem; white-space: nowrap; border: 1px solid rgba(255,255,255,0.07); }
        .smart-status-pill.ok { color: #34d399; background: rgba(16,185,129,0.10); box-shadow: 0 0 0 1px rgba(16,185,129,0.07) inset, 0 0 12px rgba(16,185,129,0.08); }
        .smart-status-pill.warning { color: #fbbf24; background: rgba(245,158,11,0.12); box-shadow: 0 0 0 1px rgba(245,158,11,0.08) inset; }
        .smart-insights-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(185px, 1fr)); gap: 7px; }
        .smart-forecast-row { display: grid; grid-template-columns: repeat(auto-fit, minmax(210px, 1fr)); gap: 7px; margin-top: 7px; padding-top: 9px; border-top: 1px solid rgba(148,163,184,0.06); }
        .smart-insight-card { --accent: #60a5fa; --accent-bg: rgba(96,165,250,0.09); --accent-glow: rgba(96,165,250,0.18); position: relative; overflow: hidden; background: rgba(7,12,24,0.50); border: 1px solid rgba(148,163,184,0.09); border-right: 3px solid var(--accent); border-radius: 11px; padding: 9px 11px; transition: transform 0.15s ease, box-shadow 0.15s ease; }
        .app-root:not(.rtl) .smart-insight-card { border-right-width: 1px; border-left: 3px solid var(--accent); }
        .smart-insight-card::before { content: ''; position: absolute; inset: 0; background: linear-gradient(90deg, color-mix(in srgb, var(--accent), transparent 72%) 0%, transparent 55%); pointer-events: none; }
        .app-root:not(.rtl) .smart-insight-card::before { background: linear-gradient(270deg, color-mix(in srgb, var(--accent), transparent 72%) 0%, transparent 55%); }
        .smart-insight-card:hover { transform: translateY(-3px); box-shadow: 0 8px 28px rgba(0,0,0,0.30), 0 0 0 1px color-mix(in srgb, var(--accent), transparent 52%) inset; }
        .smart-insight-card.warning { --accent: #f59e0b; --accent-bg: rgba(245,158,11,0.11); --accent-glow: rgba(245,158,11,0.18); border-color: rgba(245,158,11,0.26); }
        .smart-insight-card.success { --accent: #10b981; --accent-bg: rgba(16,185,129,0.10); --accent-glow: rgba(16,185,129,0.16); border-color: rgba(16,185,129,0.22); }
        .smart-insight-card.accent-fuel { --accent: #f59e0b; --accent-bg: rgba(245,158,11,0.11); --accent-glow: rgba(245,158,11,0.18); }
        .smart-insight-card.accent-energy { --accent: #38bdf8; --accent-bg: rgba(56,189,248,0.10); --accent-glow: rgba(56,189,248,0.20); }
        .smart-insight-card.accent-efficiency { --accent: #22c55e; --accent-bg: rgba(34,197,94,0.10); --accent-glow: rgba(34,197,94,0.17); }
        .smart-insight-card.accent-load, .smart-insight-card.accent-maintenanceRisk { --accent: #a78bfa; --accent-bg: rgba(167,139,250,0.11); --accent-glow: rgba(167,139,250,0.20); }
        .smart-insight-card.accent-oil { --accent: #fb7185; --accent-bg: rgba(251,113,133,0.10); --accent-glow: rgba(251,113,133,0.17); }
        .smart-insight-card.forecast { --accent: #2dd4bf; --accent-bg: rgba(45,212,191,0.09); --accent-glow: rgba(45,212,191,0.17); }
        .smart-insight-card.accent-maintenanceRisk { grid-column: span 2; }
        .smart-insight-top, .smart-insight-card h4, .smart-insight-card p, .smart-feedback-actions { position: relative; z-index: 1; }
        .smart-insight-top { display: flex; align-items: center; justify-content: space-between; gap: 8px; margin-bottom: 7px; }
        .smart-insight-icon { width: 29px; height: 29px; border-radius: 8px; display: flex; align-items: center; justify-content: center; color: var(--accent); background: var(--accent-bg); box-shadow: 0 0 0 1px color-mix(in srgb, var(--accent), transparent 65%) inset, 0 0 10px var(--accent-glow); }
        .smart-insight-badge { display: inline-flex; align-items: center; gap: 4px; color: #dce8f5; background: rgba(8,14,30,0.72); border: 1px solid rgba(148,163,184,0.12); padding: 2px 7px; border-radius: 999px; font-size: 0.69rem; font-weight: 900; max-width: 112px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
        .smart-insight-badge svg { color: var(--accent); flex-shrink: 0; }
        .smart-insight-card h4 { margin: 0 0 4px; font-size: 0.80rem; color: #edf2ff; font-weight: 900; line-height: 1.3; }
        .smart-insight-card p { margin: 0; color: #7a95b5; font-size: 0.73rem; line-height: 1.56; }
        .smart-feedback-actions { display: flex; gap: 6px; margin-top: 8px; }
        .smart-feedback-actions button { border: 1px solid color-mix(in srgb, var(--accent), transparent 65%); background: color-mix(in srgb, var(--accent-bg), transparent 35%); color: color-mix(in srgb, var(--accent), white 50%); border-radius: 7px; padding: 3px 9px; font-size: 0.69rem; font-weight: 900; cursor: pointer; transition: all 0.13s; font-family: inherit; }
        .smart-feedback-actions button:hover { background: var(--accent-bg); color: #fff; box-shadow: 0 0 10px var(--accent-glow); border-color: color-mix(in srgb, var(--accent), transparent 40%); }

        .pro-charts-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 30px; margin-bottom: 30px; }
        .pro-chart-card { background: rgba(15, 23, 42, 0.6); backdrop-filter: blur(20px); border: 1px solid rgba(255,255,255,0.05); border-radius: 30px; padding: 30px; }
        .chart-header h3 { font-size: 1.2rem; font-weight: 700; margin-bottom: 25px; color: #fff; border-right: 4px solid #3b82f6; padding-right: 15px; }
        .app-root.rtl .chart-header h3 { border-right: none; border-left: 4px solid #3b82f6; padding-right: 0; padding-left: 15px; }
        .chart-wrap { height: 350px; }

        .stations-scroll { display: flex; flex-direction: column; gap: 20px; margin-top: 20px; }
        .station-stat-row { display: flex; flex-direction: column; gap: 8px; }
        .station-info { display: flex; justify-content: space-between; font-size: 0.9rem; font-weight: 600; }
        .s-name { color: #fff; }
        .s-val { color: var(--text-muted); }
        .s-bar-bg { height: 6px; background: rgba(255,255,255,0.05); border-radius: 10px; overflow: hidden; }
        .s-bar-fill { height: 100%; border-radius: 10px; transition: width 1s ease-out; }

        @media (max-width: 1200px) {
          .pro-charts-grid { grid-template-columns: 1fr; }
          .chart-wrap { height: 280px; }
        }
        @media (max-width: 768px) {
          .pro-dashboard { padding: 10px; }
          .pro-header { flex-direction: column; align-items: stretch; gap: 12px; margin-bottom: 24px; }
          .glowing-text { font-size: 1.3rem; }
          .header-info p { font-size: 0.85rem; }
          .header-controls { flex-direction: row; }
          .modern-select { flex: 1; }
          .pro-kpi-grid { gap: 14px; }
          .smart-insights-panel { padding: 11px 13px; border-radius: 16px; margin-bottom: 22px; }
          .smart-insights-header { flex-direction: column; gap: 12px; }
          .glow-card-inner { padding: 16px; gap: 14px; }
          .glow-icon { width: 44px; height: 44px; }
          .glow-icon svg { width: 22px; height: 22px; }
          .glow-value { font-size: 1.2rem; }
          .glow-label { font-size: 0.8rem; }
          .pro-chart-card { padding: 18px; border-radius: 20px; }
          .chart-header h3 { font-size: 1rem; }
          .chart-wrap { height: 220px; }
        }
        @media (max-width: 480px) {
          .glowing-text { font-size: 1.1rem; }
          .pro-kpi-grid { grid-template-columns: 1fr 1fr; gap: 10px; }
          .smart-insights-grid, .smart-forecast-row { grid-template-columns: 1fr; }
          .glow-card-inner { padding: 12px; gap: 10px; flex-direction: column; text-align: center; }
          .glow-icon { width: 36px; height: 36px; }
          .glow-icon svg { width: 18px; height: 18px; }
          .glow-value { font-size: 1rem; }
          .glow-label { font-size: 0.7rem; }
          .pro-chart-card { padding: 12px; }
          .chart-wrap { height: 180px; }
          .modern-select { padding: 8px 14px; }
          .modern-select select { font-size: 13px; }
          .pro-refresh { width: 38px; height: 38px; }
        }
      `}</style>
    </div>
  );
}

export default Dashboard;
