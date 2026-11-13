import React, { useCallback, useMemo, useState } from 'react';
import PropTypes from 'prop-types';
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
import SmartInsightsPanel from '../analytics/insights/SmartInsightsPanel';
import { buildDashboardInsightModel } from '../analytics/insights/insightEngine';
import { buildDashboardForecasts } from '../analytics/predictions/forecastEngine';
import { splitServerInsights } from '../analytics/insights/serverInsightAdapter';
import useDashboardVisibility from '../permissions/useDashboardVisibility';
import useDashboardData from '../hooks/useDashboardData';
import HierarchicalFilter from '../components/common/HierarchicalFilter';
import { PageHeader, Button } from '../components/ui';
import useThemeTokens from '../utils/useThemeTokens';

ChartJS.register(CategoryScale, LinearScale, PointElement, LineElement, Title, Tooltip, Legend, Filler);

const stationColors = [
  'hsl(213, 55%, 50%)',
  'hsl(158, 42%, 40%)',
  'hsl(38, 70%, 48%)',
  'hsl(4, 62%, 52%)',
  'hsl(239, 55%, 50%)',
  'hsl(330, 65%, 50%)',
  'hsl(200, 60%, 48%)',
  'hsl(24, 85%, 50%)'
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

function KpiCard({ icon: Icon, label, value, color, bg }) {
  return (
    <div className="kpi-card" style={{ borderTop: `3px solid ${color}` }}>
      <div className="kpi-icon" style={{ background: bg, color: color }}>
        <Icon size={22} />
      </div>
      <div className="kpi-body">
        <div className="kpi-label">{label}</div>
        <div className="kpi-value">{value}</div>
      </div>
    </div>
  );
}

KpiCard.propTypes = {
  icon: PropTypes.elementType.isRequired,
  label: PropTypes.node.isRequired,
  value: PropTypes.node.isRequired,
  color: PropTypes.string.isRequired,
  bg: PropTypes.string.isRequired,
};

function Dashboard({ lang }) {
  const themeTokens = useThemeTokens();
  const chartOptions = useMemo(() => ({
    responsive: true,
    maintainAspectRatio: false,
    tension: 0.4,
    plugins: {
      legend: {
        display: true,
        position: 'top',
        labels: { color: themeTokens.muted, usePointStyle: true, pointStyle: 'circle', font: { size: 12 } },
      },
      tooltip: {
        backgroundColor: themeTokens.surface,
        titleColor: themeTokens.text,
        bodyColor: themeTokens.muted,
        borderColor: themeTokens.grid,
        borderWidth: 1,
        padding: 14,
        cornerRadius: 12,
      },
    },
    scales: {
      x: { grid: { display: false }, ticks: { color: themeTokens.muted, font: { size: 11 } } },
      y: { grid: { color: themeTokens.grid }, ticks: { color: themeTokens.muted, font: { size: 11 } } },
    },
  }), [themeTokens]);
  const ar = lang === 'ar';

  // ── Dashboard Visibility (role-based) ──────────────────────────────
  const visibility = useDashboardVisibility();

  const [filter, setFilter] = useState({ branchId: null, stationId: null, subStationId: null, dateFrom: null, dateTo: null });
  const [persistedInsightOverrides, setPersistedInsightOverrides] = useState([]);

  // ── Unified Data Hook (TanStack Query + Live Sockets) ──────────────
  const {
    data,
    stats,
    comparisons: comps,
    trends,
    loading,
    error: fetchError,
    refresh: refreshDashboard,
    insights: serverInsights,
  } = useDashboardData(filter);

  const handleInsightFeedback = useCallback(async (insightId, verdict) => {
    try {
      await client.post(`/api/analytics/insights/feedback`, {
        AnalyticsInsightId: insightId,
        IsHelpful: verdict === 'helpful',
        Comment: ''
      });
      setPersistedInsightOverrides((current) => [
        ...current.filter((item) => item.id !== insightId),
        { id: insightId, verdict, createdAt: new Date().toISOString() }
      ]);
    } catch (err) {
      console.warn('Insight feedback failed', err?.message || err);
    }
  }, []);

  const kpis = useMemo(() => [
    { icon: BatteryCharging, label: ar ? 'الطاقة المنتجة (يوم)' : 'Produced (Day)',      value: `${(stats.energyProducedToday || 0).toLocaleString()} kWh`,                                              color: 'var(--primary)', bg: 'var(--primary-subtle)', visibilityKey: 'energy_daily' },
    { icon: Globe,           label: ar ? 'المرسلة للشبكة (يوم)' : 'Sent to Grid (Day)', value: `${(stats.energySentToday  || 0).toLocaleString()} kWh`,                                              color: 'var(--success)', bg: 'var(--success-subtle)', visibilityKey: 'grid_export_daily' },
    { icon: BatteryCharging, label: ar ? 'الطاقة المنتجة (15 يوم)' : 'Produced (15d)',      value: `${(stats.energyProduced15d || 0).toLocaleString()} kWh`,                                              color: 'var(--primary)', bg: 'var(--primary-subtle)', visibilityKey: 'energy_15d' },
    { icon: BatteryCharging, label: ar ? 'الطاقة المنتجة (30 يوم)' : 'Produced (30d)',      value: `${(stats.energyProduced30d || stats.energyProduced15d || 0).toLocaleString()} kWh`,            color: 'var(--info)', bg: 'var(--info-subtle)', visibilityKey: 'energy_30d' },
    { icon: Globe,           label: ar ? 'المرسلة للشبكة (30 يوم)' : 'Sent to Grid (30d)', value: `${(stats.energySent30d  || stats.energySent15d || 0).toLocaleString()} kWh`,                    color: 'var(--success)', bg: 'var(--success-subtle)', visibilityKey: 'grid_export_30d' },
    { icon: Fuel,            label: ar ? 'الوقود المستهلك (يوم)' : 'Fuel Consumed (Day)',  value: `${(stats.fuelConsumedToday || 0).toLocaleString()} L`,                                             color: 'var(--warning)', bg: 'var(--warning-subtle)', visibilityKey: 'fuel_daily' },
    { icon: Fuel,            label: ar ? 'الوقود المستهلك (15 يوم)' : 'Fuel Consumed (15d)',  value: `${(stats.fuelConsumed15d  || 0).toLocaleString()} L`,                                             color: 'var(--warning)', bg: 'var(--warning-subtle)', visibilityKey: 'fuel_15d' },
    { icon: Droplets,        label: ar ? 'الزيت المستهلك (يوم)' : 'Oil Consumed (Day)',   value: `${(stats.oilConsumedToday || 0).toLocaleString()} L`,                                             color: 'var(--danger)', bg: 'var(--danger-subtle)', visibilityKey: 'oil_consumed' },
    { icon: LayoutGrid,      label: ar ? 'المخزون المتبقي وقود' : 'Remaining Fuel',     value: `${(stats.remainingFuel   || 0).toLocaleString()} L`,                                             color: 'var(--secondary)', bg: 'var(--secondary-subtle)', visibilityKey: 'fuel_stock' },
    { icon: Droplets,        label: ar ? 'المخزون المتبقي زيوت' : 'Remaining Oil',      value: `${(stats.remainingOil    || 0).toLocaleString()} L`,                                             color: 'var(--danger)', bg: 'var(--danger-subtle)', visibilityKey: 'oil_stock' },
    { icon: Zap,             label: ar ? 'إجمالي المولدات' : 'Total Generators',    value: stats.totalGenerators || 0,                                                                         color: 'var(--info)', bg: 'var(--info-subtle)', visibilityKey: 'total_generators' },
    { icon: Activity,        label: ar ? 'الكفاءة' : 'Efficiency',           value: Number.isFinite(Number(stats.efficiency)) ? `${Number(stats.efficiency).toFixed(2)}%` : '—',   color: 'var(--primary)', bg: 'var(--primary-subtle)', visibilityKey: 'efficiency' },
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
    const raw = splitServerInsights(serverInsights, lang);
    if (!persistedInsightOverrides.length) return raw;
    return {
      ...raw,
      insights: raw.insights.map((item) => {
        const override = persistedInsightOverrides.find((o) => o.id === item.id);
        return override
          ? { ...item, feedback: [{ verdict: override.verdict, createdAt: override.createdAt }, ...(item.feedback || [])] }
          : item;
      })
    };
  }, [serverInsights, lang, persistedInsightOverrides]);

  const visibleSmartInsights = useMemo(() => (
    [...smartInsights.insights, ...persistedSmart.insights]
  ), [persistedSmart.insights, smartInsights.insights]);

  const visibleSmartForecasts = useMemo(() => (
    [...smartForecasts.forecasts, ...persistedSmart.forecasts]
  ), [persistedSmart.forecasts, smartForecasts.forecasts]);

  // Build Trend Datasets
  const last7Days = useMemo(() => {
    return [...Array(7)].map((_, i) => {
      const d = parseLocalDate(data?.period?.date || filter.dateFrom);
      d.setDate(d.getDate() - (6 - i));
      return formatLocalDate(d);
    });
  }, [data?.period?.date, filter.dateFrom]);

  const buildTrendData = useCallback((trendArray, valueKey) => {
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

      const bgColor = siteColor.startsWith('hsl(')
        ? siteColor.replace('hsl(', 'hsla(').replace(')', ', 0.10)')
        : `${siteColor}15`;

      datasets.push({
        label,
        data: dataPoints,
        borderColor: siteColor,
        backgroundColor: bgColor,
        fill: true,
        pointRadius: 3,
        pointHoverRadius: 6,
        borderWidth: 3,
      });
    });
    return { labels: last7Days.map(d => d.split('-').slice(1).join('/')), datasets };
  }, [last7Days]);

  const energyTrend = useMemo(() => buildTrendData(trends.trendStation, 'sent'), [buildTrendData, trends.trendStation]);
  const producedTrend = useMemo(() => buildTrendData(trends.trendDaily, 'produced'), [buildTrendData, trends.trendDaily]);

  if (!data && loading) return <div className="spinner-wrap"><div className="ft-spinner" /></div>;

  if (!data && fetchError) {
    return (
      <div className="dashboard-page">
        <div className="section-card" style={{ padding: 30, textAlign: 'center' }}>
          <h3 style={{ color: 'var(--danger)', marginBottom: 10 }}>{ar ? 'تعذر عرض لوحة التحكم' : 'Dashboard unavailable'}</h3>
          <p style={{ color: 'var(--text-muted)', marginBottom: 18 }}>{fetchError}</p>
          <Button variant="primary" onClick={refreshDashboard} icon={RefreshCw}>
            {ar ? 'إعادة المحاولة' : 'Retry'}
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div className="dashboard-page">
      <PageHeader
        title={ar ? 'نظام مراقبة الطاقة والوقود الذكي' : 'Smart Energy & Fuel Monitor'}
        subtitle={ar ? 'لوحة تحكم لمؤشرات الأداء اللحظية والتحليلات التنبؤية' : 'Real-time KPIs, predictive analytics & fleet monitoring'}
        actions={
          <div style={{ display: 'flex', alignItems: 'center', gap: 10, flexWrap: 'wrap' }}>
            <HierarchicalFilter onFilterChange={setFilter} lang={lang} showDateRange={true} />
            <Button
              variant="outline"
              size="sm"
              onClick={refreshDashboard}
              loading={loading}
              icon={RefreshCw}
              title={ar ? 'تحديث البيانات' : 'Refresh'}
            >
              {ar ? 'تحديث' : 'Refresh'}
            </Button>
          </div>
        }
      />

      {/* KPI Grid — only show if there are visible cards */}
      {visibleKpis.length > 0 && (
        <div className="dashboard-kpi-grid">
          {visibleKpis.map((k, i) => <KpiCard key={i} {...k} />)}
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

      <div className="dashboard-charts-grid">
        <div className="dashboard-chart-card">
          <div className="chart-header">
            <h3>{ar ? 'منحنى الطاقة المرسلة للشبكة (7 أيام)' : 'Energy Sent Curve (7 Days)'}</h3>
          </div>
          <div className="chart-wrap">
            <Line data={energyTrend} options={chartOptions} />
          </div>
        </div>

        <div className="dashboard-chart-card">
          <div className="chart-header">
            <h3>{ar ? 'منحنى الطاقة المنتجة (7 أيام)' : 'Produced Energy Curve (7 Days)'}</h3>
          </div>
          <div className="chart-wrap">
            <Line data={producedTrend} options={chartOptions} />
          </div>
        </div>
      </div>

      <div className="dashboard-stations-bar">
        <div className="dashboard-chart-card">
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
    </div>
  );
}

Dashboard.propTypes = {
  lang: PropTypes.string,
};

export default Dashboard;
