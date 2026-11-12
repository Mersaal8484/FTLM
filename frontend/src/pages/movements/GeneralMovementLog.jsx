import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { AlertTriangle, ChevronDown, ChevronUp, Download, RefreshCw, ShieldAlert, ShieldCheck } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import client from '../../api/client';
import ConfirmModal from '../../components/ConfirmModal';
import Pagination from '../../components/Pagination';
import { exportToExcel } from '../reports/utils/reportExport';

const INITIAL_FILTERS = {
  dateFrom: '', dateTo: '', stationId: '', subStationId: '', kind: '',
  movementType: '', status: '', search: '', sort: 'date', sortDirection: 'desc',
};

export default function GeneralMovementLog({ lang, canApprove = false }) {
  const { t } = useTranslation();
  const locale = lang === 'ar' ? 'ar-YE' : 'en-US';
  const [filters, setFilters] = useState(INITIAL_FILTERS);
  const [applied, setApplied] = useState(INITIAL_FILTERS);
  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);
  const [data, setData] = useState({ items: [], totalCount: 0 });
  const [stations, setStations] = useState([]);
  const [sites, setSites] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [expanded, setExpanded] = useState(null);
  const [confirm, setConfirm] = useState(null);
  const [reject, setReject] = useState(null);
  const [rejectReason, setRejectReason] = useState('');

  const requestParams = useMemo(() => {
    const params = { page, pageSize };
    Object.entries(applied).forEach(([key, value]) => { if (value !== '') params[key] = value; });
    return params;
  }, [applied, page, pageSize]);

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const response = await client.get('/api/movement-ledger', { params: requestParams });
      setData({ items: response.data?.items || [], totalCount: response.data?.totalCount || 0 });
    } catch {
      setError(t('movements.log.error'));
    } finally {
      setLoading(false);
    }
  }, [requestParams, t]);

  useEffect(() => { load(); }, [load]);
  useEffect(() => {
    Promise.allSettled([client.get('/api/stations'), client.get('/api/sub-stations')]).then(([stationResult, siteResult]) => {
      if (stationResult.status === 'fulfilled') setStations(stationResult.value.data || []);
      if (siteResult.status === 'fulfilled') setSites(siteResult.value.data || []);
    });
  }, []);

  const changeSort = (sort) => {
    setApplied(current => ({
      ...current,
      sort,
      sortDirection: current.sort === sort && current.sortDirection === 'asc' ? 'desc' : 'asc',
    }));
    setPage(1);
  };

  const act = async (row, action, reason) => {
    const base = row.kind === 'oil' ? '/api/oil-transactions' : '/api/fuel-movements';
    await client.post(`${base}/${row.id}/${action}`, action === 'reject' ? { reason } : undefined);
    setConfirm(null); setReject(null); setRejectReason('');
    await load();
  };

  const exportFiltered = async () => {
    const response = await client.get('/api/movement-ledger', { params: { ...requestParams, page: 1, pageSize: 200 } });
    const rows = response.data?.items || [];
    exportToExcel(rows, [
      { header: t('movements.log.columns.date'), accessor: 'date' },
      { header: t('movements.log.columns.station'), accessor: 'station' },
      { header: t('movements.log.columns.site'), accessor: 'subStation' },
      { header: t('movements.log.columns.kind'), accessor: 'kind' },
      { header: t('movements.log.columns.type'), accessor: 'movementType' },
      { header: t('movements.log.columns.quantity'), accessor: 'quantity' },
      { header: t('movements.log.columns.status'), accessor: 'status' },
    ], 'movement-ledger');
  };

  const totalPages = Math.max(1, Math.ceil(data.totalCount / pageSize));
  const number = value => Number(value || 0).toLocaleString(locale, { maximumFractionDigits: 2 });
  const date = value => value ? new Intl.DateTimeFormat(locale, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value)) : '—';
  const statusLabel = value => t(`movements.status.${value}`);

  return (
    <section className="section-card movement-ledger" aria-label={t('movements.log.title')}>
      <div className="section-header">
        <div><h2>{t('movements.log.title')}</h2><p>{t('movements.log.subtitle')}</p></div>
        <div className="toolbar-actions">
          <button className="btn btn-ghost" onClick={load}><RefreshCw size={15} />{t('movements.log.retry')}</button>
          <button className="btn btn-primary" onClick={exportFiltered}><Download size={15} />{t('movements.log.export')}</button>
        </div>
      </div>

      <form className="filter-bar movement-ledger-filters" onSubmit={(event) => { event.preventDefault(); setApplied(filters); setPage(1); }}>
        <input className="form-control" aria-label={t('movements.log.filters.search')} placeholder={t('movements.log.filters.search')} value={filters.search} onChange={e => setFilters(f => ({ ...f, search: e.target.value }))} />
        <input className="form-control" type="date" aria-label={t('movements.log.filters.from')} value={filters.dateFrom} onChange={e => setFilters(f => ({ ...f, dateFrom: e.target.value }))} />
        <input className="form-control" type="date" aria-label={t('movements.log.filters.to')} value={filters.dateTo} onChange={e => setFilters(f => ({ ...f, dateTo: e.target.value }))} />
        <select className="form-control" aria-label={t('movements.log.filters.station')} value={filters.stationId} onChange={e => setFilters(f => ({ ...f, stationId: e.target.value, subStationId: '' }))}>
          <option value="">{t('movements.log.filters.allStations')}</option>
          {stations.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}
        </select>
        <select className="form-control" aria-label={t('movements.log.filters.site')} value={filters.subStationId} onChange={e => setFilters(f => ({ ...f, subStationId: e.target.value }))}>
          <option value="">{t('movements.log.filters.allSites')}</option>
          {sites.filter(item => !filters.stationId || String(item.stationId) === String(filters.stationId)).map(item => <option key={item.id} value={item.id}>{item.name}</option>)}
        </select>
        <select className="form-control" aria-label={t('movements.log.filters.kind')} value={filters.kind} onChange={e => setFilters(f => ({ ...f, kind: e.target.value }))}>
          <option value="">{t('movements.log.filters.allKinds')}</option><option value="fuel">{t('movements.log.kind.fuel')}</option><option value="oil">{t('movements.log.kind.oil')}</option>
        </select>
        <select className="form-control" aria-label={t('movements.log.filters.status')} value={filters.status} onChange={e => setFilters(f => ({ ...f, status: e.target.value }))}>
          <option value="">{t('movements.log.filters.allStatuses')}</option><option value="pending">{t('movements.status.pending')}</option><option value="approved">{t('movements.status.approved')}</option><option value="rejected">{t('movements.status.rejected')}</option>
        </select>
        <button className="btn btn-primary" type="submit">{t('movements.log.filters.apply')}</button>
        <button className="btn btn-ghost" type="button" onClick={() => { setFilters(INITIAL_FILTERS); setApplied(INITIAL_FILTERS); setPage(1); }}>{t('movements.log.filters.clear')}</button>
      </form>

      {loading ? (
        <div className="movement-log-skeleton" role="status" aria-label={t('movements.log.loading')}>{[1, 2, 3, 4].map(item => <div className="skeleton-row" key={item} />)}</div>
      ) : error ? (
        <div className="empty-state" role="alert"><AlertTriangle size={42} /><h3>{error}</h3><button className="btn btn-primary" onClick={load}>{t('movements.log.retry')}</button></div>
      ) : data.items.length === 0 ? (
        <div className="empty-state"><h3>{t('movements.log.emptyTitle')}</h3><p>{t('movements.log.empty')}</p></div>
      ) : (
        <div className="data-table-wrap">
          <table className="data-table movement-ledger-table">
            <thead><tr>
              <th><button className="table-sort" onClick={() => changeSort('date')}>{t('movements.log.columns.date')}</button></th>
              <th><button className="table-sort" onClick={() => changeSort('station')}>{t('movements.log.columns.station')}</button></th>
              <th>{t('movements.log.columns.site')}</th><th>{t('movements.log.columns.kind')}</th>
              <th><button className="table-sort" onClick={() => changeSort('type')}>{t('movements.log.columns.type')}</button></th>
              <th><button className="table-sort" onClick={() => changeSort('quantity')}>{t('movements.log.columns.quantity')}</button></th>
              <th>{t('movements.log.columns.status')}</th><th>{t('movements.log.columns.actions')}</th>
            </tr></thead>
            <tbody>{data.items.map(row => <React.Fragment key={`${row.kind}-${row.id}`}>
              <tr>
                <td>{date(row.date)}</td><td>{row.station}</td><td>{row.subStation}</td>
                <td>{t(`movements.log.kind.${row.kind}`)}</td><td>{row.movementType}</td>
                <td>{number(row.quantity)} {row.unit}</td><td><span className={`badge badge-${row.status}`}>{statusLabel(row.status)}</span></td>
                <td><div className="table-actions">
                  <button className="btn btn-sm btn-ghost" aria-label={t('movements.log.details')} onClick={() => setExpanded(expanded === `${row.kind}-${row.id}` ? null : `${row.kind}-${row.id}`)}>{expanded === `${row.kind}-${row.id}` ? <ChevronUp size={15} /> : <ChevronDown size={15} />}</button>
                  {canApprove && row.status === 'pending' && <><button className="btn btn-sm btn-ghost text-success" onClick={() => setConfirm(row)}><ShieldCheck size={14} />{t('movements.log.approve')}</button><button className="btn btn-sm btn-ghost text-danger" onClick={() => setReject(row)}><ShieldAlert size={14} />{t('movements.log.reject')}</button></>}
                </div></td>
              </tr>
              {expanded === `${row.kind}-${row.id}` && <tr className="detail-row"><td colSpan={8}><dl><div><dt>{t('movements.log.reference')}</dt><dd>{row.reference || '—'}</dd></div><div><dt>{t('movements.log.createdBy')}</dt><dd>{row.createdBy || '—'}</dd></div><div><dt>{t('movements.log.approvedBy')}</dt><dd>{row.approvedBy || '—'}</dd></div></dl></td></tr>}
            </React.Fragment>)}</tbody>
          </table>
        </div>
      )}
      <Pagination page={page} totalPages={totalPages} total={data.totalCount} pageSize={pageSize} onPageChange={setPage} lang={lang} />

      {confirm && <ConfirmModal open title={t('movements.log.confirmApproveTitle')} message={t('movements.log.confirmApprove')} onConfirm={() => act(confirm, 'approve')} onCancel={() => setConfirm(null)} />}
      {reject && <div className="modal-overlay"><div className="modal-box"><div className="modal-header"><h3>{t('movements.log.rejectTitle')}</h3></div><div className="modal-body"><textarea className="form-control" value={rejectReason} onChange={e => setRejectReason(e.target.value)} placeholder={t('movements.log.rejectReason')} /><div className="modal-actions"><button className="btn btn-ghost" onClick={() => setReject(null)}>{t('common.cancel')}</button><button className="btn btn-danger" onClick={() => act(reject, 'reject', rejectReason)}>{t('movements.log.reject')}</button></div></div></div></div>}
    </section>
  );
}
