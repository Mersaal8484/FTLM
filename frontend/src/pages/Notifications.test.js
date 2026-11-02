import { vi } from 'vitest';

vi.mock('../socket', () => ({
  default: { on: vi.fn(), off: vi.fn() },
}));

import { getNotificationId } from './Notifications';

describe('Notifications helpers', () => {
  test('uses Prisma id and falls back to legacy _id', () => {
    expect(getNotificationId({ id: 'notif-1', _id: 'legacy-1' })).toBe('notif-1');
    expect(getNotificationId({ _id: 'legacy-1' })).toBe('legacy-1');
    expect(getNotificationId(null)).toBeUndefined();
  });
});
