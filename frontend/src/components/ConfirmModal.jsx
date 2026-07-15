import React from 'react';
import { AlertTriangle } from 'lucide-react';

const ConfirmModal = ({ open, title, message, confirmText, cancelText, onConfirm, onCancel, danger = false }) => {
  if (!open) return null;
  return (
    <div className="ft-modal-overlay" onClick={e => e.target === e.currentTarget && onCancel()}>
      <div className="ft-modal glass-card" style={{ maxWidth: 420 }}>
        <div className="glass-card-header">
          <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
            <div style={{
              width: 36, height: 36, borderRadius: 10,
              background: danger ? '#ef444420' : '#f59e0b20',
              display: 'flex', alignItems: 'center', justifyContent: 'center',
            }}>
              <AlertTriangle size={18} style={{ color: danger ? '#ef4444' : '#f59e0b' }} />
            </div>
            <div className="glass-card-title" style={{ fontSize: 16 }}>{title}</div>
          </div>
          <button className="btn btn-ghost btn-sm btn-icon" onClick={onCancel}>✕</button>
        </div>
        <div className="glass-card-body">
          <p style={{ margin: 0, color: 'var(--text-main)', lineHeight: 1.6, fontSize: 14 }}>{message}</p>
          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 10, marginTop: 20 }}>
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
    </div>
  );
};

export default ConfirmModal;
