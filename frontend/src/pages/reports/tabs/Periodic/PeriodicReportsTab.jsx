import React, { useMemo } from 'react';
import { Calendar, Layout } from 'lucide-react';
import usePeriodicData from '../../hooks/usePeriodicData';
import ReportTable from '../../components/Tables/ReportTable';
import ReportKPICards from '../../components/Cards/ReportKPICards';

export default function PeriodicReportsTab({ filters, lang }) {
  const isAr = lang === 'ar';
  const { data, loading, error } = usePeriodicData(filters);

  const kpis = useMemo(() => {
    if (!data?.summary) return [];
    
    return [
      { title: isAr ? 'المواقع النشطة' : 'Active Sites', value: data.summary.totalRecords || '0', icon: Layout, color: 'primary' },
      { title: isAr ? 'إجمالي المخزون' : 'Total Inventory', value: ((data.summary.totalFuelReceivedLiters || 0) + (data.summary.totalOilReceivedLiters || 0)).toLocaleString('en-US') || '0', icon: Calendar, color: 'warning' },
    ];
  }, [data, isAr]);

  const columns = useMemo(() => {
    return [
      { header: isAr ? 'الفرع' : 'Branch', accessor: 'branchName' },
      { header: isAr ? 'المحطة' : 'Station', accessor: 'stationName' },
      { header: isAr ? 'الموقع' : 'SubStation', accessor: 'subStationName' },
      { header: isAr ? 'الوقود الوارد' : 'Fuel In', accessor: 'fuelIn', align: 'center' },
      { header: isAr ? 'الوقود المستهلك' : 'Fuel Out', accessor: 'fuelOut', align: 'center' },
      { header: isAr ? 'الفاقد' : 'Loss', accessor: 'fuelLoss', align: 'center' },
      { header: isAr ? 'الكفاءة %' : 'Efficiency %', accessor: 'efficiency', align: 'center' },
    ];
  }, [isAr]);

  const chartData = null;

  return (
    <div className="periodic-reports-tab">
      {error ? (
        <div className="alert-inline alert-inline-danger">{error.message || 'Error loading periodic data'}</div>
      ) : (
        <>
          <ReportKPICards items={kpis} lang={lang} />
          
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
