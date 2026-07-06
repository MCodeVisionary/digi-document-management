#!/usr/bin/env bash
# SonarQube quality gate evidence for JFrog attach (committed under .jfrog/evidence).
# Usage: run from repo root (e.g. in GitHub Actions).

set -euo pipefail

mkdir -p .jfrog/evidence

for f in sonarqube-coverage.json sonarqube-coverage.md; do
  if [[ ! -f ".jfrog/evidence/$f" ]]; then
    echo "Missing required evidence file: .jfrog/evidence/$f" >&2
    exit 1
  fi
done
