const RELOAD_KEY = 'fueltrack:chunk-reload';
const RELOAD_COOLDOWN_MS = 30_000;

export function isChunkLoadError(error) {
  return /failed to fetch dynamically imported module|error loading dynamically imported module|loading chunk [\w-]+ failed|importing a module script failed/i.test(error?.message || '');
}

// A single reload picks up the current deployment; the cooldown prevents a
// broken connection or deployment from trapping the user in a reload loop.
export function installChunkRecovery(target = window) {
  target.addEventListener('vite:preloadError', event => {
    try {
      const now = Date.now();
      const lastReload = Number(target.sessionStorage.getItem(RELOAD_KEY) || 0);
      if (lastReload && now - lastReload < RELOAD_COOLDOWN_MS) return;
      target.sessionStorage.setItem(RELOAD_KEY, String(now));
      event.preventDefault();
      target.location.reload();
    } catch {
      // If storage is unavailable, let the error boundary display the failure.
    }
  });
}
