import { HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import client from './api/client';

const hubUrl = import.meta.env.VITE_HUB_URL || '/hubs/fueltrack';

let signalRToken = null;

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

async function fetchTokenAndStart() {
  if (connection.state === HubConnectionState.Connected) return;
  try {
    const res = await client.post('/api/auth/signalr-token');
    signalRToken = res.data?.token;
    if (!signalRToken) {
      console.warn('[SignalR] No token received, retrying...');
      setTimeout(fetchTokenAndStart, 5000);
      return;
    }
    await connection.start();
    console.info('[SignalR] Connected to', hubUrl); // eslint-disable-line no-console
  } catch (err) {
    console.error('[SignalR] Connection failed:', err.message);
    setTimeout(fetchTokenAndStart, 5000);
  }
}

fetchTokenAndStart();

export default connection;
