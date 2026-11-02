import { useState, useCallback } from 'react';

const getToday = () => {
  const d = new Date();
  return new Date(d.getTime() - d.getTimezoneOffset() * 60000).toISOString().split('T')[0];
};

const getStartOfWeek = () => {
  const d = new Date();
  const day = d.getDay();
  const diff = d.getDate() - day + (day === 0 ? -6 : 1);
  const start = new Date(d.setDate(diff));
  return new Date(start.getTime() - start.getTimezoneOffset() * 60000).toISOString().split('T')[0];
};

const getStartOfMonth = () => {
  const d = new Date();
  const start = new Date(d.getFullYear(), d.getMonth(), 1);
  return new Date(start.getTime() - start.getTimezoneOffset() * 60000).toISOString().split('T')[0];
};

const getStartOfQuarter = () => {
  const d = new Date();
  const quarter = Math.floor(d.getMonth() / 3);
  const start = new Date(d.getFullYear(), quarter * 3, 1);
  return new Date(start.getTime() - start.getTimezoneOffset() * 60000).toISOString().split('T')[0];
};

export const DATE_SHORTCUTS = [
  { key: 'today', ar: 'اليوم', en: 'Today', fn: () => [getToday(), getToday()] },
  { key: 'week', ar: 'هذا الأسبوع', en: 'This Week', fn: () => [getStartOfWeek(), getToday()] },
  { key: 'month', ar: 'هذا الشهر', en: 'This Month', fn: () => [getStartOfMonth(), getToday()] },
  { key: 'quarter', ar: 'هذا الربع', en: 'This Quarter', fn: () => [getStartOfQuarter(), getToday()] },
];

export default function useReportFilters(initialState = {}) {
  const [dateFrom, setDateFrom] = useState(initialState.dateFrom || getToday());
  const [dateTo, setDateTo] = useState(initialState.dateTo || getToday());
  const [branchId, setBranchId] = useState(initialState.branchId || null);
  const [stationId, setStationId] = useState(initialState.stationId || null);
  const [subStationId, setSubStationId] = useState(initialState.subStationId || null);
  const [searchQuery, setSearchQuery] = useState(initialState.searchQuery || '');
  const [itemType, setItemType] = useState(initialState.itemType || '');
  
  const [page, setPage] = useState(initialState.page || 1);

  const applyShortcut = useCallback((shortcutKey) => {
    const shortcut = DATE_SHORTCUTS.find((s) => s.key === shortcutKey);
    if (shortcut) {
      const [from, to] = shortcut.fn();
      setDateFrom(from);
      setDateTo(to);
      setPage(1);
    }
  }, []);

  const resetFilters = useCallback(() => {
    setDateFrom(getToday());
    setDateTo(getToday());
    setBranchId(null);
    setStationId(null);
    setSubStationId(null);
    setSearchQuery('');
    setItemType('');
    setPage(1);
  }, []);

  return {
    dateFrom,
    setDateFrom: (v) => { setDateFrom(v); setPage(1); },
    dateTo,
    setDateTo: (v) => { setDateTo(v); setPage(1); },
    branchId,
    setBranchId: (v) => { setBranchId(v); setStationId(null); setSubStationId(null); setPage(1); },
    stationId,
    setStationId: (v) => { setStationId(v); setSubStationId(null); setPage(1); },
    subStationId,
    setSubStationId: (v) => { setSubStationId(v); setPage(1); },
    searchQuery,
    setSearchQuery: (v) => { setSearchQuery(v); setPage(1); },
    itemType,
    setItemType: (v) => { setItemType(v); setPage(1); },
    page,
    setPage,
    applyShortcut,
    resetFilters,
  };
}
