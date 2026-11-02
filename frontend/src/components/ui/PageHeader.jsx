import React from 'react';
import PropTypes from 'prop-types';

/**
 * FuelTrack — Unified Page Header
 * Props:
 *   title   : string | node   — main page heading (renders as h1)
 *   subtitle: string | node   — optional description below title
 *   badge   : { label, color?, bg? } — optional status badge
 *   actions : node            — slot for buttons / controls (rendered end side)
 *   style   : object          — additional wrapper styles
 */
function PageHeader({ title, subtitle, badge, actions, style = {} }) {
  return (
    <div
      className="page-header"
      style={{
        borderBottom: '1px solid var(--border)',
        paddingBottom: 20,
        marginBottom: 30,
        ...style,
      }}
    >
      <div className="page-header-row" style={{ display: 'flex', alignItems: 'flex-start', justifyContent: 'space-between', gap: 16, flexWrap: 'wrap' }}>
        <div style={{ flex: '1 1 0', minWidth: 0 }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 10, flexWrap: 'wrap' }}>
            <h1 style={{ fontSize: 'var(--text-2xl)', fontWeight: 800, letterSpacing: '-0.5px', margin: 0, lineHeight: 1.3 }}>
              {title}
            </h1>
            {badge && (
              <span
                className="badge"
                style={{
                  background: badge.bg ?? 'var(--primary-subtle)',
                  color: badge.color ?? 'var(--primary)',
                  fontSize: 'var(--text-xs)',
                  padding: '3px 10px',
                  borderRadius: 20,
                  fontWeight: 600,
                  flexShrink: 0,
                }}
              >
                {badge.label}
              </span>
            )}
          </div>
          {subtitle && (
            <p style={{ margin: '6px 0 0', fontSize: 'var(--text-base)', color: 'var(--text-muted)', lineHeight: 1.6 }}>
              {subtitle}
            </p>
          )}
        </div>
        {actions && (
          <div style={{ display: 'flex', alignItems: 'center', gap: 8, flexShrink: 0, flexWrap: 'wrap' }}>
            {actions}
          </div>
        )}
      </div>
    </div>
  );
}

PageHeader.propTypes = {
  title: PropTypes.node.isRequired,
  subtitle: PropTypes.node,
  badge: PropTypes.shape({
    label: PropTypes.node.isRequired,
    color: PropTypes.string,
    bg: PropTypes.string,
  }),
  actions: PropTypes.node,
  style: PropTypes.object,
};

export default PageHeader;
