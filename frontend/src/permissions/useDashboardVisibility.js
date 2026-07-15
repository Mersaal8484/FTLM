import { useMemo, useState, useCallback } from 'react';

const DEFAULT_KPI = {
  total_generators: true,
  running_generators: true,
  stopped_generators: true,
  fuel_stock: true,
  oil_stock: true,
  fuel_consumption: true,
  daily_readings_pending: true,
  pending_fuel_transfers: true,
  pending_approvals: true,
  alerts: true,
  notifications: true,
  recent_movements: true,
  energy_daily: true,
  grid_export_daily: true,
  energy_15d: true,
  energy_30d: true,
  grid_export_30d: true,
  fuel_daily: true,
  fuel_15d: true,
  oil_consumed: true,
  efficiency: true,
};

export default function useDashboardVisibility() {
  return useMemo(() => ({
    kpi: DEFAULT_KPI,
    charts: { fuel_chart: true, energy_chart: true },
    smart_insights: true,
  }), []);
}
