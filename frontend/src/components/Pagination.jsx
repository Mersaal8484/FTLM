import React from 'react';
import { ChevronLeft, ChevronRight, ChevronsLeft, ChevronsRight } from 'lucide-react';

/**
 * Reusable pagination component.
 *
 * Props:
 *   page       – current page (1-based)
 *   totalPages – total number of pages
 *   total      – total record count (optional, shown in info text)
 *   pageSize   – records per page (optional, shown in info text)
 *   onPageChange(page) – callback when page changes
 *   lang       – 'ar' | 'en' (optional, defaults to 'en')
 */
export default function Pagination({ page, totalPages, total, pageSize: _pageSize, onPageChange, lang }) {
  const ar = lang === 'ar';
  if (!totalPages || totalPages <= 1) return null;

  const pages = [];
  const maxVisible = 5;
  let start = Math.max(1, page - Math.floor(maxVisible / 2));
  let end = Math.min(totalPages, start + maxVisible - 1);
  if (end - start < maxVisible - 1) start = Math.max(1, end - maxVisible + 1);

  for (let i = start; i <= end; i++) pages.push(i);

  return (
    <div className="pagination-wrap" style={{
      display: 'flex', alignItems: 'center', justifyContent: 'center',
      gap: 6, padding: '14px 20px', borderTop: '1px solid var(--border)',
      flexWrap: 'wrap',
    }}>
      {/* Info text */}
      <span style={{ fontSize: 13, color: 'var(--text-muted)', marginInlineEnd: 12 }}>
        {ar
          ? `صفحة ${page} من ${totalPages}${total !== null ? ` (${total} سجل)` : ''}`
          : `Page ${page} of ${totalPages}${total !== null ? ` (${total} records)` : ''}`}
      </span>

      {/* First */}
      <button
        className="btn btn-sm btn-ghost"
        disabled={page <= 1}
        onClick={() => onPageChange(1)}
        title={ar ? 'الأولى' : 'First'}
        style={{ padding: '6px 8px' }}
      >
        <ChevronsLeft size={14} />
      </button>

      {/* Prev */}
      <button
        className="btn btn-sm btn-ghost"
        disabled={page <= 1}
        onClick={() => onPageChange(page - 1)}
        title={ar ? 'السابقة' : 'Previous'}
        style={{ padding: '6px 8px' }}
      >
        <ChevronLeft size={14} />
      </button>

      {/* Page number buttons */}
      {start > 1 && (
        <>
          <button className="btn btn-sm btn-ghost" onClick={() => onPageChange(1)} style={{ minWidth: 32 }}>1</button>
          {start > 2 && <span style={{ color: 'var(--text-muted)', padding: '0 2px' }}>…</span>}
        </>
      )}
      {pages.map(p => (
        <button
          key={p}
          className={`btn btn-sm ${p === page ? 'btn-primary' : 'btn-ghost'}`}
          onClick={() => onPageChange(p)}
          style={{ minWidth: 32 }}
        >
          {p}
        </button>
      ))}
      {end < totalPages && (
        <>
          {end < totalPages - 1 && <span style={{ color: 'var(--text-muted)', padding: '0 2px' }}>…</span>}
          <button className="btn btn-sm btn-ghost" onClick={() => onPageChange(totalPages)} style={{ minWidth: 32 }}>{totalPages}</button>
        </>
      )}

      {/* Next */}
      <button
        className="btn btn-sm btn-ghost"
        disabled={page >= totalPages}
        onClick={() => onPageChange(page + 1)}
        title={ar ? 'التالية' : 'Next'}
        style={{ padding: '6px 8px' }}
      >
        <ChevronRight size={14} />
      </button>

      {/* Last */}
      <button
        className="btn btn-sm btn-ghost"
        disabled={page >= totalPages}
        onClick={() => onPageChange(totalPages)}
        title={ar ? 'الأخيرة' : 'Last'}
        style={{ padding: '6px 8px' }}
      >
        <ChevronsRight size={14} />
      </button>
    </div>
  );
}
