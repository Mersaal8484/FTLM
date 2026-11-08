import React, { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { ArrowRightLeft, Camera, CheckCircle, AlertTriangle, Droplet } from 'lucide-react';
import client from '../api/client';
import { PageHeader, Button, FormField } from '../components/ui';

export default function TankRefillTransfer() {
  const { t } = useTranslation();
  const [tanks, setTanks] = useState([]);
  const [subStationId, setSubStationId] = useState('');
  const [tankId, setTankId] = useState('');
  const [eventType, setEventType] = useState('Receipt'); // Receipt, TransferIn, TransferOut
  const [beforeLevelCm, setBeforeLevelCm] = useState('');
  const [afterLevelCm, setAfterLevelCm] = useState('');
  const [deliveryNoteLitres, setDeliveryNoteLitres] = useState('');
  const [notes, setNotes] = useState('');
  const [photoId, setPhotoId] = useState(null);
  const [uploading, setUploading] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [successMsg, setSuccessMsg] = useState('');
  const [errorMsg, setErrorMsg] = useState('');

  useEffect(() => {
    client.get('/api/fuel-tanks').then(res => {
      setTanks(res.data?.items || res.data || []);
    }).catch(() => {});
  }, []);

  const handlePhotoUpload = async (e) => {
    const file = e.target.files?.[0];
    if (!file) return;
    if (file.size > 65 * 1024) {
      setErrorMsg('Photo exceeds 65 KB limit.');
      return;
    }
    setUploading(true);
    setErrorMsg('');
    try {
      const formData = new FormData();
      formData.append('file', file);
      const res = await client.post('/api/attachments', formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
      });
      setPhotoId(res.data?.id || res.data?.attachmentId);
    } catch (err) {
      setErrorMsg(err.response?.data?.error || 'Failed to upload photo');
    } finally {
      setUploading(false);
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!tankId || !afterLevelCm) {
      setErrorMsg('Please fill required fields');
      return;
    }
    setSubmitting(true);
    setErrorMsg('');
    setSuccessMsg('');
    try {
      const selectedTank = tanks.find(t => t.id === parseInt(tankId, 10));
      const sId = selectedTank?.subStationId || 1;

      const payload = {
        subStationId: sId,
        idempotencyKey: `refill-${Date.now()}-${Math.random().toString(36).substring(2, 7)}`,
        capturedAtUtc: new Date().toISOString(),
        tankReadings: [
          {
            fuelTankId: parseInt(tankId, 10),
            liquidLevelCm: parseFloat(afterLevelCm),
            beforeLiquidLevelCm: beforeLevelCm ? parseFloat(beforeLevelCm) : null,
            deliveryNoteLitres: deliveryNoteLitres ? parseFloat(deliveryNoteLitres) : null,
            eventType: eventType,
            photoAttachmentIds: photoId ? [photoId] : []
          }
        ]
      };

      await client.post('/api/operational-captures', payload);
      setSuccessMsg(t('operational.tanks.successRefill'));
      setBeforeLevelCm('');
      setAfterLevelCm('');
      setDeliveryNoteLitres('');
      setNotes('');
      setPhotoId(null);
    } catch (err) {
      setErrorMsg(err.response?.data?.error || err.message || 'Error recording refill');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="capture-page operational-shell" style={{ maxWidth: 800, margin: '0 auto', padding: 24 }}>
      <PageHeader
        title={t('operational.tanks.refillTitle')}
        subtitle={t('operational.tanks.refillSubtitle')}
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

      <form onSubmit={handleSubmit} style={{ display: 'grid', gap: 16 }}>
        <FormField label={t('operational.tanks.tank')}>
          <select
            value={tankId}
            onChange={e => setTankId(e.target.value)}
            style={{ width: '100%', padding: '10px 14px', borderRadius: 8, background: 'var(--surface-2)', color: 'var(--text)', border: '1px solid var(--border)' }}
            required
          >
            <option value="">-- {t('operational.tanks.selectTank')} --</option>
            {tanks.map(t => (
              <option key={t.id} value={t.id}>{t.name} ({t.subStationName || `Site ${t.subStationId}`})</option>
            ))}
          </select>
        </FormField>

        <FormField label={t('operational.tanks.eventType')}>
          <div style={{ display: 'flex', gap: 12 }}>
            {['Receipt', 'TransferIn', 'TransferOut'].map(type => (
              <label key={type} style={{ display: 'flex', alignItems: 'center', gap: 6, cursor: 'pointer' }}>
                <input
                  type="radio"
                  name="eventType"
                  value={type}
                  checked={eventType === type}
                  onChange={e => setEventType(e.target.value)}
                />
                <span>{type === 'Receipt' ? t('operational.tanks.receipt') : type === 'TransferIn' ? t('operational.tanks.transferIn') : t('operational.tanks.transferOut')}</span>
              </label>
            ))}
          </div>
        </FormField>

        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
          <FormField label={t('operational.tanks.beforeLevelCm')}>
            <input
              type="number"
              step="0.1"
              value={beforeLevelCm}
              onChange={e => setBeforeLevelCm(e.target.value)}
              placeholder="e.g. 180"
              style={{ width: '100%', padding: '10px 14px', borderRadius: 8, background: 'var(--surface-2)', color: 'var(--text)', border: '1px solid var(--border)' }}
            />
          </FormField>

          <FormField label={t('operational.tanks.afterLevelCm') + ' *'}>
            <input
              type="number"
              step="0.1"
              value={afterLevelCm}
              onChange={e => setAfterLevelCm(e.target.value)}
              placeholder="e.g. 400"
              required
              style={{ width: '100%', padding: '10px 14px', borderRadius: 8, background: 'var(--surface-2)', color: 'var(--text)', border: '1px solid var(--border)' }}
            />
          </FormField>
        </div>

        {eventType === 'Receipt' && (
          <FormField label={t('operational.tanks.deliveryNoteLitres')}>
            <input
              type="number"
              step="1"
              value={deliveryNoteLitres}
              onChange={e => setDeliveryNoteLitres(e.target.value)}
              placeholder="e.g. 5000"
              style={{ width: '100%', padding: '10px 14px', borderRadius: 8, background: 'var(--surface-2)', color: 'var(--text)', border: '1px solid var(--border)' }}
            />
          </FormField>
        )}

        <FormField label={t('operational.tanks.photoPrompt')}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
            <input
              type="file"
              accept="image/jpeg,image/webp"
              onChange={handlePhotoUpload}
              disabled={uploading}
              style={{ color: 'var(--text)' }}
            />
            {photoId && <span style={{ color: '#22c55e', fontSize: 13 }}>✓ Attached (ID: {photoId})</span>}
          </div>
        </FormField>

        <Button type="submit" disabled={submitting || uploading} style={{ marginTop: 12 }}>
          {submitting ? t('operational.tanks.submitting') : t('operational.tanks.submitRefill')}
        </Button>
      </form>
    </div>
  );
}
