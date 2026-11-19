// Keep legacy site IDs internal while presenting one option per station.
export function groupSitesByStation(sites) {
  const groups = new Map();
  for (const site of sites || []) {
    if (!site?.stationId || !site?.id || site.isActive === false) continue;
    const key = String(site.stationId);
    if (!groups.has(key)) groups.set(key, { id: site.stationId, name: site.stationName || '', sites: [] });
    groups.get(key).sites.push(site);
  }
  return Array.from(groups.values()).map(group => ({
    ...group,
    sites: group.sites.sort((a, b) => Number(a.id) - Number(b.id)),
  }));
}
