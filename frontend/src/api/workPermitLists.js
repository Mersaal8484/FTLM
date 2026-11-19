import client from './client';

// Selectors must include every page, not just the API default of 20 records.
export async function loadAllWorkPermits() {
  const items = [];
  const pageSize = 100;
  for (let page = 1; ; page++) {
    const { data } = await client.get('/api/work-permits', { params: { page, pageSize } });
    if (Array.isArray(data)) return data;
    if (!Array.isArray(data?.items) || !Number.isInteger(data.totalCount)) throw new Error('Unexpected work permit response');
    items.push(...data.items);
    if (items.length >= data.totalCount) return items;
    if (data.items.length === 0) throw new Error('Incomplete work permit response');
  }
}
