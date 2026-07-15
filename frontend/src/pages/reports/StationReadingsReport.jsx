import { useState, useEffect, useCallback, useRef } from 'react';
import { Gauge, ChevronLeft, ChevronRight } from 'lucide-react';
import client from '../../api/client';
import HierarchicalFilter from '../../components/common/HierarchicalFilter';

const StationReadingsReport = ({ lang = 'ar' }) => {
  const dir = lang === 'ar' ? 'rtl' : 'ltr';
  const isAr = lang === 'ar';

  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [filters, setFilters] = useState({ branchId: null, stationId: null, subStationId: null, dateFrom: null, dateTo: null });
  const [page, setPage] = useState(1);
  const pageSize = 20;

  const filtersRef = useRef(filters);
  const stableSetFilters = useCallback((newFilters) => {
    const prev = filtersRef.current;
    if (
      prev.branchId === newFilters.branchId &&
      prev.stationId === newFilters.stationId &&
      prev.subStationId === newFilters.subStationId &&
      prev.dateFrom === newFilters.dateFrom &&
      prev.dateTo === newFilters.dateTo
    ) {
      return;
    }
    filtersRef.current = newFilters;
    setFilters(newFilters);
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

      const res = await client.get('/api/reports/station-readings', { params });
      setData(res.data);
    } catch (err) {
      console.error('Failed to fetch station readings', err);
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
          <Gauge size={24} color="#8b5cf6" />
          {isAr ? 'تقرير قراءات المحطات' : 'Station Readings Report'}
        </h1>
      </div>

      <HierarchicalFilter onFilterChange={stableSetFilters} lang={lang} />

      {summary && (
        <div className="grid grid-cols-2 md:grid-cols-4" style={{ gap: '1rem', marginBottom: '1.5rem' }}>
          {[
            { label: isAr ? 'إجمالي القراءات' : 'Total Readings', value: summary.totalReadings },
            { label: isAr ? 'الموثقة' : 'Verified', value: summary.verifiedCount, color: '#10b981' },
            { label: isAr ? 'قيد الانتظار' : 'Pending', value: summary.pendingCount, color: '#f59e0b' },
            { label: isAr ? 'الطاقة المصدرة (كيلوواط ساعة)' : 'Total Exported (kWh)', value: fmt(summary.totalEnergyExportedKWh) },
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
                {[isAr ? 'التاريخ' : 'Date', isAr ? 'المحطة الفرعية' : 'Sub Station', isAr ? 'العداد' : 'Meter', isAr ? 'ال.previous (كيلوواط ساعة)' : 'Prev (kWh)', isAr ? 'الحالي (كيلوواط ساعة)' : 'Current (kWh)', isAr ? 'الفرق (كيلوواط ساعة)' : 'Delta (kWh)', isAr ? 'الحالة' : 'Status'].map((h, i) => (
                  <th key={i} style={{ padding: '0.75rem', textAlign: i <= 1 ? (dir === 'rtl' ? 'right' : 'left') : 'center', fontWeight: 600, color: 'var(--color-text-secondary, #6b7280)', borderBottom: '1px solid var(--color-border, #e5e7eb)' }}>{h}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {items.length > 0 ? items.map((r, idx) => (
                <tr key={idx} style={{ borderBottom: '1px solid var(--color-border, #f3f4f6)' }}>
                  <td style={{ padding: '0.75rem', color: 'var(--color-text-primary, #111827)' }}>{new Date(r.readingDate).toLocaleDateString(isAr ? 'ar-EG' : 'en-US')}</td>
                  <td style={{ padding: '0.75rem', color: 'var(--color-text-primary, #111827)' }}>{r.subStationName}</td>
                  <td style={{ padding: '0.75rem', textAlign: 'center', color: 'var(--color-text-primary, #111827)' }}>{r.meterCode || '-'}</td>
                  <td style={{ padding: '0.75rem', textAlign: 'center', color: 'var(--color-text-primary, #111827)' }}>{fmt(r.previousExportedKWh)}</td>
                  <td style={{ padding: '0.75rem', textAlign: 'center', color: 'var(--color-text-primary, #111827)' }}>{fmt(r.currentExportedKWh)}</td>
                  <td style={{ padding: '0.75rem', textAlign: 'center', fontWeight: 600, color: '#3b82f6' }}>{fmt(r.deltaExportedKWh)}</td>
                  <td style={{ padding: '0.75rem', textAlign: 'center', color: 'var(--color-text-primary, #111827)' }}>{r.statusName || '-'}</td>
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

export default StationReadingsReport;
