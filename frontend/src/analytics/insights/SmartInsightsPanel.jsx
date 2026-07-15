import React from 'react';

export default function SmartInsightsPanel({ lang, insights = [], forecasts = [], onFeedback }) {
  if (!insights.length && !forecasts.length) return null;
  const ar = lang === 'ar';
  return (
    <div style={{ padding: 16, marginBottom: 16 }}>
      <h3 style={{ fontSize: 16, fontWeight: 600, marginBottom: 12 }}>
        {ar ? 'تحليلات ذكية' : 'Smart Insights'}
      </h3>
      {insights.map((ins, i) => (
        <div key={i} style={{ padding: '8px 12px', marginBottom: 8, background: 'var(--card)', borderRadius: 8, border: '1px solid var(--border)' }}>
          <span style={{ fontWeight: 500 }}>{ins.title || ins.message}</span>
          {ins.description && <p style={{ margin: '4px 0 0', fontSize: 13, opacity: 0.7 }}>{ins.description}</p>}
        </div>
      ))}
      {forecasts.map((fc, i) => (
        <div key={`fc-${i}`} style={{ padding: '8px 12px', marginBottom: 8, background: 'var(--card)', borderRadius: 8, border: '1px solid var(--border)' }}>
          <span style={{ fontWeight: 500 }}>{fc.title || fc.label}</span>
          {fc.value !== undefined && <span style={{ marginLeft: 8, opacity: 0.7 }}>{fc.value}</span>}
        </div>
      ))}
    </div>
  );
}
