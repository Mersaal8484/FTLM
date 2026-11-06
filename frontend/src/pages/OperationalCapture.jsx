import { useEffect, useMemo, useState } from 'react';
import { Camera, CheckCircle2, ClipboardList, Droplets, Gauge, Plus, Send, Trash2, Zap } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import client from '../api/client';

const isoNow = () => new Date().toISOString().slice(0, 16);
const decimal = (value) => (value === '' || value == null ? null : Number(value));

async function compressPhoto(file) {
  const source = await createImageBitmap(file);
  const scale = Math.max(1, Math.max(source.width, source.height) / 960);
  const width = Math.round(source.width / scale);
  const height = Math.round(source.height / scale);
  const canvas = document.createElement('canvas');
  canvas.width = width; canvas.height = height;
  canvas.getContext('2d').drawImage(source, 0, 0, width, height);
  let quality = 0.86;
  let blob = await new Promise((resolve) => canvas.toBlob(resolve, 'image/jpeg', quality));
  while (blob && blob.size > 65 * 1024 && quality > 0.28) {
    quality -= 0.08;
    blob = await new Promise((resolve) => canvas.toBlob(resolve, 'image/jpeg', quality));
  }
  if (!blob || blob.size > 65 * 1024) throw new Error('Unable to compress this image below 65 KB.');
  return new File([blob], `${file.name.replace(/\.[^.]+$/, '') || 'reading'}.jpg`, { type: 'image/jpeg' });
}

function PhotoInput({ onUploaded, disabled }) {
  const { t } = useTranslation();
  const [busy, setBusy] = useState(false);
  const [preview, setPreview] = useState(null);
  const change = async (event) => {
    const file = event.target.files?.[0]; if (!file) return;
    setBusy(true);
    try {
      const compressed = await compressPhoto(file);
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

function BaseLine({ title, icon: Icon, capturedAt, onCapturedAt, photoIds, onUploaded, onRemove, children, disabled }) {
  const { t } = useTranslation();
  return <article className="capture-line">
    <header><span className="capture-line-icon"><Icon size={20} /></span><strong>{title}</strong><button type="button" className="capture-remove" onClick={onRemove} aria-label={t('operational.capture.remove')}><Trash2 size={18} /></button></header>
    <label>{t('operational.capture.capturedAt')}<input type="datetime-local" value={capturedAt} onChange={(e) => onCapturedAt(e.target.value)} /></label>
    {children}
    <div className="capture-photo-row"><PhotoInput disabled={disabled || photoIds.length > 0} onUploaded={onUploaded} />{photoIds.length > 0 && <span className="capture-photo-ok"><CheckCircle2 size={17} /> {t('operational.capture.photoReady')}</span>}</div>
  </article>;
}

export default function OperationalCapture() {
  const { t } = useTranslation();
  const [references, setReferences] = useState([]);
  const [siteId, setSiteId] = useState('');
  const [lines, setLines] = useState([]);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => { client.get('/api/operational-captures/references').then((r) => setReferences(r.data)).catch(() => setReferences([])); }, []);
  const site = useMemo(() => references.find((x) => x.subStationId === Number(siteId)), [references, siteId]);
  const add = (kind) => {
    const collection = kind === 'generator' ? site?.generators : kind === 'tank' ? site?.tanks : kind === 'feeder' ? site?.feeders : site?.meters;
    if (!site || !collection?.length) return;
    const source = collection[0];
    setLines((current) => [...current, { id: crypto.randomUUID(), kind, subjectId: kind === 'generator' ? source.id : kind === 'tank' ? source.id : kind === 'feeder' ? source.id : source.generatorId || source.id, meterId: kind === 'meter' ? source.id : null, capturedAt: isoNow(), photoAttachmentIds: [], isRunning: true, values: {} }]);
  };
  const update = (id, patch) => setLines((current) => current.map((line) => line.id === id ? { ...line, ...patch } : line));
  const updateValues = (id, patch) => setLines((current) => current.map((line) => line.id === id ? { ...line, values: { ...line.values, ...patch } } : line));
  const submit = async () => {
    if (!site || lines.length === 0) return;
    const payload = { clientRequestId: crypto.randomUUID(), subStationId: site.subStationId, generatorReadings: [], tankLevelReadings: [], feederLoadReadings: [], meterReadings: [] };
    for (const line of lines) {
      const capturedAtUtc = new Date(line.capturedAt).toISOString();
      if (line.kind === 'generator') payload.generatorReadings.push({ generatorId: line.subjectId, capturedAtUtc, photoAttachmentIds: line.photoAttachmentIds, isRunning: line.isRunning, lineVoltageVolts: decimal(line.values.voltage), currentAmps: decimal(line.values.current), waterTemperatureCelsius: decimal(line.values.waterTemp), oilPressure: decimal(line.values.oilPressure), oilTemperatureCelsius: decimal(line.values.oilTemp), activeLoadKw: decimal(line.values.activeKw), apparentLoadKva: decimal(line.values.apparentKva), reactiveLoadKvar: decimal(line.values.reactiveKvar), frequencyHz: decimal(line.values.frequency), oilAddedLitres: decimal(line.values.oilAdded), powerFactor: decimal(line.values.powerFactor) });
      if (line.kind === 'tank') payload.tankLevelReadings.push({ fuelTankId: line.subjectId, capturedAtUtc, photoAttachmentIds: line.photoAttachmentIds, liquidLevelCm: decimal(line.values.level) });
      if (line.kind === 'feeder') payload.feederLoadReadings.push({ feederId: line.subjectId, capturedAtUtc, photoAttachmentIds: line.photoAttachmentIds, activeLoadKw: decimal(line.values.activeKw), apparentLoadKva: decimal(line.values.apparentKva) });
      if (line.kind === 'meter') payload.meterReadings.push({ subjectType: Number(line.values.subjectType || 0), subjectId: line.subjectId, meterId: line.meterId, kind: Number(line.values.kind || 0), capturedAtUtc, photoAttachmentIds: line.photoAttachmentIds, rawValue: decimal(line.values.rawValue), multiplierSnapshot: decimal(line.values.multiplier) || 1, isMeterReset: Boolean(line.values.isReset), resetNote: line.values.resetNote || null });
    }
    setSubmitting(true);
    try { await client.post('/api/operational-captures', payload); setLines([]); window.alert(t('operational.capture.success')); }
    catch (error) { window.alert(error.response?.data?.error || t('operational.capture.failed')); }
    finally { setSubmitting(false); }
  };
  const sourceName = (line) => { const list = line.kind === 'generator' ? site?.generators : line.kind === 'tank' ? site?.tanks : line.kind === 'feeder' ? site?.feeders : site?.meters; return list?.find((x) => (line.kind === 'meter' ? x.id === line.meterId : x.id === line.subjectId))?.name || ''; };
  const number = (line, key, label, required = false) => <label>{label}{required && ' *'}<input type="number" step="any" required={required} value={line.values[key] ?? ''} onChange={(e) => updateValues(line.id, { [key]: e.target.value })} /></label>;
  return <section className="capture-page">
    <div className="capture-heading"><div><span className="operational-eyebrow"><Zap size={16} /> {t('operational.capture.eyebrow')}</span><h1>{t('operational.capture.title')}</h1><p>{t('operational.capture.pendingNote')}</p></div><Link className="capture-history-link" to="/operations/history"><ClipboardList size={18} />{t('operational.capture.myHistory')}</Link></div>
    <label className="capture-site">{t('operational.capture.site')}<select value={siteId} onChange={(e) => { setSiteId(e.target.value); setLines([]); }}><option value="">{t('operational.capture.chooseSite')}</option>{references.map((item) => <option key={item.subStationId} value={item.subStationId}>{item.name} ({item.code})</option>)}</select></label>
    {site && <><div className="capture-adds"><button onClick={() => add('generator')} type="button"><Zap />{t('operational.capture.generator')}</button><button onClick={() => add('tank')} type="button"><Droplets />{t('operational.capture.tank')}</button><button onClick={() => add('feeder')} type="button"><Gauge />{t('operational.capture.feeder')}</button><button onClick={() => add('meter')} type="button"><Plus />{t('operational.capture.meter')}</button></div>
      <div className="capture-lines">{lines.map((line) => <BaseLine key={line.id} title={sourceName(line)} icon={line.kind === 'tank' ? Droplets : line.kind === 'feeder' ? Gauge : Zap} capturedAt={line.capturedAt} onCapturedAt={(capturedAt) => update(line.id, { capturedAt })} photoIds={line.photoAttachmentIds} onUploaded={(photoId) => update(line.id, { photoAttachmentIds: [photoId] })} onRemove={() => setLines((current) => current.filter((x) => x.id !== line.id))} disabled={submitting}>
        {line.kind === 'generator' && <><label className="capture-switch"><input type="checkbox" checked={line.isRunning} onChange={(e) => update(line.id, { isRunning: e.target.checked })}/>{t('operational.capture.running')}</label>{line.isRunning && <div className="capture-grid">{number(line, 'voltage', t('operational.capture.voltage'), true)}{number(line, 'current', t('operational.capture.current'), true)}{number(line, 'waterTemp', t('operational.capture.waterTemp'), true)}{number(line, 'oilPressure', t('operational.capture.oilPressure'), true)}{number(line, 'oilTemp', t('operational.capture.oilTemp'), true)}{number(line, 'activeKw', t('operational.capture.activeKw'), true)}{number(line, 'apparentKva', t('operational.capture.apparentKva'))}{number(line, 'frequency', t('operational.capture.frequency'))}</div>}</>}
        {line.kind === 'tank' && <div className="capture-grid">{number(line, 'level', t('operational.capture.levelCm'), true)}</div>}
        {line.kind === 'feeder' && <div className="capture-grid">{number(line, 'activeKw', t('operational.capture.activeKw'))}{number(line, 'apparentKva', t('operational.capture.apparentKva'))}</div>}
        {line.kind === 'meter' && <div className="capture-grid">{number(line, 'rawValue', t('operational.capture.rawValue'), true)}{number(line, 'multiplier', t('operational.capture.multiplier'))}<label>{t('operational.capture.subjectType')}<select value={line.values.subjectType || '0'} onChange={(e) => { const subjectType = e.target.value; const subject = subjectType === '2' ? site.feeders?.[0] : site.generators?.[0]; update(line.id, { subjectId: subject?.id || 0 }); updateValues(line.id, { subjectType }); }}><option value="0">{t('operational.capture.energy')}</option><option value="1">{t('operational.capture.hours')}</option><option value="2">{t('operational.capture.sent')}</option></select></label><label>{t('operational.capture.subject')}<select value={line.subjectId} onChange={(e) => update(line.id, { subjectId: Number(e.target.value) })}>{(String(line.values.subjectType || '0') === '2' ? site.feeders : site.generators).map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label><label className="capture-switch"><input type="checkbox" checked={Boolean(line.values.isReset)} onChange={(e) => updateValues(line.id, { isReset: e.target.checked })}/>{t('operational.capture.meterReset')}</label>{line.values.isReset && <label>{t('operational.capture.resetNote')}<input value={line.values.resetNote || ''} onChange={(e) => updateValues(line.id, { resetNote: e.target.value })}/></label>}</div>}
      </BaseLine>)}</div>
      <button type="button" className="capture-submit" disabled={submitting || lines.length === 0} onClick={submit}><Send size={20} />{submitting ? t('operational.capture.submitting') : t('operational.capture.submit')}</button></>}
  </section>;
}
