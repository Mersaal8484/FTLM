import React from 'react';
import { FormField, Input } from './ui';
import { TANK_CALIBRATION_MODE } from '../utils/tankCalibration';

export default function TankCalibrationSection({ ar, value, onChange, showGeometry = true }) {
  const mode = Number(value?.calibrationMode ?? TANK_CALIBRATION_MODE.GEOMETRY);
  const isLinear = mode === TANK_CALIBRATION_MODE.LITRES_PER_CM;

  const setField = (k, v) => onChange({ ...value, [k]: v });

  return (
    <div style={{
      margin: '12px 0',
      padding: 12,
      borderRadius: 8,
      background: 'var(--surface-2, rgba(255,255,255,0.03))',
      border: '1px solid var(--border)',
    }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 10, gap: 8, flexWrap: 'wrap' }}>
        <span style={{ fontSize: 13, fontWeight: 600, color: 'var(--primary)' }}>
          {ar ? 'معايرة الخزان (التحويل بين سم ولتر)' : 'Tank Calibration (cm ↔ Litres)'}
        </span>
        <div style={{ display: 'inline-flex', gap: 4 }}>
          <button
            type="button"
            className={`btn btn-xs ${!isLinear ? 'btn-primary' : 'btn-ghost'}`}
            onClick={() => setField('calibrationMode', TANK_CALIBRATION_MODE.GEOMETRY)}
          >
            {ar ? 'هندسي (قطر/ارتفاع)' : 'Geometry'}
          </button>
          <button
            type="button"
            className={`btn btn-xs ${isLinear ? 'btn-primary' : 'btn-ghost'}`}
            onClick={() => setField('calibrationMode', TANK_CALIBRATION_MODE.LITRES_PER_CM)}
          >
            {ar ? 'لتر / سم (خطي)' : 'Litres / cm'}
          </button>
        </div>
      </div>

      {isLinear ? (
        <FormField label={ar ? 'معامل التحويل (لتر لكل 1 سم) *' : 'Litres per cm *'}>
          <Input
            type="number"
            step="0.0001"
            value={value?.litresPerCm ?? ''}
            onChange={(e) => setField('litresPerCm', e.target.value)}
            placeholder="10"
          />
        </FormField>
      ) : showGeometry ? (
        <>
          <div className="form-row">
            <FormField label={ar ? 'القطر (سم)' : 'Diameter (cm)'}>
              <Input type="number" step="0.1" value={value?.diameterCm ?? ''} onChange={(e) => setField('diameterCm', e.target.value)} placeholder="200" />
            </FormField>
            <FormField label={ar ? 'ارتفاع الأسطوانة (سم)' : 'Cylinder Height (cm)'}>
              <Input type="number" step="0.1" value={value?.cylinderHeightCm ?? ''} onChange={(e) => setField('cylinderHeightCm', e.target.value)} placeholder="150" />
            </FormField>
          </div>
          <div className="form-row">
            <FormField label={ar ? 'ارتفاع القمع (سم)' : 'Cone Height (cm)'}>
              <Input type="number" step="0.1" value={value?.coneHeightCm ?? ''} onChange={(e) => setField('coneHeightCm', e.target.value)} placeholder="50" />
            </FormField>
            <FormField label={ar ? 'معامل الضرب' : 'Multiplication Factor'}>
              <Input type="number" step="0.0001" value={value?.multiplicationFactor ?? '1.0'} onChange={(e) => setField('multiplicationFactor', e.target.value)} placeholder="1.0" />
            </FormField>
          </div>
        </>
      ) : (
        <small style={{ color: 'var(--text-faint)' }}>
          {ar ? 'يستخدم المعايرة الهندسية للخزان (إن وجدت في تبويب المعايرة).' : 'Uses tank geometry calibration (if configured in calibration tab).'}
        </small>
      )}
    </div>
  );
}
