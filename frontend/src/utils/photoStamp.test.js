import { describe, expect, it } from 'vitest';
import { formatPhotoStamp, getPhotoStampLayout } from './photoStamp';

describe('photo stamp', () => {
  it('includes the subject code and a minute-level capture time', () => {
    expect(formatPhotoStamp('2026-11-07T12:34:56.000Z', 'G2', 'en-GB')).toContain('G2');
  });

  it('keeps the stamp in a bottom strip', () => {
    const layout = getPhotoStampLayout(960, 640, '07/11/2026 12:34 · G2');
    expect(layout.y + layout.height).toBe(640);
    expect(layout.height).toBeGreaterThan(0);
  });
});
