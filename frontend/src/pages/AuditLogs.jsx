import React, { useState, useEffect, useCallback } from 'react';
import PropTypes from 'prop-types';
import client from '../api/client';
import { Shield, Calendar, Download, Filter, X } from 'lucide-react';
import Pagination from '../components/Pagination';
import { PageHeader, Button, FormField, Input } from '../components/ui';

const METHOD_COLORS = {
  GET: 'var(--secondary)',
  POST: 'var(--primary)',
  PUT: 'var(--warning)',
  PATCH: 'var(--warning)',
  DELETE: 'var(--danger)',
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
    } finally {
      setLoading(false);
    }
  }, [page, limit, search, entityName, fromDate, toDate, ar]);

  useEffect(() => {
    fetchAudits();
  }, [fetchAudits]);

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
    } catch {}
  };

  const clearFilters = () => {
    setSearch('');
    setMethodFilter('');
    setEntityName('');
    setFromDate('');
    setToDate('');
    setPage(1);
  };

  const hasFilters = search || methodFilter || entityName || fromDate || toDate;

  const formatTime = (ts) => {
    const d = new Date(ts);
    return d.toLocaleString(ar ? 'ar-YE-u-nu-latn' : 'en-US', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
    });
  };

  return (
    <div>
      <PageHeader
        title={
          <span style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
            <Shield size={28} style={{ color: 'var(--primary)' }} />
            {ar ? 'سجل التدقيق' : 'Audit Log'}
          </span>
        }
        subtitle={ar ? 'سجل جميع العمليات والتغييرات في النظام لضمان الامتثال' : 'Track all system operations, access history & compliance events'}
        actions={
          <div style={{ display: 'flex', gap: 10 }}>
            <Button
              variant={showFilters ? 'primary' : 'outline'}
              size="sm"
              icon={Filter}
              onClick={() => setShowFilters((o) => !o)}
            >
              {ar ? 'تصفية' : 'Filters'}
            </Button>
            <Button variant="outline" size="sm" icon={Download} onClick={handleExport}>
              {ar ? 'تصدير' : 'Export'}
            </Button>
          </div>
        }
      />

      {error && <div className="alert alert-danger" style={{ marginBottom: 16 }}>{error}</div>}

      {/* Filter bar */}
      {showFilters && (
        <div className="section-card" style={{ marginBottom: 16, padding: '14px 18px' }}>
          <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap', alignItems: 'flex-end' }}>
            <FormField label={ar ? 'بحث بالإجراء' : 'Search Action'} style={{ flex: 1, minWidth: 160 }}>
              <Input
                placeholder={ar ? 'بحث بالإجراء...' : 'Filter by action...'}
                value={search}
                onChange={(e) => {
                  setSearch(e.target.value);
                  setPage(1);
                }}
              />
            </FormField>
            <FormField label={ar ? 'الطريقة' : 'Method'} style={{ minWidth: 110 }}>
              <select
                className="form-select"
                value={methodFilter}
                onChange={(e) => {
                  setMethodFilter(e.target.value);
                  setPage(1);
                }}
                style={{ width: 110 }}
              >
                <option value="">{ar ? 'الكل' : 'All'}</option>
                {['GET', 'POST', 'PUT', 'PATCH', 'DELETE'].map((m) => (
                  <option key={m} value={m}>
                    {m}
                  </option>
                ))}
              </select>
            </FormField>
            <FormField label={ar ? 'الكيان' : 'Entity'} style={{ minWidth: 140 }}>
              <Input
                placeholder={ar ? 'اسم الكيان...' : 'Entity name...'}
                value={entityName}
                onChange={(e) => {
                  setEntityName(e.target.value);
                  setPage(1);
                }}
                style={{ width: 140 }}
              />
            </FormField>
            <FormField label={ar ? 'من تاريخ' : 'From'} style={{ minWidth: 150 }}>
              <Input
                type="date"
                value={fromDate}
                onChange={(e) => {
                  setFromDate(e.target.value);
                  setPage(1);
                }}
                style={{ width: 150 }}
              />
            </FormField>
            <FormField label={ar ? 'إلى تاريخ' : 'To'} style={{ minWidth: 150 }}>
              <Input
                type="date"
                value={toDate}
                onChange={(e) => {
                  setToDate(e.target.value);
                  setPage(1);
                }}
                style={{ width: 150 }}
              />
            </FormField>
            {hasFilters && (
              <Button variant="ghost" size="sm" icon={X} onClick={clearFilters}>
                {ar ? 'مسح' : 'Clear'}
              </Button>
            )}
          </div>
        </div>
      )}

      <div className="section-card" style={{ overflow: 'hidden' }}>
        {loading ? (
          <div className="spinner-wrap">
            <div className="ft-spinner" />
          </div>
        ) : audits.length === 0 ? (
          <div className="empty-state" style={{ padding: '40px 20px', textAlign: 'center' }}>
            <Shield size={48} style={{ color: 'var(--text-faint)', marginBottom: 12 }} />
            <h3>{ar ? 'لا توجد سجلات تدقيق' : 'No audit records found'}</h3>
          </div>
        ) : (
          <div className="table-wrap" style={{ overflowX: 'auto' }}>
            <table className="data-table">
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
                {audits.map((a) => (
                  <tr key={a.id}>
                    <td style={{ fontSize: 12, whiteSpace: 'nowrap', color: 'var(--text-muted)' }}>
                      <span style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
                        <Calendar size={12} style={{ color: 'var(--text-faint)' }} />
                        {formatTime(a.timestamp)}
                      </span>
                    </td>
                    <td style={{ fontSize: 13, fontWeight: 600 }}>{a.action}</td>
                    <td>
                      <span
                        className="badge"
                        style={{
                          background: `${METHOD_COLORS[a.method] || 'var(--text-faint)'}20`,
                          color: METHOD_COLORS[a.method] || 'var(--text-faint)',
                          fontSize: 10,
                        }}
                      >
                        {a.method}
                      </span>
                    </td>
                    <td
                      style={{
                        fontSize: 12,
                        color: 'var(--text-muted)',
                        maxWidth: 250,
                        overflow: 'hidden',
                        textOverflow: 'ellipsis',
                        whiteSpace: 'nowrap',
                      }}
                    >
                      {a.resource}
                    </td>
                    <td>
                      <span
                        className={`badge ${
                          a.statusCode >= 400
                            ? 'badge-danger'
                            : a.statusCode >= 300
                            ? 'badge-warning'
                            : 'badge-success'
                        }`}
                      >
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
            <Pagination
              page={page}
              totalPages={totalPages}
              total={total}
              pageSize={limit}
              lang={lang}
              onPageChange={setPage}
            />
          </div>
        )}
      </div>
    </div>
  );
}

AuditLogs.propTypes = {
  lang: PropTypes.string,
};

export default AuditLogs;
