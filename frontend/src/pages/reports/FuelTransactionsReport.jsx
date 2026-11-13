import { useState, useEffect, useCallback, useRef } from 'react';
import { Fuel, ChevronLeft, ChevronRight } from 'lucide-react';
import client from '../../api/client';
import HierarchicalFilter from '../../components/common/HierarchicalFilter';

const FuelTransactionsReport = ({ lang = 'ar' }) => {
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

      const res = await client.get('/api/reports/fuel-transactions', { params });
      setData(res.data);
    } catch (err) {
      console.error('Failed to fetch fuel transactions', err);
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
        <h1 className="pro-title" style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
          <Fuel size={24} color="#3b82f6" />
          {isAr ? 'تقرير معاملات الوقود' : 'Fuel Transactions Report'}
        </h1>
      </div>

      <HierarchicalFilter onFilterChange={stableSetFilters} lang={lang} />

      {summary && (
        <div className="grid grid-cols-2 md:grid-cols-4" style={{ gap: '1rem', marginBottom: '1.5rem' }}>
          {[
            { label: isAr ? 'إجمالي المعاملات' : 'Total Transactions', value: summary.totalTransactions },
            { label: isAr ? 'الوارد (لتر)' : 'Incoming (L)', value: fmt(summary.totalIncomingLiters), color: '#10b981' },
            { label: isAr ? 'النقل الداخلي (لتر)' : 'Internal Transfer (L)', value: fmt(summary.totalInternalTransferLiters), color: '#3b82f6' },
            { label: isAr ? 'الاستهلاك (لتر)' : 'Consumption (L)', value: fmt(summary.totalConsumptionLiters), color: 'var(--warning)' },
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
                {[isAr ? 'التاريخ' : 'Date', isAr ? 'المحطة الفرعية' : 'Sub Station', isAr ? 'النوع' : 'Type', isAr ? 'الكمية (لتر)' : 'Qty (L)', isAr ? 'النوع' : 'Txn Type', isAr ? 'الحالة' : 'Status'].map((h, i) => (
                  <th key={i} style={{ padding: '0.75rem', textAlign: i <= 1 ? 'start' : 'center', fontWeight: 600, color: 'var(--color-text-secondary, #6b7280)', borderBottom: '1px solid var(--color-border, #e5e7eb)' }}>{h}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {items.length > 0 ? items.map((t, idx) => (
                <tr key={idx} style={{ borderBottom: '1px solid var(--color-border, #f3f4f6)' }}>
                  <td style={{ padding: '0.75rem', color: 'var(--color-text-primary, #111827)' }}>{new Date(t.transactionDate).toLocaleDateString(isAr ? 'ar-EG-u-nu-latn' : 'en-US')}</td>
                  <td style={{ padding: '0.75rem', color: 'var(--color-text-primary, #111827)' }}>{t.subStationName}</td>
                  <td style={{ padding: '0.75rem', textAlign: 'center', color: 'var(--color-text-primary, #111827)' }}>{t.fuelTypeName || '-'}</td>
                  <td style={{ padding: '0.75rem', textAlign: 'center', fontWeight: 600, color: '#3b82f6' }}>{fmt(t.quantityLiters)}</td>
                  <td style={{ padding: '0.75rem', textAlign: 'center', color: 'var(--color-text-primary, #111827)' }}>{t.transactionTypeName || '-'}</td>
                  <td style={{ padding: '0.75rem', textAlign: 'center', color: 'var(--color-text-primary, #111827)' }}>{t.statusName || '-'}</td>
                </tr>
              )) : (
                <tr><td colSpan={6} style={{ padding: '2rem', textAlign: 'center', color: 'var(--color-text-secondary, #6b7280)' }}>{isAr ? 'لا توجد بيانات' : 'No data'}</td></tr>
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

export default FuelTransactionsReport;
