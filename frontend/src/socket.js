import { HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import client from './api/client';
import useAuthStore from './store/authStore';

const hubUrl = import.meta.env.VITE_HUB_URL || '/hubs/fueltrack';

let signalRToken = null;
let retryTimer = null;
let generation = 0;
let starting = false;

const connection = new HubConnectionBuilder()
  .withUrl(hubUrl, {
    accessTokenFactory: () => signalRToken || '',
  })
  .withAutomaticReconnect({
    nextRetryDelayInMilliseconds: (retryContext) => {
      if (retryContext.elapsedMilliseconds > 30000) return null;
      return Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 10000);
    },
  })
  .configureLogging(LogLevel.Warning)
  .build();

connection.onclose(() => console.warn('[SignalR] Connection closed'));
connection.onreconnecting(() => console.warn('[SignalR] Reconnecting...'));
connection.onreconnected(() => console.warn('[SignalR] Reconnected'));

async function fetchTokenAndStart(expectedGeneration) {
  if (!useAuthStore.getState().isAuthenticated || starting || connection.state !== HubConnectionState.Disconnected) return;
  starting = true;
  try {
    const res = await client.post('/api/auth/signalr-token');
    if (expectedGeneration !== generation || !useAuthStore.getState().isAuthenticated) return;
    signalRToken = res.data?.token;
    if (!signalRToken) {
      throw new Error('No SignalR token received');
    }
    await connection.start();
    if (expectedGeneration !== generation || !useAuthStore.getState().isAuthenticated) {
      await connection.stop();
      return;
    }
    console.info('[SignalR] Connected to', hubUrl); // eslint-disable-line no-console
  } catch (err) {
    console.error('[SignalR] Connection failed:', err.message);
    if (expectedGeneration === generation && useAuthStore.getState().isAuthenticated && err.response?.status !== 401) {
      retryTimer = setTimeout(() => fetchTokenAndStart(expectedGeneration), 5000);
    }
  } finally {
    starting = false;
    if (expectedGeneration !== generation && useAuthStore.getState().isAuthenticated) {
      fetchTokenAndStart(generation);
    }
  }
}

if (import.meta.env.MODE !== 'test' && import.meta.env.VITE_LOCAL_DEMO !== 'true') {
  useAuthStore.subscribe((state, previous) => {
    if (state.isAuthenticated === previous.isAuthenticated) return;
    generation += 1;
    clearTimeout(retryTimer);
    if (state.isAuthenticated) {
      fetchTokenAndStart(generation);
    } else {
      signalRToken = null;
      connection.stop().finally(() => {
        if (useAuthStore.getState().isAuthenticated) fetchTokenAndStart(generation);
      });
    }
  });
  if (useAuthStore.getState().isAuthenticated) fetchTokenAndStart(generation);
}

export default connection;
