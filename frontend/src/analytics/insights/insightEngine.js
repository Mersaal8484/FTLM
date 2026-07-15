export function buildDashboardInsightModel({ stats, trends, comparisons, lang }) {
  const insights = [];
  if (stats) {
    if (stats.totalFuel != null) {
      insights.push({ title: lang === 'ar' ? `إجمالي الوقود: ${stats.totalFuel}` : `Total Fuel: ${stats.totalFuel}` });
    }
    if (stats.totalEnergy != null) {
      insights.push({ title: lang === 'ar' ? `إجمالي الطاقة: ${stats.totalEnergy}` : `Total Energy: ${stats.totalEnergy}` });
    }
  }
  return { insights };
}
