import React, { useMemo } from 'react';
import { Activity, BarChart3, AlertTriangle } from 'lucide-react';
import useReportData from '../../hooks/useReportData';
import ReportTable from '../../components/Tables/ReportTable';
import ReportKPICards from '../../components/Cards/ReportKPICards';
import { BarChart } from '../../components/Charts/ReportCharts';

export default function EfficiencyReportTab({ filters, lang }) {
  const isAr = lang === 'ar';
  const { data, loading, error } = useReportData(filters, 'efficiency');

  const kpis = useMemo(() => {
    if (!data?.summary) return [];
    
    return [
      { title: isAr ? 'متوسط كفاءة النظام' : 'System Avg Efficiency', value: data.summary.averageEfficiencyPercent?.toFixed(2) || '0', icon: Activity, color: 'primary' },
      { title: isAr ? 'مواقع تحت المعدل' : 'Underperforming Sites', value: data.items.filter(i => i.efficiencyPercent < (data.summary.averageEfficiencyPercent || 0)).length || '0', icon: AlertTriangle, color: 'danger' },
      { title: isAr ? 'مواقع ممتازة' : 'Top Performing Sites', value: data.items.filter(i => i.efficiencyPercent > (data.summary.averageEfficiencyPercent || 0)).length || '0', icon: BarChart3, color: 'success' },
    ];
  }, [data, isAr]);

  const columns = useMemo(() => {
    return [
      { header: isAr ? 'المحطة' : 'Station', accessor: 'stationName' },
      { header: isAr ? 'الموقع' : 'SubStation', accessor: 'subStationName' },
      { header: isAr ? 'الكفاءة المستهدفة' : 'Target Efficiency', accessor: 'efficiencyPercent', align: 'center' },
      { header: isAr ? 'الكفاءة الفعلية' : 'Actual Efficiency', accessor: 'efficiencyPercent', align: 'center' },
      { header: isAr ? 'التباين' : 'Variance', accessor: 'variance', align: 'center', render: (row) => {
        const val = row.variance || 0;
        const color = val < 0 ? '#ef4444' : '#10b981';
        return <span style={{ color }}>{val > 0 ? '+' : ''}{val.toFixed(2)}</span>;
      }},
    ];
  }, [isAr]);

  const chartData = useMemo(() => {
    if (!data?.items?.length) return null;
    
    return {
      labels: data.items.map(i => i.subStationName).slice(0, 15),
      datasets: [
        {
          label: isAr ? 'الكفاءة الفعلية' : 'Actual Efficiency',
          data: data.items.map(i => i.efficiencyPercent).slice(0, 15),
          backgroundColor: '#3b82f6'
        }
      ]
    };
  }, [data, isAr]);

  return (
    <div className="efficiency-reports-tab">
      {error ? (
        <div className="alert-inline alert-inline-danger">{error.message || 'Error loading efficiency data'}</div>
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
