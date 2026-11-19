import React, { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { ArrowRightLeft, Camera, CheckCircle, AlertTriangle, Droplet } from 'lucide-react';
import client from '../api/client';
import { PageHeader, Button, FormField } from '../components/ui';
import OperationalBackLink from '../components/OperationalBackLink';
import useCommercialMode from '../hooks/useCommercialMode';
import GeneralMovementLog from './movements/GeneralMovementLog';

export default function TankRefillTransfer() {
  const { t, i18n } = useTranslation();
  const { isCommercial } = useCommercialMode();
  const [activeTab, setActiveTab] = useState('entry'); // 'entry' | 'log'
  const [tanks, setTanks] = useState([]);
  const [oils, setOils] = useState([]);
  const [tankId, setTankId] = useState('');
  const [eventType, setEventType] = useState('Receipt'); // Receipt, TransferIn, TransferOut, OilReceipt
  const [beforeLevelCm, setBeforeLevelCm] = useState('');
  const [afterLevelCm, setAfterLevelCm] = useState('');
  const [deliveryNoteLitres, setDeliveryNoteLitres] = useState('');
  const [photoId, setPhotoId] = useState(null);
  const [uploading, setUploading] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [successMsg, setSuccessMsg] = useState('');
  const [errorMsg, setErrorMsg] = useState('');

  // Paired transfer state for commercial mode
  const [destTankId, setDestTankId] = useState('');
  const [destBeforeLevelCm, setDestBeforeLevelCm] = useState('');
  const [destAfterLevelCm, setDestAfterLevelCm] = useState('');

  // Oil supply fields
  const [oilLookupId, setOilLookupId] = useState('');
  const [oilReference, setOilReference] = useState('');
  const [oilSupplier, setOilSupplier] = useState('');
  const [oilNotes, setOilNotes] = useState('');

  useEffect(() => {
    client.get('/api/fuel-tanks').then(res => {
      const list = res.data?.items || res.data || [];
      setTanks(list);
    }).catch(() => {});

    client.get('/api/oils').then(res => {
      const list = res.data?.items || res.data || [];
      setOils(list);
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

    if (eventType === 'OilReceipt') {
      if (!oilLookupId || !deliveryNoteLitres) {
        setErrorMsg(t('operational.tanks.fillRequiredOil', 'يرجى اختيار نوع الزيت وتحديد كمية التوريد'));
        return;
      }
      setSubmitting(true);
      setErrorMsg('');
      setSuccessMsg('');
      try {
        const payload = {
          subStationId: 0,
          oilLookupId: parseInt(oilLookupId, 10),
          quantityLiters: parseFloat(deliveryNoteLitres),
          reference: oilReference || null,
          supplierCompany: oilSupplier || null,
          transactionDate: new Date().toISOString(),
          notes: oilNotes || null,
        };
        await client.post('/api/oil-transactions', payload);
        setSuccessMsg(t('operational.tanks.successOilRefill', 'تم تسجيل توريد الزيت بنجاح'));
        setOilLookupId('');
        setOilReference('');
        setOilSupplier('');
        setOilNotes('');
        setDeliveryNoteLitres('');
        setPhotoId(null);
      } catch (err) {
        setErrorMsg(err.response?.data?.error || err.response?.data?.msg || err.message || 'Error recording oil refill');
      } finally {
        setSubmitting(false);
      }
      return;
    }

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

      const clientReqId = typeof crypto !== 'undefined' && crypto.randomUUID ? crypto.randomUUID() : 'req-' + Date.now();
      const mainLineId = typeof crypto !== 'undefined' && crypto.randomUUID ? crypto.randomUUID() : 'line-' + Date.now();

      let tankLevelReadings = [];

      if (isCommercial && eventType === 'TransferIn' && destTankId) {
        // Paired transfer: Main Tank (TransferOut) -> Daily Tank (TransferIn)
        const destLineId = typeof crypto !== 'undefined' && crypto.randomUUID ? crypto.randomUUID() : 'dest-' + Date.now();
        tankLevelReadings = [
          {
            fuelTankId: parseInt(tankId, 10), // Main Tank
            clientLineId: mainLineId,
            linkedClientLineId: destLineId,
            liquidLevelCm: parseFloat(afterLevelCm),
            beforeLiquidLevelCm: beforeLevelCm ? parseFloat(beforeLevelCm) : null,
            eventType: 2, // TransferOut
            photoAttachmentIds: photoId ? [photoId] : [],
            beforePhotoAttachmentIds: photoId ? [photoId] : []
          },
          {
            fuelTankId: parseInt(destTankId, 10), // Daily Tank
            clientLineId: destLineId,
            linkedClientLineId: mainLineId,
            liquidLevelCm: parseFloat(destAfterLevelCm || afterLevelCm),
            beforeLiquidLevelCm: destBeforeLevelCm ? parseFloat(destBeforeLevelCm) : null,
            eventType: 3, // TransferIn
            photoAttachmentIds: photoId ? [photoId] : [],
            beforePhotoAttachmentIds: photoId ? [photoId] : []
          }
        ];
      } else {
        const evCode = eventType === 'Receipt' ? 1 : eventType === 'TransferOut' ? 2 : 3;
        tankLevelReadings = [
          {
            fuelTankId: parseInt(tankId, 10),
            clientLineId: mainLineId,
            liquidLevelCm: parseFloat(afterLevelCm),
            beforeLiquidLevelCm: beforeLevelCm ? parseFloat(beforeLevelCm) : null,
            deliveryNoteLitres: deliveryNoteLitres ? parseFloat(deliveryNoteLitres) : null,
            eventType: evCode,
            photoAttachmentIds: photoId ? [photoId] : [],
            beforePhotoAttachmentIds: photoId ? [photoId] : []
          }
        ];
      }

      const payload = {
        subStationId: sId,
        clientRequestId: clientReqId,
        capturedAtUtc: new Date().toISOString(),
        tankLevelReadings,
        generatorReadings: [],
        feederLoadReadings: [],
        meterReadings: []
      };

      await client.post('/api/operational-captures', payload);
      setSuccessMsg(t('operational.tanks.successRefill'));
      setBeforeLevelCm('');
      setAfterLevelCm('');
      setDestBeforeLevelCm('');
      setDestAfterLevelCm('');
      setDeliveryNoteLitres('');
      setPhotoId(null);
    } catch (err) {
      setErrorMsg(err.response?.data?.error || err.message || 'Error recording refill');
    } finally {
      setSubmitting(false);
    }
  };

  const mainTanks = tanks.filter(t => t.tankRole === 1 || !t.parentTankId);
  const dailyTanks = tanks.filter(t => t.tankRole === 2 || t.parentTankId != null || t.generatorId != null);

  return (
    <div className="capture-page operational-shell" style={{ maxWidth: activeTab === 'log' ? 1100 : 800, margin: '0 auto', padding: 24 }}>
      <OperationalBackLink />
      <PageHeader
        title={t('operational.tanks.refillTitle')}
        subtitle={t('operational.tanks.refillSubtitle')}
      />

      {/* Tabs */}
      <div className="tab-group" style={{ display: 'flex', gap: 12, marginBottom: 20, borderBottom: '1px solid var(--border)' }}>
        <button
          type="button"
          onClick={() => setActiveTab('entry')}
          style={{
            padding: '10px 18px',
            border: 'none',
            borderBottom: activeTab === 'entry' ? '2px solid var(--primary)' : '2px solid transparent',
            background: 'transparent',
            fontWeight: activeTab === 'entry' ? 700 : 500,
            color: activeTab === 'entry' ? 'var(--primary)' : 'var(--text-muted)',
            cursor: 'pointer',
            fontSize: '1rem',
          }}
        >
          {t('operational.tanks.tabEntry', 'تسجيل التوريد / التحويل')}
        </button>
        <button
          type="button"
          onClick={() => setActiveTab('log')}
          style={{
            padding: '10px 18px',
            border: 'none',
            borderBottom: activeTab === 'log' ? '2px solid var(--primary)' : '2px solid transparent',
            background: 'transparent',
            fontWeight: activeTab === 'log' ? 700 : 500,
            color: activeTab === 'log' ? 'var(--primary)' : 'var(--text-muted)',
            cursor: 'pointer',
            fontSize: '1rem',
          }}
        >
          {t('operational.tanks.tabLog', 'سجل الحركات والتوريدات العام')}
        </button>
      </div>

      {activeTab === 'log' ? (
        <GeneralMovementLog lang={i18n.language} />
      ) : (
        <>
          {successMsg && (
            <div style={{ padding: 12, borderRadius: 'var(--radius-xs)', background: 'var(--success-subtle)', border: '1px solid var(--success)', color: 'var(--success)', marginBottom: 16, display: 'flex', alignItems: 'center', gap: 8 }}>
              <CheckCircle size={18} /> {successMsg}
            </div>
          )}
          {errorMsg && (
            <div style={{ padding: 12, borderRadius: 'var(--radius-xs)', background: 'var(--danger-subtle)', border: '1px solid var(--danger)', color: 'var(--danger)', marginBottom: 16, display: 'flex', alignItems: 'center', gap: 8 }}>
              <AlertTriangle size={18} /> {errorMsg}
            </div>
          )}

          <form onSubmit={handleSubmit} style={{ display: 'grid', gap: 16 }}>
            <FormField label={t('operational.tanks.eventType')}>
              <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap' }}>
                {(isCommercial ? ['Receipt', 'TransferIn', 'OilReceipt'] : ['Receipt', 'TransferIn', 'TransferOut', 'OilReceipt']).map(type => (
                  <label key={type} style={{ display: 'flex', alignItems: 'center', gap: 6, cursor: 'pointer' }}>
                    <input
                      type="radio"
                      name="eventType"
                      value={type}
                      checked={eventType === type}
                      onChange={e => setEventType(e.target.value)}
                    />
                    <span>
                      {type === 'Receipt'
                        ? (isCommercial ? t('operational.tanks.receiptFuel', 'توريد وقود خارجي / استلام') : t('operational.tanks.receipt'))
                        : type === 'TransferIn'
                          ? (isCommercial ? t('operational.tanks.transferMainToDaily', 'تحويل وقود (رئيسي إلى يومي)') : t('operational.tanks.transferIn'))
                          : type === 'OilReceipt'
                            ? t('operational.tanks.receiptOil', 'توريد زيت خارجي')
                            : t('operational.tanks.transferOut')}
                    </span>
                  </label>
                ))}
              </div>
            </FormField>

            {eventType === 'OilReceipt' ? (
              <>
                <FormField label={t('operational.tanks.oilType', 'نوع الزيت / خزان الزيت') + ' *'}>
                  <select
                    value={oilLookupId}
                    onChange={e => setOilLookupId(e.target.value)}
                    style={{ width: '100%', padding: '10px 14px', borderRadius: 8, background: 'var(--surface-2)', color: 'var(--text)', border: '1px solid var(--border)' }}
                    required
                  >
                    <option value="">-- {t('operational.tanks.selectOil', 'اختر نوع الزيت')} --</option>
                    {oils.map(o => (
                      <option key={o.id || o.oilLookupId} value={o.oilLookupId || o.id}>
                        {o.oilLookupName || o.name || `Oil #${o.id}`} {o.currentStock != null ? `(${o.currentStock} L)` : ''}
                      </option>
                    ))}
                  </select>
                </FormField>

                <FormField label={t('operational.tanks.deliveryNoteLitres', 'كمية سند التوريد (لتر)') + ' *'}>
                  <input
                    type="number"
                    step="0.1"
                    value={deliveryNoteLitres}
                    onChange={e => setDeliveryNoteLitres(e.target.value)}
                    placeholder="e.g. 200"
                    required
                    style={{ width: '100%', padding: '10px 14px', borderRadius: 8, background: 'var(--surface-2)', color: 'var(--text)', border: '1px solid var(--border)' }}
                  />
                </FormField>

                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
                  <FormField label={t('operational.tanks.reference', 'رقم سند التوريد / الفاتورة')}>
                    <input
                      type="text"
                      value={oilReference}
                      onChange={e => setOilReference(e.target.value)}
                      placeholder="e.g. INV-10023"
                      style={{ width: '100%', padding: '10px 14px', borderRadius: 8, background: 'var(--surface-2)', color: 'var(--text)', border: '1px solid var(--border)' }}
                    />
                  </FormField>

                  <FormField label={t('operational.tanks.supplier', 'الجهة الموردة')}>
                    <input
                      type="text"
                      value={oilSupplier}
                      onChange={e => setOilSupplier(e.target.value)}
                      placeholder="e.g. شركة النفط"
                      style={{ width: '100%', padding: '10px 14px', borderRadius: 8, background: 'var(--surface-2)', color: 'var(--text)', border: '1px solid var(--border)' }}
                    />
                  </FormField>
                </div>

                <FormField label={t('operational.tanks.notes', 'ملاحظات')}>
                  <input
                    type="text"
                    value={oilNotes}
                    onChange={e => setOilNotes(e.target.value)}
                    placeholder="e.g. توريد براميل زيت 15W-40"
                    style={{ width: '100%', padding: '10px 14px', borderRadius: 8, background: 'var(--surface-2)', color: 'var(--text)', border: '1px solid var(--border)' }}
                  />
                </FormField>
              </>
            ) : (
              <>
                <FormField label={isCommercial && eventType === 'TransferIn' ? (t('operational.tanks.sourceMainTank', 'الخزان الرئيسي (المصدر)')) : t('operational.tanks.tank')}>
                  <select
                    value={tankId}
                    onChange={e => setTankId(e.target.value)}
                    style={{ width: '100%', padding: '10px 14px', borderRadius: 8, background: 'var(--surface-2)', color: 'var(--text)', border: '1px solid var(--border)' }}
                    required
                  >
                    <option value="">-- {t('operational.tanks.selectTank')} --</option>
                    {(isCommercial && eventType === 'Receipt' ? mainTanks : tanks).map(t => (
                      <option key={t.id} value={t.id}>
                        {t.name}{isCommercial ? '' : ` (${t.subStationName || `Site ${t.subStationId}`})`}
                      </option>
                    ))}
                  </select>
                </FormField>

                {isCommercial && eventType === 'TransferIn' && (
                  <FormField label={t('operational.tanks.destDailyTank', 'الخزان اليومي (الوجهة)')}>
                    <select
                      value={destTankId}
                      onChange={e => setDestTankId(e.target.value)}
                      style={{ width: '100%', padding: '10px 14px', borderRadius: 8, background: 'var(--surface-2)', color: 'var(--text)', border: '1px solid var(--border)' }}
                      required
                    >
                      <option value="">-- {t('operational.tanks.selectDailyTank', 'اختر الخزان اليومي')} --</option>
                      {dailyTanks.map(t => (
                        <option key={t.id} value={t.id}>{t.name}</option>
                      ))}
                    </select>
                  </FormField>
                )}

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

                {isCommercial && eventType === 'TransferIn' && destTankId && (
                  <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
                    <FormField label={t('operational.tanks.destBeforeLevelCm', 'مستوى اليومي قبل (سم)')}>
                      <input
                        type="number"
                        step="0.1"
                        value={destBeforeLevelCm}
                        onChange={e => setDestBeforeLevelCm(e.target.value)}
                        placeholder="e.g. 50"
                        style={{ width: '100%', padding: '10px 14px', borderRadius: 8, background: 'var(--surface-2)', color: 'var(--text)', border: '1px solid var(--border)' }}
                      />
                    </FormField>

                    <FormField label={t('operational.tanks.destAfterLevelCm', 'مستوى اليومي بعد (سم)')}>
                      <input
                        type="number"
                        step="0.1"
                        value={destAfterLevelCm}
                        onChange={e => setDestAfterLevelCm(e.target.value)}
                        placeholder="e.g. 150"
                        style={{ width: '100%', padding: '10px 14px', borderRadius: 8, background: 'var(--surface-2)', color: 'var(--text)', border: '1px solid var(--border)' }}
                      />
                    </FormField>
                  </div>
                )}

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
              </>
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
                {photoId && <span style={{ color: 'var(--success)', fontSize: 13 }}>✓ Attached (ID: {photoId})</span>}
              </div>
            </FormField>

            <Button type="submit" disabled={submitting || uploading} style={{ marginTop: 12 }}>
              {submitting
                ? t('operational.tanks.submitting')
                : eventType === 'OilReceipt'
                  ? t('operational.tanks.submitOilRefill', 'تسجيل توريد الزيت')
                  : t('operational.tanks.submitRefill')}
            </Button>
          </form>
        </>
      )}
    </div>
  );
}
