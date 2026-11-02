import { useState, useEffect, useCallback } from 'react';
import { Fuel, Droplets, TrendingUp, AlertTriangle, Activity, Building2 } from 'lucide-react';
import client from '../api/client';
import HierarchicalFilter from '../components/common/HierarchicalFilter';

const FuelOilDashboard = ({ lang = 'ar' }) => {
  const dir = lang === 'ar' ? 'rtl' : 'ltr';
  const isAr = lang === 'ar';

  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [filters, setFilters] = useState({ branchId: null, stationId: null, subStationId: null, dateFrom: null, dateTo: null });

  const fetchData = useCallback(async () => {
    setLoading(true);
    try {
      const params = {};
      if (filters.branchId) params.branchId = filters.branchId;
      if (filters.stationId) params.stationId = filters.stationId;
      if (filters.subStationId) params.subStationId = filters.subStationId;
      if (filters.dateFrom) params.dateFrom = filters.dateFrom;
      if (filters.dateTo) params.dateTo = filters.dateTo;

      const res = await client.get('/api/reports/fuel-oil-dashboard', { params });
      setData(res.data);
    } catch (err) {
      console.error('Failed to fetch fuel-oil dashboard', err);
    } finally {
      setLoading(false);
    }
  }, [filters]);

  useEffect(() => { fetchData(); }, [fetchData]);

  const fmt = (n) => {
    if (n == null) return '0';
    return Number(n).toLocaleString(isAr ? 'ar-EG' : 'en-US', { maximumFractionDigits: 1 });
  };

  if (loading) {
    return (
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', minHeight: '400px' }}>
        <div style={{ width: '40px', height: '40px', border: '4px solid #e5e7eb', borderTopColor: '#3b82f6', borderRadius: '50%', animation: 'spin 1s linear infinite' }} />
      </div>
    );
  }

  if (!data) {
    return (
      <div style={{ textAlign: 'center', padding: '2rem', color: 'var(--color-text-secondary, #6b7280)' }}>
        {isAr ? 'لا توجد بيانات' : 'No data available'}
      </div>
    );
  }

  const { summary, consumptions, trends, alerts } = data;

  return (
    <div dir={dir}>
      {/* Header */}
      <div className="pro-header" style={{ marginBottom: '1.5rem' }}>
        <div>
          <h1 className="pro-title">{isAr ? 'لوحة الوقود والزيت' : 'Fuel & Oil Dashboard'}</h1>
          <p style={{ color: 'var(--color-text-secondary, #6b7280)', fontSize: '0.875rem', marginTop: '0.25rem' }}>
            {isAr ? 'مراقبة استهلاك الوقود والزيت عبر المحطات' : 'Monitor fuel and oil consumption across stations'}
          </p>
        </div>
      </div>

      {/* Filter */}
      <HierarchicalFilter onFilterChange={setFilters} lang={lang} />

      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4" style={{ gap: '1rem', marginBottom: '1.5rem' }}>
        {/* Fuel Consumed */}
        <div className="modern-card animate-fade-up" style={{ padding: '1.25rem', display: 'flex', alignItems: 'center', gap: '1rem' }}>
          <div style={{ width: '48px', height: '48px', borderRadius: '12px', background: 'var(--primary)', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
            <Fuel size={24} color="#fff" />
          </div>
          <div>
            <div style={{ fontSize: '1.5rem', fontWeight: 700, color: 'var(--color-text-primary, #111827)' }}>{fmt(summary.totalFuelConsumedLiters)}</div>
            <div style={{ fontSize: '0.8rem', color: 'var(--color-text-secondary, #6b7280)' }}>{isAr ? 'إجمالي استهلاك الوقود (لتر)' : 'Total Fuel Consumed (L)'}</div>
          </div>
        </div>

        {/* Oil Consumed */}
        <div className="modern-card animate-fade-up" style={{ padding: '1.25rem', display: 'flex', alignItems: 'center', gap: '1rem', animationDelay: '0.1s' }}>
          <div style={{ width: '48px', height: '48px', borderRadius: '12px', background: 'var(--warning)', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
            <Droplets size={24} color="#fff" />
          </div>
          <div>
            <div style={{ fontSize: '1.5rem', fontWeight: 700, color: 'var(--color-text-primary, #111827)' }}>{fmt(summary.totalOilConsumedLiters)}</div>
            <div style={{ fontSize: '0.8rem', color: 'var(--color-text-secondary, #6b7280)' }}>{isAr ? 'إجمالي استهلاك الزيت (لتر)' : 'Total Oil Consumed (L)'}</div>
          </div>
        </div>

        {/* Fuel Cost */}
        <div className="modern-card animate-fade-up" style={{ padding: '1.25rem', display: 'flex', alignItems: 'center', gap: '1rem', animationDelay: '0.2s' }}>
          <div style={{ width: '48px', height: '48px', borderRadius: '12px', background: 'var(--success)', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
            <TrendingUp size={24} color="#fff" />
          </div>
          <div>
            <div style={{ fontSize: '1.5rem', fontWeight: 700, color: 'var(--color-text-primary, #111827)' }}>{fmt(summary.fuelCost)}</div>
            <div style={{ fontSize: '0.8rem', color: 'var(--color-text-secondary, #6b7280)' }}>{isAr ? 'تكلفة الوقود' : 'Fuel Cost'}</div>
          </div>
        </div>

        {/* Active Stations */}
        <div className="modern-card animate-fade-up" style={{ padding: '1.25rem', display: 'flex', alignItems: 'center', gap: '1rem', animationDelay: '0.3s' }}>
          <div style={{ width: '48px', height: '48px', borderRadius: '12px', background: 'var(--secondary)', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
            <Building2 size={24} color="#fff" />
          </div>
          <div>
            <div style={{ fontSize: '1.5rem', fontWeight: 700, color: 'var(--color-text-primary, #111827)' }}>{summary.activeStations}</div>
            <div style={{ fontSize: '0.8rem', color: 'var(--color-text-secondary, #6b7280)' }}>{isAr ? 'المحطات النشطة' : 'Active Stations'}</div>
          </div>
        </div>
      </div>

      {/* Alerts */}
      {alerts && alerts.length > 0 && (
        <div className="modern-card animate-fade-in" style={{ padding: '1.25rem', marginBottom: '1.5rem', borderLeft: '4px solid var(--warning)' }}>
          <h3 style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '0.75rem', fontWeight: 600, color: 'var(--color-text-primary, #111827)' }}>
            <AlertTriangle size={20} color="var(--warning)" />
            {isAr ? 'تنبيهات' : 'Alerts'}
            <span style={{ fontSize: '0.75rem', background: 'var(--warning)', color: '#fff', borderRadius: '9999px', padding: '0.125rem 0.5rem' }}>{alerts.length}</span>
          </h3>
          <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
            {alerts.slice(0, 5).map((alert, idx) => (
              <div key={idx} style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', padding: '0.5rem 0.75rem', borderRadius: '0.5rem', background: alert.severity === 'critical' ? '#fef2f2' : '#fffbeb' }}>
                <span style={{
                  fontSize: '0.7rem', fontWeight: 600, padding: '0.125rem 0.5rem', borderRadius: '9999px',
                  background: alert.severity === 'critical' ? '#fecaca' : '#fde68a',
                  color: alert.severity === 'critical' ? '#991b1b' : '#92400e',
                }}>
                  {alert.severity}
                </span>
                <span style={{ fontSize: '0.85rem', color: 'var(--color-text-primary, #111827)', flex: 1 }}>{alert.message}</span>
                {alert.subStationName && (
                  <span style={{ fontSize: '0.75rem', color: 'var(--color-text-secondary, #6b7280)' }}>{alert.subStationName}</span>
                )}
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Two column layout: Consumption table + Trends */}
      <div className="grid grid-cols-1 lg:grid-cols-2" style={{ gap: '1.5rem' }}>
        {/* Consumption Table */}
        <div className="modern-card animate-fade-up" style={{ overflow: 'hidden' }}>
          <div style={{ padding: '1rem 1.25rem', borderBottom: '1px solid var(--color-border, #e5e7eb)', fontWeight: 600, color: 'var(--color-text-primary, #111827)' }}>
            {isAr ? 'تفاصيل الاستهلاك' : 'Consumption Details'}
          </div>
          <div style={{ overflowX: 'auto', maxHeight: '400px', overflowY: 'auto' }}>
            <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '0.85rem' }}>
              <thead style={{ position: 'sticky', top: 0, background: 'var(--color-bg-secondary, #f9fafb)', zIndex: 1 }}>
                <tr>
                  <th style={{ padding: '0.75rem', textAlign: 'start', fontWeight: 600, color: 'var(--color-text-secondary, #6b7280)', borderBottom: '1px solid var(--color-border, #e5e7eb)' }}>{isAr ? 'التاريخ' : 'Date'}</th>
                  <th style={{ padding: '0.75rem', textAlign: 'start', fontWeight: 600, color: 'var(--color-text-secondary, #6b7280)', borderBottom: '1px solid var(--color-border, #e5e7eb)' }}>{isAr ? 'المحطة الفرعية' : 'Sub Station'}</th>
                  <th style={{ padding: '0.75rem', textAlign: 'center', fontWeight: 600, color: 'var(--color-text-secondary, #6b7280)', borderBottom: '1px solid var(--color-border, #e5e7eb)' }}>{isAr ? 'الوقود (لتر)' : 'Fuel (L)'}</th>
                  <th style={{ padding: '0.75rem', textAlign: 'center', fontWeight: 600, color: 'var(--color-text-secondary, #6b7280)', borderBottom: '1px solid var(--color-border, #e5e7eb)' }}>{isAr ? 'الزيت (لتر)' : 'Oil (L)'}</th>
                  <th style={{ padding: '0.75rem', textAlign: 'center', fontWeight: 600, color: 'var(--color-text-secondary, #6b7280)', borderBottom: '1px solid var(--color-border, #e5e7eb)' }}>{isAr ? 'الكفاءة %' : 'Efficiency %'}</th>
                </tr>
              </thead>
              <tbody>
                {consumptions && consumptions.length > 0 ? consumptions.map((c, idx) => (
                  <tr key={idx} style={{ borderBottom: '1px solid var(--color-border, #f3f4f6)' }}>
                    <td style={{ padding: '0.75rem', color: 'var(--color-text-primary, #111827)' }}>{new Date(c.date).toLocaleDateString(isAr ? 'ar-EG' : 'en-US')}</td>
                    <td style={{ padding: '0.75rem', color: 'var(--color-text-primary, #111827)' }}>{c.subStationName}</td>
                    <td style={{ padding: '0.75rem', textAlign: 'center', fontWeight: 600, color: '#3b82f6' }}>{fmt(c.fuelLiters)}</td>
                    <td style={{ padding: '0.75rem', textAlign: 'center', fontWeight: 600, color: 'var(--warning)' }}>{fmt(c.oilLiters)}</td>
                    <td style={{ padding: '0.75rem', textAlign: 'center' }}>
                      <span style={{
                        display: 'inline-block', padding: '0.125rem 0.5rem', borderRadius: '9999px', fontSize: '0.75rem', fontWeight: 600,
                        background: c.efficiencyPercent >= 80 ? '#dcfce7' : c.efficiencyPercent >= 60 ? '#fef3c7' : '#fecaca',
                        color: c.efficiencyPercent >= 80 ? '#166534' : c.efficiencyPercent >= 60 ? '#92400e' : '#991b1b',
                      }}>
                        {fmt(c.efficiencyPercent)}%
                      </span>
                    </td>
                  </tr>
                )) : (
                  <tr>
                    <td colSpan={5} style={{ padding: '2rem', textAlign: 'center', color: 'var(--color-text-secondary, #6b7280)' }}>
                      {isAr ? 'لا توجد بيانات استهلاك' : 'No consumption data'}
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>

        {/* Trends Table */}
        <div className="modern-card animate-fade-up" style={{ overflow: 'hidden', animationDelay: '0.1s' }}>
          <div style={{ padding: '1rem 1.25rem', borderBottom: '1px solid var(--color-border, #e5e7eb)', fontWeight: 600, color: 'var(--color-text-primary, #111827)' }}>
            {isAr ? 'الاتجاهات' : 'Trends'}
          </div>
          <div style={{ overflowX: 'auto', maxHeight: '400px', overflowY: 'auto' }}>
            <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '0.85rem' }}>
              <thead style={{ position: 'sticky', top: 0, background: 'var(--color-bg-secondary, #f9fafb)', zIndex: 1 }}>
                <tr>
                  <th style={{ padding: '0.75rem', textAlign: 'start', fontWeight: 600, color: 'var(--color-text-secondary, #6b7280)', borderBottom: '1px solid var(--color-border, #e5e7eb)' }}>{isAr ? 'التاريخ' : 'Date'}</th>
                  <th style={{ padding: '0.75rem', textAlign: 'center', fontWeight: 600, color: 'var(--color-text-secondary, #6b7280)', borderBottom: '1px solid var(--color-border, #e5e7eb)' }}>{isAr ? 'الوقود (لتر)' : 'Fuel (L)'}</th>
                  <th style={{ padding: '0.75rem', textAlign: 'center', fontWeight: 600, color: 'var(--color-text-secondary, #6b7280)', borderBottom: '1px solid var(--color-border, #e5e7eb)' }}>{isAr ? 'الزيت (لتر)' : 'Oil (L)'}</th>
                  <th style={{ padding: '0.75rem', textAlign: 'center', fontWeight: 600, color: 'var(--color-text-secondary, #6b7280)', borderBottom: '1px solid var(--color-border, #e5e7eb)' }}>{isAr ? 'الطاقة (كيلوواط ساعة)' : 'Energy (kWh)'}</th>
                </tr>
              </thead>
              <tbody>
                {trends && trends.length > 0 ? trends.map((t, idx) => (
                  <tr key={idx} style={{ borderBottom: '1px solid var(--color-border, #f3f4f6)' }}>
                    <td style={{ padding: '0.75rem', color: 'var(--color-text-primary, #111827)' }}>{new Date(t.date).toLocaleDateString(isAr ? 'ar-EG' : 'en-US')}</td>
                    <td style={{ padding: '0.75rem', textAlign: 'center', fontWeight: 600, color: '#3b82f6' }}>{fmt(t.fuelConsumedLiters)}</td>
                    <td style={{ padding: '0.75rem', textAlign: 'center', fontWeight: 600, color: 'var(--warning)' }}>{fmt(t.oilConsumedLiters)}</td>
                    <td style={{ padding: '0.75rem', textAlign: 'center', fontWeight: 600, color: '#10b981' }}>{fmt(t.energyProducedKWh)}</td>
                  </tr>
                )) : (
                  <tr>
                    <td colSpan={4} style={{ padding: '2rem', textAlign: 'center', color: 'var(--color-text-secondary, #6b7280)' }}>
                      {isAr ? 'لا توجد بيانات اتجاهات' : 'No trend data'}
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  );
};

export default FuelOilDashboard;
