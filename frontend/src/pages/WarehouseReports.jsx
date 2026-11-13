import React, { useState, useEffect, useMemo } from 'react';
import { BarChart2, Download, Printer } from 'lucide-react';
import client from '../api/client';

function WarehouseReports({ lang, embedded = false }) {
  const ar = lang === 'ar';
  const [activeTab, setActiveTab] = useState('stock');
  const [filters, setFilters] = useState({ from: '', to: '', stationId: '', generatorId: '', subStationId: '', category: '' });
  const [reportData, setReportData] = useState([]);
  const [loading, setLoading] = useState(false);

  const loadReport = async () => {
    setLoading(true);
    try {
      let res;
      const params = {};
      if (filters.stationId) params.stationId = filters.stationId;
      if (filters.from) params.startDate = filters.from;
      if (filters.to) params.endDate = filters.to;
      if (filters.generatorId) params.generatorId = filters.generatorId;
      if (filters.subStationId) params.subStationId = filters.subStationId;
      if (filters.category) params.type = filters.category;

      if (activeTab === 'stock') {
        res = await client.get('/api/warehouse/reports/stock', { params });
      } else if (activeTab === 'movements') {
        if (!params.stationId || !params.startDate || !params.endDate) {
          setReportData([]);
          setLoading(false);
          return;
        }
        res = await client.get('/api/warehouse/reports/movements', { params });
      } else if (activeTab === 'lowstock') {
        res = await client.get('/api/warehouse/reports/low-stock-alert');
      } else if (activeTab === 'supply') {
        if (!params.stationId || !params.startDate || !params.endDate) {
          setReportData([]);
          setLoading(false);
          return;
        }
        res = await client.get('/api/warehouse/reports/supply', { params });
      } else if (activeTab === 'dispatch') {
        const id = params.subStationId || params.stationId;
        if (!id) {
          setReportData([]);
          setLoading(false);
          return;
        }
        const route = params.subStationId
          ? `/api/warehouse/reports/by-substation/${params.subStationId}`
          : `/api/warehouse/reports/by-substation/${params.stationId}`;
        res = await client.get(route, { params: { startDate: params.startDate, endDate: params.endDate } });
      } else if (activeTab === 'generator') {
        if (!params.generatorId) {
          setReportData([]);
          setLoading(false);
          return;
        }
        res = await client.get(`/api/warehouse/reports/by-generator/${params.generatorId}`, { params });
      } else if (activeTab === 'inventory') {
        if (!params.stationId || !params.type) {
          setReportData([]);
          setLoading(false);
          return;
        }
        const year = params.startDate ? new Date(params.startDate).getFullYear() : new Date().getFullYear();
        params.year = year;
        params.period = 'FullYear';
        res = await client.get('/api/warehouse/reports/periodic', { params });
      }

      const data = res?.data?.items || res?.data || res?.data?.transactions || res?.data?.lowStockItems || [];
      setReportData(Array.isArray(data) ? data : []);
    } catch {
      setReportData([]);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { loadReport(); }, [activeTab, filters]); // eslint-disable-line

  const exportExcel = async () => {
    try {
      const params = { type: activeTab, ...filters };
      const res = await client.get('/api/warehouse/reports/export', { params, responseType: 'blob' });
      const url = window.URL.createObjectURL(new Blob([res.data]));
      const a = document.createElement('a'); a.href = url;
      a.download = `${activeTab}-report.xlsx`;
      a.click(); window.URL.revokeObjectURL(url);
    } catch {
      // silent
    }
  };

  const printReport = () => window.print();

  const columns = useMemo(() => {
    if (activeTab === 'stock') return [ar ? 'القطعة' : 'Part', ar ? 'الصنف' : 'Category', ar ? 'الوحدة' : 'Unit', ar ? 'الموقع' : 'Station', ar ? 'الكمية الحالية' : 'Current', ar ? 'المحجوز' : 'Reserved', ar ? 'المتاح' : 'Available', ar ? 'الحد الأدنى' : 'Min Stock'];
    if (activeTab === 'movements' || activeTab === 'generator' || activeTab === 'dispatch') return [ar ? '#' : '#', ar ? 'البيان' : 'Description', ar ? 'النوع' : 'Type', ar ? 'الكمية' : 'Qty', ar ? 'الرصيد قبل' : 'Before', ar ? 'الرصيد بعد' : 'After', ar ? 'التاريخ' : 'Date'];
    if (activeTab === 'lowstock') return [ar ? 'القطعة' : 'Part', ar ? 'الصنف' : 'Category', ar ? 'الوحدة' : 'Unit', ar ? 'الموقع' : 'Station', ar ? 'الحد الأدنى' : 'Min Stock'];
    if (activeTab === 'supply') return [ar ? 'رقم التوريد' : 'Supply #', ar ? 'الموقع' : 'Station', ar ? 'التاريخ' : 'Date', ar ? 'المورد' : 'Supplier', ar ? 'الفاتورة' : 'Invoice', ar ? 'الحالة' : 'Status', ar ? 'عدد المواد' : 'Items'];
    if (activeTab === 'inventory') return [ar ? '# ' : '#', ar ? 'البيان' : 'Description', ar ? 'الكمية' : 'Qty', ar ? 'المبلغ' : 'Amount', ar ? 'التاريخ' : 'Date'];
    return [ar ? '# ' : '#', ar ? 'البيان' : 'Description', ar ? 'الكمية' : 'Qty', ar ? 'المبلغ' : 'Amount', ar ? 'التاريخ' : 'Date'];
  }, [activeTab, ar]);

  const renderCell = (row, i) => {
    if (activeTab === 'stock') {
      return (
        <>
          <td>{row.partName || row.SparePartName || '—'}</td>
          <td>{row.category || row.PartCategoryLookupName || '—'}</td>
          <td>{row.unitName || row.UnitLookupName || '—'}</td>
          <td>{row.stationName || '—'}</td>
          <td>{row.currentQuantity ?? '—'}</td>
          <td>{row.reservedQuantity ?? '—'}</td>
          <td>{row.availableQuantity ?? '—'}</td>
          <td>{row.minStockQuantity ?? '—'}</td>
        </>
      );
    }
    if (activeTab === 'lowstock') {
      return (
        <>
          <td>{row.partName || row.SparePartName || '—'}</td>
          <td>{row.category || row.PartCategoryLookupName || '—'}</td>
          <td>{row.unitName || row.UnitLookupName || '—'}</td>
          <td>{row.stationName || '—'}</td>
          <td>{row.minStockQuantity ?? '—'}</td>
        </>
      );
    }
    if (activeTab === 'supply') {
      return (
        <>
          <td>{row.supplyNumber || '—'}</td>
          <td>{row.stationName || '—'}</td>
          <td>{row.supplyDate || row.createdAt || '—'}</td>
          <td>{row.supplierName || '—'}</td>
          <td>{row.invoiceNumber || '—'}</td>
          <td>{row.status || '—'}</td>
          <td>{row.itemsCount ?? row.itemCount ?? '—'}</td>
        </>
      );
    }
    if (activeTab === 'movements' || activeTab === 'generator' || activeTab === 'dispatch') {
      return (
        <>
          <td>{i + 1}</td>
          <td>{row.sparePartName || row.description || row.partName || row.stationName || '—'}</td>
          <td>{row.transactionTypeName || row.type || '—'}</td>
          <td>{row.quantity ?? row.qty ?? '—'}</td>
          <td>{row.balanceBefore ?? '—'}</td>
          <td>{row.balanceAfter ?? '—'}</td>
          <td>{row.transactionDate || row.date || row.createdAt || '—'}</td>
        </>
      );
    }
    return (
      <>
        <td>{i + 1}</td>
        <td>{row.description || row.partName || row.stationName || '—'}</td>
        <td>{row.quantity ?? row.qty ?? '—'}</td>
        <td>{row.amount ?? '—'}</td>
        <td>{row.date || row.createdAt || '—'}</td>
      </>
    );
  };

  const tabs = [
    { key: 'stock', label: ar ? 'تقرير المخزون' : 'Stock Report' },
    { key: 'movements', label: ar ? 'الحركات' : 'Movements' },
    { key: 'lowstock', label: ar ? 'نقص المخزون' : 'Low Stock' },
    { key: 'supply', label: ar ? 'التوريد حسب الفترة' : 'Supply by Period' },
    { key: 'dispatch', label: ar ? 'الصرف حسب الموقع' : 'Dispatch by Site' },
    { key: 'generator', label: ar ? 'الصرف حسب المولد' : 'Dispatch by Generator' },
    { key: 'inventory', label: ar ? 'الجرد الدوري' : 'Periodic Inventory' },
  ];

  const showDateRange = ['movements', 'supply', 'dispatch', 'generator', 'inventory'].includes(activeTab);
  const showStation = ['stock', 'movements', 'supply', 'inventory'].includes(activeTab);
  const showGenerator = ['generator'].includes(activeTab);
  const showSubStation = ['dispatch'].includes(activeTab);
  const showCategory = ['inventory'].includes(activeTab);

  return (
    <div style={{ animation: 'fadeIn 0.4s ease' }}>
      {!embedded && <div className="page-header">
        <div className="page-header-row">
          <div>
            <h1 style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
              <BarChart2 size={24} style={{ color: 'var(--primary)' }} />
              {ar ? 'تقارير المخازن' : 'Warehouse Reports'}
            </h1>
            <p style={{ opacity: 0.8, fontSize: '0.9rem' }}>
              {ar ? 'تقارير متقدمة للمخازن والحركات' : 'Advanced warehouse and movement reports'}
            </p>
          </div>
          <div style={{ display: 'flex', gap: 8 }}>
            <button className="btn btn-ghost" onClick={printReport} style={{ display: 'flex', alignItems: 'center', gap: 6 }}><Printer size={16} /> {ar ? 'طباعة' : 'Print'}</button>
            <button className="btn btn-ghost" onClick={exportExcel} style={{ display: 'flex', alignItems: 'center', gap: 6 }}><Download size={16} /> {ar ? 'تصدير' : 'Export'}</button>
          </div>
        </div>
      </div>}

      {embedded && (
        <div className="page-header-row" style={{ marginBottom: 20 }}>
          <div>
            <h2 style={{ display: 'flex', alignItems: 'center', gap: 10, margin: 0 }}>
              <BarChart2 size={21} style={{ color: 'var(--primary)' }} />
              {ar ? 'تقارير المخازن' : 'Warehouse Reports'}
            </h2>
          </div>
          <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
            <button className="btn btn-ghost" onClick={printReport} style={{ display: 'flex', alignItems: 'center', gap: 6 }}><Printer size={16} /> {ar ? 'طباعة' : 'Print'}</button>
            <button className="btn btn-ghost" onClick={exportExcel} style={{ display: 'flex', alignItems: 'center', gap: 6 }}><Download size={16} /> {ar ? 'تصدير' : 'Export'}</button>
          </div>
        </div>
      )}

      <div style={{ display: 'flex', gap: 8, marginBottom: 20, flexWrap: 'wrap' }}>
        {tabs.map(t => (
          <button key={t.key} className={`btn ${activeTab === t.key ? 'btn-primary' : 'btn-ghost'}`} onClick={() => setActiveTab(t.key)}>{t.label}</button>
        ))}
      </div>

      <div className="section-card" style={{ padding: 16, marginBottom: 20, display: 'flex', gap: 12, flexWrap: 'wrap', alignItems: 'center' }}>
        {showDateRange && (
          <>
            <input type="date" className="form-control" value={filters.from} onChange={e => setFilters({ ...filters, from: e.target.value })} style={{ width: 160 }} />
            <input type="date" className="form-control" value={filters.to} onChange={e => setFilters({ ...filters, to: e.target.value })} style={{ width: 160 }} />
          </>
        )}
        {showStation && (
          <input className="form-control" value={filters.stationId} onChange={e => setFilters({ ...filters, stationId: e.target.value })} placeholder={ar ? 'رقم الموقع' : 'Station ID'} style={{ width: 160 }} />
        )}
        {showGenerator && (
          <input className="form-control" value={filters.generatorId} onChange={e => setFilters({ ...filters, generatorId: e.target.value })} placeholder={ar ? 'رقم المولد' : 'Generator ID'} style={{ width: 160 }} />
        )}
        {showSubStation && (
          <input className="form-control" value={filters.subStationId} onChange={e => setFilters({ ...filters, subStationId: e.target.value })} placeholder={ar ? 'رقم الموقع الفرعي' : 'SubStation ID'} style={{ width: 160 }} />
        )}
        {showCategory && (
          <select className="form-control" value={filters.category} onChange={e => setFilters({ ...filters, category: e.target.value })} style={{ width: 180 }}>
            <option value="">{ar ? 'كل الأنواع' : 'All Types'}</option>
            <option value="FullYear">{ar ? 'سنة كاملة' : 'Full Year'}</option>
            <option value="FirstHalf">{ar ? 'النصف الأول' : 'First Half'}</option>
            <option value="SecondHalf">{ar ? 'النصف الثاني' : 'Second Half'}</option>
          </select>
        )}
      </div>

      {loading ? (
        <div className="ft-spinner" style={{ width: 40, height: 40, margin: '40px auto' }} />
      ) : (
        <div className="data-table" style={{ overflowX: 'auto' }}>
          <table>
            <thead>
              <tr>
                {columns.map((c, i) => <th key={i}>{c}</th>)}
              </tr>
            </thead>
            <tbody>
              {reportData.length === 0 && (
                <tr><td colSpan={columns.length} style={{ textAlign: 'center', padding: 40, opacity: 0.7 }}>{ar ? 'لا توجد بيانات' : 'No data'}</td></tr>
              )}
              {reportData.map((r, i) => (
                <tr key={i}>{renderCell(r, i)}</tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

export default WarehouseReports;
