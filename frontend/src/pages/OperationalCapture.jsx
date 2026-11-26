import { useEffect, useMemo, useState } from 'react';
import { Camera, CheckCircle2, ClipboardList, Droplets, Gauge, Plus, Send, Trash2, Zap, Sun } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import client from '../api/client';
import { drawPhotoStamp } from '../utils/photoStamp';
import OperationalBackLink from '../components/OperationalBackLink';
import useCommercialMode from '../hooks/useCommercialMode';
import CommercialCaptureHub from './CommercialCaptureHub';

const isoNow = () => new Date().toISOString().slice(0, 16);
const decimal = (value) => (value === '' || value == null ? null : Number(value));
const readApiError = (err, fallback) => {
  const data = err?.response?.data;
  const raw = (typeof data === 'string' && data.trim())
    || data?.detail
    || data?.Detail
    || data?.error
    || data?.Error
    || data?.message
    || data?.Message
    || data?.title;
  if (typeof raw === 'string' && raw.trim()) {
    return raw.replace(/\s*\(Parameter '[^']+'\)\.?$/, '').trim();
  }
  return fallback;
};

export async function compressPhoto(file, stamp) {
  const source = await createImageBitmap(file);
  const scale = Math.max(1, Math.max(source.width, source.height) / 960);
  const width = Math.round(source.width / scale);
  const height = Math.round(source.height / scale);
  const canvas = document.createElement('canvas');
  canvas.width = width; canvas.height = height;
  const context = canvas.getContext('2d');
  context.drawImage(source, 0, 0, width, height);
  drawPhotoStamp(context, width, height, stamp.capturedAt, stamp.subjectCode, stamp.locale, stamp.timeZone);
  let quality = 0.86;
  let blob = await new Promise((resolve) => canvas.toBlob(resolve, 'image/jpeg', quality));
  while (blob && blob.size > 65 * 1024 && quality > 0.28) {
    quality -= 0.08;
    blob = await new Promise((resolve) => canvas.toBlob(resolve, 'image/jpeg', quality));
  }
  if (!blob || blob.size > 65 * 1024) throw new Error('Unable to compress this image below 65 KB.');
  return new File([blob], `${file.name.replace(/\.[^.]+$/, '') || 'reading'}.jpg`, { type: 'image/jpeg' });
}

function PhotoInput({ onUploaded, disabled, stamp }) {
  const { t } = useTranslation();
  const [busy, setBusy] = useState(false);
  const [preview, setPreview] = useState(null);
  const change = async (event) => {
    const file = event.target.files?.[0]; if (!file) return;
    setBusy(true);
    try {
      const compressed = await compressPhoto(file, stamp);
      setPreview(URL.createObjectURL(compressed));
      const data = new FormData(); data.append('file', compressed);
      const response = await client.post('/api/attachments', data, { headers: { 'Content-Type': 'multipart/form-data' } });
      onUploaded(response.data.id);
    } catch (error) { window.alert(error.response?.data?.error || error.message); }
    finally { setBusy(false); event.target.value = ''; }
  };
  return <label className="capture-photo">
    <input type="file" accept="image/jpeg,image/webp" capture="environment" onChange={change} disabled={busy || disabled} />
    {preview ? <img src={preview} alt="" /> : <Camera size={22} />}
    <span>{busy ? t('operational.capture.compressing') : t('operational.capture.addPhoto')}</span>
  </label>;
}

function BaseLine({ title, code, icon: Icon, capturedAt, onCapturedAt, photoIds, onUploaded, onRemove, children, disabled, timeZone }) {
  const { t } = useTranslation();
  return <article className="capture-line">
    <header><span className="capture-line-icon"><Icon size={20} /></span><strong>{title}</strong><button type="button" className="capture-remove" onClick={onRemove} aria-label={t('operational.capture.remove')}><Trash2 size={18} /></button></header>
    {/* A line may override the round time; leaving it empty inherits the submission time. */}
    <label>{t('operational.capture.capturedAt')}<input type="datetime-local" value={capturedAt} onChange={(e) => onCapturedAt(e.target.value)} /></label>
    {children}
    <div className="capture-photo-row"><PhotoInput disabled={disabled || photoIds.length > 0} onUploaded={onUploaded} stamp={{ capturedAt, subjectCode: code, locale: document.documentElement.lang, timeZone }} />{photoIds.length > 0 && <span className="capture-photo-ok"><CheckCircle2 size={17} /> {t('operational.capture.photoReady')}</span>}</div>
  </article>;
}

export default function OperationalCapture() {
  const { t } = useTranslation();
  const { isCommercial } = useCommercialMode();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const requestedKind = searchParams.get('kind');
  const requestedGeneratorId = Number(searchParams.get('generatorId'));
  const requestedTankId = Number(searchParams.get('tankId'));
  const isHourlyCommercial = isCommercial && (requestedKind === 'hourly-generator' || requestedKind === 'generator');
  const isDailyGeneratorCommercial = isCommercial && requestedKind === 'daily-generator';
  const isDailyFeedersCommercial = isCommercial && requestedKind === 'daily-feeders';
  const isFocusedCommercial = isHourlyCommercial || isDailyGeneratorCommercial || isDailyFeedersCommercial;
  const [references, setReferences] = useState([]);
  const [siteId, setSiteId] = useState('');
  const [lines, setLines] = useState([]);
  const [previousReadings, setPreviousReadings] = useState([]);
  const [previousTankReadings, setPreviousTankReadings] = useState([]);
  const [previousTankBalances, setPreviousTankBalances] = useState([]);
  const [tankConversions, setTankConversions] = useState({});
  const [fuelTanks, setFuelTanks] = useState(null);
  const [captureKind, setCaptureKind] = useState('periodic');
  const [businessDate, setBusinessDate] = useState(() => new Date().toISOString().slice(0, 10));
  const [submissionTime, setSubmissionTime] = useState(isoNow());
  const [clientSentAt] = useState(() => new Date().toISOString());
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    client.get('/api/operational-captures/references').then((r) => {
      const list = r.data || [];
      setReferences(list);
      if (list.length > 0 && (!siteId || isCommercial)) {
        setSiteId(String(list[0].subStationId));
      }
    }).catch(() => setReferences([]));
  }, [isCommercial]);
  useEffect(() => {
    if (!isCommercial) { setFuelTanks([]); return; }
    let active = true;
    client.get('/api/fuel-tanks')
      .then((response) => {
        if (!active) return;
        const data = response.data;
        setFuelTanks(Array.isArray(data) ? data : (data?.items || []));
      })
      .catch(() => { if (active) setFuelTanks([]); });
    return () => { active = false; };
  }, [isCommercial]);
  const tankLevelSignature = lines
    .filter((line) => line.kind === 'tank')
    .map((line) => `${line.subjectId}:${line.values.level ?? ''}`)
    .join('|');
  useEffect(() => {
    if (!isCommercial) {
      setTankConversions({});
      return undefined;
    }
    const tankLines = lines.filter((line) => line.kind === 'tank');
    if (tankLines.length === 0) return undefined;

    let cancelled = false;
    const immediateUpdates = {};
    const toFetch = [];

    for (const line of tankLines) {
      const tankId = line.subjectId;
      const raw = line.values.level;
      if (raw === '' || raw == null) {
        immediateUpdates[tankId] = null;
        continue;
      }
      const cm = Number(raw);
      const metaMax = (fuelTanks || []).find((ft) => ft.id === tankId)?.maxLevelCm ?? tankConversions[tankId]?.maxLevelCm ?? null;
      if (!Number.isFinite(cm) || cm < 0) {
        immediateUpdates[tankId] = { cm: raw, litres: null, maxLevelCm: metaMax, error: t('operational.commercialCapture.tankLevelNegative'), loading: false };
        continue;
      }
      if (metaMax != null && cm > Number(metaMax)) {
        immediateUpdates[tankId] = { cm, litres: null, maxLevelCm: Number(metaMax), error: t('operational.commercialCapture.tankLevelOutOfRange', { max: metaMax }), loading: false };
        continue;
      }
      immediateUpdates[tankId] = { ...(tankConversions[tankId] || {}), cm, error: null, loading: true };
      toFetch.push({ tankId, cm, metaMax });
    }

    setTankConversions((prev) => {
      const next = { ...prev };
      for (const [id, val] of Object.entries(immediateUpdates)) {
        if (val === null) delete next[id];
        else next[id] = val;
      }
      return next;
    });

    let timer = null;
    if (toFetch.length > 0) {
      timer = setTimeout(() => {
        toFetch.forEach(({ tankId, cm, metaMax }) => {
          client.get(`/api/fuel-tanks/${tankId}/level-to-litres`, { params: { cm } })
            .then((res) => {
              if (cancelled) return;
              const data = res.data;
              setTankConversions((prev) => ({
                ...prev,
                [tankId]: {
                  cm,
                  litres: data?.litres ?? null,
                  maxLevelCm: data?.maxLevelCm ?? metaMax ?? null,
                  calibrationMode: data?.calibrationMode ?? null,
                  litresPerCm: data?.litresPerCm ?? null,
                  error: null,
                  loading: false,
                },
              }));
            })
            .catch((err) => {
              if (cancelled) return;
              const msg = readApiError(err, t('operational.commercialCapture.tankLevelOutOfRange', { max: metaMax ?? '—' }));
              setTankConversions((prev) => ({
                ...prev,
                [tankId]: {
                  cm,
                  litres: null,
                  maxLevelCm: metaMax ?? null,
                  error: msg,
                  loading: false,
                },
              }));
            });
        });
      }, 180);
    }

    return () => {
      cancelled = true;
      if (timer) clearTimeout(timer);
    };
  }, [isCommercial, tankLevelSignature, fuelTanks]);
  const site = useMemo(() => references.find((x) => x.subStationId === Number(siteId)), [references, siteId]);
  useEffect(() => {
    if (!siteId) { setPreviousReadings([]); setPreviousTankReadings([]); setPreviousTankBalances([]); return; }
    client.get('/api/operational-captures/previous-readings', { params: { subStationId: siteId } })
      .then((response) => {
        setPreviousReadings(response.data?.readings || []);
        setPreviousTankReadings(response.data?.tankReadings || []);
        setPreviousTankBalances(response.data?.tankBalances || []);
      })
      .catch(() => { setPreviousReadings([]); setPreviousTankReadings([]); setPreviousTankBalances([]); });
  }, [siteId]);

  const add = (kind) => {
    if (kind === 'solar') {
      const solarMeters = site?.meters?.filter(m => m.meterTypeCode === 'SolarProductionMeter' || m.meterTypeCode === 'SolarMeter') || [];
      if (!site || !solarMeters.length) return;
      const existingIds = new Set(lines.filter(line => line.kind === 'solar').map(line => line.subjectId));
      const sources = solarMeters.filter(source => !existingIds.has(source.id));
      setLines((current) => [...current, ...sources.map(source => ({
        id: crypto.randomUUID(), clientLineId: crypto.randomUUID(), kind: 'solar',
        subjectId: source.id,
        meterId: source.id, capturedAt: '', photoAttachmentIds: [], beforePhotoAttachmentIds: [], isRunning: true,
        values: {},
      }))]);
      return;
    }
    const collection = kind === 'generator' ? site?.generators : kind === 'tank' ? site?.tanks : kind === 'feeder' ? site?.feeders : site?.meters;
    if (!site || !collection?.length) return;
    const existingIds = new Set(lines.filter(line => line.kind === kind).map(line => line.subjectId));
    const sources = kind === 'feeder' || kind === 'generator'
      ? collection.filter(source => !existingIds.has(source.id))
      : collection.slice(0, 1);
    setLines((current) => [...current, ...sources.map(source => ({
      id: crypto.randomUUID(), clientLineId: crypto.randomUUID(), kind,
      subjectId: kind === 'generator' ? source.id : kind === 'tank' ? source.id : kind === 'feeder' ? source.id : source.generatorId || source.id,
      meterId: kind === 'meter' ? source.id : null, capturedAt: '', photoAttachmentIds: [], beforePhotoAttachmentIds: [], isRunning: true,
      values: kind === 'feeder' ? { multiplier: String(source.meterMultiplier || 1) } : {},
    }))]);
  };
  useEffect(() => {
    if (!isFocusedCommercial || !site) return;
    const createLine = (kind, source) => ({
      id: crypto.randomUUID(), clientLineId: crypto.randomUUID(), kind,
      subjectId: source.id, meterId: kind === 'solar' ? source.id : null,
      capturedAt: '', photoAttachmentIds: [], beforePhotoAttachmentIds: [], isRunning: true,
      values: kind === 'feeder' ? { multiplier: String(source.meterMultiplier || 1) } : {},
    });
    if (isHourlyCommercial) {
      setCaptureKind('periodic');
      const generator = site.generators?.find((item) => item.id === requestedGeneratorId);
      setLines(generator ? [createLine('generator', generator)] : []);
      return;
    }
    if (isDailyGeneratorCommercial) {
      setCaptureKind('dailyClosing');
      const generator = site.generators?.find((item) => item.id === requestedGeneratorId);
      const tank = (fuelTanks || []).find((item) => item.id === requestedTankId && (item.tankRole === 2 || item.tankRole === 'Daily') && item.generatorId === requestedGeneratorId);
      setLines(generator && tank ? [createLine('generator', generator), createLine('tank', tank)] : []);
      return;
    }
    if (isDailyFeedersCommercial) {
      setCaptureKind('dailyClosing');
      const feeders = (site.feeders || []).map((item) => createLine('feeder', item));
      const solar = (site.meters || [])
        .filter((item) => item.meterTypeCode === 'SolarProductionMeter' || item.meterTypeCode === 'SolarMeter')
        .map((item) => createLine('solar', item));
      setLines([...feeders, ...solar]);
      return;
    }
  }, [isFocusedCommercial, isHourlyCommercial, isDailyGeneratorCommercial, isDailyFeedersCommercial, requestedGeneratorId, requestedTankId, site, fuelTanks]);
  const update = (id, patch) => setLines((current) => current.map((line) => line.id === id ? { ...line, ...patch } : line));
  const updateValues = (id, patch) => setLines((current) => current.map((line) => line.id === id ? { ...line, values: { ...line.values, ...patch } } : line));
  const previousFor = (subjectType, subjectId, official = false) => {
    const reading = previousReadings.find(item => item.subjectType === subjectType && item.subjectId === subjectId);
    return official ? reading?.latestOfficialDailyClosing : reading?.latestAny;
  };
  const counterWarning = (subjectType, subjectId, rawValue, official = false) => {
    const previousItem = previousReadings.find(item => item.subjectType === subjectType && item.subjectId === subjectId);
    const previous = official ? previousItem?.latestOfficialDailyClosing : previousItem?.latestAny;
    if (rawValue === '' || rawValue == null || !previous) return null;
    if (Number(rawValue) < Number(previous.rawValue)) return t('operational.capture.counterLowerWarning');
    if (previousItem?.recentAverageDelta > 0 && Number(rawValue) - Number(previous.rawValue) > Number(previousItem.recentAverageDelta) * 3)
      return t('operational.capture.counterAbnormalJumpWarning');
    return null;
  };
  const counterHint = (subjectType, subjectId, official = false) => {
    const previous = previousFor(subjectType, subjectId, official);
    return previous ? `${t('operational.capture.lastReading')}: ${previous.rawValue} · ${new Date(previous.capturedAtUtc).toLocaleString()}` : t('operational.capture.noPreviousReading');
  };
  const closingDifference = (subjectType, subjectId, rawValue, multiplier = 1) => {
    const previous = previousFor(subjectType, subjectId, true);
    if (!previous || rawValue === '' || rawValue == null) return t('operational.capture.noPreviousReading');
    return `${t('operational.capture.liveDifference')}: ${((Number(rawValue) - Number(previous.rawValue)) * Number(multiplier || 1)).toLocaleString()}`;
  };
  const dailyPreview = useMemo(() => {
    if (captureKind !== 'dailyClosing') return null;
    const deltaFor = (subjectType, subjectId, rawValue, multiplier = 1) => {
      const previous = previousFor(subjectType, subjectId, true);
      if (!previous || rawValue === '' || rawValue == null || Number(rawValue) < Number(previous.rawValue)) return 0;
      return (Number(rawValue) - Number(previous.rawValue)) * Number(multiplier || 1);
    };
    const producedGen = lines.filter(line => line.kind === 'generator')
      .reduce((total, line) => total + deltaFor(0, line.subjectId, line.values.energyRaw), 0);
    const producedSolar = lines.filter(line => line.kind === 'solar')
      .reduce((total, line) => total + deltaFor(3, line.subjectId, line.values.energyRaw, line.values.multiplier), 0);
    const produced = producedGen + producedSolar;
    const sent = lines.filter(line => line.kind === 'feeder')
      .reduce((total, line) => total + deltaFor(2, line.subjectId, line.values.energyRaw, line.values.multiplier), 0);
    const tankSummaries = isCommercial
      ? lines.filter(line => line.kind === 'tank').map((line) => {
          const tid = line.subjectId;
          const conv = tankConversions[tid];
          const closingLitres = conv?.litres != null ? Number(conv.litres) : null;
          const bal = previousTankBalances.find((b) => b.fuelTankId === tid);
          const prev = previousTankReadings.find((r) => r.fuelTankId === tid);
          const openingLitres = bal?.openingLitres != null
            ? Number(bal.openingLitres)
            : (prev?.openingLitres != null ? Number(prev.openingLitres) : (prev?.computedLitres != null ? Number(prev.computedLitres) : null));
          const transferInLitres = bal?.transferInLitres != null
            ? Number(bal.transferInLitres)
            : (prev?.transferInLitres != null ? Number(prev.transferInLitres) : 0);
          const consumedLitres = openingLitres != null && closingLitres != null
            ? Math.max(0, openingLitres + transferInLitres - closingLitres)
            : null;
          const list = site?.tanks || fuelTanks || [];
          const name = list.find((x) => x.id === tid)?.name || (fuelTanks || []).find((x) => x.id === tid)?.name || '';
          return { tankId: tid, name, openingLitres, transferInLitres, closingLitres, consumedLitres };
        })
      : [];
    return { produced, producedGen, producedSolar, sent, unexplained: produced - sent, tankSummaries };
  }, [captureKind, lines, previousReadings, previousTankBalances, previousTankReadings, tankConversions, isCommercial, fuelTanks, site]);
  const submit = async () => {
    if (!site || lines.length === 0) return;
    const invalidReset = lines.some((line) => {
      const counters = line.kind === 'generator'
        ? [{ key: 'energyRaw', subjectType: 0 }, { key: 'hoursRaw', subjectType: 1 }]
        : line.kind === 'feeder'
          ? [{ key: 'energyRaw', subjectType: 2 }]
          : line.kind === 'solar'
            ? [{ key: 'energyRaw', subjectType: 3 }]
            : [];
      return counters.some(counter => {
        const previous = previousFor(counter.subjectType, line.subjectId, captureKind === 'dailyClosing');
        const resetKey = `${counter.key}Reset`;
        const noteKey = `${counter.key}ResetNote`;
        return previous && line.values[counter.key] !== '' && Number(line.values[counter.key]) < Number(previous.rawValue) && (!line.values[resetKey] || !String(line.values[noteKey] || '').trim());
      });
    });
    if (invalidReset) { window.alert(t('operational.capture.counterResetRequired')); return; }
    const invalidTankLine = lines.find((line) => {
      if (line.kind !== 'tank') return false;
      const raw = line.values.level;
      if (raw === '' || raw == null) return false;
      const cm = Number(raw);
      const max = tankConversions[line.subjectId]?.maxLevelCm ?? (fuelTanks || []).find((ft) => ft.id === line.subjectId)?.maxLevelCm ?? null;
      if (!Number.isFinite(cm) || cm < 0) return true;
      if (max != null && cm > Number(max)) return true;
      return Boolean(tankConversions[line.subjectId]?.error);
    });
    if (invalidTankLine) {
      const max = tankConversions[invalidTankLine.subjectId]?.maxLevelCm ?? (fuelTanks || []).find((ft) => ft.id === invalidTankLine.subjectId)?.maxLevelCm ?? null;
      const msg = tankConversions[invalidTankLine.subjectId]?.error
        || (max != null ? t('operational.commercialCapture.tankLevelOutOfRange', { max }) : t('operational.commercialCapture.tankLevelNegative'));
      window.alert(msg);
      return;
    }
    const payload = { clientRequestId: crypto.randomUUID(), subStationId: site.subStationId, submissionCapturedAtUtc: new Date(submissionTime).toISOString(), clientSentAtUtc: clientSentAt, isOfflineQueue: false, generatorReadings: [], tankLevelReadings: [], feederLoadReadings: [], meterReadings: [] };
    for (const line of lines) {
      const capturedAtUtc = line.capturedAt ? new Date(line.capturedAt).toISOString() : new Date(submissionTime).toISOString();
      if (line.kind === 'generator') {
        payload.generatorReadings.push({ generatorId: line.subjectId, clientLineId: line.clientLineId, capturedAtUtc, photoAttachmentIds: line.photoAttachmentIds, isRunning: line.isRunning, lineVoltageVolts: decimal(line.values.voltage), currentAmps: decimal(line.values.current), waterTemperatureCelsius: decimal(line.values.waterTemp), oilPressure: decimal(line.values.oilPressure), oilTemperatureCelsius: decimal(line.values.oilTemp), activeLoadKw: decimal(line.values.activeKw), apparentLoadKva: decimal(line.values.apparentKva), reactiveLoadKvar: decimal(line.values.reactiveKvar), frequencyHz: decimal(line.values.frequency), oilAddedLitres: decimal(line.values.oilAdded), powerFactor: decimal(line.values.powerFactor), peakLoadKw: decimal(line.values.peakKw), airPressureBar: decimal(line.values.airPressure) });
        for (const counter of [{ key: 'energyRaw', subjectType: 0 }, { key: 'hoursRaw', subjectType: 1 }]) {
          if (line.values[counter.key] !== '' && line.values[counter.key] != null) payload.meterReadings.push({ subjectType: counter.subjectType, subjectId: line.subjectId, meterId: site.generators.find(generator => generator.id === line.subjectId)?.meterId || null, kind: captureKind === 'dailyClosing' ? 1 : 0, clientLineId: crypto.randomUUID(), businessDate: captureKind === 'dailyClosing' ? businessDate : null, capturedAtUtc, photoAttachmentIds: line.photoAttachmentIds, rawValue: decimal(line.values[counter.key]), multiplierSnapshot: 1, isMeterReset: Boolean(line.values[`${counter.key}Reset`]), resetNote: line.values[`${counter.key}ResetNote`] || null });
        }
      }
      if (line.kind === 'tank') payload.tankLevelReadings.push({ fuelTankId: line.subjectId, clientLineId: line.clientLineId, capturedAtUtc, photoAttachmentIds: line.photoAttachmentIds, liquidLevelCm: decimal(line.values.level), eventType: Number(line.values.eventType || 0), linkedClientLineId: line.values.linkedClientLineId || null, beforeLiquidLevelCm: decimal(line.values.beforeLevel), beforePhotoAttachmentIds: line.beforePhotoAttachmentIds || [], deliveryNoteLitres: decimal(line.values.deliveryNoteLitres) });
      if (line.kind === 'feeder') {
        payload.feederLoadReadings.push({ feederId: line.subjectId, clientLineId: line.clientLineId, capturedAtUtc, photoAttachmentIds: line.photoAttachmentIds, activeLoadKw: decimal(line.values.activeKw), apparentLoadKva: decimal(line.values.apparentKva) });
        if (line.values.energyRaw !== '' && line.values.energyRaw != null) payload.meterReadings.push({ subjectType: 2, subjectId: line.subjectId, meterId: site.feeders.find(feeder => feeder.id === line.subjectId)?.meterId || null, kind: captureKind === 'dailyClosing' ? 1 : 0, clientLineId: crypto.randomUUID(), businessDate: captureKind === 'dailyClosing' ? businessDate : null, capturedAtUtc, photoAttachmentIds: line.photoAttachmentIds, rawValue: decimal(line.values.energyRaw), multiplierSnapshot: decimal(line.values.multiplier) || 1, isMeterReset: Boolean(line.values.energyRawReset), resetNote: line.values.energyRawResetNote || null });
      }
      if (line.kind === 'solar') {
        if (line.values.energyRaw !== '' && line.values.energyRaw != null) {
          payload.meterReadings.push({
            subjectType: 3,
            subjectId: line.subjectId,
            meterId: line.subjectId,
            kind: captureKind === 'dailyClosing' ? 1 : 0,
            clientLineId: crypto.randomUUID(),
            businessDate: captureKind === 'dailyClosing' ? businessDate : null,
            capturedAtUtc,
            photoAttachmentIds: line.photoAttachmentIds,
            rawValue: decimal(line.values.energyRaw),
            multiplierSnapshot: decimal(line.values.multiplier) || 1,
            isMeterReset: Boolean(line.values.energyRawReset),
            resetNote: line.values.energyRawResetNote || null
          });
        }
      }
    }
    setSubmitting(true);
    try {
      await client.post('/api/operational-captures', payload);
      setLines([]);
      window.alert(t('operational.capture.success'));
      if (isFocusedCommercial) navigate('/operations/capture');
    }
    catch (error) { window.alert(readApiError(error, t('operational.capture.failed'))); }
    finally { setSubmitting(false); }
  };
  const sourceName = (line) => {
    if (line.kind === 'solar') {
      return site?.meters?.find((x) => x.id === line.subjectId)?.name || t('operational.capture.solarProduction');
    }
    const list = line.kind === 'generator' ? site?.generators : line.kind === 'tank' ? site?.tanks : site?.feeders;
    return list?.find((x) => x.id === line.subjectId)?.name || (fuelTanks || []).find((x) => x.id === line.subjectId)?.name || '';
  };
  const sourceCode = (line) => {
    if (line.kind === 'solar') {
      const source = site?.meters?.find((x) => x.id === line.subjectId);
      return source?.serialNumber || `SOLAR-${line.subjectId}`;
    }
    const list = line.kind === 'generator' ? site?.generators : line.kind === 'tank' ? site?.tanks : site?.feeders;
    const source = list?.find((x) => x.id === line.subjectId) || (fuelTanks || []).find((x) => x.id === line.subjectId);
    return source?.code || source?.serialNumber || (line.kind === 'generator' ? `G${line.subjectId}` : `${line.kind}-${line.subjectId}`);
  };
  const previousTankFor = (tankId) => previousTankReadings.find((item) => item.fuelTankId === tankId);
  const tankLevelHint = (tankId) => {
    const previous = previousTankFor(tankId);
    if (!previous) return t('operational.capture.noPreviousReading');
    const litresText = previous.computedLitres != null ? ` · ${Number(previous.computedLitres).toLocaleString('en-US')} L` : '';
    return `${t('operational.capture.lastReading')}: ${Number(previous.liquidLevelCm).toLocaleString('en-US')} cm${litresText} · ${new Date(previous.capturedAtUtc).toLocaleString('en-US')}`;
  };
  const tankLevelDifference = (tankId, value) => {
    const previous = previousTankFor(tankId);
    if (!previous || value === '' || value == null) return t('operational.capture.noPreviousReading');
    const diffCm = Number(value) - Number(previous.liquidLevelCm);
    const conv = tankConversions[tankId];
    const diffLitres = conv?.litres != null && previous.computedLitres != null
      ? Number(conv.litres) - Number(previous.computedLitres)
      : null;
    return `${t('operational.capture.liveDifference')}: ${diffCm.toLocaleString('en-US')} cm${diffLitres != null ? ` · ${diffLitres.toLocaleString('en-US')} L` : ''}`;
  };
  const number = (line, key, label, required = false) => <label>{label}{required && ' *'}<input type="number" step="any" required={required} value={line.values[key] ?? ''} onChange={(e) => updateValues(line.id, { [key]: e.target.value })} /></label>;
  const dailyTankLine = isDailyGeneratorCommercial ? lines.find((line) => line.kind === 'tank') : null;
  if (isCommercial && !isFocusedCommercial) return <CommercialCaptureHub />;
  const rawGeneratorId = searchParams.get('generatorId');
  const numericGeneratorId = Number(rawGeneratorId);
  const isInvalidGeneratorId = !rawGeneratorId || !Number.isFinite(numericGeneratorId) || numericGeneratorId <= 0;
  const isUnknownGenerator = Boolean(site && (isHourlyCommercial || isDailyGeneratorCommercial) && !site.generators?.some((g) => g.id === numericGeneratorId));
  const isInvalidDailyTank = isDailyGeneratorCommercial && fuelTanks !== null && (!Number.isFinite(requestedTankId) || requestedTankId <= 0 || !fuelTanks.some((tank) => tank.id === requestedTankId && (tank.tankRole === 2 || tank.tankRole === 'Daily') && tank.generatorId === numericGeneratorId));
  if ((isHourlyCommercial || isDailyGeneratorCommercial) && (isInvalidGeneratorId || isUnknownGenerator || isInvalidDailyTank)) {
    return (
      <section className="capture-page">
        <OperationalBackLink />
        <p className="capture-empty">{t('operational.capture.unknownGenerator', 'Unknown generator. The link may be invalid.')}</p>
        <Link to="/operations/capture">{t('operational.capture.backToOperations')}</Link>
      </section>
    );
  }
  return <section className="capture-page">
    <OperationalBackLink />
    <div className="capture-heading"><div><span className="operational-eyebrow"><Zap size={16} /> {t('operational.capture.eyebrow')}</span><h1>{t('operational.capture.title')}</h1><p>{t('operational.capture.pendingNote')}</p></div>{isFocusedCommercial ? <Link className="capture-history-link" to="/operations/capture">{t('operational.capture.backToOperations')}</Link> : <Link className="capture-history-link" to="/operations/history"><ClipboardList size={18} />{t('operational.capture.myHistory')}</Link>}</div>
    {!isCommercial && <label className="capture-site">{t('operational.capture.site')}<select value={siteId} onChange={(e) => { setSiteId(e.target.value); setLines([]); }}><option value="">{t('operational.capture.chooseSite')}</option>{references.map((item) => <option key={item.subStationId} value={item.subStationId}>{item.name} ({item.code})</option>)}</select></label>}
    {site && !isFocusedCommercial && <div className="ft-tabs" style={{ marginBottom: 16 }}><button type="button" className={`ft-tab ${captureKind === 'periodic' ? 'active' : ''}`} onClick={() => setCaptureKind('periodic')}>{isCommercial ? t('operational.commercialCapture.hourlyTab') : t('operational.capture.periodicRound')}</button><button type="button" className={`ft-tab ${captureKind === 'dailyClosing' ? 'active' : ''}`} onClick={() => setCaptureKind('dailyClosing')}>{t('operational.capture.dailyClosing')}</button></div>}
    {site && captureKind === 'dailyClosing' && <label className="capture-site">{t('operational.capture.businessDate')}<input type="date" value={businessDate} onChange={(event) => setBusinessDate(event.target.value)} /></label>}
    {site && <label className="capture-site">{t('operational.capture.capturedAt')}<input type="datetime-local" value={submissionTime} onChange={(e) => setSubmissionTime(e.target.value)} /></label>}
    {site && <>{!isFocusedCommercial && <div className="capture-adds">
      <button onClick={() => add('generator')} type="button"><Zap />{t('operational.capture.generator')}</button>
      <button onClick={() => add('tank')} type="button"><Droplets />{captureKind === 'dailyClosing' ? (t('operational.capture.dailyTankClosing', 'خزانات الوقود (إقفال)')) : t('operational.capture.tank')}</button>
      <button onClick={() => add('feeder')} type="button"><Gauge />{isCommercial ? (t('sidebar.meters', 'عداد الفيدر')) : t('operational.capture.feeder')}</button>
      {site?.meters?.some(m => m.meterTypeCode === 'SolarProductionMeter' || m.meterTypeCode === 'SolarMeter') && (
        <button onClick={() => add('solar')} type="button"><Sun />{t('operational.capture.solarProduction')}</button>
      )}
    </div>}
      <div className="capture-lines">{lines.filter((line) => !isDailyGeneratorCommercial || line.kind !== 'tank').map((line) => <BaseLine key={line.id} title={sourceName(line)} code={sourceCode(line)} icon={line.kind === 'tank' ? Droplets : line.kind === 'feeder' ? Gauge : line.kind === 'solar' ? Sun : Zap} capturedAt={line.capturedAt || submissionTime} onCapturedAt={(capturedAt) => update(line.id, { capturedAt })} photoIds={line.photoAttachmentIds} onUploaded={(photoId) => update(line.id, { photoAttachmentIds: [photoId] })} onRemove={() => setLines((current) => isDailyGeneratorCommercial ? [] : current.filter((x) => x.id !== line.id))} disabled={submitting} timeZone={site.timeZone || 'Asia/Aden'}>
        {line.kind === 'generator' && <>
          {!isHourlyCommercial && <label className="capture-switch"><input type="checkbox" checked={line.isRunning} onChange={(e) => update(line.id, { isRunning: e.target.checked })}/>{t('operational.capture.running')}</label>}
          {!isDailyGeneratorCommercial && (isHourlyCommercial || line.isRunning) && <div className="capture-grid">{number(line, 'voltage', t('operational.capture.voltage'), true)}{number(line, 'current', t('operational.capture.current'), true)}{number(line, 'waterTemp', t('operational.capture.waterTemp'), true)}{number(line, 'oilPressure', t('operational.capture.oilPressure'), true)}{number(line, 'oilTemp', t('operational.capture.oilTemp'), true)}{number(line, 'activeKw', t('operational.capture.activeKw'), true)}{!isHourlyCommercial && <>{number(line, 'peakKw', t('operational.capture.peakKw'))}{number(line, 'airPressure', t('operational.capture.airPressure'))}{number(line, 'apparentKva', t('operational.capture.apparentKva'))}{number(line, 'frequency', t('operational.capture.frequency'))}</>}</div>}
          {!isHourlyCommercial && <div className="capture-grid">
            <label>{t('operational.capture.energyCounter')}<input type="number" step="any" value={line.values.energyRaw ?? ''} onChange={(event) => updateValues(line.id, { energyRaw: event.target.value })} /><small>{counterHint(0, line.subjectId, captureKind === 'dailyClosing')}</small>{captureKind === 'dailyClosing' && <small>{closingDifference(0, line.subjectId, line.values.energyRaw)}</small>}{counterWarning(0, line.subjectId, line.values.energyRaw, captureKind === 'dailyClosing') && <><span className="form-error">{counterWarning(0, line.subjectId, line.values.energyRaw, captureKind === 'dailyClosing')}</span><label className="capture-switch"><input type="checkbox" checked={Boolean(line.values.energyRawReset)} onChange={(event) => updateValues(line.id, { energyRawReset: event.target.checked })}/>{t('operational.capture.meterReset')}</label>{line.values.energyRawReset && <input value={line.values.energyRawResetNote || ''} placeholder={t('operational.capture.resetNote')} onChange={(event) => updateValues(line.id, { energyRawResetNote: event.target.value })}/>}</>}</label>
            <label>{t('operational.capture.runningHoursCounter')}<input type="number" step="any" value={line.values.hoursRaw ?? ''} onChange={(event) => updateValues(line.id, { hoursRaw: event.target.value })} /><small>{counterHint(1, line.subjectId, captureKind === 'dailyClosing')}</small>{captureKind === 'dailyClosing' && <small>{closingDifference(1, line.subjectId, line.values.hoursRaw)}</small>}{counterWarning(1, line.subjectId, line.values.hoursRaw, captureKind === 'dailyClosing') && <><span className="form-error">{counterWarning(1, line.subjectId, line.values.hoursRaw, captureKind === 'dailyClosing')}</span><label className="capture-switch"><input type="checkbox" checked={Boolean(line.values.hoursRawReset)} onChange={(event) => updateValues(line.id, { hoursRawReset: event.target.checked })}/>{t('operational.capture.meterReset')}</label>{line.values.hoursRawReset && <input value={line.values.hoursRawResetNote || ''} placeholder={t('operational.capture.resetNote')} onChange={(event) => updateValues(line.id, { hoursRawResetNote: event.target.value })}/>}</>}</label>
          </div>}
          {isDailyGeneratorCommercial && dailyTankLine && (() => {
            const tid = dailyTankLine.subjectId;
            const conv = tankConversions[tid];
            const metaMax = conv?.maxLevelCm ?? (fuelTanks || []).find((ft) => ft.id === tid)?.maxLevelCm ?? undefined;
            const hasRate = conv?.litresPerCm != null && Number(conv.litresPerCm) > 0;
            return (
              <div className="capture-grid">
                <label>
                  {`${t('operational.commercialCapture.dailyTankLevelCm')} — ${sourceName(dailyTankLine)}`} *
                  <input
                    type="number"
                    min="0"
                    max={metaMax}
                    step="any"
                    required
                    value={dailyTankLine.values.level ?? ''}
                    onChange={(event) => updateValues(dailyTankLine.id, { level: event.target.value })}
                  />
                  {conv?.loading && <small>{t('operational.commercialCapture.loadingConversion')}</small>}
                  {conv?.error && <span className="form-error">{conv.error}</span>}
                  {!conv?.error && conv?.litres != null && (
                    <small>
                      {t('operational.commercialCapture.approxLitres', { litres: Number(conv.litres).toLocaleString('en-US') })}
                      {hasRate ? ` (${t('operational.commercialCapture.calibrationRateDisplay', { rate: conv.litresPerCm })})` : ''}
                    </small>
                  )}
                  <small>{tankLevelHint(tid)}</small>
                  <small>{tankLevelDifference(tid, dailyTankLine.values.level)}</small>
                </label>
              </div>
            );
          })()}
        </>}
        {line.kind === 'tank' && <div className="capture-grid">
          <label>{t('operational.capture.levelCm')} *<input type="number" min="0" max={tankConversions[line.subjectId]?.maxLevelCm ?? (fuelTanks || []).find((ft) => ft.id === line.subjectId)?.maxLevelCm ?? undefined} step="any" required value={line.values.level ?? ''} onChange={(e) => updateValues(line.id, { level: e.target.value })} />{isCommercial && tankConversions[line.subjectId]?.error && <span className="form-error">{tankConversions[line.subjectId].error}</span>}{isCommercial && !tankConversions[line.subjectId]?.error && tankConversions[line.subjectId]?.litres != null && <small>{t('operational.commercialCapture.approxLitres', { litres: Number(tankConversions[line.subjectId].litres).toLocaleString('en-US') })}</small>}<small>{tankLevelHint(line.subjectId)}</small><small>{tankLevelDifference(line.subjectId, line.values.level)}</small></label>
          {!isDailyGeneratorCommercial && <>
            <label>{t('operational.capture.tankEventType')}<select value={line.values.eventType || '0'} onChange={(e) => updateValues(line.id, { eventType: e.target.value })}><option value="0">{t('operational.capture.tankEventLevel')}</option><option value="1">{t('operational.capture.tankEventDelivery')}</option><option value="2">{t('operational.capture.tankEventTransferOut')}</option><option value="3">{t('operational.capture.tankEventTransferIn')}</option></select></label>
            {Number(line.values.eventType || 0) > 0 && <>{number(line, 'beforeLevel', t('operational.capture.beforeLevelCm'), true)}{Number(line.values.eventType) === 1 && number(line, 'deliveryNoteLitres', t('operational.capture.rawValue'), true)}<label>{t('operational.capture.linkedTank')}<select value={line.values.linkedClientLineId || ''} onChange={(e) => updateValues(line.id, { linkedClientLineId: e.target.value || null })}><option value="">—</option>{lines.filter((candidate) => candidate.kind === 'tank' && candidate.id !== line.id).map((candidate) => <option key={candidate.id} value={candidate.clientLineId}>{sourceName(candidate)}</option>)}</select></label><div className="capture-photo-row"><PhotoInput disabled={submitting || (line.beforePhotoAttachmentIds?.length || 0) > 0} onUploaded={(photoId) => update(line.id, { beforePhotoAttachmentIds: [photoId] })} stamp={{ capturedAt: line.capturedAt || submissionTime, subjectCode: sourceCode(line), locale: document.documentElement.lang, timeZone: site.timeZone || 'Asia/Aden' }} />{(line.beforePhotoAttachmentIds?.length || 0) > 0 && <span className="capture-photo-ok"><CheckCircle2 size={17} /> {t('operational.capture.photoReady')}</span>}</div></>}
          </>}
        </div>}
        {line.kind === 'feeder' && <div className="capture-grid">{!isDailyFeedersCommercial && <>{number(line, 'activeKw', t('operational.capture.activeKw'))}{number(line, 'apparentKva', t('operational.capture.apparentKva'))}</>}<label>{t('operational.capture.energyCounter')}<input type="number" step="any" value={line.values.energyRaw ?? ''} onChange={(event) => updateValues(line.id, { energyRaw: event.target.value })} /><small>{counterHint(2, line.subjectId, captureKind === 'dailyClosing')}</small>{captureKind === 'dailyClosing' && <small>{closingDifference(2, line.subjectId, line.values.energyRaw, line.values.multiplier)}</small>}{counterWarning(2, line.subjectId, line.values.energyRaw, captureKind === 'dailyClosing') && <><span className="form-error">{counterWarning(2, line.subjectId, line.values.energyRaw, captureKind === 'dailyClosing')}</span><label className="capture-switch"><input type="checkbox" checked={Boolean(line.values.energyRawReset)} onChange={(event) => updateValues(line.id, { energyRawReset: event.target.checked })}/>{t('operational.capture.meterReset')}</label>{line.values.energyRawReset && <input value={line.values.energyRawResetNote || ''} placeholder={t('operational.capture.resetNote')} onChange={(event) => updateValues(line.id, { energyRawResetNote: event.target.value })}/>}</>}</label>{!isDailyFeedersCommercial && number(line, 'multiplier', t('operational.capture.multiplier'))}</div>}
        {line.kind === 'solar' && <div className="capture-grid"><label>{t('operational.capture.solarCumulativeCounter')} *<input type="number" step="any" required value={line.values.energyRaw ?? ''} onChange={(event) => updateValues(line.id, { energyRaw: event.target.value })} /><small>{counterHint(3, line.subjectId, captureKind === 'dailyClosing')}</small>{captureKind === 'dailyClosing' && <small>{closingDifference(3, line.subjectId, line.values.energyRaw, line.values.multiplier || 1)}</small>}{counterWarning(3, line.subjectId, line.values.energyRaw, captureKind === 'dailyClosing') && <><span className="form-error">{counterWarning(3, line.subjectId, line.values.energyRaw, captureKind === 'dailyClosing')}</span><label className="capture-switch"><input type="checkbox" checked={Boolean(line.values.energyRawReset)} onChange={(event) => updateValues(line.id, { energyRawReset: event.target.checked })}/>{t('operational.capture.meterReset')}</label>{line.values.energyRawReset && <input value={line.values.energyRawResetNote || ''} placeholder={t('operational.capture.resetNote')} onChange={(event) => updateValues(line.id, { energyRawResetNote: event.target.value })}/>}</>}</label>{number(line, 'multiplier', t('operational.capture.multiplier'))}</div>}
      </BaseLine>)}</div>
      {dailyPreview && <aside className="capture-summary" aria-live="polite">
        <strong>{t('operational.capture.dailyPreview')}</strong>
        <span>{t('operational.capture.producedEnergy')}: {dailyPreview.produced.toLocaleString()}{dailyPreview.producedSolar > 0 ? ` (${t('operational.capture.generator')}: ${dailyPreview.producedGen.toLocaleString()} + ☀️: ${dailyPreview.producedSolar.toLocaleString()})` : ''}</span>
        <span>{t('operational.capture.sentEnergy')}: {dailyPreview.sent.toLocaleString()}</span>
        <span>{t('operational.balance.unexplained', 'الفاقد والاستهلاك المساعد للمحطة')}: {dailyPreview.unexplained.toLocaleString()}</span>
        {dailyPreview.tankSummaries?.filter((ts) => ts.closingLitres != null).map((ts) => (
          <div key={ts.tankId} style={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
            <span>{ts.name}: {t('operational.commercialCapture.closingStock')}: {`${ts.closingLitres.toLocaleString('en-US')} L`}</span>
            {ts.openingLitres != null && (
              <small>
                {t('operational.commercialCapture.openingStock')}: {ts.openingLitres.toLocaleString('en-US')} L · {t('operational.commercialCapture.transferInToday')}: {ts.transferInLitres.toLocaleString('en-US')} L
                {ts.consumedLitres != null && ` · ${t('operational.commercialCapture.dailyFuelConsumed')}: ${ts.consumedLitres.toLocaleString('en-US')} L`}
              </small>
            )}
          </div>
        ))}
        <small>{t('operational.capture.previewUntilApproval')}</small>
      </aside>}
      <button type="button" className="capture-submit" disabled={submitting || lines.length === 0} onClick={submit}><Send size={20} />{submitting ? t('operational.capture.submitting') : t('operational.capture.submit')}</button></>}
  </section>;
}
