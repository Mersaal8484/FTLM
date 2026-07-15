import React from 'react';

export function LoadingSpinner({ size = 32, text = 'جاري التحميل...' }) {
  return (
    <div style={{
      display: 'flex',
      flexDirection: 'column',
      alignItems: 'center',
      justifyContent: 'center',
      padding: '40px',
      fontFamily: 'Tajawal, sans-serif',
      direction: 'rtl',
    }}>
      <div style={{
        width: size,
        height: size,
        border: '3px solid #e0e0e0',
        borderTopColor: '#3498db',
        borderRadius: '50%',
        animation: 'spin 0.8s linear infinite',
      }} />
      {text && (
        <p style={{ marginTop: '12px', color: '#666', fontSize: '14px' }}>
          {text}
        </p>
      )}
      <style>{`
        @keyframes spin {
          to { transform: rotate(360deg); }
        }
      `}</style>
    </div>
  );
}

export function TableLoader({ rows = 5 }) {
  return (
    <div style={{ padding: '16px' }}>
      {Array.from({ length: rows }).map((_, i) => (
        <div key={i} style={{
          display: 'flex',
          gap: '12px',
          marginBottom: '12px',
          alignItems: 'center',
        }}>
          <div style={{
            flex: 1,
            height: '14px',
            backgroundColor: '#f0f0f0',
            borderRadius: '4px',
            animation: `pulse 1.5s ease-in-out ${i * 0.1}s infinite`,
          }} />
          <div style={{
            flex: 1,
            height: '14px',
            backgroundColor: '#f0f0f0',
            borderRadius: '4px',
            animation: `pulse 1.5s ease-in-out ${i * 0.1 + 0.2}s infinite`,
          }} />
          <div style={{
            width: '80px',
            height: '14px',
            backgroundColor: '#f0f0f0',
            borderRadius: '4px',
            animation: `pulse 1.5s ease-in-out ${i * 0.1 + 0.4}s infinite`,
          }} />
        </div>
      ))}
      <style>{`
        @keyframes pulse {
          0%, 100% { opacity: 1; }
          50% { opacity: 0.4; }
        }
      `}</style>
    </div>
  );
}
