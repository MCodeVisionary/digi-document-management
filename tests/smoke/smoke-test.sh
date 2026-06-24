#!/usr/bin/env bash
# Smoke test for digi-document-management. Usage: SMOKE_BASE_URL=http://host:8080 ./smoke-test.sh
set -euo pipefail
BASE="${SMOKE_BASE_URL:-http://localhost:8080}"
echo "Smoke testing $BASE"

code=$(curl -s -o /dev/null -w '%{http_code}' "$BASE/health")
[ "$code" = "200" ] || { echo "health check failed ($code)"; exit 1; }

code=$(curl -s -o /dev/null -w '%{http_code}' "$BASE/api/documents")
[ "$code" = "400" ] || { echo "expected 400 for missing owner, got $code"; exit 1; }

echo "smoke OK"
