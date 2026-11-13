import React from 'react';

export function LoadingSpinner({ size = 32, text = 'جاري التحميل...' }) {
  return (
    <div className="loading-state">
      <div className="ft-spinner" style={{ width: size, height: size }} />
      {text && (
        <p className="loading-state-text">
          {text}
        </p>
      )}
    </div>
  );
}

export function TableLoader({ rows = 5 }) {
  return (
    <div className="table-loader">
      {Array.from({ length: rows }).map((_, i) => (
        <div key={i} className="table-loader-row">
          <div className="skeleton table-loader-cell" style={{ animationDelay: `${i * 0.1}s` }} />
          <div className="skeleton table-loader-cell" style={{ animationDelay: `${i * 0.1 + 0.2}s` }} />
          <div className="skeleton table-loader-cell table-loader-cell-short" style={{ animationDelay: `${i * 0.1 + 0.4}s` }} />
        </div>
      ))}
    </div>
  );
}
