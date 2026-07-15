import React, { useState, useEffect, useCallback } from 'react';
import client from '../api/client';
import { Shield, Search, Calendar, Download, Filter, X } from 'lucide-react';
import Pagination from '../components/Pagination';

const METHOD_COLORS = {
  GET: 'var(--secondary)', POST: 'var(--primary)',
  PUT: 'var(--warning)', PATCH: 'var(--warning)',
  DELETE: 'var(--danger)'
};

function AuditLogs({ lang }) {
  const ar = lang === 'ar';
  const [audits, setAudits] = useState([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [limit] = useState(50);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [search, setSearch] = useState('');
  const [methodFilter, setMethodFilter] = useState('');
  const [entityName, setEntityName] = useState('');
  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');
  const [showFilters, setShowFilters] = useState(false);

  const totalPages = Math.ceil(total / limit);

  const fetchAudits = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const params = { page, pageSize: limit };
      if (search) params.action = search;
      if (entityName) params.entityName = entityName;
      if (fromDate) params.startDate = new Date(fromDate).toISOString();
      if (toDate) params.endDate = new Date(toDate + 'T23:59:59').toISOString();

      const res = await client.get('/api/audits', { params });
      setAudits(res.data.items || []);
      setTotal(res.data.totalCount || 0);
    } catch (err) {
      setError(err.response?.data?.msg || (ar ? 'فشل تحميل سجل التدقيق' : 'Failed to load audit log'));
      setAudits([]);
    } finally { setLoading(false); }
  }, [page, limit, search, entityName, fromDate, toDate, ar]);

  useEffect(() => { fetchAudits(); }, [fetchAudits]);

  const handleExport = async () => {
    try {
      const params = {};
      if (fromDate) params.startDate = new Date(fromDate).toISOString();
      if (toDate) params.endDate = new Date(toDate + 'T23:59:59').toISOString();

      const res = await client.get('/api/audits/export', { params, responseType: 'blob' });
      const url = window.URL.createObjectURL(new Blob([res.data]));
      const a = document.createElement('a');
      a.href = url;
      a.download = 'audit-log.csv';
      a.click();
      window.URL.revokeObjectURL(url);
    } catch { }
  };

  const clearFilters = () => {
    setSearch(''); setMethodFilter(''); setEntityName(''); setFromDate(''); setToDate(''); setPage(1);
  };

  const hasFilters = search || methodFilter || entityName || fromDate || toDate;

  const formatTime = (ts) => {
    const d = new Date(ts);
    return d.toLocaleString(ar ? 'ar-YE' : 'en-US', {
      year: 'numeric', month: '2-digit', day: '2-digit',
      hour: '2-digit', minute: '2-digit', second: '2-digit'
    });
  };

  return (
    <div>
      <div className="page-header">
        <div className="page-header-row">
          <div>
            <h1 style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
              <Shield size={28} style={{ color: 'var(--primary)' }} />
              {ar ? 'سجل التدقيق' : 'Audit Log'}
            </h1>
            <p>{ar ? 'سجل جميع العمليات والتغييرات في النظام' : 'Track all system operations and changes'}</p>
          </div>
          <div style={{ display: 'flex', gap: 10 }}>
            <button className="btn btn-ghost" onClick={() => setShowFilters(o => !o)}>
              <Filter size={14} /> {ar ? 'تصفية' : 'Filters'}
            </button>
            <button className="btn btn-ghost" onClick={handleExport}>
              <Download size={14} /> {ar ? 'تصدير' : 'Export'}
            </button>
          </div>
        </div>
      </div>

      {error && <div className="alert alert-danger" style={{ marginBottom: 16, padding: 12, fontSize: 13 }}>{error}</div>}

      {showFilters && (
        <div className="glass-card" style={{ padding: 16, marginBottom: 20 }}>
          <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap', alignItems: 'end' }}>
            <div className="search-bar" style={{ flex: 1, minWidth: 160 }}>
              <Search size={16} />
              <input className="form-control" placeholder={ar ? 'بحث عن إجراء...' : 'Search action...'}
                value={search} onChange={e => { setSearch(e.target.value); setPage(1); }} />
            </div>
            <div>
              <label className="form-label" style={{ marginBottom: 4, fontSize: 11 }}>
                {ar ? 'الطريقة' : 'Method'}
              </label>
              <select className="form-select" value={methodFilter}
                onChange={e => { setMethodFilter(e.target.value); setPage(1); }}
                style={{ width: 120 }}>
                <option value="">{ar ? 'الكل' : 'All'}</option>
                {['GET', 'POST', 'PUT', 'PATCH', 'DELETE'].map(m => (
                  <option key={m} value={m}>{m}</option>
                ))}
              </select>
            </div>
            <div>
              <label className="form-label" style={{ marginBottom: 4, fontSize: 11 }}>
                {ar ? 'الكيان' : 'Entity'}
              </label>
              <input className="form-control" placeholder={ar ? 'اسم الكيان...' : 'Entity name...'}
                value={entityName} onChange={e => { setEntityName(e.target.value); setPage(1); }}
                style={{ width: 140 }} />
            </div>
            <div>
              <label className="form-label" style={{ marginBottom: 4, fontSize: 11 }}>
                {ar ? 'من تاريخ' : 'From'}
              </label>
              <input type="date" className="form-control" value={fromDate}
                onChange={e => { setFromDate(e.target.value); setPage(1); }} style={{ width: 150 }} />
            </div>
            <div>
              <label className="form-label" style={{ marginBottom: 4, fontSize: 11 }}>
                {ar ? 'إلى تاريخ' : 'To'}
              </label>
              <input type="date" className="form-control" value={toDate}
                onChange={e => { setToDate(e.target.value); setPage(1); }} style={{ width: 150 }} />
            </div>
            {hasFilters && (
              <button className="btn btn-sm btn-ghost" onClick={clearFilters} style={{ marginBottom: 0 }}>
                <X size={14} /> {ar ? 'مسح' : 'Clear'}
              </button>
            )}
          </div>
        </div>
      )}

      <div className="glass-card" style={{ overflow: 'hidden' }}>
        {loading ? (
          <div className="spinner-wrap"><div className="ft-spinner" /></div>
        ) : audits.length === 0 ? (
          <div className="empty-state">
            <Shield size={48} />
            <h3>{ar ? 'لا توجد سجلات تدقيق' : 'No audit records found'}</h3>
          </div>
        ) : (
          <div className="table-wrap" style={{ overflowX: 'auto' }}>
            <table className="ft-table">
              <thead>
                <tr>
                  <th>{ar ? 'الوقت' : 'Timestamp'}</th>
                  <th>{ar ? 'الإجراء' : 'Action'}</th>
                  <th>{ar ? 'الطريقة' : 'Method'}</th>
                  <th>{ar ? 'الرابط' : 'Resource'}</th>
                  <th>{ar ? 'الحالة' : 'Status'}</th>
                  <th>{ar ? 'IP' : 'IP'}</th>
                </tr>
              </thead>
              <tbody>
                {audits.map(a => (
                  <tr key={a.id}>
                    <td style={{ fontSize: 12, whiteSpace: 'nowrap', color: 'var(--text-muted)' }}>
                      <span style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
                        <Calendar size={12} style={{ color: 'var(--text-faint)' }} />
                        {formatTime(a.timestamp)}
                      </span>
                    </td>
                    <td style={{ fontSize: 13 }}>{a.action}</td>
                    <td>
                      <span className="badge" style={{
                        background: `${METHOD_COLORS[a.method] || 'var(--text-faint)'}20`,
                        color: METHOD_COLORS[a.method] || 'var(--text-faint)',
                        fontSize: 10
                      }}>
                        {a.method}
                      </span>
                    </td>
                    <td style={{ fontSize: 12, color: 'var(--text-muted)', maxWidth: 250, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                      {a.resource}
                    </td>
                    <td>
                      <span className={`badge ${a.statusCode >= 400 ? 'badge-danger' : a.statusCode >= 300 ? 'badge-warning' : 'badge-success'}`}>
                        {a.statusCode}
                      </span>
                    </td>
                    <td style={{ fontSize: 12, color: 'var(--text-faint)', fontFamily: 'monospace' }}>{a.ip}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {totalPages > 1 && (
          <div style={{ padding: '12px 20px', borderTop: '1px solid var(--border)' }}>
            <Pagination page={page} totalPages={totalPages} total={total} pageSize={limit} lang={lang}
              onPageChange={setPage} />
          </div>
        )}
      </div>
    </div>
  );
}

export default AuditLogs;
