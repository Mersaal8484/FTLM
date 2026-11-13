import React, { useState, useEffect, useMemo } from 'react';
import { PackageSearch, AlertTriangle, RefreshCw } from 'lucide-react';
import client from '../api/client';
import Pagination from '../components/Pagination';

const fmt = (v, d = 0) => v != null ? Number(v).toLocaleString('en-US', { maximumFractionDigits: d }) : '—';

function WarehouseStock({ lang, currentUser }) {
  const ar = lang === 'ar';
  const [stock, setStock] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [stationFilter, setStationFilter] = useState('');
  const [stations, setStations] = useState([]);
  const [page, setPage] = useState(1);
  const PAGE_SIZE = 20;
  const [activeTab, setActiveTab] = useState('all');

  const perms = currentUser?.permissions?.warehouse || {};
  const canRequest = currentUser?.role === 'admin' || perms.add || perms.request;

  const load = async () => {
    setLoading(true); setError(null);
    try {
      const params = { _t: Date.now() };
      if (stationFilter) params.stationId = stationFilter;
      const [stockRes, stRes] = await Promise.allSettled([
        client.get('/api/warehouse/stock', { params }),
        client.get('/api/stations', { params: { _t: Date.now() } }),
      ]);
      if (stockRes.status === 'fulfilled') setStock(stockRes.value.data?.items || stockRes.value.data || []);
      if (stRes.status === 'fulfilled') setStations(stRes.value.data || []);
    } catch { setError(ar ? 'فشل تحميل البيانات' : 'Failed to load data'); }
    finally { setLoading(false); }
  };

  useEffect(() => { load(); }, [stationFilter]);

  const filtered = useMemo(() => {
    let data = stock;
    if (activeTab === 'low') data = data.filter(s => (s.minStockQuantity != null) && (s.currentQuantity <= s.minStockQuantity));
    else if (activeTab === 'near') data = data.filter(s => (s.minStockQuantity != null) && (s.currentQuantity > s.minStockQuantity) && (s.currentQuantity <= s.minStockQuantity * 1.2));
    return data;
  }, [stock, activeTab]);

  const paginated = useMemo(() => {
    const start = (page - 1) * PAGE_SIZE;
    return filtered.slice(start, start + PAGE_SIZE);
  }, [filtered, page]);

  useEffect(() => { setPage(1); }, [stationFilter, activeTab]);

  const rowColor = (row) => {
    if (row.minStockQuantity == null) return {};
    if (row.currentQuantity <= row.minStockQuantity) return { background: 'var(--danger-subtle)' };
    if (row.currentQuantity <= row.minStockQuantity * 1.2) return { background: 'var(--warning-subtle)' };
    return { background: 'color-mix(in srgb, var(--success) 7%, transparent)' };
  };

  const requestSupply = async (row) => {
    try {
      const qty = Math.max((row.minStockQuantity || 0) - (row.currentQuantity || 0), 0);
      await client.post('/api/warehouse/dispatch', {
        subStationId: row.stationId,
        stationId: row.stationId,
        items: [{ sparePartId: row.sparePartId, requestedQuantity: qty, notes: '' }],
      });
      alert(ar ? 'تم إرسال طلب التوريد' : 'Supply request sent');
    } catch { alert(ar ? 'فشل إرسال الطلب' : 'Failed to send request'); }
  };

  return (
    <div style={{ animation: 'fadeIn 0.4s ease' }}>
      <div className="page-header">
        <div className="page-header-row">
          <div>
            <h1 style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
              <PackageSearch size={24} style={{ color: 'var(--primary)' }} />
              {ar ? 'المخزون الفعلي' : 'Warehouse Stock'}
            </h1>
            <p style={{ opacity: 0.8, fontSize: '0.9rem' }}>
              {ar ? 'عرض المخزون الفعلي والاحتياطي والمتاح' : 'View actual, reserved, and available stock'}
            </p>
          </div>
          <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
            <select className="form-control" value={stationFilter} onChange={e => setStationFilter(e.target.value)} style={{ width: 220 }}>
              <option value="">{ar ? 'كل المواقع' : 'All Stations'}</option>
              {stations.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
            </select>
            <button className="btn btn-ghost" onClick={load}><RefreshCw size={16} /></button>
          </div>
        </div>
      </div>

      {error && <div className="alert-inline alert-danger" style={{ marginBottom: 20 }}>{error}</div>}

      <div style={{ display: 'flex', gap: 8, marginBottom: 20 }}>
        <button className={`btn ${activeTab === 'all' ? 'btn-primary' : 'btn-ghost'}`} onClick={() => setActiveTab('all')}>{ar ? 'الكل' : 'All'}</button>
        <button className={`btn ${activeTab === 'low' ? 'btn-primary' : 'btn-ghost'}`} onClick={() => setActiveTab('low')}>{ar ? 'نقص حاد' : 'Low Stock'}</button>
        <button className={`btn ${activeTab === 'near' ? 'btn-primary' : 'btn-ghost'}`} onClick={() => setActiveTab('near')}>{ar ? 'قرب النقص' : 'Near Min'}</button>
      </div>

      {loading ? (
        <div className="ft-spinner" style={{ width: 40, height: 40, margin: '40px auto' }} />
      ) : (
        <>
          <div className="data-table" style={{ overflowX: 'auto' }}>
            <table>
              <thead>
                <tr>
                  <th>{ar ? 'رقم القطعة' : 'Part Number'}</th>
                  <th>{ar ? 'الاسم' : 'Name'}</th>
                  <th>{ar ? 'الفئة' : 'Category'}</th>
                  <th>{ar ? 'الوحدة' : 'Unit'}</th>
                  <th>{ar ? 'الكمية الحالية' : 'Current Qty'}</th>
                  <th>{ar ? 'المحجوز' : 'Reserved'}</th>
                  <th>{ar ? 'المتاح' : 'Available'}</th>
                  <th>{ar ? 'الحد الأدنى' : 'Min Stock'}</th>
                  <th style={{ textAlign: 'center' }}>{ar ? 'إجراء' : 'Action'}</th>
                </tr>
              </thead>
              <tbody>
                {paginated.length === 0 && (
                  <tr><td colSpan={9} style={{ textAlign: 'center', padding: 40, opacity: 0.7 }}>{ar ? 'لا توجد بيانات' : 'No data'}</td></tr>
                )}
                {paginated.map(row => (
                  <tr key={row.sparePartId + '-' + row.stationId} style={rowColor(row)}>
                    <td style={{ fontWeight: 700 }}>{row.partNumber}</td>
                    <td>{row.sparePartName}</td>
                    <td>{row.category || '—'}</td>
                    <td>{row.unitName || '—'}</td>
                    <td style={{ fontWeight: 700 }}>{fmt(row.currentQuantity)}</td>
                    <td style={{ color: 'var(--warning)' }}>{fmt(row.reservedQuantity)}</td>
                    <td style={{ color: row.availableQuantity <= 0 ? 'var(--danger)' : 'var(--primary)', fontWeight: 700 }}>{fmt(row.availableQuantity)}</td>
                    <td>{fmt(row.minStockQuantity)}</td>
                    <td style={{ textAlign: 'center' }}>
                      {(row.availableQuantity <= 0 || row.currentQuantity <= (row.minStockQuantity || 0)) && canRequest && (
                        <button className="btn btn-sm btn-primary" onClick={() => requestSupply(row)} style={{ display: 'inline-flex', alignItems: 'center', gap: 4 }}>
                          <AlertTriangle size={14} /> {ar ? 'طلب توريد' : 'Request Supply'}
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <Pagination page={page} setPage={setPage} total={filtered.length} pageSize={PAGE_SIZE} />
        </>
      )}
    </div>
  );
}

export default WarehouseStock;
