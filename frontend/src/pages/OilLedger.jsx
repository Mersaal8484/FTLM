import React, { useEffect, useMemo, useState } from 'react';
import { ChevronDown, ChevronUp, Droplet, RefreshCw } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import client from '../api/client';
import useCommercialMode from '../hooks/useCommercialMode';

const number = (value) => Number(value || 0).toLocaleString(undefined, { maximumFractionDigits: 3 });

export default function OilLedger({ lang }) {
  const { t } = useTranslation();
  const { isCommercialMode } = useCommercialMode();
  const [month, setMonth] = useState(() => new Date().toISOString().slice(0, 7));
  const [oilLookupId, setOilLookupId] = useState('');
  const [rows, setRows] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [expanded, setExpanded] = useState('');
  const [transactions, setTransactions] = useState([]);

  const load = async () => {
    const [year, monthNumber] = month.split('-').map(Number);
    setLoading(true);
    setError('');
    try {
      const response = await client.get('/api/oil-ledger', { params: { year, month: monthNumber, oilLookupId: oilLookupId || undefined } });
      setRows(response.data.rows || []);
      setExpanded('');
      setTransactions([]);
    } catch {
      setError(t('oilLedger.loadError'));
      setRows([]);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, [month, oilLookupId]); // eslint-disable-line react-hooks/exhaustive-deps

  const oils = useMemo(() => Array.from(new Map(rows.map((row) => [row.oilLookupId, row.oilLookupName])).entries()), [rows]);
  const totals = useMemo(() => rows.reduce((total, row) => ({
    inbound: total.inbound + Number(row.inbound || 0), outbound: total.outbound + Number(row.outbound || 0),
    transferIn: total.transferIn + Number(row.transferIn || 0), transferOut: total.transferOut + Number(row.transferOut || 0), loss: total.loss + Number(row.loss || 0),
  }), { inbound: 0, outbound: 0, transferIn: 0, transferOut: 0, loss: 0 }), [rows]);

  const toggleDay = async (date) => {
    if (expanded === date) { setExpanded(''); return; }
    setExpanded(date);
    try {
      const response = await client.get('/api/oil-ledger/day', { params: { date, oilLookupId: oilLookupId || undefined } });
      setTransactions(response.data || []);
    } catch {
      setTransactions([]);
    }
  };

  return (
    <div className="oil-ledger-page" dir={lang === 'ar' ? 'rtl' : 'ltr'}>
      <div className="page-header" style={{ marginBottom: 20 }}>
        <div>
          <h1 style={{ display: 'flex', gap: 10, alignItems: 'center' }}><Droplet size={25} />{t('oilLedger.title')}</h1>
          <p>{t('oilLedger.subtitle')}</p>
        </div>
        <div style={{ display: 'flex', alignItems: 'end', gap: 10, flexWrap: 'wrap' }}>
          <label>{t('oilLedger.month')}<input aria-label={t('oilLedger.month')} className="form-control" type="month" value={month} onChange={(event) => setMonth(event.target.value)} /></label>
          <label>{t('oilLedger.oil')}<select aria-label={t('oilLedger.oil')} className="form-select" value={oilLookupId} onChange={(event) => setOilLookupId(event.target.value)}><option value="">{t('oilLedger.allOils')}</option>{oils.map(([id, name]) => <option key={id} value={id}>{name}</option>)}</select></label>
          <button className="btn btn-ghost" onClick={load} disabled={loading}><RefreshCw size={16} />{t('oilLedger.refresh')}</button>
        </div>
      </div>
      <div className="section-card" style={{ overflowX: 'auto' }}>
        {error && <div className="alert alert-error">{error}</div>}
        {loading ? <div className="empty-state">{t('oilLedger.loading')}</div> : rows.length === 0 ? <div className="empty-state">{t('oilLedger.empty')}</div> : (
          <table className="data-table oil-ledger-table">
            <thead><tr><th>{t('oilLedger.date')}</th><th>{t('oilLedger.oil')}</th><th>{t('oilLedger.opening')}</th><th>{t('oilLedger.inbound')}</th><th>{isCommercialMode ? t('oilLedger.outboundCommercial') : t('oilLedger.outbound')}</th><th>{t('oilLedger.transferIn')}</th><th>{t('oilLedger.transferOut')}</th><th>{t('oilLedger.loss')}</th><th>{t('oilLedger.closing')}</th><th>{t('oilLedger.details')}</th></tr></thead>
            <tbody>{rows.map((row) => <React.Fragment key={`${row.oilLookupId}-${row.date}`}><tr><td>{row.date}</td><td>{row.oilLookupName}</td><td>{number(row.opening)}</td><td>{number(row.inbound)}</td><td>{number(row.outbound)}</td><td>{number(row.transferIn)}</td><td>{number(row.transferOut)}</td><td>{number(row.loss)}</td><td style={{ fontWeight: 700 }}>{number(row.closing)}</td><td><button className="btn btn-ghost" aria-label={t('oilLedger.expandDay')} onClick={() => toggleDay(row.date)}>{expanded === row.date ? <ChevronUp size={16} /> : <ChevronDown size={16} />}</button></td></tr>{expanded === row.date && <tr className="oil-ledger-details"><td colSpan="10">{transactions.length === 0 ? t('oilLedger.noTransactions') : transactions.map((item) => <div key={item.id}>{item.typeName} · {number(item.quantityLiters)} L {item.reference ? `· ${item.reference}` : ''} {item.supplier ? `· ${item.supplier}` : ''}</div>)}</td></tr>}</React.Fragment>)}</tbody>
            <tfoot><tr><td colSpan="3">{t('oilLedger.totals')}</td><td>{number(totals.inbound)}</td><td>{number(totals.outbound)}</td><td>{number(totals.transferIn)}</td><td>{number(totals.transferOut)}</td><td>{number(totals.loss)}</td><td colSpan="2" /></tr></tfoot>
          </table>
        )}
      </div>
    </div>
  );
}
