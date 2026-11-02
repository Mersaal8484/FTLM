import React, { useEffect, useRef } from 'react';
import PropTypes from 'prop-types';
import { X } from 'lucide-react';

/**
 * FuelTrack — Unified Modal Component
 * Features: backdrop, close button, Esc key, focus-trap, fadeIn animation
 *
 * Props:
 *   open        : bool       — controls visibility
 *   onClose     : func       — called when user closes modal
 *   title       : node       — modal header title
 *   children    : node       — modal body content
 *   footer      : node       — optional footer slot (action buttons)
 *   maxWidth    : number     — max-width in px (default 560)
 *   closeOnBackdrop : bool   — close when clicking backdrop (default true)
 */
function Modal({
  open,
  onClose,
  title,
  children,
  footer,
  maxWidth = 560,
  closeOnBackdrop = true,
}) {
  const dialogRef = useRef(null);

  // Esc key to close
  useEffect(() => {
    if (!open) return;
    const handler = (e) => { if (e.key === 'Escape') onClose?.(); };
    document.addEventListener('keydown', handler);
    return () => document.removeEventListener('keydown', handler);
  }, [open, onClose]);

  // Focus trap — move focus into modal when it opens
  useEffect(() => {
    if (open && dialogRef.current) {
      const focusable = dialogRef.current.querySelectorAll(
        'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
      );
      if (focusable.length) focusable[0].focus();
    }
  }, [open]);

  // Prevent body scroll when open
  useEffect(() => {
    document.body.style.overflow = open ? 'hidden' : '';
    return () => { document.body.style.overflow = ''; };
  }, [open]);

  if (!open) return null;

  return (
    <div
      className="modal-overlay"
      onClick={(e) => { if (closeOnBackdrop && e.target === e.currentTarget) onClose?.(); }}
      role="dialog"
      aria-modal="true"
      aria-label={typeof title === 'string' ? title : undefined}
      style={{ animation: 'fadeIn 0.15s ease' }}
    >
      <div
        ref={dialogRef}
        className="modal-box"
        style={{ maxWidth, width: '100%', animation: 'slideUp 0.18s ease' }}
      >
        {/* Header */}
        <div className="modal-header">
          <div className="modal-title">{title}</div>
          <button
            className="modal-close"
            onClick={onClose}
            aria-label="Close modal"
            type="button"
          >
            <X size={18} />
          </button>
        </div>

        {/* Body */}
        <div className="modal-body">{children}</div>

        {/* Footer */}
        {footer && <div className="modal-footer">{footer}</div>}
      </div>

      <style>{`
        @keyframes slideUp {
          from { transform: translateY(18px); opacity: 0; }
          to   { transform: translateY(0);    opacity: 1; }
        }
      `}</style>
    </div>
  );
}

Modal.propTypes = {
  open: PropTypes.bool.isRequired,
  onClose: PropTypes.func,
  title: PropTypes.node,
  children: PropTypes.node,
  footer: PropTypes.node,
  maxWidth: PropTypes.number,
  closeOnBackdrop: PropTypes.bool,
};

export default Modal;
