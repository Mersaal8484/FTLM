import { describe, expect, it } from 'vitest';
import fs from 'node:fs';
import path from 'node:path';
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

  it('defines every literal translation key used by frontend source in both languages', () => {
    const sourceRoot = path.resolve(process.cwd(), 'src');
    const files = [];
    const visit = directory => fs.readdirSync(directory, { withFileTypes: true }).forEach(entry => {
      const fullPath = path.join(directory, entry.name);
      if (entry.isDirectory()) visit(fullPath);
      else if (/\.(js|jsx)$/.test(entry.name) && !/\.test\./.test(entry.name)) files.push(fullPath);
    });
    visit(sourceRoot);

    const literalPattern = /\bt\(\s*['"]([^'"]+)['"]/g;
    const templatePattern = /\bt\(\s*`([^`]*\$\{[^`]+)`/g;
    const allowedDynamicPrefixes = [
      'operational.actions.', 'operational.log.', 'movements.status.', 'movements.log.kind.',
      'maintenanceService.', 'notifications.',
    ];
    const literalKeys = new Set();
    const unapprovedDynamicKeys = [];

    files.forEach(file => {
      const source = fs.readFileSync(file, 'utf8');
      for (const match of source.matchAll(literalPattern)) literalKeys.add(match[1]);
      for (const match of source.matchAll(templatePattern)) {
        const prefix = match[1].split('${')[0];
        if (!allowedDynamicPrefixes.some(allowed => prefix.startsWith(allowed))) unapprovedDynamicKeys.push(`${path.relative(sourceRoot, file)}: ${match[1]}`);
      }
    });

    expect(unapprovedDynamicKeys, 'Dynamic t() keys must use an explicit allowed prefix').toEqual([]);
    const missing = [...literalKeys].flatMap(key => ['en', 'ar']
      .filter(language => !leafKeys(appTranslations[language].translation).includes(key))
      .map(language => `${language}:${key}`));
    expect(missing, 'Missing literal i18n keys').toEqual([]);
  });
});
