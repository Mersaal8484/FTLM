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
    { duration: '30s', target: 5 },
    { duration: '2m', target: 5 },
    { duration: '30s', target: 0 },
  ],
  thresholds: {
    http_req_duration: ['p(95)<1000'],
  },
};

export default function () {
  const headers = {
    Authorization: `Bearer ${token}`,
  };

  const res = http.get(`${BASE_URL}/api/dashboard/stats`, { headers });
  check(res, { 'dashboard 200': (r) => r.status === 200 });

  sleep(2);
}
