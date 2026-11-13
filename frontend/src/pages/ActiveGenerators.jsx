import React, { useEffect, useState, useMemo } from 'react';
import { Zap, Search } from 'lucide-react';
import client from '../api/client';
import Pagination from '../components/Pagination';

const statusConfig = {
  'شغال': { cls: 'badge-success', dot: 'online', label: { ar: 'يعمل', en: 'Running' } },
  'واقف': { cls: 'badge-warning', dot: 'idle', label: { ar: 'خامل', en: 'Idle' } },
  'تحت الصيانة': { cls: 'badge-danger', dot: 'offline', label: { ar: 'صيانة', en: 'Maintenance' } },
};

function StatusBadge({ status, lang }) {
  if (!status) {
    return (
      <span className="badge badge-secondary">
        <span className="status-dot idle" />
        {lang === 'ar' ? 'غير محدد' : 'Unknown'}
      </span>
    );
  }
  const cfg = statusConfig[status];
  if (!cfg) {
    return (
      <span className="badge badge-secondary">
        <span className="status-dot idle" />
        {status}
      </span>
    );
  }
  return (
    <span className={`badge ${cfg.cls}`}>
      <span className={`status-dot ${cfg.dot}`} />
      {cfg.label[lang] || status}
    </span>
  );
}

function ActiveGenerators({ t, lang, currentUser }) {
  const ar = lang === 'ar';
  const [generators, setGenerators] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const PAGE_SIZE = 20;

  useEffect(() => {
    const load = async () => {
      setLoading(true);
      setError(null);
      try {
        const res = await client.get('/api/generators/active', { params: { _t: Date.now() } });
        setGenerators(Array.isArray(res.data) ? res.data : []);
      } catch {
        setError(t.noData);
      } finally {
        setLoading(false);
      }
    };
    load();
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  const filtered = generators.filter(g => {
    const q = search.toLowerCase();
    return !search ||
      g.name?.toLowerCase().includes(q) ||
      g.location?.toLowerCase().includes(q) ||
      g.manufacturer?.toLowerCase().includes(q) ||
      g.controllerName?.toLowerCase().includes(q);
  });

  const filteredTotal = filtered.length;
  const totalPages = Math.max(1, Math.ceil(filteredTotal / PAGE_SIZE));
  const safePage = Math.min(page, totalPages);
  const paginatedFiltered = filtered.slice((safePage - 1) * PAGE_SIZE, safePage * PAGE_SIZE);

  const handleSearchChange = (e) => { setSearch(e.target.value); setPage(1); };

  const groupedGenerators = useMemo(() => {
    const groups = {};
    paginatedFiltered.forEach(gen => {
      const key = gen.location || 'unassigned';
      if (!groups[key]) {
        groups[key] = { label: gen.location || (ar ? 'غير محدد' : 'Unassigned'), gens: [] };
      }
      groups[key].gens.push(gen);
    });
    return groups;
  }, [paginatedFiltered, ar]);

  return (
    <div>
      <div className="page-header">
        <div className="page-header-row">
          <div>
            <h1>{t.generators}</h1>
            <p>{ar ? 'المولدات النشطة المرتبطة بشاشات التحكم' : 'Active generators linked to control screens'}</p>
          </div>
        </div>
      </div>

      <div style={{ marginBottom: 20 }}>
        <div className="search-bar" style={{ maxWidth: 340 }}>
          <Search size={16} />
          <input className="form-control"
            placeholder={ar ? 'بحث بالاسم أو الموقع أو الشاشة...' : 'Search by name, location or controller...'}
            value={search} onChange={handleSearchChange} />
        </div>
      </div>

      {error && <div className="alert-inline alert-inline-danger">{error}</div>}

      {loading ? (
        <div className="spinner-wrap"><div className="ft-spinner" /></div>
      ) : filtered.length === 0 ? (
        <div className="empty-state"><Zap size={48} /><h3>{t.noData}</h3></div>
      ) : (
        <>
          <div className="grouped-container">
            {Object.entries(groupedGenerators).map(([groupKey, group]) => (
              <div key={groupKey} className="station-group" style={{ marginBottom: 40 }}>
                <div className="station-group-header">
                  <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', width: 42, height: 42, borderRadius: 10, background: 'var(--primary-light)', color: 'var(--primary)' }}>
                  </div>
                  <div>
                    <h3 style={{ margin: 0, fontSize: 18, fontWeight: 'bold', color: 'var(--text-main)' }}>{group.label}</h3>
                    <div style={{ fontSize: 12, color: 'var(--text-muted)', marginTop: 4 }}>{ar ? 'المولدات النشطة المرتبطة بشاشات التحكم' : 'Active generators linked to control screens'}</div>
                  </div>
                  <span style={{ marginInlineStart: 'auto', padding: '6px 14px', background: 'var(--primary)', color: '#fff', borderRadius: 20, fontSize: 13, fontWeight: 'bold' }}>
                    {group.gens.length} {ar ? 'مولدات' : 'Generators'}
                  </span>
                </div>
                <div className="grid-auto">
                  {group.gens.map(gen => (
                    <div key={gen.id} className="gen-card">
                      <div className="gen-card-header">
                        <div>
                          <div className="gen-name">{gen.name}</div>
                          <div className="gen-model">{gen.manufacturer} {gen.model && `— ${gen.model}`}</div>
                        </div>
                        <StatusBadge status={gen.statusLookupName} lang={lang} />
                        <span className={`status-badge ${gen.isActive ? 'status-active' : 'status-inactive'}`}>
                          {gen.isActive ? (ar ? 'نشط' : 'Active') : (ar ? 'غير نشط' : 'Inactive')}
                        </span>
                      </div>
                      <div className="gen-stats">
                        <div className="gen-stat-item">
                          <div className="gen-stat-label">{ar ? 'الشاشة المرتبطة' : 'Linked Controller'}</div>
                          <div className="gen-stat-value" style={{ fontSize: 13 }}>{gen.controllerName || '—'} {gen.controllerCode && <span style={{ color: 'var(--text-muted)', fontSize: 11 }}>({gen.controllerCode})</span>}</div>
                        </div>
                        <div className="gen-stat-item">
                          <div className="gen-stat-label">{ar ? 'نوع الشاشة' : 'Controller Type'}</div>
                          <div className="gen-stat-value">{gen.controllerType || '—'}</div>
                        </div>
                        <div className="gen-stat-item">
                          <div className="gen-stat-label">{ar ? 'القدرة الاسمية' : 'Nominal Power'}</div>
                          <div className="gen-stat-value">{gen.nominalPower ? `${gen.nominalPower} KW` : '—'}</div>
                        </div>
                        <div className="gen-stat-item">
                          <div className="gen-stat-label">{ar ? 'القدرة الفعلية' : 'Actual Power'}</div>
                          <div className="gen-stat-value">{gen.actualPowerKW ? `${gen.actualPowerKW} KW` : '—'}</div>
                        </div>
                        <div className="gen-stat-item">
                          <div className="gen-stat-label">{ar ? 'نوع المولد' : 'Type'}</div>
                          <div className="gen-stat-value">{gen.generatorTypeLookupName || '—'}</div>
                        </div>
                        <div className="gen-stat-item">
                          <div className="gen-stat-label">{ar ? 'الطاقة الحالية' : 'Current Energy'}</div>
                          <div className="gen-stat-value">{gen.controllerEnergyReading ? `${gen.controllerEnergyReading.toLocaleString('en-US')} KWh` : '—'}</div>
                        </div>
                        <div className="gen-stat-item">
                          <div className="gen-stat-label">{ar ? 'ساعات التشغيل' : 'Operating Hours'}</div>
                          <div className="gen-stat-value">{gen.controllerOperatingHours ? `${gen.controllerOperatingHours} h` : '—'}</div>
                        </div>
                        <div className="gen-stat-item">
                          <div className="gen-stat-label">{ar ? 'الفولت' : 'Voltage'}</div>
                          <div className="gen-stat-value">{gen.voltageLookupName || '—'}</div>
                        </div>
                        <div className="gen-stat-item">
                          <div className="gen-stat-label">{ar ? 'التردد' : 'Frequency'}</div>
                          <div className="gen-stat-value">{gen.frequencyLookupName || '—'}</div>
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            ))}
          </div>
          {filteredTotal > PAGE_SIZE && (
            <Pagination page={safePage} totalPages={totalPages} total={filteredTotal} pageSize={PAGE_SIZE} lang={lang}
              onPageChange={setPage} />
          )}
        </>
      )}
    </div>
  );
}

export default ActiveGenerators;
