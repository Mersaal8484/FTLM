import React, { useState, useEffect } from 'react';
import client from '../api/client';
import { Droplet, Check, X, AlertTriangle, PackageSearch, MapPin,   Fuel } from 'lucide-react';

function OilInventory({ lang }) {
  const ar = lang === 'ar';
  const [oils, setOils] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [successMsg, _setSuccessMsg] = useState('');
  const [selectedOil, setSelectedOil] = useState(null);

  const fetchOils = async () => {
    setLoading(true);
    try {
      const oilsRes = await client.get('/api/oils');
      setOils(oilsRes.data);
    } catch {
      setError(ar ? 'فشل تحميل البيانات' : 'Failed to load data');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { fetchOils(); }, []); // eslint-disable-line react-hooks/exhaustive-deps

  const lowStock = oils.filter(o => o.currentStock <= o.minStock).length;
  const totalOils = oils.length;
  const totalLiters = oils.reduce((s, o) => s + (o.currentStock || 0), 0);

  // ── تجميع الزيوت بناءً على المحطة (Group by Station) ──
  const groupedOils = {};
  oils.forEach(oil => {
    const stationName = oil.stationName || (ar ? 'غير محدد (بدون محطة)' : 'Unassigned');
    if (!groupedOils[stationName]) groupedOils[stationName] = [];
    groupedOils[stationName].push(oil);
  });

  return (
    <div>
      {/* Page Header */}
      <div className="page-header">
        <div className="page-header-row">
          <div>
            <h1 style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
              <Droplet size={22} style={{ color: 'var(--warning)' }} />
              {ar ? 'مخزون الزيوت' : 'Oil Inventory'}
            </h1>
            <p>{ar ? 'إدارة ومتابعة مخزون زيوت المحركات' : 'Track and manage engine oil stock'}</p>
          </div>
        </div>
      </div>

      {/* KPI Cards */}
      <div className="kpi-grid" style={{ marginBottom: 24 }}>
        <div className="kpi-card" style={{ '--kpi-color': 'var(--warning)', '--kpi-bg': 'var(--warning-glow)' }}>
          <div className="kpi-icon"><Droplet size={22} /></div>
          <div>
            <div className="kpi-value">{totalOils}</div>
            <div className="kpi-label">{ar ? 'أنواع الزيوت' : 'Oil Types'}</div>
          </div>
        </div>
        <div className="kpi-card" style={{ '--kpi-color': 'var(--primary)', '--kpi-bg': 'var(--primary-glow)' }}>
          <div className="kpi-icon"><PackageSearch size={22} /></div>
          <div>
            <div className="kpi-value">{totalLiters.toFixed(1)}</div>
            <div className="kpi-label">{ar ? 'إجمالي المخزون (لتر)' : 'Total Stock (L)'}</div>
          </div>
        </div>
        <div className="kpi-card" style={{ '--kpi-color': 'var(--danger)', '--kpi-bg': 'var(--danger-glow)' }}>
          <div className="kpi-icon"><AlertTriangle size={22} /></div>
          <div>
            <div className="kpi-value">{lowStock}</div>
            <div className="kpi-label">{ar ? 'تنبيهات النقص' : 'Low Stock Alerts'}</div>
          </div>
        </div>
      </div>

      {/* Alerts */}
      {error && (
        <div className="alert alert-danger" style={{ marginBottom: 16 }}>
          <X size={14} style={{ display: 'inline', marginLeft: 6 }} />{error}
        </div>
      )}
      {successMsg && (
        <div className="alert alert-success" style={{ marginBottom: 16 }}>{successMsg}</div>
      )}

      {successMsg && (
        <div className="alert alert-success" style={{ marginBottom: 16 }}>{successMsg}</div>
      )}

      {/* Table */}
      {loading ? (
        <div className="spinner-wrap"><div className="ft-spinner" /></div>
      ) : oils.length === 0 ? (
        <div className="empty-state">
          <Droplet size={48} />
          <h3>{ar ? 'لا يوجد زيوت مسجلة' : 'No oils registered'}</h3>
          <p>{ar ? 'أضف نوع زيت جديد للبدء' : 'Add a new oil type to get started'}</p>
        </div>
      ) : (
        <div className="grouped-container">
          {Object.entries(groupedOils).map(([stationName, stationOils]) => (
            <div key={stationName} className="station-group" style={{ marginBottom: 40 }}>
              <div className="station-group-header" style={{ display: 'flex', alignItems: 'center', gap: 15, padding: '15px 20px', background: 'var(--bg-card)', border: '1px solid var(--border)', borderBottom: '2px solid var(--primary)', borderRadius: '12px', marginBottom: 20, boxShadow: '0 4px 15px rgba(0,0,0,0.1)' }}>
                <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', width: 42, height: 42, borderRadius: 10, background: 'var(--primary-light)', color: 'var(--primary)' }}>
                  <MapPin size={22} />
                </div>
                <div>
                  <h3 style={{ margin: 0, fontSize: 18, fontWeight: 'bold', color: 'var(--text-main)' }}>{stationName}</h3>
                  <div style={{ fontSize: 12, color: 'var(--text-muted)', marginTop: 4 }}>{ar ? 'مخزون الزيوت التابع لهذا الموقع' : 'Oil inventory for this location'}</div>
                </div>
                <span style={{ marginRight: ar ? 'auto' : 0, marginLeft: ar ? 0 : 'auto', padding: '6px 14px', background: 'var(--warning)', color: '#fff', borderRadius: 20, fontSize: 13, fontWeight: 'bold' }}>
                  {stationOils.length} {ar ? 'أنواع زيوت' : 'Oil Types'}
                </span>
              </div>
              
              <div className="glass-card" style={{ padding: 0 }}>
                <div style={{ overflowX: 'auto' }}>
                  <table className="ft-table">
                    <thead>
                      <tr>
                        <th>#</th>
                        <th>{ar ? 'نوع الزيت' : 'Oil Type'}</th>
                        <th>{ar ? 'المنطقة' : 'Area'}</th>
                        <th>{ar ? 'الكمية الحالية' : 'Current Stock'}</th>
                        <th>{ar ? 'الحد الأدنى' : 'Min Stock'}</th>
                        <th>{ar ? 'الحالة' : 'Status'}</th>
                      </tr>
                    </thead>
                    <tbody>
                      {stationOils.map((oil, idx) => {
                        const isLow = oil.currentStock <= oil.minStock;
                        const tankCount = oil.fuelTanks?.length || 0;
                        return (
                          <tr key={oil.id} style={{ cursor: 'pointer', ...isLow ? { background: 'hsla(4,84%,60%,0.04)' } : {} }} onClick={() => setSelectedOil(oil)}>
                            <td style={{ color: 'var(--text-faint)', width: 40 }}>{idx + 1}</td>

                            {/* Oil Name */}
                            <td>
                              <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                                <div style={{
                                  width: 32, height: 32, borderRadius: 8, flexShrink: 0,
                                  background: isLow ? 'var(--danger-glow)' : 'var(--warning-glow)',
                                  display: 'flex', alignItems: 'center', justifyContent: 'center',
                                }}>
                                  <Droplet size={14} style={{ color: isLow ? 'var(--danger)' : 'var(--warning)' }} />
                                </div>
                                <div>
                                  <div style={{ fontWeight: 600 }}>{oil.name}</div>
                                  {tankCount > 0 && (
                                    <div style={{ fontSize: 11, color: 'var(--text-muted)' }}>{tankCount} {ar ? 'خزان' : 'tanks'}</div>
                                  )}
                                </div>
                              </div>
                            </td>

                            {/* Area */}
                            <td>
                              <div style={{ fontSize: 13 }}>{oil.subStationName || '—'}</div>
                            </td>

                            {/* Current Stock */}
                            <td>
                              <span style={{ fontWeight: 700, fontSize: 15, color: isLow ? 'var(--danger)' : 'var(--text)' }}>
                                {oil.currentStock} <span style={{ fontSize: 11, color: 'var(--text-muted)', fontWeight: 400 }}>{ar ? 'لتر' : 'L'}</span>
                              </span>
                            </td>

                            {/* Min Stock */}
                            <td>
                              <span style={{ color: 'var(--text-muted)' }}>
                                {oil.minStock} <span style={{ fontSize: 11, fontWeight: 400 }}>{ar ? 'لتر' : 'L'}</span>
                              </span>
                            </td>

                            {/* Status Badge */}
                            <td>
                              {isLow ? (
                                <span className="badge badge-danger">
                                  <AlertTriangle size={10} /> {ar ? 'أقل من الحد' : 'Low Stock'}
                                </span>
                              ) : (
                                <span className="badge badge-success">
                                  <Check size={10} /> {ar ? 'كافٍ' : 'Sufficient'}
                                </span>
                              )}
                            </td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* ── OIL DETAIL MODAL ── */}
      {selectedOil && (
        <div className="modal-overlay" style={{ backdropFilter: 'blur(8px)', zIndex: 1000 }}>
          <div className="glass-card" style={{ width: '100%', maxWidth: 680, maxHeight: '85vh', overflowY: 'auto' }}>
            <div className="glass-card-header">
              <div className="glass-card-title" style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                <Droplet size={20} style={{ color: 'var(--warning)' }} />
                {ar ? 'تفاصيل نوع الزيت' : 'Oil Type Details'}
              </div>
              <button className="btn btn-ghost btn-sm btn-icon" onClick={() => setSelectedOil(null)}><X size={16} /></button>
            </div>
            <div className="glass-card-body">
              {/* Oil Header */}
              <div style={{ display: 'flex', alignItems: 'center', gap: 14, marginBottom: 20 }}>
                <div style={{ width: 48, height: 48, borderRadius: 12, background: 'var(--warning-glow)', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                  <Droplet size={24} style={{ color: 'var(--warning)' }} />
                </div>
                <div>
                  <div style={{ fontSize: 18, fontWeight: 700, color: 'var(--text-main)' }}>{selectedOil.name}</div>
                  <div style={{ fontSize: 13, color: 'var(--text-muted)', display: 'flex', alignItems: 'center', gap: 6, marginTop: 2 }}>
                    <MapPin size={13} />
                    {selectedOil.stationName || (ar ? 'غير محدد' : 'Unassigned')}
                    {selectedOil.subStationName && <> — {selectedOil.subStationName}</>}
                  </div>
                </div>
              </div>

              {/* Stock Levels */}
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: 12, marginBottom: 24 }}>
                {[
                  { label: ar ? 'المخزون الحالي' : 'Current Stock', value: selectedOil.currentStock, color: 'var(--primary)', unit: 'L' },
                  { label: ar ? 'الحد الأدنى' : 'Min Stock', value: selectedOil.minStock, color: 'var(--text-muted)', unit: 'L' },
                  { label: ar ? 'الحد الأقصى' : 'Max Stock', value: selectedOil.maxStock, color: 'var(--success)', unit: 'L' },
                ].map((item, i) => (
                  <div key={i} style={{ background: 'rgba(0,0,0,0.15)', borderRadius: 10, padding: '14px 12px', textAlign: 'center', border: '1px solid var(--border)' }}>
                    <div style={{ fontSize: 11, color: 'var(--text-muted)', marginBottom: 6 }}>{item.label}</div>
                    <div style={{ fontSize: 22, fontWeight: 700, color: item.color }}>{item.value ?? '—'}</div>
                    <div style={{ fontSize: 11, color: 'var(--text-faint)' }}>{item.unit}</div>
                  </div>
                ))}
              </div>

              {/* Fuel Tanks */}
              {selectedOil.fuelTanks && selectedOil.fuelTanks.length > 0 && (
                <div>
                  <div style={{ fontSize: 14, fontWeight: 600, color: 'var(--text-main)', marginBottom: 10, display: 'flex', alignItems: 'center', gap: 8 }}>
                    <Fuel size={15} style={{ color: 'var(--primary)' }} />
                    {ar ? 'خزانات الوقود المرتبطة' : 'Associated Fuel Tanks'} ({selectedOil.fuelTanks.length})
                  </div>
                  <div style={{ overflowX: 'auto' }}>
                    <table className="ft-table" style={{ width: '100%', fontSize: 13 }}>
                      <thead>
                        <tr>
                          <th>#</th>
                          <th>{ar ? 'اسم الخزان' : 'Tank Name'}</th>
                          <th>{ar ? 'نوع الوقود' : 'Fuel Type'}</th>
                          <th>{ar ? 'السعة' : 'Capacity'}</th>
                          <th>{ar ? 'المخزون' : 'Stock'}</th>
                          <th>{ar ? 'الحد الأدنى' : 'Min'}</th>
                          <th>{ar ? 'الحالة' : 'Status'}</th>
                        </tr>
                      </thead>
                      <tbody>
                        {selectedOil.fuelTanks.map((tank, i) => {
                          const tankLow = (tank.currentStock || 0) <= (tank.minStock || 0);
                          return (
                            <tr key={tank.id || i}>
                              <td style={{ color: 'var(--text-faint)' }}>{i + 1}</td>
                              <td style={{ fontWeight: 600 }}>{tank.name}</td>
                              <td>{tank.fuelTypeName || tank.fuelTypeCode || '—'}</td>
                              <td>{tank.capacity ?? '—'} L</td>
                              <td style={{ fontWeight: 700, color: tankLow ? 'var(--danger)' : 'var(--text)' }}>{tank.currentStock ?? '—'} L</td>
                              <td>{tank.minStock ?? '—'} L</td>
                              <td>
                                {tankLow ? (
                                  <span className="badge badge-danger"><AlertTriangle size={10} /> {ar ? 'نقص' : 'Low'}</span>
                                ) : (
                                  <span className="badge badge-success"><Check size={10} /> {ar ? 'ok' : 'OK'}</span>
                                )}
                              </td>
                            </tr>
                          );
                        })}
                      </tbody>
                    </table>
                  </div>
                </div>
              )}
              {(!selectedOil.fuelTanks || selectedOil.fuelTanks.length === 0) && (
                <div style={{ padding: 20, textAlign: 'center', color: 'var(--text-faint)', fontSize: 13, background: 'rgba(0,0,0,0.15)', borderRadius: 10 }}>
                  {ar ? 'لا توجد خزانات وقود مرتبطة بهذا النوع' : 'No fuel tanks linked to this oil type'}
                </div>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

export default OilInventory;