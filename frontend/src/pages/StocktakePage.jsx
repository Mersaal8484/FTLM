import { useEffect, useState } from 'react';
import { Camera, RefreshCw } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import client from '../api/client';

const today = () => new Date().toISOString().slice(0, 10);
const requestId = () => globalThis.crypto?.randomUUID?.() || `${Date.now()}-${Math.random()}`;
const statusKey = (status) => ({ 0: 'pending', 1: 'approved', 2: 'rejected' }[status] || String(status || 'pending').toLowerCase());
const quantity = (value) => Number(value || 0).toLocaleString(undefined, { maximumFractionDigits: 3 });

export default function StocktakePage({ lang }) {
  const { t } = useTranslation();
  const statusLabel = {
    pending: t('stocktake.pending'),
    approved: t('stocktake.approved'),
    rejected: t('stocktake.rejected'),
  };
  const [date, setDate] = useState(today);
  const [draft, setDraft] = useState(null);
  const [items, setItems] = useState([]);
  const [selected, setSelected] = useState(null);
  const [photo, setPhoto] = useState(null);
  const [notes, setNotes] = useState('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  const load = async () => {
    setError('');
    try {
      const [draftResponse, listResponse] = await Promise.all([
        client.get('/api/stocktakes/draft', { params: { date } }),
        client.get('/api/stocktakes', { params: { page: 1, pageSize: 10 } }),
      ]);
      setDraft(draftResponse.data);
      setItems(listResponse.data.items || []);
    } catch { setError(t('stocktake.loadError')); }
  };
  useEffect(() => { load(); }, [date]);

  const updateCount = (kind, id, value) => setDraft((current) => ({ ...current, [kind]: current[kind].map((line) => line[`${kind === 'fuelLines' ? 'fuelTank' : 'oil'}Id`] === id
    ? { ...line, countedLiters: value, varianceLiters: Number(value || 0) - Number(line.bookLiters || 0), usableCountedLiters: Math.max(0, Number(value || 0) - Number(line.deadStockLiters || 0)) }
    : line) }));

  const submit = async (event) => {
    event.preventDefault(); setError('');
    if (!photo || !['image/jpeg', 'image/webp'].includes(photo.type) || photo.size > 65 * 1024) { setError(t('stocktake.photoError')); return; }
    setBusy(true);
    try {
      const upload = new FormData(); upload.append('file', photo);
      const attachment = await client.post('/api/attachments', upload, { headers: { 'Content-Type': 'multipart/form-data' } });
      await client.post('/api/stocktakes', {
        stocktakeDate: date, notes, clientRequestId: requestId(), attachmentIds: [attachment.data.id],
        fuelLines: (draft.fuelLines || []).map((line) => ({ fuelTankId: line.fuelTankId, countedLiters: Number(line.countedLiters || 0), dipLevelCm: null })),
        oilLines: (draft.oilLines || []).map((line) => ({ oilId: line.oilId, countedLiters: Number(line.countedLiters || 0) })),
      });
      setNotes(''); setPhoto(null); await load();
    } catch { setError(t('stocktake.submitError')); } finally { setBusy(false); }
  };

  const review = async (id, action) => {
    const reason = action === 'reject' ? window.prompt(t('stocktake.reason')) : null;
    if (action === 'reject' && !reason?.trim()) return;
    try { await client.post(`/api/stocktakes/${id}/${action}`, action === 'reject' ? { reason } : {}); await load(); }
    catch { setError(t('stocktake.reviewError')); }
  };

  const view = async (id) => {
    setError('');
    try {
      const response = await client.get(`/api/stocktakes/${id}`);
      const stocktake = response.data;
      const photos = await Promise.all((stocktake.attachmentIds || []).map(async (attachmentId) => {
        const photoResponse = await client.get(`/api/stocktakes/${id}/photos/${attachmentId}`, { responseType: 'blob' });
        return { attachmentId, url: URL.createObjectURL(photoResponse.data) };
      }));
      setSelected({ ...stocktake, photos });
    } catch { setError(t('stocktake.loadError')); }
  };

  return <main className="stocktake-page" dir={lang === 'ar' ? 'rtl' : 'ltr'}>
    <header className="page-header"><div><h1>{t('stocktake.title')}</h1><p>{t('stocktake.subtitle')}</p></div><button className="btn btn-ghost" onClick={load}><RefreshCw size={16}/>{t('stocktake.refresh')}</button></header>
    {error && <div className="alert alert-error">{error}</div>}
    {!draft ? <div className="empty-state">{t('common.loading')}</div> : <form className="section-card stocktake-form" onSubmit={submit}>
      <div className="stocktake-toolbar"><label>{t('stocktake.date')}<input className="form-control" type="date" value={date} onChange={(e) => setDate(e.target.value)} /></label><label>{t('stocktake.notes')}<input className="form-control" value={notes} onChange={(e) => setNotes(e.target.value)} /></label><label>{t('stocktake.photo')}<input type="file" accept="image/jpeg,image/webp" onChange={(e) => setPhoto(e.target.files?.[0] || null)} /></label></div>
      <StocktakeLines title={t('stocktake.fuel')} lines={draft.fuelLines} kind="fuelLines" t={t} onChange={updateCount} />
      <StocktakeLines title={t('stocktake.oils')} lines={draft.oilLines} kind="oilLines" t={t} onChange={updateCount} />
      <button className="btn btn-primary" disabled={busy}><Camera size={16}/>{busy ? t('stocktake.submitting') : t('stocktake.submit')}</button>
    </form>}
    <section className="section-card"><h2>{t('stocktake.history')}</h2>{items.length === 0 ? <div className="empty-state">{t('stocktake.noData')}</div> : <table className="data-table"><thead><tr><th>{t('stocktake.date')}</th><th>{t('stocktake.status')}</th><th>{t('stocktake.variance')}</th><th /></tr></thead><tbody>{items.map((item) => <tr key={item.id}><td>{item.stocktakeDate}</td><td>{statusLabel[statusKey(item.status)]}</td><td>{quantity(Number(item.totalFuelVarianceLiters) + Number(item.totalOilVarianceLiters))}</td><td><button type="button" className="btn btn-ghost" onClick={() => view(item.id)}>{t('stocktake.view')}</button>{statusKey(item.status) === 'pending' && <><button type="button" className="btn btn-ghost" onClick={() => review(item.id, 'approve')}>{t('stocktake.approve')}</button><button type="button" className="btn btn-danger" onClick={() => review(item.id, 'reject')}>{t('stocktake.reject')}</button></>}</td></tr>)}</tbody></table>}</section>
    {selected && <section className="section-card stocktake-detail"><h2>{t('stocktake.details')}</h2><div>{selected.stocktakeDate} · {statusLabel[statusKey(selected.status)]}</div><div className="stocktake-photo-list">{selected.photos.length === 0 ? <span>{t('stocktake.noPhoto')}</span> : selected.photos.map((photo) => <img key={photo.attachmentId} src={photo.url} alt={t('stocktake.photo')} />)}</div></section>}
  </main>;
}

function StocktakeLines({ title, lines = [], kind, t, onChange }) {
  const idKey = kind === 'fuelLines' ? 'fuelTankId' : 'oilId';
  const nameKey = kind === 'fuelLines' ? 'tankName' : 'oilName';
  return <section className="stocktake-lines"><h2>{title}</h2><table className="data-table"><thead><tr><th>{title}</th><th>{t('stocktake.book')}</th><th>{t('stocktake.counted')}</th><th>{t('stocktake.usable')}</th><th>{t('stocktake.variance')}</th></tr></thead><tbody>{lines.map((line) => <tr key={line[idKey]}><td>{line[nameKey]}</td><td>{quantity(line.bookLiters)}</td><td><input className="form-control" type="number" min="0" step="0.001" value={line.countedLiters ?? ''} onChange={(e) => onChange(kind, line[idKey], e.target.value)} /></td><td>{kind === 'fuelLines' ? quantity(line.usableCountedLiters) : '—'}</td><td>{quantity(line.varianceLiters)}</td></tr>)}</tbody></table></section>;
}
