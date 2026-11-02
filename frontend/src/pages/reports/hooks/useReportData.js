import { useState, useCallback, useEffect } from 'react';
import client from '../../../api/client';

export default function useReportData(filters, endpoint) {
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  const fetchData = useCallback(async () => {
    if (!endpoint) return;

    setLoading(true);
    setError(null);
    try {
      const params = { page: filters.page, pageSize: 20, timeZoneId: 'Asia/Riyadh' };
      if (filters.branchId) params.branchId = filters.branchId;
      if (filters.stationId) params.stationId = filters.stationId;
      if (filters.subStationId) params.subStationId = filters.subStationId;
      if (filters.dateFrom) params.dateFrom = filters.dateFrom;
      if (filters.dateTo) params.dateTo = filters.dateTo;
      if (filters.searchQuery) params.search = filters.searchQuery;
      if (filters.itemType) params.itemType = filters.itemType;
      
      const res = await client.get(`/api/reports/${endpoint}`, { params });
      setData(res.data);
    } catch (err) {
      console.error(`Failed to fetch ${endpoint} report data`, err);
      setError(err);
    } finally {
      setLoading(false);
    }
  }, [filters, endpoint]);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  return { data, loading, error, refetch: fetchData };
}
