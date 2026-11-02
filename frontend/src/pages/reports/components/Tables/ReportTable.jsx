import React from 'react';
import Pagination from '../../../../components/Pagination';

export default function ReportTable({ 
  columns, 
  data, 
  loading, 
  page, 
  totalPages, 
  onPageChange, 
  lang 
}) {
  const isAr = lang === 'ar';

  if (loading) {
    return (
      <div className="report-table-loading">
        <div className="spinner"></div>
      </div>
    );
  }

  if (!data || data.length === 0) {
    return (
      <div className="report-table-empty">
        {isAr ? 'لا توجد بيانات للعرض' : 'No data available'}
      </div>
    );
  }

  return (
    <div className="report-table-container">
      <div className="table-responsive">
        <table className="report-table">
          <thead>
            <tr>
              {columns.map((col, idx) => (
                <th key={idx} className={col.align ? `text-${col.align}` : ''}>
                  {col.header}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {data.map((row, rowIdx) => (
              <tr key={rowIdx}>
                {columns.map((col, colIdx) => (
                  <td key={colIdx} className={col.align ? `text-${col.align}` : ''}>
                    {col.render ? col.render(row) : row[col.accessor]}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      
      {totalPages > 1 && (
        <div className="report-pagination">
          <Pagination
            page={page}
            pages={totalPages}
            onPageChange={onPageChange}
          />
        </div>
      )}
    </div>
  );
}
