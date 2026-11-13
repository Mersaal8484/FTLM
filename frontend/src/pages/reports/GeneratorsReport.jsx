import { useState, useEffect, useCallback, useRef } from 'react';
import { Zap, ChevronLeft, ChevronRight } from 'lucide-react';
import client from '../../api/client';
import HierarchicalFilter from '../../components/common/HierarchicalFilter';

const GeneratorsReport = ({ lang = 'ar' }) => {
  const dir = lang === 'ar' ? 'rtl' : 'ltr';
  const isAr = lang === 'ar';

  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [filters, setFilters] = useState({ branchId: null, stationId: null, subStationId: null, dateFrom: null, dateTo: null });
  const [page, setPage] = useState(1);
  const pageSize = 20;

  const filtersRef = useRef(filters);
  const stableSetFilters = useCallback((updater) => {
    const newFilters = typeof updater === 'function' ? updater(filtersRef.current) : updater;
    const prev = filtersRef.current;
    const changed = Object.keys(newFilters).some(k => newFilters[k] !== prev[k]);
    if (changed) {
      filtersRef.current = newFilters;
      setFilters(newFilters);
    }
  }, []);

  const fetchData = useCallback(async () => {
    setLoading(true);
    try {
      const params = { page, pageSize };
      if (filters.branchId) params.branchId = filters.branchId;
      if (filters.stationId) params.stationId = filters.stationId;
      if (filters.subStationId) params.subStationId = filters.subStationId;
      if (filters.dateFrom) params.dateFrom = filters.dateFrom;
      if (filters.dateTo) params.dateTo = filters.dateTo;

      const res = await client.get('/api/reports/generators', { params });
      setData(res.data);
    } catch (err) {
      console.error('Failed to fetch generators report', err);
    } finally {
      setLoading(false);
    }
  }, [filters, page]);

  useEffect(() => { fetchData(); }, [fetchData]);
  useEffect(() => { setPage(1); }, [filters]);

  const fmt = (n) => n == null ? '0' : Number(n).toLocaleString(isAr ? 'ar-EG-u-nu-latn' : 'en-US', { maximumFractionDigits: 1 });

  if (loading) {
    return (
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', minHeight: '400px' }}>
        <div style={{ width: '40px', height: '40px', border: '4px solid #e5e7eb', borderTopColor: '#3b82f6', borderRadius: '50%', animation: 'spin 1s linear infinite' }} />
      </div>
    );
  }

  const items = data?.items || [];
  const summary = data?.summary;

  return (
    <div dir={dir}>
      <div className="pro-header" style={{ marginBottom: '1.5rem' }}>
        <div>
          <h1 className="pro-title" style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
            <Zap size={24} color="#3b82f6" />
            {isAr ? 'تقرير المولدات' : 'Generators Report'}
          </h1>
        </div>
      </div>

      <HierarchicalFilter onFilterChange={stableSetFilters} lang={lang} showDateRange={true} />

      {/* Summary */}
      {summary && (
        <div className="grid grid-cols-2 md:grid-cols-4" style={{ gap: '1rem', marginBottom: '1.5rem' }}>
          {[
            { label: isAr ? 'إجمالي المولدات' : 'Total Generators', value: summary.totalGenerators },
            { label: isAr ? 'النشطة' : 'Active', value: summary.activeGenerators, color: '#10b981' },
            { label: isAr ? 'الطاقة الكلية (كيلوواط)' : 'Total Power (kW)', value: fmt(summary.totalActualPower) },
            { label: isAr ? 'متوسط الكفاءة' : 'Avg Efficiency', value: `${fmt(summary.averageEfficiencyPercent)}%` },
          ].map((s, i) => (
            <div key={i} className="modern-card animate-fade-up" style={{ padding: '1rem', animationDelay: `${i * 0.05}s` }}>
              <div style={{ fontSize: '0.75rem', color: 'var(--color-text-secondary, #6b7280)', marginBottom: '0.25rem' }}>{s.label}</div>
              <div style={{ fontSize: '1.25rem', fontWeight: 700, color: s.color || 'var(--color-text-primary, #111827)' }}>{s.value}</div>
            </div>
          ))}
        </div>
      )}

      {/* Table */}
      <div className="modern-card animate-fade-in" style={{ overflow: 'hidden' }}>
        <div style={{ overflowX: 'auto' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '0.85rem' }}>
            <thead>
              <tr style={{ background: 'var(--color-bg-secondary, #f9fafb)' }}>
                {[isAr ? 'المولد' : 'Generator', isAr ? 'المحطة الفرعية' : 'Sub Station', isAr ? 'القدرة (كيلوواط)' : 'Power (kW)', isAr ? 'الطاقة (كيلوواط ساعة)' : 'Energy (kWh)', isAr ? 'الوقود (لتر)' : 'Fuel (L)', isAr ? 'الكفاءة %' : 'Eff %', isAr ? 'الحالة' : 'Status'].map((h, i) => (
                  <th key={i} style={{ padding: '0.75rem', textAlign: i === 0 ? 'start' : 'center', fontWeight: 600, color: 'var(--color-text-secondary, #6b7280)', borderBottom: '1px solid var(--color-border, #e5e7eb)' }}>{h}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {items.length > 0 ? items.map((g, idx) => (
                <tr key={idx} style={{ borderBottom: '1px solid var(--color-border, #f3f4f6)' }}>
                  <td style={{ padding: '0.75rem', fontWeight: 500, color: 'var(--color-text-primary, #111827)' }}>{g.name}</td>
                  <td style={{ padding: '0.75rem', textAlign: 'center', color: 'var(--color-text-primary, #111827)' }}>{g.subStationName}</td>
                  <td style={{ padding: '0.75rem', textAlign: 'center', fontWeight: 600, color: '#3b82f6' }}>{fmt(g.actualPowerKW)}</td>
                  <td style={{ padding: '0.75rem', textAlign: 'center', fontWeight: 600, color: '#10b981' }}>{fmt(g.totalEnergyProducedKWh)}</td>
                  <td style={{ padding: '0.75rem', textAlign: 'center', fontWeight: 600, color: 'var(--warning)' }}>{fmt(g.totalFuelConsumedLiters)}</td>
                  <td style={{ padding: '0.75rem', textAlign: 'center' }}>
                    <span style={{
                      display: 'inline-block', padding: '0.125rem 0.5rem', borderRadius: '9999px', fontSize: '0.75rem', fontWeight: 600,
                      background: g.efficiencyPercent >= 80 ? '#dcfce7' : g.efficiencyPercent >= 60 ? '#fef3c7' : '#fecaca',
                      color: g.efficiencyPercent >= 80 ? '#166534' : g.efficiencyPercent >= 60 ? '#92400e' : '#991b1b',
                    }}>{fmt(g.efficiencyPercent)}%</span>
                  </td>
                  <td style={{ padding: '0.75rem', textAlign: 'center', color: 'var(--color-text-primary, #111827)' }}>{g.statusName || '-'}</td>
                </tr>
              )) : (
                <tr><td colSpan={7} style={{ padding: '2rem', textAlign: 'center', color: 'var(--color-text-secondary, #6b7280)' }}>{isAr ? 'لا توجد بيانات' : 'No data'}</td></tr>
              )}
            </tbody>
          </table>
        </div>

        {/* Pagination */}
        {items.length >= pageSize && (
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '0.5rem', padding: '1rem', borderTop: '1px solid var(--color-border, #e5e7eb)' }}>
            <button onClick={() => setPage(p => Math.max(1, p - 1))} disabled={page === 1} style={{ padding: '0.375rem 0.75rem', border: '1px solid var(--color-border, #d1d5db)', borderRadius: '0.375rem', cursor: page === 1 ? 'not-allowed' : 'pointer', opacity: page === 1 ? 0.5 : 1 }}>
              {isAr ? <ChevronRight size={16} /> : <ChevronLeft size={16} />}
            </button>
            <span style={{ fontSize: '0.85rem', color: 'var(--color-text-secondary, #6b7280)' }}>{isAr ? `صفحة ${page}` : `Page ${page}`}</span>
            <button onClick={() => setPage(p => p + 1)} disabled={items.length < pageSize} style={{ padding: '0.375rem 0.75rem', border: '1px solid var(--color-border, #d1d5db)', borderRadius: '0.375rem', cursor: items.length < pageSize ? 'not-allowed' : 'pointer', opacity: items.length < pageSize ? 0.5 : 1 }}>
              {isAr ? <ChevronLeft size={16} /> : <ChevronRight size={16} />}
            </button>
          </div>
        )}
      </div>
    </div>
  );
};

export default GeneratorsReport;
