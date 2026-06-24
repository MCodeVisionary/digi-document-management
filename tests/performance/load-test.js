// k6 load test for digi-document-management.
// Run: k6 run --vus 50 --duration 60s tests/performance/load-test.js
import http from 'k6/http';
import { check, sleep } from 'k6';

const BASE = __ENV.PERF_BASE_URL || 'http://localhost:8080';

export const options = {
  vus: Number(__ENV.VUS || 50),
  duration: __ENV.DURATION || '60s',
  thresholds: {
    http_req_duration: ['p(95)<300'],
    http_req_failed: ['rate<0.01'],
  },
};

export default function () {
  const res = http.get(`${BASE}/api/documents?owner=perf@docportal.io`);
  check(res, { 'status is 200': (r) => r.status === 200 });
  sleep(1);
}
