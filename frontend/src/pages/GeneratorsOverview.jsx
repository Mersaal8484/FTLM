import React, { useEffect, useState, useMemo, useCallback } from 'react';
import {
  Zap, Search, ChevronDown, ChevronRight, RefreshCw, X,
  MapPin, Clock, Activity, AlertTriangle, Shield
} from 'lucide-react';
import client from '../api/client';
import connection from '../socket';
import { PageHeader, Button, Modal, FormField, Input } from '../components/ui';

/* ─── Status config ─────────────────────────────────────────────── */
const statusConfig = {
  'شغال':         { cls: 'badge-success', dot: 'online',  label: { ar: 'يعمل',   en: 'Running'     } },
  'واقف':         { cls: 'badge-warning', dot: 'idle',    label: { ar: 'خامل',   en: 'Idle'         } },
  'تحت الصيانة': { cls: 'badge-danger',  dot: 'offline', label: { ar: 'صيانة',  en: 'Maintenance'  } },
};

function StatusBadge({ status, lang }) {
  const cfg = statusConfig[status];
  if (!cfg) return (
    <span className="badge badge-secondary">
      <span className="status-dot idle" />
      {status || (lang === 'ar' ? 'غير محدد' : 'Unknown')}
    </span>
  );
  return (
    <span className={`badge ${cfg.cls}`}>
      <span className={`status-dot ${cfg.dot}`} />
      {cfg.label[lang] || status}
    </span>
  );
}

const fmt = (v, u = '') => v != null && v !== '' ? `${Number(v).toLocaleString('en-US')} ${u}`.trim() : '—';

/* ─── Detail Modal ───────────────────────────────────────────────── */
function GeneratorDetailModal({ gen, lang, onClose }) {
  const ar = lang === 'ar';
  if (!gen) return null;

  const fields = [
    { label: ar ? 'الاسم' : 'Name', value: gen.name },
    { label: ar ? 'الحالة' : 'Status', value: <StatusBadge status={gen.statusLookupName} lang={lang} /> },
    { label: ar ? 'الشاشة المرتبطة' : 'Linked Controller', value: gen.controllerName || (ar ? 'بدون شاشة' : 'No Controller') },
    { label: ar ? 'كود الشاشة' : 'Controller Code', value: gen.controllerCode || '—' },
    { label: ar ? 'نوع الشاشة' : 'Controller Type', value: gen.controllerType || '—' },
    { label: ar ? 'القدرة الاسمية' : 'Nominal Power', value: fmt(gen.nominalPower, 'KW') },
    { label: ar ? 'القدرة الفعلية' : 'Actual Power', value: fmt(gen.actualPowerKW, 'KW') },
    { label: ar ? 'نوع المولد' : 'Generator Type', value: gen.generatorTypeLookupName || '—' },
    { label: ar ? 'نوع الملكية' : 'Ownership', value: gen.ownershipTypeLookupName || gen.ownerName || '—' },
    { label: ar ? 'المنطقة' : 'Branch', value: gen.branchName || '—' },
    { label: ar ? 'المحطة' : 'Station', value: gen.stationName || '—' },
    { label: ar ? 'الموقع' : 'Location', value: gen.subStationName || '—' },
    { label: ar ? 'آخر قراءة طاقة (معتمدة)' : 'Last Energy Reading (Approved)', value: fmt(gen.controllerEnergyReading, 'KWh') },
    { label: ar ? 'آخر قراءة ساعات تشغيل (معتمدة)' : 'Last Operating Hours (Approved)', value: fmt(gen.controllerOperatingHours, 'h') },
    { label: ar ? 'الشركة المصنعة' : 'Manufacturer', value: gen.manufacturer || '—' },
    { label: ar ? 'الموديل' : 'Model', value: gen.model || '—' },
    { label: ar ? 'الرقم التسلسلي' : 'Serial No.', value: gen.serialNumber || '—' },
    { label: ar ? 'الجهد الكهربائي' : 'Voltage', value: gen.voltageLookupName || '—' },
    { label: ar ? 'التردد' : 'Frequency', value: gen.frequencyLookupName || '—' },
  ];

  return (
    <Modal
      open={!!gen}
      onClose={onClose}
      title={
        <span style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
          <Zap size={20} style={{ color: 'var(--primary)' }} />
          {gen.name}
        </span>
      }
      maxWidth={640}
    >
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px 24px' }}>
        {fields.map(({ label, value }, i) => (
          <div key={i} style={{ borderBottom: '1px solid var(--border)', paddingBottom: 10 }}>
            <div style={{ fontSize: 11, color: 'var(--text-muted)', marginBottom: 3, fontWeight: 600 }}>{label}</div>
            <div style={{ fontSize: 13, fontWeight: 700, color: 'var(--text)' }}>{value}</div>
          </div>
        ))}
      </div>
    </Modal>
  );
}

/* ─── Main Component ─────────────────────────────────────────────── */
function GeneratorsOverview({ lang }) {
  const ar = lang === 'ar';
  const [generators, setGenerators] = useState([]);
  const [loading, setLoading]       = useState(true);
  const [error, setError]           = useState(null);
  const [search, setSearch]         = useState('');
  const [statusFilter, setStatusFilter] = useState('all');
  const [ownerFilter, setOwnerFilter]   = useState('all');
  const [selectedGen, setSelectedGen] = useState(null);

  const [openBranches, setOpenBranches] = useState({});
  const [openStations, setOpenStations] = useState({});

  const load = useCallback(async () => {
    setLoading(true); setError(null);
    try {
      const res = await client.get('/api/generators/active', { params: { _t: Date.now() } });
      setGenerators(Array.isArray(res.data) ? res.data : []);
    } catch {
      setError(ar ? 'فشل تحميل بيانات المولدات' : 'Failed to load generators');
    } finally {
      setLoading(false);
    }
  }, [ar]);

  useEffect(() => { load(); }, [load]);

  // Live updates via SignalR
  useEffect(() => {
    const handler = () => load();
    connection.on('GeneratorUpdated', handler);
    connection.on('ReadingApproved', handler);
    return () => {
      connection.off('GeneratorUpdated', handler);
      connection.off('ReadingApproved', handler);
    };
  }, [load]);

  /* ── KPIs ───────────────────────────────────────────────────── */
  const kpi = useMemo(() => ({
    total: generators.length,
    running:     generators.filter(g => g.statusLookupName === 'شغال').length,
    idle:        generators.filter(g => g.statusLookupName === 'واقف').length,
    maintenance: generators.filter(g => g.statusLookupName === 'تحت الصيانة').length,
  }), [generators]);

  /* ── Unique ownership types for filter ─────────────────────── */
  const ownershipTypes = useMemo(() => {
    return [...new Set(generators.map(g => g.ownershipTypeLookupName).filter(Boolean))];
  }, [generators]);

  /* ── Filter & Search ────────────────────────────────────────── */
  const filtered = useMemo(() => {
    return generators.filter(g => {
      const q = search.toLowerCase();
      const matchSearch = !search ||
        g.name?.toLowerCase().includes(q) ||
        g.subStationName?.toLowerCase().includes(q) ||
        g.stationName?.toLowerCase().includes(q) ||
        g.branchName?.toLowerCase().includes(q) ||
        g.controllerName?.toLowerCase().includes(q) ||
        g.generatorTypeLookupName?.toLowerCase().includes(q);
      const matchStatus = statusFilter === 'all' || g.statusLookupName === statusFilter;
      const matchOwner  = ownerFilter === 'all' || g.ownershipTypeLookupName === ownerFilter;
      return matchSearch && matchStatus && matchOwner;
    });
  }, [generators, search, statusFilter, ownerFilter]);

  const grouped = useMemo(() => {
    const tree = {};
    filtered.forEach(row => {
      const brKey = row.branchId || 0;
      const stKey = row.stationId || 0;
      const ssKey = row.subStationId || 0;
      if (!tree[brKey]) tree[brKey] = { id: brKey, name: row.branchName || (ar ? 'منطقة غير محددة' : 'Unknown Branch'), stations: {} };
      if (!tree[brKey].stations[stKey]) tree[brKey].stations[stKey] = { id: stKey, name: row.stationName || (ar ? 'محطة غير محددة' : 'Unknown Station'), rows: {} };
      if (!tree[brKey].stations[stKey].rows[ssKey]) tree[brKey].stations[stKey].rows[ssKey] = [];
      tree[brKey].stations[stKey].rows[ssKey].push(row);
    });
    return Object.values(tree);
  }, [filtered, ar]);

  useEffect(() => {
    if (grouped.length > 0 && Object.keys(openBranches).length === 0) {
      setOpenBranches({ [grouped[0].id]: true });
    }
  }, [grouped]); // eslint-disable-line

  const toggleBranch  = id => setOpenBranches(p => ({ ...p, [id]: !p[id] }));
  const toggleStation = id => setOpenStations(p => ({ ...p, [id]: !p[id] }));

  const handleSearchChange = e => setSearch(e.target.value);

  /* ─────────────────────────────── RENDER ─────────────────── */
  return (
    <div style={{ animation: 'fadeIn 0.3s ease' }}>

      <PageHeader
        title={
          <span style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
            <Zap size={24} style={{ color: 'var(--primary)' }} />
            {ar ? 'عرض المولدات' : 'Generators Overview'}
          </span>
        }
        subtitle={ar ? 'لوحة متابعة تشغيلية لجميع المولدات وآخر قراءاتها المعتمدة' : 'Operational dashboard for all generators and their last approved readings'}
        actions={
          <Button variant="ghost" onClick={load}>
            <RefreshCw size={15} /> {ar ? 'تحديث' : 'Refresh'}
          </Button>
        }
      />

      {/* ── KPI Cards ──────────────────────────────────────── */}
      <div className="kpi-grid" style={{ marginBottom: 28 }}>
        <div className="kpi-card" style={{ '--kpi-color': 'var(--primary)', '--kpi-bg': 'var(--primary-glow)' }}>
          <div className="kpi-icon"><Zap size={22} /></div>
          <div><div className="kpi-value">{kpi.total}</div><div className="kpi-label">{ar ? 'إجمالي المولدات' : 'Total Generators'}</div></div>
        </div>
        <div className="kpi-card" style={{ '--kpi-color': '#10b981', '--kpi-bg': 'rgba(16,185,129,0.12)' }}>
          <div className="kpi-icon" style={{ color: '#10b981' }}><Activity size={22} /></div>
          <div><div className="kpi-value">{kpi.running}</div><div className="kpi-label">{ar ? 'يعمل' : 'Running'}</div></div>
        </div>
        <div className="kpi-card" style={{ '--kpi-color': 'var(--warning)', '--kpi-bg': 'var(--warning-glow)' }}>
          <div className="kpi-icon"><Clock size={22} /></div>
          <div><div className="kpi-value">{kpi.idle}</div><div className="kpi-label">{ar ? 'خامل' : 'Idle'}</div></div>
        </div>
        <div className="kpi-card" style={{ '--kpi-color': 'var(--danger)', '--kpi-bg': 'var(--danger-glow)' }}>
          <div className="kpi-icon"><AlertTriangle size={22} /></div>
          <div><div className="kpi-value">{kpi.maintenance}</div><div className="kpi-label">{ar ? 'تحت الصيانة' : 'Maintenance'}</div></div>
        </div>
      </div>

      {/* ── Toolbar ─────────────────────────────────────────── */}
      <div style={{ display: 'flex', gap: 10, flexWrap: 'wrap', alignItems: 'center', marginBottom: 20 }}>
        {/* Search */}
        <div className="search-bar" style={{ flex: 1, minWidth: 220 }}>
          <Search size={16} />
          <input
            className="form-control"
            placeholder={ar ? 'بحث باسم المولد أو الموقع أو الشاشة...' : 'Search by name, location or controller...'}
            value={search}
            onChange={handleSearchChange}
          />
        </div>

        {/* Status filter */}
        <FormField label={ar ? 'الحالة' : 'Status'} style={{ minWidth: 150 }}>
          <select
            className="form-select"
            value={statusFilter}
            onChange={e => setStatusFilter(e.target.value)}
          >
            <option value="all">{ar ? 'كل الحالات' : 'All Statuses'}</option>
            <option value="شغال">{ar ? 'يعمل' : 'Running'}</option>
            <option value="واقف">{ar ? 'خامل' : 'Idle'}</option>
            <option value="تحت الصيانة">{ar ? 'صيانة' : 'Maintenance'}</option>
          </select>
        </FormField>

        {/* Ownership filter */}
        <FormField label={ar ? 'الملكية' : 'Ownership'} style={{ minWidth: 160 }}>
          <select
            className="form-select"
            value={ownerFilter}
            onChange={e => setOwnerFilter(e.target.value)}
          >
            <option value="all">{ar ? 'كل أنواع الملكية' : 'All Ownership'}</option>
            {ownershipTypes.map(t => <option key={t} value={t}>{t}</option>)}
          </select>
        </FormField>

        <span style={{ fontSize: 13, color: 'var(--text-muted)', whiteSpace: 'nowrap' }}>
          {filtered.length} {ar ? 'مولد' : 'generator(s)'}
        </span>
      </div>

      {/* ── Error ───────────────────────────────────────────── */}
      {error && <div className="alert-inline alert-inline-danger">{error}</div>}

      {/* ── Loading ─────────────────────────────────────────── */}
      {loading && (
        <div className="spinner-wrap"><div className="ft-spinner" /></div>
      )}

      {/* ── Empty state ─────────────────────────────────────── */}
      {!loading && filtered.length === 0 && (
        <div className="empty-state">
          <Zap size={48} />
          <h3>{ar ? 'لا توجد مولدات' : 'No generators found'}</h3>
        </div>
      )}

      {/* ── Tree View ──────────────────────────────────────── */}
      {!loading && filtered.length > 0 && (
        <div className="section-card" style={{ marginBottom: 0 }}>
          {grouped.map(branch => (
            <div key={branch.id} style={{ marginBottom: 10 }}>
              <button onClick={() => toggleBranch(branch.id)}
                style={{ width: '100%', display: 'flex', alignItems: 'center', gap: 10, padding: '12px 16px', background: 'var(--primary)', color: '#fff', border: 'none', borderRadius: 10, cursor: 'pointer', fontWeight: 700, fontSize: 14, textAlign: 'start' }}>
                {openBranches[branch.id] ? <ChevronDown size={16} /> : <ChevronRight size={16} />}
                <span style={{ flex: 1 }}>{branch.name}</span>
                <span style={{ background: 'rgba(255,255,255,0.2)', borderRadius: 99, padding: '2px 10px', fontSize: 12 }}>
                  {Object.values(branch.stations).reduce((a, st) => a + Object.values(st.rows).reduce((b, r) => b + r.length, 0), 0)} {ar ? 'مولد' : 'generator(s)'}
                </span>
              </button>

              {openBranches[branch.id] && Object.values(branch.stations).map(station => (
                <div key={station.id} style={{ marginTop: 4, marginInlineStart: 16 }}>
                  <button onClick={() => toggleStation(station.id)}
                    style={{ width: '100%', display: 'flex', alignItems: 'center', gap: 10, padding: '10px 14px', background: 'var(--card-2)', border: '1px solid var(--border)', borderRadius: 8, cursor: 'pointer', fontWeight: 600, fontSize: 13, textAlign: 'start', color: 'var(--text)' }}>
                    {openStations[station.id] ? <ChevronDown size={14} style={{ color: 'var(--primary)' }} /> : <ChevronRight size={14} style={{ color: 'var(--primary)' }} />}
                    <span style={{ flex: 1 }}>{ar ? 'محطة: ' : 'Station: '}{station.name}</span>
                    <span style={{ background: 'var(--primary-glow)', color: 'var(--primary)', borderRadius: 99, padding: '2px 8px', fontSize: 11, fontWeight: 700 }}>
                      {Object.values(station.rows).reduce((a, r) => a + r.length, 0)}
                    </span>
                  </button>

                  {openStations[station.id] && Object.entries(station.rows).map(([ssId, rows]) => {
                    const ssName = rows[0]?.subStationName || (ar ? 'موقع غير محدد' : 'Unknown site');
                    return (
                      <div key={ssId} style={{ marginTop: 4, marginInlineStart: 16, marginBottom: 8 }}>
                        <div style={{ display: 'flex', alignItems: 'center', gap: 8, padding: '8px 12px', background: 'var(--card)', border: '1px solid var(--border)', borderRadius: 6, marginBottom: 4, fontWeight: 600, fontSize: 12, color: 'var(--text-muted)' }}>
                          <MapPin size={13} style={{ color: 'var(--text-muted)' }} />
                          <span>{ssName}</span>
                        </div>
                        <div style={{ overflowX: 'auto', marginInlineStart: 8 }}>
                          <table className="data-table" style={{ borderTop: 'none', fontSize: 13, minWidth: 960 }}>
                            <thead>
                              <tr style={{ background: 'var(--card-2)' }}>
                                <th>{ar ? 'اسم المولد' : 'Generator'}</th>
                                <th style={{ textAlign: 'center' }}>{ar ? 'الحالة' : 'Status'}</th>
                                <th style={{ textAlign: 'center' }}>{ar ? 'الشاشة المرتبطة' : 'Controller'}</th>
                                <th style={{ textAlign: 'center' }}>{ar ? 'القدرة الفعلية' : 'Actual Power'}</th>
                                <th style={{ textAlign: 'center' }}>{ar ? 'آخر قراءة طاقة' : 'Last Energy'}</th>
                                <th style={{ textAlign: 'center' }}>{ar ? 'ساعات التشغيل' : 'Run Hours'}</th>
                                <th style={{ textAlign: 'center' }}>{ar ? 'نوع المولد' : 'Type'}</th>
                                <th style={{ textAlign: 'center' }}>{ar ? 'الملكية' : 'Ownership'}</th>
                              </tr>
                            </thead>
                            <tbody>
                              {rows.map(gen => (
                                <tr key={gen.id} style={{ cursor: 'pointer' }} onClick={() => setSelectedGen(gen)}>
                                  <td style={{ fontWeight: 700 }}>{gen.name}</td>
                                  <td style={{ textAlign: 'center' }}><StatusBadge status={gen.statusLookupName} lang={lang} /></td>
                                  <td style={{ textAlign: 'center', fontSize: 13 }}>
                                    {gen.controllerName
                                      ? <><strong>{gen.controllerName}</strong>{gen.controllerCode && <span style={{ color: 'var(--text-muted)', fontSize: 11 }}> ({gen.controllerCode})</span>}</>
                                      : <span style={{ color: 'var(--text-muted)' }}>{ar ? 'بدون شاشة' : 'No Controller'}</span>
                                    }
                                  </td>
                                  <td style={{ textAlign: 'center', fontWeight: 700 }}>{fmt(gen.actualPowerKW, 'KW')}</td>
                                  <td style={{ textAlign: 'center', color: 'var(--primary)', fontWeight: 800 }}>
                                    {fmt(gen.controllerEnergyReading, 'KWh')}
                                  </td>
                                  <td style={{ textAlign: 'center', fontWeight: 700 }}>
                                    {fmt(gen.controllerOperatingHours, 'h')}
                                  </td>
                                  <td style={{ textAlign: 'center', fontSize: 13 }}>{gen.generatorTypeLookupName || '—'}</td>
                                  <td style={{ textAlign: 'center', fontSize: 13 }}>
                                    {gen.ownershipTypeLookupName || gen.ownerName || '—'}
                                  </td>
                                </tr>
                              ))}
                            </tbody>
                          </table>
                        </div>
                      </div>
                    );
                  })}
                </div>
              ))}
            </div>
          ))}
        </div>
      )}

      {/* ── Detail Modal ────────────────────────────────────── */}
      {selectedGen && (
        <GeneratorDetailModal gen={selectedGen} lang={lang} onClose={() => setSelectedGen(null)} />
      )}
    </div>
  );
}

export default GeneratorsOverview;
