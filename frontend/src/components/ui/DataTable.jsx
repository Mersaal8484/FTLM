import React, { useMemo, useState } from 'react';
import PropTypes from 'prop-types';
import { ChevronUp, ChevronDown, Search, ChevronsLeft, ChevronLeft, ChevronRight, ChevronsRight } from 'lucide-react';

/**
 * FuelTrack — Professional DataTable Component
 *
 * Props:
 *   columns     : Array<{ key, label, sortable?, render?, width?, align? }>
 *   data        : Array<object>
 *   loading     : bool
 *   emptyMessage: string
 *   searchable  : bool   — shows search bar (client-side filter on all string values)
 *   searchPlaceholder: string
 *   pageSize    : number — rows per page (default 20)
 *   onRowClick  : func(row) — optional row click handler
 *   rowKey      : string | func — unique key per row (default 'id')
 *   compact     : bool   — tighter row padding
 *   ar          : bool   — RTL / Arabic mode
 */
function DataTable({
  columns = [],
  data = [],
  loading = false,
  emptyMessage = 'No data',
  searchable = false,
  searchPlaceholder = 'Search...',
  pageSize = 20,
  onRowClick,
  rowKey = 'id',
  compact = false,
  ar = false,
}) {
  const [search, setSearch] = useState('');
  const [sortKey, setSortKey] = useState(null);
  const [sortDir, setSortDir] = useState('asc');
  const [page, setPage] = useState(1);

  // ── Client-side search ──────────────────────────────────────────────────
  const filtered = useMemo(() => {
    if (!search.trim()) return data;
    const q = search.trim().toLowerCase();
    return data.filter((row) =>
      columns.some((col) => {
        const val = row[col.key];
        return val != null && String(val).toLowerCase().includes(q);
      })
    );
  }, [data, search, columns]);

  // ── Sort ────────────────────────────────────────────────────────────────
  const sorted = useMemo(() => {
    if (!sortKey) return filtered;
    return [...filtered].sort((a, b) => {
      const av = a[sortKey];
      const bv = b[sortKey];
      if (av == null && bv == null) return 0;
      if (av == null) return 1;
      if (bv == null) return -1;
      const cmp = typeof av === 'number'
        ? av - bv
        : String(av).localeCompare(String(bv), ar ? 'ar' : 'en');
      return sortDir === 'asc' ? cmp : -cmp;
    });
  }, [filtered, sortKey, sortDir, ar]);

  // ── Pagination ──────────────────────────────────────────────────────────
  const totalPages = Math.max(1, Math.ceil(sorted.length / pageSize));
  const safePage = Math.min(page, totalPages);
  const paged = useMemo(
    () => sorted.slice((safePage - 1) * pageSize, safePage * pageSize),
    [sorted, safePage, pageSize]
  );

  const handleSort = (key) => {
    if (sortKey === key) {
      setSortDir((d) => (d === 'asc' ? 'desc' : 'asc'));
    } else {
      setSortKey(key);
      setSortDir('asc');
    }
    setPage(1);
  };

  const getRowKey = (row, idx) =>
    typeof rowKey === 'function' ? rowKey(row) : (row[rowKey] ?? idx);

  const cellPad = compact ? '8px 12px' : '12px 14px';

  // ── Skeleton loader ─────────────────────────────────────────────────────
  if (loading) {
    return (
      <div className="table-wrapper" style={{ overflowX: 'auto' }}>
        <table className="data-table" style={{ width: '100%', borderCollapse: 'collapse' }}>
          <thead>
            <tr>
              {columns.map((col) => (
                <th key={col.key} style={{ padding: cellPad, textAlign: col.align ?? (ar ? 'right' : 'left'), fontWeight: 600, fontSize: 'var(--text-xs)', color: 'var(--text-muted)', textTransform: 'uppercase', letterSpacing: '0.5px', borderBottom: '1px solid var(--border)', background: 'var(--surface-2)' }}>
                  {col.label}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {Array.from({ length: pageSize > 5 ? 5 : pageSize }).map((_, i) => (
              <tr key={i}>
                {columns.map((col) => (
                  <td key={col.key} style={{ padding: cellPad, borderBottom: '1px solid var(--border-light)' }}>
                    <div className="skeleton" style={{ height: 14, borderRadius: 4, background: 'var(--surface-3)', animation: 'pulse 1.5s ease-in-out infinite', opacity: 0.6 }} />
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
        <style>{`@keyframes pulse { 0%,100%{opacity:.4} 50%{opacity:.8} }`}</style>
      </div>
    );
  }

  return (
    <div>
      {/* Search bar */}
      {searchable && (
        <div style={{ marginBottom: 12, position: 'relative', maxWidth: 320 }}>
          <Search size={14} style={{ position: 'absolute', insetInlineStart: 10, top: '50%', transform: 'translateY(-50%)', color: 'var(--text-muted)', pointerEvents: 'none' }} />
          <input
            type="text"
            className="form-control"
            placeholder={searchPlaceholder}
            value={search}
            onChange={(e) => { setSearch(e.target.value); setPage(1); }}
            style={{ paddingInlineStart: 32, fontSize: 'var(--text-sm)' }}
          />
        </div>
      )}

      {/* Table */}
      <div style={{ overflowX: 'auto', borderRadius: 'var(--radius-sm)', border: '1px solid var(--border)' }}>
        <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 'var(--text-sm)' }}>
          <thead>
            <tr style={{ background: 'var(--surface-2)' }}>
              {columns.map((col) => (
                <th
                  key={col.key}
                  onClick={col.sortable ? () => handleSort(col.key) : undefined}
                  style={{
                    padding: cellPad,
                    textAlign: col.align ?? (ar ? 'right' : 'left'),
                    fontWeight: 600,
                    fontSize: 'var(--text-xs)',
                    color: 'var(--text-muted)',
                    textTransform: 'uppercase',
                    letterSpacing: '0.5px',
                    borderBottom: '1px solid var(--border)',
                    whiteSpace: 'nowrap',
                    cursor: col.sortable ? 'pointer' : 'default',
                    userSelect: 'none',
                    width: col.width,
                  }}
                >
                  <span style={{ display: 'inline-flex', alignItems: 'center', gap: 4 }}>
                    {col.label}
                    {col.sortable && (
                      <span style={{ display: 'inline-flex', flexDirection: 'column', lineHeight: 1 }}>
                        <ChevronUp size={10} style={{ color: sortKey === col.key && sortDir === 'asc' ? 'var(--primary)' : 'var(--text-disabled)' }} />
                        <ChevronDown size={10} style={{ color: sortKey === col.key && sortDir === 'desc' ? 'var(--primary)' : 'var(--text-disabled)', marginTop: -2 }} />
                      </span>
                    )}
                  </span>
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {paged.length === 0 ? (
              <tr>
                <td colSpan={columns.length} style={{ padding: '48px 24px', textAlign: 'center', color: 'var(--text-muted)', fontSize: 'var(--text-base)' }}>
                  {emptyMessage}
                </td>
              </tr>
            ) : (
              paged.map((row, idx) => (
                <tr
                  key={getRowKey(row, idx)}
                  onClick={onRowClick ? () => onRowClick(row) : undefined}
                  style={{
                    background: idx % 2 === 0 ? 'transparent' : 'var(--surface-2)',
                    cursor: onRowClick ? 'pointer' : 'default',
                    transition: 'background var(--transition)',
                  }}
                  onMouseEnter={(e) => { if (onRowClick) e.currentTarget.style.background = 'var(--bg-selected)'; }}
                  onMouseLeave={(e) => { if (onRowClick) e.currentTarget.style.background = idx % 2 === 0 ? 'transparent' : 'var(--surface-2)'; }}
                >
                  {columns.map((col) => (
                    <td key={col.key} style={{ padding: cellPad, borderBottom: '1px solid var(--border-light)', textAlign: col.align ?? (ar ? 'right' : 'left'), color: 'var(--text)', verticalAlign: 'middle' }}>
                      {col.render ? col.render(row[col.key], row) : (row[col.key] ?? '—')}
                    </td>
                  ))}
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {/* Pagination */}
      {totalPages > 1 && (
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginTop: 14, flexWrap: 'wrap', gap: 8 }}>
          <span style={{ fontSize: 'var(--text-xs)', color: 'var(--text-muted)' }}>
            {ar
              ? `${sorted.length} نتيجة — صفحة ${safePage} من ${totalPages}`
              : `${sorted.length} results — Page ${safePage} of ${totalPages}`}
          </span>
          <div style={{ display: 'flex', gap: 4 }}>
            {[
              { icon: ChevronsLeft, action: () => setPage(1), disabled: safePage === 1 },
              { icon: ChevronLeft,  action: () => setPage((p) => Math.max(1, p - 1)), disabled: safePage === 1 },
              { icon: ChevronRight, action: () => setPage((p) => Math.min(totalPages, p + 1)), disabled: safePage === totalPages },
              { icon: ChevronsRight,action: () => setPage(totalPages), disabled: safePage === totalPages },
            ].map(({ icon: Ic, action, disabled }, i) => (
              <button
                key={i}
                onClick={action}
                disabled={disabled}
                className="btn btn-ghost"
                style={{ padding: '4px 8px', height: 30, opacity: disabled ? 0.35 : 1 }}
                type="button"
              >
                <Ic size={14} />
              </button>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

DataTable.propTypes = {
  columns: PropTypes.arrayOf(
    PropTypes.shape({
      key: PropTypes.string.isRequired,
      label: PropTypes.node.isRequired,
      sortable: PropTypes.bool,
      render: PropTypes.func,
      width: PropTypes.oneOfType([PropTypes.string, PropTypes.number]),
      align: PropTypes.string,
    })
  ).isRequired,
  data: PropTypes.array,
  loading: PropTypes.bool,
  emptyMessage: PropTypes.string,
  searchable: PropTypes.bool,
  searchPlaceholder: PropTypes.string,
  pageSize: PropTypes.number,
  onRowClick: PropTypes.func,
  rowKey: PropTypes.oneOfType([PropTypes.string, PropTypes.func]),
  compact: PropTypes.bool,
  ar: PropTypes.bool,
};

export default DataTable;
