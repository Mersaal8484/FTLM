import fs from 'node:fs';
import path from 'node:path';
import { describe, expect, it } from 'vitest';

describe('theme token coverage', () => {
  it('defines operational and chart tokens in the light theme', () => {
    const css = fs.readFileSync(path.resolve(process.cwd(), 'src/styles.css'), 'utf8');
    const light = css.match(/\.theme-light\s*\{([\s\S]*?)\n\s*\}/)?.[1] || '';
    ['--operational-grid', '--operational-amber', '--operational-amber-subtle', '--operational-teal', '--chart-grid', '--chart-tooltip-bg']
      .forEach(token => expect(light, token).toContain(token));
  });

  it('keeps the energy balance and maintenance dashboards free of hard-coded color literals', () => {
    ['EnergyBalanceBoard.jsx', 'MaintenanceDashboard.jsx'].forEach(file => {
      const source = fs.readFileSync(path.resolve(process.cwd(), `src/pages/${file}`), 'utf8');
      expect(source, file).not.toMatch(/#[0-9a-f]{3,8}|rgba?\(|hsla?\(/i);
    });
  });
});
