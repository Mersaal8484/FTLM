import '@testing-library/jest-dom';
// Node 25+ exposes its own optional localStorage, which can shadow jsdom's.
const storage = window.localStorage ?? (() => {
  const data = new Map();
  return {
    get length() { return data.size; },
    key: index => Array.from(data.keys())[index] ?? null,
    getItem: key => data.get(String(key)) ?? null,
    setItem: (key, value) => data.set(String(key), String(value)),
    removeItem: key => data.delete(String(key)),
    clear: () => data.clear(),
  };
})();
Object.defineProperty(globalThis, 'localStorage', { value: storage, configurable: true });
await import('./i18n');
