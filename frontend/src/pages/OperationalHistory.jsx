import { useEffect, useState } from 'react';
import { ClipboardList, Clock3, Plus } from 'lucide-react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import client from '../api/client';
import OperationalBackLink from '../components/OperationalBackLink';

export default function OperationalHistory() {
  const { t } = useTranslation(); const [items, setItems] = useState([]);
  useEffect(() => { client.get('/api/operational-captures/my-history').then((r) => setItems(r.data)).catch(() => setItems([])); }, []);
  return <section className="capture-page"><OperationalBackLink /><div className="capture-heading"><div><span className="operational-eyebrow"><ClipboardList size={16} /> {t('operational.capture.myHistory')}</span><h1>{t('operational.history.title')}</h1><p>{t('operational.history.note')}</p></div><Link className="capture-history-link" to="/operations/capture"><Plus size={18} />{t('operational.history.newCapture')}</Link></div><div className="capture-history">{items.map((item) => <article key={item.id}><div><span><Clock3 size={15} /> {new Date(item.receivedAtUtc).toLocaleString('en-US')}</span>{item.tankReadings?.length > 0 && <div style={{ display: 'flex', gap: 10, flexWrap: 'wrap', marginTop: 4, fontSize: 13 }}>{item.tankReadings.map((tr) => <span key={tr.id}>{tr.fuelTankName}: <strong>{Number(tr.liquidLevelCm).toLocaleString()} cm</strong> · <strong>{Number(tr.computedLitres).toLocaleString()} L</strong></span>)}</div>}</div><b>{item.status || t('operational.history.pending')}</b><small>{item.generatorReadingCount + item.tankLevelReadingCount + item.feederLoadReadingCount + item.meterReadingCount} {t('operational.history.lines')}</small></article>)}{items.length === 0 && <p className="capture-empty">{t('operational.history.empty')}</p>}</div></section>;
}
