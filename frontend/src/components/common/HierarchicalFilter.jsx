import { useState, useEffect, useCallback, useRef } from 'react';
import { Filter, ChevronDown, X } from 'lucide-react';
import client from '../../api/client';

const HierarchicalFilter = ({ onFilterChange, lang = 'ar', showDateRange = true }) => {
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

  const isInitialMount = useRef(true);

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
  }, [selectedBranch, selectedStation, selectedSubStation, dateFrom, dateTo]);

  const handleClear = useCallback(() => {
    setSelectedBranch('');
    setSelectedStation('');
    setSelectedSubStation('');
    setDateFrom('');
    setDateTo('');
  }, []);

  const hasActiveFilters = selectedBranch || selectedStation || selectedSubStation || dateFrom || dateTo;

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
              style={{ width: '100%', paddingRight: '2rem' }}
            >
              <option value="">{isAr ? 'الكل' : 'All'}</option>
              {branches.map(b => (
                <option key={b.id} value={b.id}>{b.name}</option>
              ))}
            </select>
            <ChevronDown size={16} style={{ position: 'absolute', left: '0.5rem', top: '50%', transform: 'translateY(-50%)', pointerEvents: 'none', color: 'var(--color-text-secondary, #9ca3af)' }} />
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
              style={{ width: '100%', paddingRight: '2rem' }}
            >
              <option value="">{loadingStations ? (isAr ? 'جاري التحميل...' : 'Loading...') : (isAr ? 'الكل' : 'All')}</option>
              {stations.map(s => (
                <option key={s.id} value={s.id}>{s.name}</option>
              ))}
            </select>
            <ChevronDown size={16} style={{ position: 'absolute', left: '0.5rem', top: '50%', transform: 'translateY(-50%)', pointerEvents: 'none', color: 'var(--color-text-secondary, #9ca3af)' }} />
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
              style={{ width: '100%', paddingRight: '2rem' }}
            >
              <option value="">{loadingSubStations ? (isAr ? 'جاري التحميل...' : 'Loading...') : (isAr ? 'الكل' : 'All')}</option>
              {subStations.map(ss => (
                <option key={ss.id} value={ss.id}>{ss.name}</option>
              ))}
            </select>
            <ChevronDown size={16} style={{ position: 'absolute', left: '0.5rem', top: '50%', transform: 'translateY(-50%)', pointerEvents: 'none', color: 'var(--color-text-secondary, #9ca3af)' }} />
          </div>
        </div>

        {/* Date From */}
        {showDateRange && (
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
        )}

        {/* Date To */}
        {showDateRange && (
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
