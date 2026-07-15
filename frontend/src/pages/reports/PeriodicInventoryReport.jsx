import { useState, useEffect, useCallback, useRef } from 'react';
import { Calendar, ChevronLeft, ChevronRight } from 'lucide-react';
import client from '../../api/client';
import HierarchicalFilter from '../../components/common/HierarchicalFilter';

const PeriodicInventoryReport = ({ lang = 'ar' }) => {
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

      const res = await client.get('/api/reports/periodic-inventory', { params });
      setData(res.data);
    } catch (err) {
      console.error('Failed to fetch periodic inventory', err);
    } finally {
      setLoading(false);
    }
  }, [filters, page]);

  useEffect(() => { fetchData(); }, [fetchData]);
  useEffect(() => { setPage(1); }, [filters]);

  const fmt = (n) => n == null ? '0' : Number(n).toLocaleString(isAr ? 'ar-EG' : 'en-US', { maximumFractionDigits: 1 });

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
        <h1 className="pro-title" style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
          <Calendar size={24} color="#10b981" />
          {isAr ? 'تقرير الجرد الدوري' : 'Periodic Inventory Report'}
        </h1>
      </div>

      <HierarchicalFilter onFilterChange={stableSetFilters} lang={lang} />

      {summary && (
        <div className="grid grid-cols-2 md:grid-cols-4" style={{ gap: '1rem', marginBottom: '1.5rem' }}>
          {[
            { label: isAr ? 'إجمالي السجلات' : 'Total Records', value: summary.totalRecords },
            { label: isAr ? 'الموثقة' : 'Verified', value: summary.verifiedCount, color: '#10b981' },
            { label: isAr ? 'الوقود المستلم (لتر)' : 'Fuel Received (L)', value: fmt(summary.totalFuelReceivedLiters), color: '#3b82f6' },
            { label: isAr ? 'الزيت المستلم (لتر)' : 'Oil Received (L)', value: fmt(summary.totalOilReceivedLiters), color: '#f59e0b' },
          ].map((s, i) => (
            <div key={i} className="modern-card animate-fade-up" style={{ padding: '1rem', animationDelay: `${i * 0.05}s` }}>
              <div style={{ fontSize: '0.75rem', color: 'var(--color-text-secondary, #6b7280)', marginBottom: '0.25rem' }}>{s.label}</div>
              <div style={{ fontSize: '1.25rem', fontWeight: 700, color: s.color || 'var(--color-text-primary, #111827)' }}>{s.value}</div>
            </div>
          ))}
        </div>
      )}

      <div className="modern-card animate-fade-in" style={{ overflow: 'hidden' }}>
        <div style={{ overflowX: 'auto' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '0.85rem' }}>
            <thead>
              <tr style={{ background: 'var(--color-bg-secondary, #f9fafb)' }}>
                {[isAr ? 'الفترة' : 'Period', isAr ? 'المحطة الفرعية' : 'Sub Station', isAr ? 'الوقود المستلم' : 'Fuel Recv', isAr ? 'الوقود المستهلك' : 'Fuel Used', isAr ? 'الزيت المستلم' : 'Oil Recv', isAr ? 'الزيت المستهلك' : 'Oil Used', isAr ? 'الحالة' : 'Status'].map((h, i) => (
                  <th key={i} style={{ padding: '0.75rem', textAlign: i <= 1 ? (dir === 'rtl' ? 'right' : 'left') : 'center', fontWeight: 600, color: 'var(--color-text-secondary, #6b7280)', borderBottom: '1px solid var(--color-border, #e5e7eb)' }}>{h}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {items.length > 0 ? items.map((inv, idx) => (
                <tr key={idx} style={{ borderBottom: '1px solid var(--color-border, #f3f4f6)' }}>
                  <td style={{ padding: '0.75rem', color: 'var(--color-text-primary, #111827)' }}>{inv.periodTypeName} {inv.year}/{inv.month}</td>
                  <td style={{ padding: '0.75rem', color: 'var(--color-text-primary, #111827)' }}>{inv.subStationName}</td>
                  <td style={{ padding: '0.75rem', textAlign: 'center', fontWeight: 600, color: '#10b981' }}>{fmt(inv.fuelReceivedLiters)}</td>
                  <td style={{ padding: '0.75rem', textAlign: 'center', fontWeight: 600, color: '#f59e0b' }}>{fmt(inv.fuelConsumedLiters)}</td>
                  <td style={{ padding: '0.75rem', textAlign: 'center', fontWeight: 600, color: '#10b981' }}>{fmt(inv.oilReceivedLiters)}</td>
                  <td style={{ padding: '0.75rem', textAlign: 'center', fontWeight: 600, color: '#f59e0b' }}>{fmt(inv.oilConsumedLiters)}</td>
                  <td style={{ padding: '0.75rem', textAlign: 'center', color: 'var(--color-text-primary, #111827)' }}>{inv.statusName || '-'}</td>
                </tr>
              )) : (
                <tr><td colSpan={7} style={{ padding: '2rem', textAlign: 'center', color: 'var(--color-text-secondary, #6b7280)' }}>{isAr ? 'لا توجد بيانات' : 'No data'}</td></tr>
              )}
            </tbody>
          </table>
        </div>

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

export default PeriodicInventoryReport;
