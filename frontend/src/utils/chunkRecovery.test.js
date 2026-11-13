import { describe, expect, it, vi } from 'vitest';
import { installChunkRecovery, isChunkLoadError } from './chunkRecovery';

describe('chunk recovery', () => {
  it('recognizes browser errors for missing lazy-loaded modules', () => {
    expect(isChunkLoadError(new Error('error loading dynamically imported module: /assets/page.js'))).toBe(true);
    expect(isChunkLoadError(new Error('Failed to fetch dynamically imported module'))).toBe(true);
    expect(isChunkLoadError(new Error('Application error'))).toBe(false);
  });

  it('reloads once on a preload failure and avoids a reload loop', () => {
    let handler;
    let stored = null;
    const target = {
      addEventListener: vi.fn((name, callback) => { handler = callback; }),
      sessionStorage: {
        getItem: () => stored,
        setItem: (_key, value) => { stored = value; },
      },
      location: { reload: vi.fn() },
    };
    installChunkRecovery(target);
    const first = { preventDefault: vi.fn() };
    const second = { preventDefault: vi.fn() };
    handler(first);
    handler(second);
    expect(target.location.reload).toHaveBeenCalledTimes(1);
    expect(first.preventDefault).toHaveBeenCalledOnce();
    expect(second.preventDefault).not.toHaveBeenCalled();
  });
});
