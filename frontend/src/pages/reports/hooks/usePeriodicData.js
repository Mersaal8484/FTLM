import useReportData from './useReportData';

export default function usePeriodicData(filters) {
  return useReportData(filters, 'periodic-inventory');
}
