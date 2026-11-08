import { describe, expect, it } from 'vitest';
import { appTranslations } from './i18n';

function leafKeys(value, prefix = '') {
  return Object.entries(value).flatMap(([key, child]) => {
    const path = prefix ? `${prefix}.${key}` : key;
    return child && typeof child === 'object' ? leafKeys(child, path) : [path];
  });
}

describe('translation parity', () => {
  it('keeps Arabic and English translation keys aligned', () => {
    expect(leafKeys(appTranslations.ar.translation).sort())
      .toEqual(leafKeys(appTranslations.en.translation).sort());
  });
});
