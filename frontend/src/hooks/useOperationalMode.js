import { useEffect, useState } from 'react';
import client from '../api/client';

let cachedMode;
let inFlightRequest;

function loadMode() {
  if (cachedMode !== undefined) return Promise.resolve(cachedMode);
  if (!inFlightRequest) {
    inFlightRequest = client.get('/api/customer-features/operational-data-mode')
      .then((response) => Boolean(response.data?.isNewMode))
      .then((value) => {
        cachedMode = value;
        return value;
      })
      .finally(() => { inFlightRequest = undefined; });
  }
  return inFlightRequest;
}

export default function useOperationalMode() {
  const [state, setState] = useState(() => ({
    isNewMode: cachedMode === true,
    modeResolved: cachedMode !== undefined,
  }));

  useEffect(() => {
    let active = true;
    loadMode()
      .then((isNewMode) => active && setState({ isNewMode, modeResolved: true }))
      .catch(() => active && setState({ isNewMode: false, modeResolved: true }));
    return () => { active = false; };
  }, []);

  return state;
}

export function resetOperationalModeCache() {
  cachedMode = undefined;
  inFlightRequest = undefined;
}
