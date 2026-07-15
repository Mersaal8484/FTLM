import React from 'react';
import { CheckCircle, XCircle, AlertTriangle, Info, X } from 'lucide-react';
import useToastStore from '../store/toastStore';

const TOAST_STYLES = {
  success: {
    background: 'hsla(142, 71%, 45%, 0.12)',
    border: '1px solid hsla(142, 71%, 45%, 0.3)',
    color: '#4ade80',
    icon: CheckCircle,
  },
  error: {
    background: 'hsla(0, 84%, 60%, 0.12)',
    border: '1px solid hsla(0, 84%, 60%, 0.3)',
    color: '#f87171',
    icon: XCircle,
  },
  warning: {
    background: 'hsla(38, 92%, 55%, 0.12)',
    border: '1px solid hsla(38, 92%, 55%, 0.3)',
    color: '#fbbf24',
    icon: AlertTriangle,
  },
  info: {
    background: 'hsla(217, 91%, 60%, 0.12)',
    border: '1px solid hsla(217, 91%, 60%, 0.3)',
    color: '#60a5fa',
    icon: Info,
  },
};

export default function Toast() {
  const toasts = useToastStore((s) => s.toasts);
  const removeToast = useToastStore((s) => s.removeToast);

  if (!toasts.length) return null;

  return (
    <div
      style={{
        position: 'fixed',
        top: 20,
        right: 20,
        zIndex: 9999,
        display: 'flex',
        flexDirection: 'column',
        gap: 8,
        maxWidth: 380,
      }}
    >
      {toasts.map((t) => {
        const style = TOAST_STYLES[t.type] || TOAST_STYLES.info;
        const Icon = style.icon;
        return (
          <div
            key={t.id}
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: 10,
              padding: '12px 16px',
              borderRadius: 10,
              background: style.background,
              border: style.border,
              backdropFilter: 'blur(12px)',
              boxShadow: '0 8px 24px rgba(0,0,0,0.25)',
              animation: 'toast-slide-in 0.25s ease-out',
              minWidth: 240,
            }}
          >
            <Icon size={18} style={{ color: style.color, flexShrink: 0 }} />
            <span style={{ color: style.color, fontSize: 14, fontWeight: 500, flex: 1 }}>
              {t.message}
            </span>
            <button
              onClick={() => removeToast(t.id)}
              style={{
                background: 'none',
                border: 'none',
                color: style.color,
                cursor: 'pointer',
                padding: 2,
                opacity: 0.6,
                flexShrink: 0,
              }}
              onMouseEnter={(e) => (e.currentTarget.style.opacity = '1')}
              onMouseLeave={(e) => (e.currentTarget.style.opacity = '0.6')}
            >
              <X size={14} />
            </button>
          </div>
        );
      })}
    </div>
  );
}
