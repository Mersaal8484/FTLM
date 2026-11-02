import React from 'react';

export default function ReportKPICards({ items, lang }) {
  const isAr = lang === 'ar';

  if (!items || items.length === 0) return null;

  return (
    <div className="kpi-cards-row">
      {items.map((kpi, idx) => (
        <div key={idx} className={`kpi-card ${kpi.color || 'primary'}`}>
          <div className="kpi-icon-wrapper">
            {kpi.icon && <kpi.icon size={24} />}
          </div>
          <div className="kpi-content">
            <h4 className="kpi-title">{kpi.title}</h4>
            <div className="kpi-value">{kpi.value}</div>
            {kpi.trend && (
              <div className={`kpi-trend ${kpi.trendDir === 'up' ? 'text-success' : kpi.trendDir === 'down' ? 'text-danger' : 'text-gray-400'}`}>
                {kpi.trendDir === 'up' ? '↑' : kpi.trendDir === 'down' ? '↓' : '–'} {kpi.trend}
              </div>
            )}
          </div>
        </div>
      ))}
    </div>
  );
}
