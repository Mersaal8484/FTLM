export function splitServerInsights(serverInsights, lang) {
  if (!serverInsights || !Array.isArray(serverInsights)) {
    return { insights: [], forecasts: [] };
  }
  const insights = serverInsights
    .filter((s) => s.type !== 'forecast')
    .map((s) => ({ title: s.message || s.title, description: s.description }));
  const forecasts = serverInsights
    .filter((s) => s.type === 'forecast')
    .map((s) => ({ title: s.message || s.title, value: s.value }));
  return { insights, forecasts };
}
