import { useEffect, useMemo, useState } from 'react';
import { Camera, CheckCircle2, ClipboardList, Droplets, Gauge, Plus, Send, Trash2, Zap } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import client from '../api/client';
import { drawPhotoStamp } from '../utils/photoStamp';

const isoNow = () => new Date().toISOString().slice(0, 16);
const decimal = (value) => (value === '' || value == null ? null : Number(value));

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
  const [references, setReferences] = useState([]);
  const [siteId, setSiteId] = useState('');
  const [lines, setLines] = useState([]);
  const [previousReadings, setPreviousReadings] = useState([]);
  const [captureKind, setCaptureKind] = useState('periodic');
  const [businessDate, setBusinessDate] = useState(() => new Date().toISOString().slice(0, 10));
  const [submissionTime, setSubmissionTime] = useState(isoNow());
  const [clientSentAt] = useState(() => new Date().toISOString());
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => { client.get('/api/operational-captures/references').then((r) => setReferences(r.data)).catch(() => setReferences([])); }, []);
  const site = useMemo(() => references.find((x) => x.subStationId === Number(siteId)), [references, siteId]);
  useEffect(() => {
    if (!siteId) { setPreviousReadings([]); return; }
    client.get('/api/operational-captures/previous-readings', { params: { subStationId: siteId } })
      .then((response) => setPreviousReadings(response.data?.readings || []))
      .catch(() => setPreviousReadings([]));
  }, [siteId]);

  const add = (kind) => {
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
    const produced = lines.filter(line => line.kind === 'generator')
      .reduce((total, line) => total + deltaFor(0, line.subjectId, line.values.energyRaw), 0);
    const sent = lines.filter(line => line.kind === 'feeder')
      .reduce((total, line) => total + deltaFor(2, line.subjectId, line.values.energyRaw, line.values.multiplier), 0);
    return { produced, sent, unexplained: produced - sent };
  }, [captureKind, lines, previousReadings]);
  const submit = async () => {
    if (!site || lines.length === 0) return;
    const invalidReset = lines.some((line) => {
      const counters = line.kind === 'generator'
        ? [{ key: 'energyRaw', subjectType: 0 }, { key: 'hoursRaw', subjectType: 1 }]
        : line.kind === 'feeder' ? [{ key: 'energyRaw', subjectType: 2 }] : [];
      return counters.some(counter => {
        const previous = previousFor(counter.subjectType, line.subjectId, captureKind === 'dailyClosing');
        const resetKey = `${counter.key}Reset`;
        const noteKey = `${counter.key}ResetNote`;
        return previous && line.values[counter.key] !== '' && Number(line.values[counter.key]) < Number(previous.rawValue) && (!line.values[resetKey] || !String(line.values[noteKey] || '').trim());
      });
    });
    if (invalidReset) { window.alert(t('operational.capture.counterResetRequired')); return; }
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
    }
    setSubmitting(true);
    try { await client.post('/api/operational-captures', payload); setLines([]); window.alert(t('operational.capture.success')); }
    catch (error) { window.alert(error.response?.data?.error || t('operational.capture.failed')); }
    finally { setSubmitting(false); }
  };
  const sourceName = (line) => { const list = line.kind === 'generator' ? site?.generators : line.kind === 'tank' ? site?.tanks : site?.feeders; return list?.find((x) => x.id === line.subjectId)?.name || ''; };
  const sourceCode = (line) => {
    const list = line.kind === 'generator' ? site?.generators : line.kind === 'tank' ? site?.tanks : site?.feeders;
    const source = list?.find((x) => x.id === line.subjectId);
    return source?.code || source?.serialNumber || (line.kind === 'generator' ? `G${line.subjectId}` : `${line.kind}-${line.subjectId}`);
  };
  const number = (line, key, label, required = false) => <label>{label}{required && ' *'}<input type="number" step="any" required={required} value={line.values[key] ?? ''} onChange={(e) => updateValues(line.id, { [key]: e.target.value })} /></label>;
  return <section className="capture-page">
    <div className="capture-heading"><div><span className="operational-eyebrow"><Zap size={16} /> {t('operational.capture.eyebrow')}</span><h1>{t('operational.capture.title')}</h1><p>{t('operational.capture.pendingNote')}</p></div><Link className="capture-history-link" to="/operations/history"><ClipboardList size={18} />{t('operational.capture.myHistory')}</Link></div>
    <label className="capture-site">{t('operational.capture.site')}<select value={siteId} onChange={(e) => { setSiteId(e.target.value); setLines([]); }}><option value="">{t('operational.capture.chooseSite')}</option>{references.map((item) => <option key={item.subStationId} value={item.subStationId}>{item.name} ({item.code})</option>)}</select></label>
    {site && <div className="ft-tabs" style={{ marginBottom: 16 }}><button type="button" className={`ft-tab ${captureKind === 'periodic' ? 'active' : ''}`} onClick={() => setCaptureKind('periodic')}>{t('operational.capture.periodicRound')}</button><button type="button" className={`ft-tab ${captureKind === 'dailyClosing' ? 'active' : ''}`} onClick={() => setCaptureKind('dailyClosing')}>{t('operational.capture.dailyClosing')}</button></div>}
    {site && captureKind === 'dailyClosing' && <label className="capture-site">{t('operational.capture.businessDate')}<input type="date" value={businessDate} onChange={(event) => setBusinessDate(event.target.value)} /></label>}
    {site && <label className="capture-site">{t('operational.capture.capturedAt')}<input type="datetime-local" value={submissionTime} onChange={(e) => setSubmissionTime(e.target.value)} /></label>}
    {site && <><div className="capture-adds"><button onClick={() => add('generator')} type="button"><Zap />{t('operational.capture.generator')}</button>{captureKind === 'periodic' && <button onClick={() => add('tank')} type="button"><Droplets />{t('operational.capture.tank')}</button>}<button onClick={() => add('feeder')} type="button"><Gauge />{t('operational.capture.feeder')}</button></div>
      <div className="capture-lines">{lines.map((line) => <BaseLine key={line.id} title={sourceName(line)} code={sourceCode(line)} icon={line.kind === 'tank' ? Droplets : line.kind === 'feeder' ? Gauge : Zap} capturedAt={line.capturedAt || submissionTime} onCapturedAt={(capturedAt) => update(line.id, { capturedAt })} photoIds={line.photoAttachmentIds} onUploaded={(photoId) => update(line.id, { photoAttachmentIds: [photoId] })} onRemove={() => setLines((current) => current.filter((x) => x.id !== line.id))} disabled={submitting} timeZone={site.timeZone || 'Asia/Aden'}>
        {line.kind === 'generator' && <>
          <label className="capture-switch"><input type="checkbox" checked={line.isRunning} onChange={(e) => update(line.id, { isRunning: e.target.checked })}/>{t('operational.capture.running')}</label>
          {line.isRunning && <div className="capture-grid">{number(line, 'voltage', t('operational.capture.voltage'), true)}{number(line, 'current', t('operational.capture.current'), true)}{number(line, 'waterTemp', t('operational.capture.waterTemp'), true)}{number(line, 'oilPressure', t('operational.capture.oilPressure'), true)}{number(line, 'oilTemp', t('operational.capture.oilTemp'), true)}{number(line, 'activeKw', t('operational.capture.activeKw'), true)}{number(line, 'peakKw', t('operational.capture.activeKw'))}{number(line, 'airPressure', t('operational.capture.oilPressure'))}{number(line, 'apparentKva', t('operational.capture.apparentKva'))}{number(line, 'frequency', t('operational.capture.frequency'))}</div>}
          <div className="capture-grid">
            <label>{t('operational.capture.energyCounter')}<input type="number" step="any" value={line.values.energyRaw ?? ''} onChange={(event) => updateValues(line.id, { energyRaw: event.target.value })} /><small>{counterHint(0, line.subjectId, captureKind === 'dailyClosing')}</small>{captureKind === 'dailyClosing' && <small>{closingDifference(0, line.subjectId, line.values.energyRaw)}</small>}{counterWarning(0, line.subjectId, line.values.energyRaw, captureKind === 'dailyClosing') && <><span className="form-error">{counterWarning(0, line.subjectId, line.values.energyRaw, captureKind === 'dailyClosing')}</span><label className="capture-switch"><input type="checkbox" checked={Boolean(line.values.energyRawReset)} onChange={(event) => updateValues(line.id, { energyRawReset: event.target.checked })}/>{t('operational.capture.meterReset')}</label>{line.values.energyRawReset && <input value={line.values.energyRawResetNote || ''} placeholder={t('operational.capture.resetNote')} onChange={(event) => updateValues(line.id, { energyRawResetNote: event.target.value })}/>}</>}</label>
            <label>{t('operational.capture.runningHoursCounter')}<input type="number" step="any" value={line.values.hoursRaw ?? ''} onChange={(event) => updateValues(line.id, { hoursRaw: event.target.value })} /><small>{counterHint(1, line.subjectId, captureKind === 'dailyClosing')}</small>{captureKind === 'dailyClosing' && <small>{closingDifference(1, line.subjectId, line.values.hoursRaw)}</small>}{counterWarning(1, line.subjectId, line.values.hoursRaw, captureKind === 'dailyClosing') && <><span className="form-error">{counterWarning(1, line.subjectId, line.values.hoursRaw, captureKind === 'dailyClosing')}</span><label className="capture-switch"><input type="checkbox" checked={Boolean(line.values.hoursRawReset)} onChange={(event) => updateValues(line.id, { hoursRawReset: event.target.checked })}/>{t('operational.capture.meterReset')}</label>{line.values.hoursRawReset && <input value={line.values.hoursRawResetNote || ''} placeholder={t('operational.capture.resetNote')} onChange={(event) => updateValues(line.id, { hoursRawResetNote: event.target.value })}/>}</>}</label>
          </div>
        </>}
        {line.kind === 'tank' && <div className="capture-grid">{number(line, 'level', t('operational.capture.levelCm'), true)}<label>{t('operational.capture.tank')}<select value={line.values.eventType || '0'} onChange={(e) => updateValues(line.id, { eventType: e.target.value })}><option value="0">{t('operational.capture.levelCm')}</option><option value="1">{t('operational.capture.tank')}</option><option value="2">{t('operational.capture.tank')}</option><option value="3">{t('operational.capture.tank')}</option></select></label>{Number(line.values.eventType || 0) > 0 && <>{number(line, 'beforeLevel', t('operational.capture.levelCm'), true)}{Number(line.values.eventType) === 1 && number(line, 'deliveryNoteLitres', t('operational.capture.rawValue'), true)}<label>{t('operational.capture.tank')}<select value={line.values.linkedClientLineId || ''} onChange={(e) => updateValues(line.id, { linkedClientLineId: e.target.value || null })}><option value="">—</option>{lines.filter((candidate) => candidate.kind === 'tank' && candidate.id !== line.id).map((candidate) => <option key={candidate.id} value={candidate.clientLineId}>{sourceName(candidate)}</option>)}</select></label><div className="capture-photo-row"><PhotoInput disabled={submitting || (line.beforePhotoAttachmentIds?.length || 0) > 0} onUploaded={(photoId) => update(line.id, { beforePhotoAttachmentIds: [photoId] })} stamp={{ capturedAt: line.capturedAt || submissionTime, subjectCode: sourceCode(line), locale: document.documentElement.lang, timeZone: site.timeZone || 'Asia/Aden' }} />{(line.beforePhotoAttachmentIds?.length || 0) > 0 && <span className="capture-photo-ok"><CheckCircle2 size={17} /> {t('operational.capture.photoReady')}</span>}</div></>}</div>}
        {line.kind === 'feeder' && <div className="capture-grid">{number(line, 'activeKw', t('operational.capture.activeKw'))}{number(line, 'apparentKva', t('operational.capture.apparentKva'))}<label>{t('operational.capture.energyCounter')}<input type="number" step="any" value={line.values.energyRaw ?? ''} onChange={(event) => updateValues(line.id, { energyRaw: event.target.value })} /><small>{counterHint(2, line.subjectId, captureKind === 'dailyClosing')}</small>{captureKind === 'dailyClosing' && <small>{closingDifference(2, line.subjectId, line.values.energyRaw, line.values.multiplier)}</small>}{counterWarning(2, line.subjectId, line.values.energyRaw, captureKind === 'dailyClosing') && <><span className="form-error">{counterWarning(2, line.subjectId, line.values.energyRaw, captureKind === 'dailyClosing')}</span><label className="capture-switch"><input type="checkbox" checked={Boolean(line.values.energyRawReset)} onChange={(event) => updateValues(line.id, { energyRawReset: event.target.checked })}/>{t('operational.capture.meterReset')}</label>{line.values.energyRawReset && <input value={line.values.energyRawResetNote || ''} placeholder={t('operational.capture.resetNote')} onChange={(event) => updateValues(line.id, { energyRawResetNote: event.target.value })}/>}</>}</label>{number(line, 'multiplier', t('operational.capture.multiplier'))}</div>}
      </BaseLine>)}</div>
      {dailyPreview && <aside className="capture-summary" aria-live="polite"><strong>{t('operational.capture.dailyPreview')}</strong><span>{t('operational.capture.producedEnergy')}: {dailyPreview.produced.toLocaleString()}</span><span>{t('operational.capture.sentEnergy')}: {dailyPreview.sent.toLocaleString()}</span><span>{t('operational.capture.unexplainedDifference')}: {dailyPreview.unexplained.toLocaleString()}</span><small>{t('operational.capture.previewUntilApproval')}</small></aside>}
      <button type="button" className="capture-submit" disabled={submitting || lines.length === 0} onClick={submit}><Send size={20} />{submitting ? t('operational.capture.submitting') : t('operational.capture.submit')}</button></>}
  </section>;
}
