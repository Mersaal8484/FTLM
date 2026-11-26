export const TANK_CALIBRATION_MODE = {
  GEOMETRY: 0,
  LITRES_PER_CM: 1,
};

export function isGeometryConfigured(config) {
  const diameterCm = Number(config?.diameterCm);
  const cylinderHeightCm = config?.cylinderHeightCm === '' || config?.cylinderHeightCm == null ? 0 : Number(config.cylinderHeightCm);
  const coneHeightCm = Number(config?.coneHeightCm);
  const factor = config?.multiplicationFactor === '' || config?.multiplicationFactor == null ? 1 : Number(config.multiplicationFactor);
  return diameterCm > 0 && cylinderHeightCm >= 0 && coneHeightCm > 0 && factor > 0;
}

export function isCalibrationConfigured(config) {
  const mode = Number(config?.calibrationMode ?? TANK_CALIBRATION_MODE.GEOMETRY);
  if (mode === TANK_CALIBRATION_MODE.LITRES_PER_CM) {
    return Number(config?.litresPerCm) > 0;
  }
  return isGeometryConfigured(config);
}

export function calculateConicalTankLitres({ diameterCm, cylinderHeightCm = 0, coneHeightCm, multiplicationFactor = 1, levelCm }) {
  const d = Number(diameterCm);
  const hCyl = Number(cylinderHeightCm || 0);
  const hCone = Number(coneHeightCm);
  const factor = Number(multiplicationFactor || 1);
  const level = Number(levelCm);
  if (!(d > 0 && hCyl >= 0 && hCone > 0 && factor > 0) || Number.isNaN(level) || level < 0 || level > hCone + hCyl) {
    return null;
  }
  const radius = d / 2;
  const coneVol = (Math.PI * radius * radius * hCone) / 3;
  const vol = level <= hCone
    ? (Math.PI * radius * radius * level * level * level) / (3 * hCone * hCone)
    : coneVol + Math.PI * radius * radius * (level - hCone);
  return Math.round(((vol * factor) / 1000) * 1000) / 1000;
}

export function convertHeightToLitres(config, heightCm) {
  if (heightCm === '' || heightCm == null) return null;
  const level = Number(heightCm);
  if (Number.isNaN(level) || level < 0) return null;
  const mode = Number(config?.calibrationMode ?? TANK_CALIBRATION_MODE.GEOMETRY);
  if (mode === TANK_CALIBRATION_MODE.LITRES_PER_CM) {
    const lpc = Number(config?.litresPerCm);
    if (!(lpc > 0)) return null;
    return Math.round(level * lpc * 1000) / 1000;
  }
  return calculateConicalTankLitres({
    diameterCm: config?.diameterCm,
    cylinderHeightCm: config?.cylinderHeightCm,
    coneHeightCm: config?.coneHeightCm,
    multiplicationFactor: config?.multiplicationFactor,
    levelCm: level,
  });
}

export function resolveEnteredLitres(config, unit, litresValue, heightValue) {
  if (unit === 'cm') {
    return convertHeightToLitres(config, heightValue);
  }
  if (litresValue === '' || litresValue == null) return null;
  const n = Number(litresValue);
  return Number.isNaN(n) ? null : n;
}

export function validateTankStockInputs({ ar, config, fields }) {
  const hasHeightMode = fields.some((f) => f.unit === 'cm' && f.heightValue !== '' && f.heightValue != null);
  if (hasHeightMode && !isCalibrationConfigured(config)) {
    return ar
      ? 'الإدخال بالارتفاع (سم) يتطلب تحديد معامل اللتر/سم أو أبعاد المعايرة أولاً.'
      : 'Height (cm) entry requires a valid Litres/cm factor or tank geometry calibration.';
  }

  for (const f of fields) {
    if (f.unit === 'cm' && f.heightValue !== '' && f.heightValue != null) {
      const converted = convertHeightToLitres(config, f.heightValue);
      if (converted === null) {
        return ar
          ? `قيمة الارتفاع لحقل "${f.label}" غير صالحة أو تتجاوز أبعاد المعايرة.`
          : `Height value for "${f.label}" is invalid or outside calibrated dimensions.`;
      }
    }
  }

  const cap = fields.find((f) => f.key === 'capacity')?.resolvedLitres;
  const minAlert = fields.find((f) => f.key === 'minAlert')?.resolvedLitres;
  const deadStock = fields.find((f) => f.key === 'deadStock')?.resolvedLitres;
  const actual = fields.find((f) => f.key === 'actualStock')?.resolvedLitres;

  if (cap == null || Number.isNaN(cap) || cap <= 0) {
    return ar ? 'سعة الخزان يجب أن تكون أكبر من صفر.' : 'Tank capacity must be greater than zero.';
  }
  if (minAlert != null && (minAlert < 0 || minAlert > cap)) {
    return ar ? 'الحد الأدنى للمخزون يجب أن يكون بين 0 وسعة الخزان.' : 'Minimum alert must be between 0 and tank capacity.';
  }
  if (actual != null && (actual < 0 || actual > cap)) {
    return ar ? 'المخزون الفعلي يجب أن يكون بين 0 وسعة الخزان.' : 'Actual stock must be between 0 and tank capacity.';
  }
  if (deadStock != null && (deadStock < 0 || deadStock > cap)) {
    return ar
      ? 'النقطة الميتة يجب أن تكون بين 0 وسعة الخزان.'
      : 'Dead stock must be between 0 and tank capacity.';
  }

  return '';
}
