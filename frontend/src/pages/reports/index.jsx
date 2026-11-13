import React, { useEffect, useMemo, useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { FileText, Calendar, BarChart3, PackageSearch, Printer, FileSpreadsheet } from 'lucide-react';
import './reports.css';
import usePermissions from '../../permissions/usePermissions';
import WarehouseReports from '../WarehouseReports';
import useReportFilters from './hooks/useReportFilters';
import ReportFilters from './components/Filters/ReportFilters';
import DailyReportsTab from './tabs/Daily/DailyReportsTab';
import PeriodicReportsTab from './tabs/Periodic/PeriodicReportsTab';
import EfficiencyReportTab from './tabs/Efficiency/EfficiencyReportTab';

const MAIN_TABS = [
  { key: 'daily', ar: 'التقارير اليومية', en: 'Daily Reports', icon: FileText, color: 'primary' },
  { key: 'periodic', ar: 'التقارير الدورية', en: 'Periodic Reports', icon: Calendar, color: 'warning' },
  { key: 'efficiency', ar: 'تقرير الكفاءة', en: 'Efficiency Report', icon: BarChart3, color: 'success' },
];

export default function Reports({ lang, currentUser }) {
  const isAr = lang === 'ar';
  const filters = useReportFilters();
  const location = useLocation();
  const navigate = useNavigate();
  const { can } = usePermissions(currentUser);
  const canViewStandardReports = can('reports');
  const canViewWarehouseReports = can('warehouse');
  const initialGroup = location.pathname === '/reports/warehouse'
    ? 'warehouse'
    : (canViewStandardReports ? 'daily' : 'warehouse');
  const [activeGroup, setActiveGroup] = useState(initialGroup);
  const availableTabs = useMemo(() => [
    ...(canViewStandardReports ? MAIN_TABS : []),
    ...(canViewWarehouseReports ? [{ key: 'warehouse', ar: 'تقارير المخازن', en: 'Warehouse Reports', icon: PackageSearch, color: 'warehouse' }] : []),
  ], [canViewStandardReports, canViewWarehouseReports]);

  useEffect(() => {
    if (location.pathname === '/reports/warehouse' && canViewWarehouseReports) {
      setActiveGroup('warehouse');
    }
  }, [location.pathname, canViewWarehouseReports]);

  const selectGroup = (key) => {
    setActiveGroup(key);
    filters.resetFilters();
    navigate(key === 'warehouse' ? '/reports/warehouse' : '/reports', { replace: true });
  };

  const handleExportExcel = () => {
    // Will be implemented in reportExport.js
  };

  const handlePrint = () => {
    window.print();
  };

  return (
    <div className="reports-dashboard">
      <div className="report-header">
        <div className="report-header-title">
          <FileText size={28} className="text-primary" />
          {isAr ? 'مركز التقارير' : 'Reports Center'}
        </div>
        
        {activeGroup !== 'warehouse' && <div className="report-actions d-flex gap-2">
          <button className="btn btn-outline-success d-flex align-items-center gap-2" onClick={handleExportExcel}>
            <FileSpreadsheet size={16} />
            {isAr ? 'تصدير Excel' : 'Export Excel'}
          </button>
          <button className="btn btn-outline-primary d-flex align-items-center gap-2" onClick={handlePrint}>
            <Printer size={16} />
            {isAr ? 'طباعة' : 'Print'}
          </button>
        </div>}
      </div>

      {activeGroup !== 'warehouse' && <ReportFilters filters={filters} lang={lang} />}

      <div className="report-tabs-level1 mt-4">
        {availableTabs.map(tab => (
          <button
            key={tab.key}
            className={`report-tab-btn ${activeGroup === tab.key ? 'active' : ''}`}
            onClick={() => selectGroup(tab.key)}
          >
            <tab.icon size={18} />
            {isAr ? tab.ar : tab.en}
          </button>
        ))}
      </div>

      <div className="report-content">
        {activeGroup === 'daily' && <DailyReportsTab filters={filters} lang={lang} />}
        {activeGroup === 'periodic' && <PeriodicReportsTab filters={filters} lang={lang} />}
        {activeGroup === 'efficiency' && <EfficiencyReportTab filters={filters} lang={lang} />}
        {activeGroup === 'warehouse' && <WarehouseReports lang={lang} embedded />}
      </div>
    </div>
  );
}
