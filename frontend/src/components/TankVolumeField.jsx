import React from 'react';
import { FormField, Input } from './ui';
import { convertHeightToLitres } from '../utils/tankCalibration';

export default function TankVolumeField({
  ar,
  label,
  unit,
  onUnitChange,
  litresValue,
  onLitresChange,
  heightValue,
  onHeightChange,
  calibrationConfig,
  placeholder = '0',
  required = false,
  step = 'any',
}) {
  const isCm = unit === 'cm';
  const convertedLitres = isCm ? convertHeightToLitres(calibrationConfig, heightValue) : null;

  return (
    <FormField
      label={(
        <span style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 8, width: '100%' }}>
          <span>{label}{required ? ' *' : ''}</span>
          <span className="ft-unit-switch" style={{ display: 'inline-flex', gap: 2, fontSize: 11 }}>
            <button
              type="button"
              className={`btn btn-xs ${!isCm ? 'btn-primary' : 'btn-ghost'}`}
              onClick={() => onUnitChange('litres')}
              style={{ padding: '2px 6px', fontSize: 11 }}
            >
              {ar ? 'لتر' : 'Litres'}
            </button>
            <button
              type="button"
              className={`btn btn-xs ${isCm ? 'btn-primary' : 'btn-ghost'}`}
              onClick={() => onUnitChange('cm')}
              style={{ padding: '2px 6px', fontSize: 11 }}
            >
              {ar ? 'ارتفاع (سم)' : 'Height (cm)'}
            </button>
          </span>
        </span>
      )}
    >
      <Input
        type="number"
        step={step}
        value={isCm ? (heightValue ?? '') : (litresValue ?? '')}
        onChange={(e) => (isCm ? onHeightChange(e.target.value) : onLitresChange(e.target.value))}
        placeholder={isCm ? (ar ? 'سم' : 'cm') : placeholder}
        required={required}
      />
      {isCm && (
        <small style={{ display: 'block', marginTop: 4, color: convertedLitres != null ? 'var(--primary)' : 'var(--text-faint)' }}>
          {convertedLitres != null
            ? (ar ? `المحول: ${convertedLitres.toLocaleString('en-US')} لتر` : `Converted: ${convertedLitres.toLocaleString('en-US')} L`)
            : (ar ? 'أدخل ارتفاعاً ومعايرة صالحة للحساب' : 'Enter height and valid calibration to convert')}
        </small>
      )}
    </FormField>
  );
}
