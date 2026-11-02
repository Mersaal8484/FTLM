import React from 'react';
import PropTypes from 'prop-types';

/**
 * FuelTrack — Unified Button Component
 * Variants : primary | secondary | success | danger | warning | ghost | outline
 * Sizes    : sm | md | lg
 */
const Button = React.forwardRef(function Button(
  {
    children,
    variant = 'primary',
    size = 'md',
    loading = false,
    disabled = false,
    icon: Icon = null,
    iconPosition = 'start',
    type = 'button',
    className = '',
    style = {},
    onClick,
    ...rest
  },
  ref
) {
  const variantClass = {
    primary:   'btn-primary',
    secondary: 'btn-secondary',
    success:   'btn-success',
    danger:    'btn-danger',
    warning:   'btn-warning',
    ghost:     'btn-ghost',
    outline:   'btn-outline',
  }[variant] ?? 'btn-primary';

  const sizeStyle = {
    sm: { fontSize: 'var(--text-xs)', padding: '5px 12px', height: 30, gap: 5 },
    md: { fontSize: 'var(--text-sm)', padding: '7px 16px', height: 36, gap: 6 },
    lg: { fontSize: 'var(--text-base)', padding: '9px 22px', height: 42, gap: 8 },
  }[size] ?? {};

  const isDisabled = disabled || loading;

  return (
    <button
      ref={ref}
      type={type}
      className={`btn ${variantClass} ${className}`}
      disabled={isDisabled}
      onClick={!isDisabled ? onClick : undefined}
      style={{ display: 'inline-flex', alignItems: 'center', justifyContent: 'center', ...sizeStyle, ...style }}
      {...rest}
    >
      {loading ? (
        <>
          <span className="ft-spinner" style={{ width: 14, height: 14, flexShrink: 0 }} />
          {children && <span style={{ marginInlineStart: 6 }}>{children}</span>}
        </>
      ) : (
        <>
          {Icon && iconPosition === 'start' && <Icon size={size === 'sm' ? 13 : size === 'lg' ? 17 : 15} style={{ flexShrink: 0 }} />}
          {children && <span>{children}</span>}
          {Icon && iconPosition === 'end' && <Icon size={size === 'sm' ? 13 : size === 'lg' ? 17 : 15} style={{ flexShrink: 0 }} />}
        </>
      )}
    </button>
  );
});

Button.displayName = 'Button';

Button.propTypes = {
  children: PropTypes.node,
  variant: PropTypes.oneOf(['primary', 'secondary', 'success', 'danger', 'warning', 'ghost', 'outline']),
  size: PropTypes.oneOf(['sm', 'md', 'lg']),
  loading: PropTypes.bool,
  disabled: PropTypes.bool,
  icon: PropTypes.elementType,
  iconPosition: PropTypes.oneOf(['start', 'end']),
  type: PropTypes.string,
  className: PropTypes.string,
  style: PropTypes.object,
  onClick: PropTypes.func,
};

export default Button;
