import http from 'k6/http';
import { check } from 'k6';
import { BASE_URL, TEST_USER } from './config.js';

export function getToken() {
  const res = http.post(`${BASE_URL}/api/auth/login`, JSON.stringify(TEST_USER), {
    headers: { 'Content-Type': 'application/json' },
  });
  check(res, { 'login 200': (r) => r.status === 200 });
  return JSON.parse(res.body).token;
}
