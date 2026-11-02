import React, { useState, useMemo } from 'react';
import { Settings, Droplet, Zap, Layout, Truck, ArrowRightLeft, AlertCircle } from 'lucide-react';
import useReportData from '../../hooks/useReportData';
import ReportTable from '../../components/Tables/ReportTable';
import ReportKPICards from '../../components/Cards/ReportKPICards';
import { BarChart } from '../../components/Charts/ReportCharts';

const DAILY_TABS = [
  { key: 'generators', ar: 'المولدات', en: 'Generators', icon: Settings, endpoint: 'generators' },
  { key: 'fuel', ar: 'الوقود', en: 'Fuel', icon: Zap, endpoint: 'fuel-transactions' },
  { key: 'oil', ar: 'الزيوت', en: 'Oils', icon: Droplet, endpoint: 'oil-transactions' },
  { key: 'station', ar: 'قراءات الموقع', en: 'Site Readings', icon: Layout, endpoint: 'station-readings' },
  { key: 'incoming', ar: 'الوارد', en: 'Incoming', icon: Truck, endpoint: 'fuel-incoming' },
  { key: 'transfer', ar: 'النقل', en: 'Transfer', icon: ArrowRightLeft, endpoint: 'fuel-transfer' },
  { key: 'loss', ar: 'الفاقد', en: 'Loss', icon: AlertCircle, endpoint: 'fuel-loss' },
];

export default function DailyReportsTab({ filters, lang }) {
  const isAr = lang === 'ar';
  const [activeSubTab, setActiveSubTab] = useState('generators');
  
  const currentTabInfo = DAILY_TABS.find(t => t.key === activeSubTab);
  const { data, loading, error } = useReportData(filters, currentTabInfo.endpoint);

  const kpis = useMemo(() => {
    if (!data) return [];
    
    if (activeSubTab === 'generators') {
      const summary = data.summary || {};
        return [
          { title: isAr ? 'إجمالي الطاقة (KWh)' : 'Total Energy (KWh)', value: summary.totalEnergyProducedKwh?.toFixed(2) || '0', icon: Zap, color: 'primary' },
          { title: isAr ? 'الوقود المستهلك (L)' : 'Fuel Consumed (L)', value: summary.totalFuelConsumedLiters?.toFixed(2) || '0', icon: Droplet, color: 'warning' },
          { title: isAr ? 'ساعات التشغيل' : 'Operating Hours', value: summary.totalOperatingHours?.toFixed(1) || '0', icon: Settings, color: 'success' },
          { title: isAr ? 'متوسط الكفاءة' : 'Avg Efficiency', value: summary.averageEfficiencyPercent?.toFixed(3) || '0', icon: Layout, color: 'danger' }
        ];
    }
    
    return [
      { title: isAr ? 'إجمالي السجلات' : 'Total Records', value: data.pagination?.total || data.items?.length || 0, icon: Layout, color: 'primary' }
    ];
  }, [data, activeSubTab, isAr]);

  const columns = useMemo(() => {
    if (activeSubTab === 'generators') {
      return [
        { header: isAr ? 'التاريخ' : 'Date', accessor: 'dateStr' },
        { header: isAr ? 'الموقع' : 'Location', accessor: 'subStationName' },
        { header: isAr ? 'المولد' : 'Generator', accessor: 'generatorName' },
        { header: isAr ? 'الطاقة المنتجة (KWh)' : 'Energy (KWh)', accessor: 'totalEnergyProducedKwh', align: 'center' },
        { header: isAr ? 'الوقود المستهلك (L)' : 'Fuel (L)', accessor: 'totalFuelConsumedLiters', align: 'center' },
        { header: isAr ? 'ساعات التشغيل' : 'Hours', accessor: 'operatingHours', align: 'center' },
      ];
    }
     if (activeSubTab === 'station') {
        return [
         { header: isAr ? 'التاريخ' : 'Date', accessor: 'dateStr' },
         { header: isAr ? 'الموقع' : 'Location', accessor: 'subStationName' },
         { header: isAr ? 'الطاقة الصادرة السابقة (KWh)' : 'Prev Exported (KWh)', accessor: 'previousExportedKwh', align: 'center' },
         { header: isAr ? 'الطاقة الصادرة الحالية (KWh)' : 'Current Exported (KWh)', accessor: 'currentExportedKwh', align: 'center' },
         { header: isAr ? 'صافي الطاقة (KWh)' : 'Net Exported (KWh)', accessor: 'deltaExportedKwh', align: 'center' },
        ];
     }
    return [
      { header: isAr ? 'المعرف' : 'ID', accessor: 'id' },
      { header: isAr ? 'التاريخ' : 'Date', accessor: 'dateStr' },
    ];
  }, [activeSubTab, isAr]);

  const chartData = useMemo(() => {
    if (!data?.items?.length || activeSubTab !== 'generators') return null;
    
      return {
        labels: data.items.map(i => i.generatorName || i.dateStr).slice(0, 10),
        datasets: [
          {
            label: isAr ? 'الطاقة المنتجة' : 'Energy Produced',
            data: data.items.map(i => i.totalEnergyProducedKwh).slice(0, 10),
            backgroundColor: '#3b82f6'
          },
          {
            label: isAr ? 'الوقود المستهلك' : 'Fuel Consumed',
            data: data.items.map(i => i.totalFuelConsumedLiters).slice(0, 10),
            backgroundColor: 'var(--warning)'
          }
        ]
      };
  }, [data, activeSubTab, isAr]);

  return (
    <div className="daily-reports-tab">
      <div className="report-tabs-level2">
        {DAILY_TABS.map(tab => (
          <button
            key={tab.key}
            className={`subtab-pill ${activeSubTab === tab.key ? 'active' : ''}`}
            onClick={() => setActiveSubTab(tab.key)}
          >
            <tab.icon size={16} />
            {isAr ? tab.ar : tab.en}
          </button>
        ))}
      </div>

      {error ? (
        <div className="alert-inline alert-inline-danger">{error.message || 'Error loading data'}</div>
      ) : (
        <>
          <ReportKPICards items={kpis} lang={lang} />
          
          {chartData && (
            <div style={{ marginBottom: '1.5rem' }}>
              <BarChart data={chartData} />
            </div>
          )}

          <ReportTable
            columns={columns}
            data={data?.items || []}
            loading={loading}
            page={filters.page}
            totalPages={data?.pagination?.pages || 1}
            onPageChange={filters.setPage}
            lang={lang}
          />
        </>
      )}
    </div>
  );
}
