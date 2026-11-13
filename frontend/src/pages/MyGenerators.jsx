import React, { useState, useEffect } from 'react';
import client from '../api/client';
import { Zap, Plus, AlertCircle, Droplet, Layout, Activity } from 'lucide-react';
import { Link } from 'react-router-dom';

const statusCfg = {
  Running:     { cls: 'badge-success', dot: 'online', label: 'يعمل' },
  Idle:        { cls: 'badge-warning', dot: 'idle',   label: 'خامل' },
  Maintenance: { cls: 'badge-danger',  dot: 'offline', label: 'صيانة' },
  Offline:     { cls: 'badge-muted',   dot: 'offline', label: 'متوقف' },
};

const getGeneratorSiteName = (generator) => (
  generator?.subStationName || generator?.stationName || '—'
);

function MyGenerators({ lang, currentUser }) {
  const ar = lang === 'ar';
  const canAddOil = currentUser?.role === 'admin' || currentUser?.permissions?.oil?.add || currentUser?.permissions?.oil?.create;
  const [generators, setGenerators] = useState([]);
  const [loading, setLoading] = useState(true);
  const siteCards = Array.from(
    new Map(
      generators
        .map(g => {
          const { stationId, subStationId } = g;
          if (!stationId) return null;
          const key = subStationId || `station:${stationId}`;
          const stationName = g.stationName || stationId;
          const siteName = getGeneratorSiteName(g);
          return [key, { key, stationId, subStationId, stationName, siteName }];
        })
        .filter(Boolean)
    ).values()
  );

  useEffect(() => {
    const fetchData = async () => {
      setLoading(true);
      try {
        const genRes = await client.get('/api/generators/my');
        setGenerators(Array.isArray(genRes.data) ? genRes.data : []);
      } catch {}
      finally { setLoading(false); }
    };
    fetchData();
  }, []);

  return (
    <div>
      <div className="page-header">
        <div>
          <h1 style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
            <Zap size={22} style={{ color: 'var(--primary)' }} />
            {ar ? 'مولداتي' : 'My Generators'}
          </h1>
          <p>{ar ? `مرحباً ${currentUser?.name || ''} — اختر المولد لإدخال البيانات` : `Welcome ${currentUser?.name || ''} — Select a generator to enter data`}</p>
        </div>
      </div>

      {loading ? (
        <div className="spinner-wrap"><div className="ft-spinner" /></div>
      ) : (
        <>
          {/* قسم رصد المحطة / الموقع */}
          <div style={{ marginBottom: 40 }}>
            <h2 style={{ fontSize: 13, display: 'flex', alignItems: 'center', gap: 10, marginBottom: 20, color: 'var(--secondary)', textTransform: 'uppercase', letterSpacing: '1px', fontWeight: 800 }}>
              <Layout size={18} />
              {ar ? 'رصد الطاقة المرسلة (للموقع ككل)' : 'Site Energy Monitoring (Site-wide)'}
               <div style={{ flex: 1, height: '1px', background: 'var(--border)', marginInlineStart: 15 }} />
            </h2>
            <div className="grid-auto" style={{ gridTemplateColumns: 'repeat(auto-fill, minmax(350px, 1fr))' }}>
              {siteCards.map(site => {
                const readingUrl = site.subStationId
                  ? `/stations/${site.stationId}/reading?subStation=${site.subStationId}&siteName=${encodeURIComponent(site.siteName)}`
                  : `/stations/${site.stationId}/reading`;
                return (
                  <div key={site.key} className="section-card" style={{ padding: '24px', display: 'flex', justifyContent: 'space-between', alignItems: 'center', background: 'var(--card-2)', border: '1px solid var(--border-light)' }}>
                    <div>
                      <div style={{ fontWeight: 800, fontSize: 16, color: 'var(--text)', marginBottom: 4 }}>{site.siteName}</div>
                      <div style={{ fontSize: 11, color: 'var(--text-muted)', textTransform: 'uppercase', letterSpacing: '0.5px' }}>
                        {ar ? 'محطة توليد / موقع' : 'Power Station / Site'}
                      </div>
                    </div>
                    <Link to={readingUrl} className="btn btn-primary" style={{ padding: '10px 20px', fontSize: 13, gap: 10, boxShadow: '0 4px 12px hsla(210,100%,50%,0.2)' }}>
                      <Activity size={16} />
                      {ar ? 'إدخال قراءة الموقع' : 'Enter Site Reading'}
                    </Link>
                  </div>
                );
              })}
            </div>
          </div>

          <h2 style={{ fontSize: 13, display: 'flex', alignItems: 'center', gap: 10, marginBottom: 20, color: 'var(--primary)', textTransform: 'uppercase', letterSpacing: '1px', fontWeight: 800 }}>
            <Zap size={18} />
            {ar ? 'قراءات المولدات الفردية' : 'Individual Generator Readings'}
            <div style={{ flex: 1, height: '1px', background: 'var(--border)', marginInlineStart: 15 }} />
          </h2>

          <div className="grid-auto">
            {generators.map(gen => {
              const cfg = statusCfg[gen.status] || statusCfg.Idle;
              const oilLow = gen.assignedOilTypes && gen.assignedOilTypes.some(o => o.currentStock <= o.minStock);

              return (
                <div key={gen.id} className="gen-card">
                  <div className="gen-card-header">
                    <div>
                      <div className="gen-name">{gen.name}</div>
                      <div className="gen-model">{getGeneratorSiteName(gen)}</div>
                    </div>
                    <span className={`badge ${cfg.cls}`}>
                      <span className={`status-dot ${cfg.dot}`} />
                      {cfg.label}
                    </span>
                  </div>

                  <div className="gen-stats">
                    <div className="gen-stat-item">
                      <div className="gen-stat-label">{ar ? 'آخر قراءة طاقة' : 'Last Power'}</div>
                      <div className="gen-stat-value">{gen.lastPowerRecord ? `${gen.lastPowerRecord.toLocaleString('en-US')} KWh` : '—'}</div>
                    </div>
                    <div className="gen-stat-item">
                      <div className="gen-stat-label">{ar ? 'آخر قراءة ساعات' : 'Last Hours'}</div>
                      <div className="gen-stat-value">{gen.lastHoursRecord ? `${gen.lastHoursRecord} h` : '—'}</div>
                    </div>
                    <div className="gen-stat-item">
                      <div className="gen-stat-label">{ar ? 'القدرة' : 'Capacity'}</div>
                      <div className="gen-stat-value">{gen.actualPowerKW ? `${gen.actualPowerKW} KW` : '—'}</div>
                    </div>
                    <div className="gen-stat-item">
                      <div className="gen-stat-label">{ar ? 'نوع الزيت' : 'Oil Type'}</div>
                      <div className="gen-stat-value" style={{ fontSize: 12, color: oilLow ? 'var(--danger)' : 'var(--text)' }}>
                        {gen.assignedOilTypes && gen.assignedOilTypes.length > 0 
                          ? gen.assignedOilTypes.map(o => o.name).join(' ، ')
                          : '—'}
                        {oilLow && ' ⚠'}
                      </div>
                    </div>
                  </div>

                  {/* Alerts */}
                  {oilLow && (
                    <div style={{
                      display: 'flex', alignItems: 'center', gap: 8,
                      background: 'var(--danger-glow)', border: '1px solid hsla(4,84%,60%,0.25)',
                      borderRadius: 8, padding: '8px 12px', fontSize: 12,
                    }}>
                      <AlertCircle size={14} style={{ color: 'var(--danger)', flexShrink: 0 }} />
                      <span style={{ color: 'var(--danger)' }}>
                        {gen.assignedOilTypes?.filter(o => o.currentStock <= o.minStock).map(o => (
                          <div key={o.id}>{ar ? `مخزون زيت (${o.name}) منخفض: ${o.currentStock} لتر` : `Oil (${o.name}) low: ${o.currentStock} L`}</div>
                        ))}
                      </span>
                    </div>
                  )}

                  {/* Actions */}
                  <div className="gen-card-actions" style={{ flexWrap: 'wrap' }}>
                    <Link to={`/generators/${gen.id}/add-reading`} className="btn btn-primary btn-sm">
                      <Plus size={13} /> {ar ? 'إدخال قراءة' : 'Add Reading'}
                    </Link>
                    {canAddOil && (
                      <Link to={`/generators/${gen.id}/add-oil`} className="btn btn-sm" style={{
                        background: 'var(--warning-glow)', color: 'var(--warning)', border: '1px solid transparent',
                      }}>
                        <Droplet size={13} /> {ar ? 'إضافة زيت' : 'Add Oil'}
                      </Link>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        </>
      )}
    </div>
  );
}

export default MyGenerators;
