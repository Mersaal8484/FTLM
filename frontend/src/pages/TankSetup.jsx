import React, { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { Eye, CheckCircle, AlertTriangle, Layers } from 'lucide-react';
import client from '../api/client';
import { PageHeader, Button, FormField } from '../components/ui';

export default function TankSetup() {
  const { t } = useTranslation();
  const [tanks, setTanks] = useState([]);
  const [tankId, setTankId] = useState('');
  const [diameterCm, setDiameterCm] = useState('');
  const [cylinderHeightCm, setCylinderHeightCm] = useState('');
  const [coneHeightCm, setConeHeightCm] = useState('');
  const [multiplicationFactor, setMultiplicationFactor] = useState('1.0');
  const [deadStockMode, setDeadStockMode] = useState('litres'); // 'litres' | 'cm'
  const [deadStockLitres, setDeadStockLitres] = useState('');
  const [deadStockLevelCm, setDeadStockLevelCm] = useState('');
  const [preview, setPreview] = useState(null);
  const [saving, setSaving] = useState(false);
  const [successMsg, setSuccessMsg] = useState('');
  const [errorMsg, setErrorMsg] = useState('');

  useEffect(() => {
    client.get('/api/fuel-tanks').then(res => {
      setTanks(res.data?.items || res.data || []);
    }).catch(() => {});
  }, []);

  const handleSelectTank = (id) => {
    setTankId(id);
    setPreview(null);
    setSuccessMsg('');
    setErrorMsg('');
    const tank = tanks.find(t => t.id === parseInt(id, 10));
    if (tank) {
      setDiameterCm(tank.diameterCm || tank.tankDiameterCm || '');
      setCylinderHeightCm(tank.cylinderHeightCm || '');
      setConeHeightCm(tank.coneHeightCm || '');
      setMultiplicationFactor(tank.multiplicationFactor || '1.0');
      setDeadStockLitres(tank.deadStockLiters != null ? String(tank.deadStockLiters) : (tank.deadStockLitres != null ? String(tank.deadStockLitres) : ''));
      setDeadStockLevelCm(tank.deadStockLevelCm != null ? String(tank.deadStockLevelCm) : '');
      if (tank.deadStockInputUnit === 'cm') {
        setDeadStockMode('cm');
      } else {
        setDeadStockMode('litres');
      }
    }
  };

  const buildPayload = () => ({
    diameterCm: parseFloat(diameterCm),
    cylinderHeightCm: parseFloat(cylinderHeightCm),
    coneHeightCm: parseFloat(coneHeightCm),
    multiplicationFactor: parseFloat(multiplicationFactor || '1.0'),
    deadStockLitres: deadStockMode === 'litres' && deadStockLitres !== '' ? parseFloat(deadStockLitres) : null,
    deadStockLevelCm: deadStockMode === 'cm' && deadStockLevelCm !== '' ? parseFloat(deadStockLevelCm) : null,
  });

  const handlePreview = async () => {
    if (!diameterCm || !cylinderHeightCm || !coneHeightCm) {
      setErrorMsg('Please specify diameter, cylinder height, and cone height');
      return;
    }
    setErrorMsg('');
    try {
      const payload = buildPayload();
      const res = await client.post(`/api/fuel-tanks/${tankId || 1}/calibration/preview`, payload);
      setPreview(res.data);
    } catch (err) {
      setErrorMsg(err.response?.data?.error || 'Failed to compute calibration preview');
    }
  };

  const handleSave = async (e) => {
    e.preventDefault();
    if (!tankId) {
      setErrorMsg('Please select a tank');
      return;
    }
    setSaving(true);
    setErrorMsg('');
    setSuccessMsg('');
    try {
      const payload = buildPayload();
      await client.put(`/api/fuel-tanks/${tankId}/calibration`, payload);
      setSuccessMsg(t('operational.tanks.setupSuccess'));
    } catch (err) {
      setErrorMsg(err.response?.data?.error || 'Failed to save tank calibration');
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="capture-page operational-shell" style={{ maxWidth: 800, margin: '0 auto', padding: 24 }}>
      <PageHeader
        title={t('operational.tanks.setupTitle')}
        subtitle={t('operational.tanks.setupSubtitle')}
      />

      {successMsg && (
        <div style={{ padding: 12, borderRadius: 8, background: 'rgba(34,197,94,0.15)', border: '1px solid #22c55e', color: '#22c55e', marginBottom: 16, display: 'flex', alignItems: 'center', gap: 8 }}>
          <CheckCircle size={18} /> {successMsg}
        </div>
      )}
      {errorMsg && (
        <div style={{ padding: 12, borderRadius: 8, background: 'rgba(239,68,68,0.15)', border: '1px solid #ef4444', color: '#ef4444', marginBottom: 16, display: 'flex', alignItems: 'center', gap: 8 }}>
          <AlertTriangle size={18} /> {errorMsg}
        </div>
      )}

      <form onSubmit={handleSave} style={{ display: 'grid', gap: 16 }}>
        <FormField label={t('operational.tanks.tank')}>
          <select
            value={tankId}
            onChange={e => handleSelectTank(e.target.value)}
            style={{ width: '100%', padding: '10px 14px', borderRadius: 8, background: 'var(--surface-2)', color: 'var(--text)', border: '1px solid var(--border)' }}
            required
          >
            <option value="">-- {t('operational.tanks.selectTank')} --</option>
            {tanks.map(t => (
              <option key={t.id} value={t.id}>{t.name} ({t.subStationName || `Site ${t.subStationId}`})</option>
            ))}
          </select>
        </FormField>

        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
          <FormField label={t('operational.tanks.diameterCm')}>
            <input
              type="number"
              step="0.1"
              value={diameterCm}
              onChange={e => setDiameterCm(e.target.value)}
              placeholder="e.g. 200"
              required
              style={{ width: '100%', padding: '10px 14px', borderRadius: 8, background: 'var(--surface-2)', color: 'var(--text)', border: '1px solid var(--border)' }}
            />
          </FormField>

          <FormField label={t('operational.tanks.cylinderHeightCm')}>
            <input
              type="number"
              step="0.1"
              value={cylinderHeightCm}
              onChange={e => setCylinderHeightCm(e.target.value)}
              placeholder="e.g. 300"
              required
              style={{ width: '100%', padding: '10px 14px', borderRadius: 8, background: 'var(--surface-2)', color: 'var(--text)', border: '1px solid var(--border)' }}
            />
          </FormField>
        </div>

        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
          <FormField label={t('operational.tanks.coneHeightCm')}>
            <input
              type="number"
              step="0.1"
              value={coneHeightCm}
              onChange={e => setConeHeightCm(e.target.value)}
              placeholder="e.g. 50"
              required
              style={{ width: '100%', padding: '10px 14px', borderRadius: 8, background: 'var(--surface-2)', color: 'var(--text)', border: '1px solid var(--border)' }}
            />
          </FormField>

          <FormField label={t('operational.tanks.factor')}>
            <input
              type="number"
              step="0.0001"
              value={multiplicationFactor}
              onChange={e => setMultiplicationFactor(e.target.value)}
              placeholder="1.0"
              required
              style={{ width: '100%', padding: '10px 14px', borderRadius: 8, background: 'var(--surface-2)', color: 'var(--text)', border: '1px solid var(--border)' }}
            />
          </FormField>
        </div>

        <div style={{ display: 'grid', gridTemplateColumns: '1fr 2fr', gap: 16, alignItems: 'end' }}>
          <FormField label={t('operational.tanks.deadStockMode')}>
            <select
              value={deadStockMode}
              onChange={e => {
                setDeadStockMode(e.target.value);
                setPreview(null);
              }}
              style={{ width: '100%', padding: '10px 14px', borderRadius: 8, background: 'var(--surface-2)', color: 'var(--text)', border: '1px solid var(--border)' }}
            >
              <option value="litres">{t('operational.tanks.deadStockModeLitres')}</option>
              <option value="cm">{t('operational.tanks.deadStockModeCm')}</option>
            </select>
          </FormField>

          {deadStockMode === 'litres' ? (
            <FormField label={t('operational.tanks.deadStockLitres')}>
              <input
                type="number"
                step="0.1"
                min="0"
                value={deadStockLitres}
                onChange={e => setDeadStockLitres(e.target.value)}
                placeholder="e.g. 100"
                style={{ width: '100%', padding: '10px 14px', borderRadius: 8, background: 'var(--surface-2)', color: 'var(--text)', border: '1px solid var(--border)' }}
              />
            </FormField>
          ) : (
            <FormField label={t('operational.tanks.deadStockLevelCm')}>
              <input
                type="number"
                step="0.1"
                min="0"
                value={deadStockLevelCm}
                onChange={e => setDeadStockLevelCm(e.target.value)}
                placeholder="e.g. 25"
                style={{ width: '100%', padding: '10px 14px', borderRadius: 8, background: 'var(--surface-2)', color: 'var(--text)', border: '1px solid var(--border)' }}
              />
            </FormField>
          )}
        </div>

        <div style={{ display: 'flex', gap: 12 }}>
          <Button type="button" variant="secondary" onClick={handlePreview}>
            <Eye size={16} /> {t('operational.tanks.preview')}
          </Button>
          <Button type="submit" disabled={saving}>
            {saving ? t('operational.tanks.savingSetup') : t('operational.tanks.saveSetup')}
          </Button>
        </div>
      </form>

      {preview && (
        <div style={{ marginTop: 24, padding: 18, borderRadius: 10, background: 'var(--surface-2)', border: '1px solid var(--border)' }}>
          <h3 style={{ marginBottom: 12, display: 'flex', alignItems: 'center', gap: 8 }}>
            <Layers size={18} color="var(--primary)" /> {t('operational.tanks.preview')}
          </h3>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))', gap: 12 }}>
            <div>
              <span style={{ fontSize: 12, color: 'var(--text-muted)' }}>{t('operational.tanks.fullHeight')}</span>
              <p style={{ fontSize: 18, fontWeight: 700 }}>{preview.totalHeightCm || (parseFloat(cylinderHeightCm) + parseFloat(coneHeightCm))} {t('operational.tanks.cm')}</p>
            </div>
            <div>
              <span style={{ fontSize: 12, color: 'var(--text-muted)' }}>{t('operational.tanks.totalVol')}</span>
              <p style={{ fontSize: 18, fontWeight: 700, color: '#22c55e' }}>{Number(preview.computedCapacityLitres || preview.capacityLitres || 0).toLocaleString()} {t('operational.tanks.litres')}</p>
            </div>
            <div>
              <span style={{ fontSize: 12, color: 'var(--text-muted)' }}>{t('operational.tanks.deadStock')}</span>
              <p style={{ fontSize: 16, fontWeight: 700, color: 'var(--text)' }}>
                {preview.deadStockLitres != null ? `${Number(preview.deadStockLitres).toLocaleString()} ${t('operational.tanks.litres')}` : (deadStockLitres ? `${deadStockLitres} ${t('operational.tanks.litres')}` : '—')}
                {preview.deadStockLevelCm != null && (
                  <span style={{ display: 'block', fontSize: 12, fontWeight: 400, color: 'var(--text-muted)' }}>
                    ({preview.deadStockLevelCm} {t('operational.tanks.cm')})
                  </span>
                )}
              </p>
            </div>
            <div>
              <span style={{ fontSize: 12, color: 'var(--text-muted)' }}>{t('operational.tanks.usableVol')}</span>
              <p style={{ fontSize: 18, fontWeight: 700, color: 'var(--primary)' }}>
                {Math.max(0, (Number(preview.computedCapacityLitres || 0) - (preview.deadStockLitres != null ? Number(preview.deadStockLitres) : (parseFloat(deadStockLitres) || 0)))).toLocaleString()} {t('operational.tanks.litres')}
              </p>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
