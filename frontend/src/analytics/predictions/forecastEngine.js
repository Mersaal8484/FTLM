export function buildDashboardForecasts({ stats, trends, lang }) {
  const forecasts = [];
  if (trends && trends.length) {
    forecasts.push({
      title: lang === 'ar' ? 'توقع الاتجاه' : 'Trend Forecast',
      label: lang === 'ar' ? 'بيانات الاتجاه متاحة' : 'Trend data available',
    });
  }
  return { forecasts };
}
