import { describe, expect, test } from 'vitest';
import { groupSitesByStation } from './stationSites';

describe('groupSitesByStation', () => {
  test('groups accessible active legacy records without changing them', () => {
    const sites = [
      { id: 9, stationId: 2, stationName: 'عدن', name: 'ب' },
      { id: 3, stationId: 2, stationName: 'عدن', name: 'أ' },
      { id: 7, stationId: 4, stationName: 'صنعاء' },
      { id: 8, stationId: 4, stationName: 'صنعاء', isActive: false },
    ];
    const groups = groupSitesByStation(sites);
    expect(groups.map(group => [group.id, group.sites.map(site => site.id)])).toEqual([[2, [3, 9]], [4, [7]]]);
    expect(sites.map(site => site.id)).toEqual([9, 3, 7, 8]);
  });
});
