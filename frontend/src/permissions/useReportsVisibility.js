import { useMemo } from 'react';

const DEFAULT_TABS = {
  generators: true,
  oil: true,
  fuel: true,
  station: true,
  'fuel-incoming': true,
  'fuel-transfer': true,
  'fuel-loss': true,
};

export default function useReportsVisibility() {
  return useMemo(() => ({
    tabs: DEFAULT_TABS,
    periodic_generation: true,
    periodic_fuel: true,
    periodic_oil: true,
  }), []);
}
