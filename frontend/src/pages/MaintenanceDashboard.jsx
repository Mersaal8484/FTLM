import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Wrench, AlertTriangle, FileText, PackageSearch, BarChart3, PieChart, Activity, Clock, ArrowRight, ArrowLeft } from 'lucide-react';
import client from '../api/client';

function MaintenanceDashboard({ lang, currentUser }) {
  const ar = lang === 'ar';
  const navigate = useNavigate();
  const [data, setData] = useState(null);
  const [serviceDueSummary, setServiceDueSummary] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const load = async () => {
      try {
        const [dashRes, dueRes] = await Promise.allSettled([
          client.get('/api/maintenance/dashboard'),
          client.get('/api/service-due/dashboard'),
        ]);
        if (dashRes.status === 'fulfilled') setData(dashRes.value.data);
        if (dueRes.status === 'fulfilled') setServiceDueSummary(dueRes.value.data);
      } catch { setData({}); }
      finally { setLoading(false); }
    };
    load();
  }, []);

  if (loading) {
    return <div className="ft-spinner" style={{ width: 40, height: 40, margin: '40px auto' }} />;
  }

  const kpis = data?.kpis || {
    openPermits: 0,
    criticalPermits: 0,
    overdueParts: 0,
    lowStockParts: 0,
  };

  const recentPermits = data?.recentPermits || [];
  const dueParts = data?.dueParts || [];
  const permitsByType = data?.permitsByType || [];
  const permitsByPriority = data?.permitsByPriority || [];
  const chartColors = ['var(--chart-1)', 'var(--chart-2)', 'var(--chart-3)', 'var(--chart-4)', 'var(--chart-5)', 'var(--chart-6)'];

  return (
    <div style={{ animation: 'fadeIn 0.4s ease' }}>
      <div className="page-header">
        <div className="page-header-row">
          <div>
            <h1 style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
              <Wrench size={24} style={{ color: 'var(--primary)' }} />
              {ar ? 'لوحة الصيانة' : 'Maintenance Dashboard'}
            </h1>
            <p style={{ opacity: 0.8, fontSize: '0.9rem' }}>
              {ar ? 'نظرة عامة على الصيانة والمخازن' : 'Maintenance and warehouse overview'}
            </p>
          </div>
        </div>
      </div>

      <div className="kpi-grid" style={{ marginBottom: 28 }}>
        <div className="kpi-card" style={{ '--kpi-color': 'var(--primary)', '--kpi-bg': 'var(--primary-glow)' }}>
          <div className="kpi-icon"><FileText size={22} /></div>
          <div><div className="kpi-value">{kpis.openPermits}</div><div className="kpi-label">{ar ? 'تصاريح مفتوحة' : 'Open Permits'}</div></div>
        </div>
        <div className="kpi-card" style={{ '--kpi-color': 'var(--danger)', '--kpi-bg': 'var(--danger-subtle)' }}>
          <div className="kpi-icon"><AlertTriangle size={22} /></div>
          <div><div className="kpi-value">{kpis.criticalPermits}</div><div className="kpi-label">{ar ? 'تصاريح حرجة' : 'Critical Permits'}</div></div>
        </div>
        <div className="kpi-card" style={{ '--kpi-color': 'var(--danger)', '--kpi-bg': 'var(--danger-subtle)' }}>
          <div className="kpi-icon"><Wrench size={22} /></div>
          <div><div className="kpi-value">{kpis.overdueParts}</div><div className="kpi-label">{ar ? 'قطع متأخرة' : 'Overdue Parts'}</div></div>
        </div>
        <div className="kpi-card" style={{ '--kpi-color': 'var(--warning)', '--kpi-bg': 'var(--warning)15' }}>
          <div className="kpi-icon"><PackageSearch size={22} /></div>
          <div><div className="kpi-value">{kpis.lowStockParts}</div><div className="kpi-label">{ar ? 'نقص مخزون' : 'Low Stock Parts'}</div></div>
        </div>
      </div>

      {/* Phase 4 Service Due Engine Widget (Hidden in legacy mode when endpoint returns 404) */}
      {serviceDueSummary && (
        <div className="panel" style={{ padding: '16px 20px', borderRadius: 8, border: '1px solid var(--border-color)', marginBottom: 20, background: 'var(--card-bg)' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 12 }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
              <div style={{ width: 40, height: 40, borderRadius: 8, background: 'var(--primary-subtle)', display: 'flex', alignItems: 'center', justifyContent: 'center', color: 'var(--primary)' }}>
                <Clock size={22} />
              </div>
              <div>
                <h3 style={{ margin: 0, fontSize: '1.05rem', fontWeight: 600 }}>
                  {ar ? 'فترات الصيانة واستحقاقات ساعات التشغيل' : 'Service Intervals & Running Hours'}
                </h3>
                <p style={{ margin: 0, fontSize: '0.85rem', color: 'var(--text-muted)' }}>
                  {ar ? (
                    `إجمالي المستحق: ${serviceDueSummary.totalDueCount} | المتأخر: ${serviceDueSummary.totalOverdueCount}`
                  ) : (
                    `Total Due: ${serviceDueSummary.totalDueCount} | Overdue: ${serviceDueSummary.totalOverdueCount}`
                  )}
                </p>
              </div>
            </div>

            <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
              {serviceDueSummary.totalOverdueCount > 0 && (
                <span style={{ background: 'var(--danger-subtle)', color: 'var(--danger)', border: '1px solid var(--danger)', padding: '4px 10px', borderRadius: 16, fontSize: '0.8rem', fontWeight: 600 }}>
                  {serviceDueSummary.totalOverdueCount} {ar ? 'متأخر' : 'Overdue'}
                </span>
              )}
              {serviceDueSummary.totalDueCount > 0 && (
                <span style={{ background: 'var(--warning-subtle)', color: 'var(--warning)', border: '1px solid var(--warning)', padding: '4px 10px', borderRadius: 16, fontSize: '0.8rem', fontWeight: 600 }}>
                  {serviceDueSummary.totalDueCount} {ar ? 'مستحق' : 'Due'}
                </span>
              )}
              <button
                className="btn btn-primary btn-sm"
                onClick={() => navigate('/maintenance/service')}
                style={{ display: 'flex', alignItems: 'center', gap: 6 }}
              >
                <span>{ar ? 'عرض جداول المولدات' : 'View Maintenance Table'}</span>
                {ar ? <ArrowLeft size={14} /> : <ArrowRight size={14} />}
              </button>
            </div>
          </div>
        </div>
      )}

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 20, marginBottom: 20 }}>
        <div className="section-card">
          <h3 style={{ marginBottom: 16, display: 'flex', alignItems: 'center', gap: 8 }}>
            <FileText size={18} style={{ color: 'var(--primary)' }} />
            {ar ? 'تصاريح مفتوحة حديثة' : 'Recent Open Permits'}
          </h3>
          {recentPermits.length === 0 ? (
            <div className="empty-state">{ar ? 'لا توجد تصاريح' : 'No permits'}</div>
          ) : (
            <table className="data-table">
              <thead><tr><th>{ar ? 'الرقم' : '#'}</th><th>{ar ? 'النوع' : 'Type'}</th><th>{ar ? 'الحالة' : 'Status'}</th></tr></thead>
              <tbody>
                {recentPermits.slice(0, 5).map(p => (
                  <tr key={p.id}><td>{p.permitNumber}</td><td>{p.maintenanceType}</td><td>{p.status}</td></tr>
                ))}
              </tbody>
            </table>
          )}
        </div>

        <div className="section-card">
          <h3 style={{ marginBottom: 16, display: 'flex', alignItems: 'center', gap: 8 }}>
            <AlertTriangle size={18} style={{ color: 'var(--danger)' }} />
            {ar ? 'قطع تحتاج استبدال' : 'Parts Due for Replacement'}
          </h3>
          {dueParts.length === 0 ? (
            <div className="empty-state">{ar ? 'لا توجد قطع' : 'No parts'}</div>
          ) : (
            <table className="data-table">
              <thead><tr><th>{ar ? 'القطعة' : 'Part'}</th><th>{ar ? 'المولد' : 'Generator'}</th><th>{ar ? 'الساعات المتبقية' : 'Remaining'}</th></tr></thead>
              <tbody>
                {dueParts.slice(0, 5).map(p => (
                  <tr key={p.id}><td>{p.partName}</td><td>{p.generatorName}</td><td style={{ color: p.remainingHours < 0 ? 'var(--danger)' : 'var(--warning)', fontWeight: 700 }}>{p.remainingHours}</td></tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 20 }}>
        <div className="section-card">
          <h3 style={{ marginBottom: 16, display: 'flex', alignItems: 'center', gap: 8 }}>
            <PieChart size={18} style={{ color: 'var(--secondary)' }} />
            {ar ? 'التصاريح حسب النوع' : 'Permits by Type'}
          </h3>
          {permitsByType.length === 0 ? (
            <div className="empty-state maintenance-chart-empty"><PieChart size={30} /><span>{ar ? 'لا توجد بيانات حسب النوع' : 'No permit type data yet'}</span></div>
          ) : (
            <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
              {permitsByType.map((item, i) => (
                <div key={i} style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                  <div style={{ flex: 1, height: 12, background: 'var(--border)', borderRadius: 99, overflow: 'hidden' }}>
                    <div style={{ height: '100%', width: `${Math.min(100, (item.count / Math.max(1, permitsByType.reduce((a, b) => a + b.count, 0))) * 100)}%`, background: chartColors[i % chartColors.length], borderRadius: 99 }} />
                  </div>
                  <span style={{ fontSize: 'var(--text-sm)', minWidth: 80 }}>{item.type}</span>
                  <span style={{ fontWeight: 700, minWidth: 30, textAlign: 'center' }}>{item.count}</span>
                </div>
              ))}
            </div>
          )}
        </div>

        <div className="section-card">
          <h3 style={{ marginBottom: 16, display: 'flex', alignItems: 'center', gap: 8 }}>
            <BarChart3 size={18} style={{ color: 'var(--warning)' }} />
            {ar ? 'التصاريح حسب الأولوية' : 'Permits by Priority'}
          </h3>
          {permitsByPriority.length === 0 ? (
            <div className="empty-state maintenance-chart-empty"><BarChart3 size={30} /><span>{ar ? 'لا توجد بيانات حسب الأولوية' : 'No permit priority data yet'}</span></div>
          ) : (
            <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
              {permitsByPriority.map((item, i) => (
                <div key={i} style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                  <div style={{ flex: 1, height: 12, background: 'var(--border)', borderRadius: 99, overflow: 'hidden' }}>
                    <div style={{ height: '100%', width: `${Math.min(100, (item.count / Math.max(1, permitsByPriority.reduce((a, b) => a + b.count, 0))) * 100)}%`, background: chartColors[(i + 2) % chartColors.length], borderRadius: 99 }} />
                  </div>
                  <span style={{ fontSize: 'var(--text-sm)', minWidth: 80 }}>{item.priority}</span>
                  <span style={{ fontWeight: 700, minWidth: 30, textAlign: 'center' }}>{item.count}</span>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

export default MaintenanceDashboard;
