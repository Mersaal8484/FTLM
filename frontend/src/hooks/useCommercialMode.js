import { useEffect, useState } from 'react';
import client from '../api/client';

let cachedMode;
let inFlightRequest;

function loadMode() {
  if (cachedMode !== undefined) return Promise.resolve(cachedMode);
  if (!inFlightRequest) {
    inFlightRequest = client.get('/api/commercial-station-mode')
      .then((response) => Boolean(response.data?.isCommercialMode))
      .then((value) => {
        cachedMode = value;
        return value;
      })
      .finally(() => { inFlightRequest = undefined; });
  }
  return inFlightRequest;
}

export default function useCommercialMode() {
  const [state, setState] = useState(() => ({
    isCommercialMode: cachedMode === true,
    modeResolved: cachedMode !== undefined,
  }));

  useEffect(() => {
    let active = true;
    loadMode()
      .then((isCommercialMode) => active && setState({ isCommercialMode, modeResolved: true }))
      .catch(() => active && setState({ isCommercialMode: false, modeResolved: true }));
    return () => { active = false; };
  }, []);

  return state;
}

export function resetCommercialModeCache() {
  cachedMode = undefined;
  inFlightRequest = undefined;
}
