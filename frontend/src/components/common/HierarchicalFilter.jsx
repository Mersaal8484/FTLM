import { useState, useEffect, useCallback, useRef } from 'react';
import { Filter, ChevronDown, X } from 'lucide-react';
import client from '../../api/client';

const HierarchicalFilter = ({ onFilterChange, lang = 'ar', showDateRange = true, showMonthPicker = false }) => {
  const dir = lang === 'ar' ? 'rtl' : 'ltr';
  const isAr = lang === 'ar';

  const [branches, setBranches] = useState([]);
  const [stations, setStations] = useState([]);
  const [subStations, setSubStations] = useState([]);

  const [selectedBranch, setSelectedBranch] = useState('');
  const [selectedStation, setSelectedStation] = useState('');
  const [selectedSubStation, setSelectedSubStation] = useState('');
  const [dateFrom, setDateFrom] = useState('');
  const [dateTo, setDateTo] = useState('');
  const [selectedMonth, setSelectedMonth] = useState('');
  const [selectedYear, setSelectedYear] = useState('');

  const isInitialMount = useRef(true);

  const getCurrentMonthYear = useCallback(() => {
    const now = new Date();
    return {
      month: String(now.getMonth() + 1).padStart(2, '0'),
      year: String(now.getFullYear())
    };
  }, []);

  useEffect(() => {
    if (showMonthPicker) {
      const { month, year } = getCurrentMonthYear();
      setSelectedMonth(month);
      setSelectedYear(year);
    }
  }, [showMonthPicker, getCurrentMonthYear]);

  const [loadingBranches, setLoadingBranches] = useState(false);
  const [loadingStations, setLoadingStations] = useState(false);
  const [loadingSubStations, setLoadingSubStations] = useState(false);

  // Load branches
  useEffect(() => {
    setLoadingBranches(true);
    client.get('/api/branches')
      .then(res => setBranches(res.data))
      .catch(() => {})
      .finally(() => setLoadingBranches(false));
  }, []);

  // Load stations when branch changes
  useEffect(() => {
    if (!selectedBranch) {
      setStations([]);
      setSubStations([]);
      setSelectedStation('');
      setSelectedSubStation('');
      return;
    }
    setLoadingStations(true);
    client.get(`/api/stations/by-branch/${selectedBranch}`)
      .then(res => setStations(res.data))
      .catch(() => {})
      .finally(() => setLoadingStations(false));
    setSelectedStation('');
    setSelectedSubStation('');
    setSubStations([]);
  }, [selectedBranch]);

  // Load sub-stations when station changes
  useEffect(() => {
    if (!selectedStation) {
      setSubStations([]);
      setSelectedSubStation('');
      return;
    }
    setLoadingSubStations(true);
    client.get(`/api/sub-stations/by-station/${selectedStation}`)
      .then(res => setSubStations((res.data || []).filter(s => s.isActive !== false)))
      .catch(() => {})
      .finally(() => setLoadingSubStations(false));
    setSelectedSubStation('');
  }, [selectedStation]);

  // Compute date range from month/year when month picker is active
  useEffect(() => {
    if (!showMonthPicker || !selectedMonth || !selectedYear) return;
    const year = Number(selectedYear);
    const month = Number(selectedMonth);
    const from = new Date(year, month - 1, 1);
    const to = new Date(year, month, 0, 23, 59, 59);
    setDateFrom(from.toISOString().split('T')[0]);
    setDateTo(to.toISOString().split('T')[0]);
  }, [showMonthPicker, selectedMonth, selectedYear]);

  // Emit filter changes (skip initial mount — parent already has matching default state)
  useEffect(() => {
    if (isInitialMount.current) {
      isInitialMount.current = false;
      return;
    }
    onFilterChange({
      branchId: selectedBranch ? Number(selectedBranch) : null,
      stationId: selectedStation ? Number(selectedStation) : null,
      subStationId: selectedSubStation ? Number(selectedSubStation) : null,
      dateFrom: dateFrom || null,
      dateTo: dateTo || null,
    });
  }, [selectedBranch, selectedStation, selectedSubStation, dateFrom, dateTo, selectedMonth, selectedYear, onFilterChange]);

  const handleClear = useCallback(() => {
    setSelectedBranch('');
    setSelectedStation('');
    setSelectedSubStation('');
    setDateFrom('');
    setDateTo('');
    setSelectedMonth('');
    setSelectedYear('');
  }, []);

  const hasActiveFilters = selectedBranch || selectedStation || selectedSubStation || dateFrom || dateTo || selectedMonth || selectedYear;

  return (
    <div className="filter-bar" dir={dir} style={{ marginBottom: '1rem' }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', flexWrap: 'wrap' }}>
        {/* Branch */}
        <div style={{ position: 'relative', minWidth: '180px' }}>
          <label style={{ fontSize: '0.75rem', color: 'var(--color-text-secondary, #6b7280)', marginBottom: '0.25rem', display: 'block' }}>
            {isAr ? 'الفرع' : 'Branch'}
          </label>
          <div style={{ position: 'relative' }}>
            <select
              className="modern-select"
              value={selectedBranch}
              onChange={e => setSelectedBranch(e.target.value)}
              style={{ width: '100%', paddingInlineEnd: '2rem' }}
            >
              <option value="">{isAr ? 'الكل' : 'All'}</option>
              {branches.map(b => (
                <option key={b.id} value={b.id}>{b.name}</option>
              ))}
            </select>
            <ChevronDown size={16} style={{ position: 'absolute', insetInlineStart: '0.5rem', top: '50%', transform: 'translateY(-50%)', pointerEvents: 'none', color: 'var(--color-text-secondary, #9ca3af)' }} />
          </div>
        </div>

        {/* Station */}
        <div style={{ position: 'relative', minWidth: '180px' }}>
          <label style={{ fontSize: '0.75rem', color: 'var(--color-text-secondary, #6b7280)', marginBottom: '0.25rem', display: 'block' }}>
            {isAr ? 'المحطة' : 'Station'}
          </label>
          <div style={{ position: 'relative' }}>
            <select
              className="modern-select"
              value={selectedStation}
              onChange={e => setSelectedStation(e.target.value)}
              disabled={!selectedBranch || loadingStations}
              style={{ width: '100%', paddingInlineEnd: '2rem' }}
            >
              <option value="">{loadingStations ? (isAr ? 'جاري التحميل...' : 'Loading...') : (isAr ? 'الكل' : 'All')}</option>
              {stations.map(s => (
                <option key={s.id} value={s.id}>{s.name}</option>
              ))}
            </select>
            <ChevronDown size={16} style={{ position: 'absolute', insetInlineStart: '0.5rem', top: '50%', transform: 'translateY(-50%)', pointerEvents: 'none', color: 'var(--color-text-secondary, #9ca3af)' }} />
          </div>
        </div>

        {/* Sub Station */}
        <div style={{ position: 'relative', minWidth: '180px' }}>
          <label style={{ fontSize: '0.75rem', color: 'var(--color-text-secondary, #6b7280)', marginBottom: '0.25rem', display: 'block' }}>
            {isAr ? 'المحطة الفرعية' : 'Sub Station'}
          </label>
          <div style={{ position: 'relative' }}>
            <select
              className="modern-select"
              value={selectedSubStation}
              onChange={e => setSelectedSubStation(e.target.value)}
              disabled={!selectedStation || loadingSubStations}
              style={{ width: '100%', paddingInlineEnd: '2rem' }}
            >
              <option value="">{loadingSubStations ? (isAr ? 'جاري التحميل...' : 'Loading...') : (isAr ? 'الكل' : 'All')}</option>
              {subStations.map(ss => (
                <option key={ss.id} value={ss.id}>{ss.name}</option>
              ))}
            </select>
            <ChevronDown size={16} style={{ position: 'absolute', insetInlineStart: '0.5rem', top: '50%', transform: 'translateY(-50%)', pointerEvents: 'none', color: 'var(--color-text-secondary, #9ca3af)' }} />
          </div>
        </div>

        {/* Month/Year Picker or Date Range */}
        {showDateRange && (
          <>
            {showMonthPicker ? (
              <>
                {/* Month */}
                <div style={{ minWidth: '140px' }}>
                  <label style={{ fontSize: '0.75rem', color: 'var(--color-text-secondary, #6b7280)', marginBottom: '0.25rem', display: 'block' }}>
                    {isAr ? 'الشهر' : 'Month'}
                  </label>
                  <div style={{ position: 'relative' }}>
                    <select
                      className="modern-select"
                      value={selectedMonth}
                      onChange={e => setSelectedMonth(e.target.value)}
                      style={{ width: '100%', paddingInlineEnd: '2rem' }}
                    >
                      <option value="">{isAr ? 'الكل' : 'All'}</option>
                      {[
                        { value: '01', label: isAr ? 'يناير' : 'January' },
                        { value: '02', label: isAr ? 'فبراير' : 'February' },
                        { value: '03', label: isAr ? 'مارس' : 'March' },
                        { value: '04', label: isAr ? 'أبريل' : 'April' },
                        { value: '05', label: isAr ? 'مايو' : 'May' },
                        { value: '06', label: isAr ? 'يونيو' : 'June' },
                        { value: '07', label: isAr ? 'يوليو' : 'July' },
                        { value: '08', label: isAr ? 'أغسطس' : 'August' },
                        { value: '09', label: isAr ? 'سبتمبر' : 'September' },
                        { value: '10', label: isAr ? 'أكتوبر' : 'October' },
                        { value: '11', label: isAr ? 'نوفمبر' : 'November' },
                        { value: '12', label: isAr ? 'ديسمبر' : 'December' },
                      ].map(m => (
                        <option key={m.value} value={m.value}>{m.label}</option>
                      ))}
                    </select>
                    <ChevronDown size={16} style={{ position: 'absolute', insetInlineStart: '0.5rem', top: '50%', transform: 'translateY(-50%)', pointerEvents: 'none', color: 'var(--color-text-secondary, #9ca3af)' }} />
                  </div>
                </div>

                {/* Year */}
                <div style={{ minWidth: '110px' }}>
                  <label style={{ fontSize: '0.75rem', color: 'var(--color-text-secondary, #6b7280)', marginBottom: '0.25rem', display: 'block' }}>
                    {isAr ? 'السنة' : 'Year'}
                  </label>
                  <div style={{ position: 'relative' }}>
                    <select
                      className="modern-select"
                      value={selectedYear}
                      onChange={e => setSelectedYear(e.target.value)}
                      style={{ width: '100%', paddingInlineEnd: '2rem' }}
                    >
                      <option value="">{isAr ? 'الكل' : 'All'}</option>
                      {Array.from({ length: 11 }, (_, i) => {
                        const y = new Date().getFullYear() - 5 + i;
                        return <option key={y} value={String(y)}>{y}</option>;
                      })}
                    </select>
                    <ChevronDown size={16} style={{ position: 'absolute', insetInlineStart: '0.5rem', top: '50%', transform: 'translateY(-50%)', pointerEvents: 'none', color: 'var(--color-text-secondary, #9ca3af)' }} />
                  </div>
                </div>
              </>
            ) : (
              <>
                {/* Date From */}
                <div style={{ minWidth: '150px' }}>
                  <label style={{ fontSize: '0.75rem', color: 'var(--color-text-secondary, #6b7280)', marginBottom: '0.25rem', display: 'block' }}>
                    {isAr ? 'من تاريخ' : 'From Date'}
                  </label>
                  <input
                    type="date"
                    className="modern-select"
                    value={dateFrom}
                    onChange={e => setDateFrom(e.target.value)}
                    style={{ width: '100%' }}
                  />
                </div>

                {/* Date To */}
                <div style={{ minWidth: '150px' }}>
                  <label style={{ fontSize: '0.75rem', color: 'var(--color-text-secondary, #6b7280)', marginBottom: '0.25rem', display: 'block' }}>
                    {isAr ? 'إلى تاريخ' : 'To Date'}
                  </label>
                  <input
                    type="date"
                    className="modern-select"
                    value={dateTo}
                    onChange={e => setDateTo(e.target.value)}
                    style={{ width: '100%' }}
                  />
                </div>
              </>
            )}
          </>
        )}

        {/* Clear */}
        {hasActiveFilters && (
          <button
            onClick={handleClear}
            style={{
              display: 'flex', alignItems: 'center', gap: '0.25rem',
              padding: '0.5rem 0.75rem', marginTop: '1rem',
              background: 'var(--color-danger, #ef4444)', color: '#fff',
              border: 'none', borderRadius: '0.375rem', cursor: 'pointer',
              fontSize: '0.8rem',
            }}
          >
            <X size={14} />
            {isAr ? 'مسح' : 'Clear'}
          </button>
        )}
      </div>
    </div>
  );
};

export default HierarchicalFilter;
