import React from 'react';
import { AlertTriangle, X } from 'lucide-react';

const ConfirmModal = ({ open, title, message, confirmText, cancelText, onConfirm, onCancel, danger = false }) => {
  if (!open) return null;
  return (
    <div className="modal-overlay" onClick={e => e.target === e.currentTarget && onCancel()}>
      <div className="modal-box" style={{ maxWidth: 420 }}>
        <div className="modal-header">
          <div className="modal-title" style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
            <div style={{
              width: 34, height: 34, borderRadius: 'var(--radius-sm)',
              background: danger ? 'var(--danger-subtle)' : 'var(--warning-subtle)',
              display: 'flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0,
            }}>
              <AlertTriangle size={17} style={{ color: danger ? 'var(--danger)' : 'var(--warning)' }} />
            </div>
            <span>{title}</span>
          </div>
          <button className="modal-close" onClick={onCancel} aria-label="Close"><X size={18} /></button>
        </div>
        <div className="modal-body">
          <p style={{ margin: 0, color: 'var(--text-muted)', lineHeight: 1.7, fontSize: 'var(--text-base)' }}>{message}</p>
        </div>
        <div className="modal-footer">
          <button className="btn btn-ghost" onClick={onCancel}>{cancelText || 'Cancel'}</button>
          <button
            className={`btn ${danger ? 'btn-danger' : 'btn-primary'}`}
            onClick={onConfirm}
          >
            {confirmText || 'Confirm'}
          </button>
        </div>
      </div>
    </div>
  );
};

export default ConfirmModal;
