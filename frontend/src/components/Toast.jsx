import React from 'react';
import { CheckCircle, XCircle, AlertTriangle, Info, X } from 'lucide-react';
import useToastStore from '../store/toastStore';

const TOAST_STYLES = {
  success: {
    className: 'toast-success',
    icon: CheckCircle,
  },
  error: {
    className: 'toast-error',
    icon: XCircle,
  },
  warning: {
    className: 'toast-warning',
    icon: AlertTriangle,
  },
  info: {
    className: 'toast-info',
    icon: Info,
  },
};

export default function Toast() {
  const toasts = useToastStore((s) => s.toasts);
  const removeToast = useToastStore((s) => s.removeToast);

  if (!toasts.length) return null;

  return (
    <div className="toast-container" aria-live="polite">
      {toasts.map((t) => {
        const style = TOAST_STYLES[t.type] || TOAST_STYLES.info;
        const Icon = style.icon;
        return (
          <div key={t.id} className={`toast ${style.className}`}>
            <Icon size={18} className="toast-icon" />
            <span className="toast-message">
              {t.message}
            </span>
            <button
              onClick={() => removeToast(t.id)}
              className="toast-close"
              aria-label="Close"
            >
              <X size={14} />
            </button>
          </div>
        );
      })}
    </div>
  );
}
