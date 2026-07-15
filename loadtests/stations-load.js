import http from 'k6/http';
import { check, sleep } from 'k6';
import { BASE_URL } from './config.js';
import { getToken } from './auth.js';

let token;

export function setup() {
  token = getToken();
}

export const options = {
  stages: [
    { duration: '30s', target: 10 },   // ramp up
    { duration: '1m', target: 10 },     // steady state
    { duration: '30s', target: 20 },    // peak
    { duration: '1m', target: 20 },     // sustained peak
    { duration: '30s', target: 0 },     // ramp down
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'],
    http_req_failed: ['rate<0.1'],
  },
};

export default function () {
  const headers = {
    Authorization: `Bearer ${token}`,
    'Content-Type': 'application/json',
  };

  const stations = http.get(`${BASE_URL}/api/stations`, { headers });
  check(stations, { 'stations 200': (r) => r.status === 200 });

  sleep(1);

  const dashboard = http.get(`${BASE_URL}/api/dashboard/stats`, { headers });
  check(dashboard, { 'dashboard 200': (r) => r.status === 200 });

  sleep(1);
}
