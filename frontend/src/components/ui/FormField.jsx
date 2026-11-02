import React, { useId } from 'react';
import PropTypes from 'prop-types';

/**
 * FuelTrack — FormField wrapper
 * Wraps a label, required marker, child input/select, and error/hint message.
 *
 * Props:
 *   label    : string   — field label text
 *   required : bool     — shows red asterisk
 *   error    : string   — error message (red)
 *   hint     : string   — helper text (muted)
 *   children : node     — the actual input element
 *   htmlFor  : string   — manually specify id (auto-generated if omitted)
 */
function FormField({ label, required = false, error, hint, children, htmlFor, style = {} }) {
  const autoId = useId();
  const fieldId = htmlFor ?? autoId;

  // Clone child to inject id if it's a single React element
  const child = React.Children.count(children) === 1 && React.isValidElement(children)
    ? React.cloneElement(children, { id: children.props.id || fieldId })
    : children;

  return (
    <div className="form-group" style={{ marginBottom: 16, ...style }}>
      {label && (
        <label
          htmlFor={fieldId}
          className="form-label"
          style={{ display: 'block', marginBottom: 6, fontSize: 'var(--text-sm)', fontWeight: 600, color: error ? 'var(--danger)' : 'var(--text)' }}
        >
          {label}
          {required && (
            <span style={{ color: 'var(--danger)', marginInlineStart: 3, fontSize: 'var(--text-xs)' }}>*</span>
          )}
        </label>
      )}
      {child}
      {error && (
        <p style={{ margin: '5px 0 0', fontSize: 'var(--text-xs)', color: 'var(--danger)', lineHeight: 1.5 }}>
          {error}
        </p>
      )}
      {!error && hint && (
        <p style={{ margin: '5px 0 0', fontSize: 'var(--text-xs)', color: 'var(--text-muted)', lineHeight: 1.5 }}>
          {hint}
        </p>
      )}
    </div>
  );
}

FormField.propTypes = {
  label: PropTypes.string,
  required: PropTypes.bool,
  error: PropTypes.string,
  hint: PropTypes.string,
  children: PropTypes.node.isRequired,
  htmlFor: PropTypes.string,
  style: PropTypes.object,
};

// ─────────────────────────────────────────────────────────────────────────────

/**
 * FuelTrack — Input component
 * A styled text/number/date input integrated with design system.
 *
 * Props: standard HTML input props + prefix/suffix icon
 */
const Input = React.forwardRef(function Input(
  { prefix: PrefixIcon, suffix: SuffixIcon, className = '', style = {}, ...rest },
  ref
) {
  if (!PrefixIcon && !SuffixIcon) {
    return (
      <input
        ref={ref}
        className={`form-control ${className}`}
        style={style}
        {...rest}
      />
    );
  }

  return (
    <div style={{ position: 'relative', display: 'flex', alignItems: 'center' }}>
      {PrefixIcon && (
        <PrefixIcon
          size={14}
          style={{
            position: 'absolute',
            insetInlineStart: 10,
            color: 'var(--text-muted)',
            pointerEvents: 'none',
            flexShrink: 0,
          }}
        />
      )}
      <input
        ref={ref}
        className={`form-control ${className}`}
        style={{
          paddingInlineStart: PrefixIcon ? 32 : undefined,
          paddingInlineEnd: SuffixIcon ? 32 : undefined,
          width: '100%',
          ...style,
        }}
        {...rest}
      />
      {SuffixIcon && (
        <SuffixIcon
          size={14}
          style={{
            position: 'absolute',
            insetInlineEnd: 10,
            color: 'var(--text-muted)',
            pointerEvents: 'none',
            flexShrink: 0,
          }}
        />
      )}
    </div>
  );
});

Input.displayName = 'Input';

Input.propTypes = {
  prefix: PropTypes.elementType,
  suffix: PropTypes.elementType,
  className: PropTypes.string,
  style: PropTypes.object,
};

export { FormField, Input };
export default FormField;
