# digi-document-management

Backend document service for the **DocPortal** project. Stores document
binaries on local machine storage and metadata in a **SQLite** database
(users, documents, storage location, upload timestamp, audit trail). Part of
the **DocPortal** AppTrust application.

## Stack

| Concern   | Technology |
|-----------|------------|
| API       | ASP.NET Core 8 (C#) |
| DB        | SQLite via EF Core |
| Storage   | Local filesystem (`/data/documents`, PVC-backed in k8s) |
| Container | Docker |
| Deploy    | Helm (PVC for durable storage) |

## API

| Method | Path | Purpose |
|--------|------|---------|
| GET    | `/health` | liveness / readiness |
| GET    | `/api/documents?owner=` | list a client's documents |
| GET    | `/api/documents/{id}` | document metadata |
| POST   | `/api/documents` | upload (multipart: owner, docType, filename, file) |

## Layout

```
src/DigiDocumentManagement/        ASP.NET Core host + EF models
tests/DigiDocumentManagement.UnitTests/
tests/DigiDocumentManagement.FunctionalTests/
helm/digi-document-management/     Helm chart
Dockerfile
.github/workflows/ci.yml           CI/CD pipeline
```

## Local dev

```bash
dotnet run --project src/DigiDocumentManagement
# GET http://localhost:5000/health
```

## Tests

| Type        | Command |
|-------------|---------|
| Unit        | `dotnet test tests/DigiDocumentManagement.UnitTests` (xUnit + EF InMemory) |
| Functional  | `dotnet test tests/DigiDocumentManagement.FunctionalTests` (WebApplicationFactory) |
| Performance | `k6 run tests/performance/load-test.js` |

---

## CI/CD Pipeline & Evidence

On every push to `main`, the pipeline builds both artifacts, attaches
**SLSA v1.0 provenance** and **test-result evidence** to each, promotes through
four lifecycle stages, and registers a **DocPortal** AppTrust version.

### Artifacts published to JFrog

| Artifact | Repository |
|----------|-----------|
| Docker image (`digi-document-management`) | `docportal-doc-management-docker-dev-local` |
| Helm chart (`digi-document-management`) | `docportal-doc-management-helm-dev-local` |

### Evidence flow

#### 1. SLSA provenance (all artifacts, at publish time)

SLSA v1.0 provenance is attached to every artifact immediately after publish.
Each record captures the GitHub Actions run ID, commit SHA, and ref.

```bash
# Docker image
jf evd create \
  --package-name "digi-document-management" --package-version "$VERSION" \
  --package-repo-name "docportal-doc-management-docker-dev-local" \
  --predicate provenance-docker.json \
  --predicate-type "https://slsa.dev/provenance/v1" \
  --provider-id github \
  --key "$EVIDENCE_PRIVATE_KEY" --key-alias "docportal-evidence-key"

# Helm chart
jf evd create \
  --package-name "digi-document-management" --package-version "$VERSION" \
  --package-repo-name "docportal-doc-management-helm-dev-local" \
  --predicate provenance-helm.json \
  --predicate-type "https://slsa.dev/provenance/v1" \
  --provider-id github \
  --key "$EVIDENCE_PRIVATE_KEY" --key-alias "docportal-evidence-key"
```

The provenance JSON uses the SLSA v1.0 schema (`buildDefinition` +
`runDetails`), which renders as the GitHub logo in JFrog's evidence panel.

#### 2. Build-level evidence (SLSA provenance + build signature)

Attached to the JFrog build-info record after `jf rt build-publish`:

```bash
# SLSA provenance on the build
jf evd create \
  --build-name "digi-document-management" --build-number "$GITHUB_RUN_NUMBER" \
  --project "docportal" \
  --predicate provenance.json \
  --predicate-type "https://slsa.dev/provenance/v1" \
  --provider-id github \
  --key "$EVIDENCE_PRIVATE_KEY" --key-alias "docportal-evidence-key"

# Build signature
jf evd create \
  --build-name "digi-document-management" --build-number "$GITHUB_RUN_NUMBER" \
  --project "docportal" \
  --predicate build-sig.json \
  --predicate-type "https://jfrog.com/evidence/build-signature/v1" \
  --key "$EVIDENCE_PRIVATE_KEY" --key-alias "docportal-evidence-key"
```

#### 3. DEV stage — unit tests (xUnit + EF InMemory)

```bash
# Docker and Helm both get unit-test evidence
jf evd create \
  --package-name "digi-document-management" --package-version "$VERSION" \
  --package-repo-name "<repo>" \
  --predicate reports/unit-predicate.json \
  --predicate-type "https://jfrog.com/evidence/testing-results/v1" \
  --key "$EVIDENCE_PRIVATE_KEY" --key-alias "docportal-evidence-key"
```

Predicate fields: `name`, `stage` (`DEV`), `result` (`PASSED`/`FAILED`),
`build`, `suites` (tool: `xunit`, passed, failed, total), `timestamp`.

Release bundle is created and promoted to **DEV**.

#### 4. QA stage — functional tests (xUnit + WebApplicationFactory)

```bash
# Docker and Helm both get functional-test evidence
jf evd create \
  --package-name "digi-document-management" --package-version "$VERSION" \
  --package-repo-name "<repo>" \
  --predicate reports/functional-predicate.json \
  --predicate-type "https://jfrog.com/evidence/integration-results/v1" \
  --key "$EVIDENCE_PRIVATE_KEY" --key-alias "docportal-evidence-key"
```

Release bundle promoted to **QA**.

#### 5. STAGING stage — performance tests (k6)

```bash
# Docker gets performance evidence (with raw k6 JSON attached)
jf evd create \
  --package-name "digi-document-management" --package-version "$VERSION" \
  --package-repo-name "docportal-doc-management-docker-dev-local" \
  --predicate reports/perf-predicate.json \
  --predicate-type "https://jfrog.com/evidence/performance-results/v1" \
  --attach-local reports/performance.json \
  --attach-artifactory-temp-path "docportal-doc-management-generic-dev-local/digi-document-management/$GITHUB_RUN_NUMBER/evidence-temp" \
  --key "$EVIDENCE_PRIVATE_KEY" --key-alias "docportal-evidence-key"
```

Predicate fields: `tool` (k6), `total_requests`, `p95_ms`, `failure_rate`,
`passed` (pass criteria: `failure_rate < 0.01` and `p95_ms < 300`).

Release bundle promoted to **STAGING**.

#### 6. PROD stage

Release bundle promoted to **PROD**.

#### 7. Release bundle evidence

```bash
jf evd create \
  --release-bundle "digi-document-management" --release-bundle-version "$VERSION" \
  --project "docportal" \
  --predicate reports/unit-predicate.json \
  --predicate-type "https://jfrog.com/evidence/testing-results/v1" \
  --key "$EVIDENCE_PRIVATE_KEY" --key-alias "docportal-evidence-key"
```

### Evidence summary per artifact

| Artifact | Evidence records |
|----------|-----------------|
| Docker image | SLSA provenance (GitHub), build SLSA, build signature, unit tests, functional tests, performance |
| Helm chart | SLSA provenance (GitHub), unit tests, functional tests |
| Release bundle | unit test results |

### AppTrust

After PROD promotion, a **DocPortal** AppTrust application version is created
combining all three services. The manifest at
`docportal-client-portal-generic-dev-local/docportal/manifest.json` tracks
each service version. When all three are present, `jf apptrust version-create
docportal <N> --skip-unassigned` creates the version, `version-promote` moves
it through QA and STAGING, and `version-release` releases to PROD.

### Required secrets / variables

| Name | Purpose |
|------|---------|
| `vars.JF_URL` | JFrog platform URL |
| `secrets.EVIDENCE_PRIVATE_KEY` | PEM private key for signing evidence |
| OIDC provider `github-docportal` | Keyless auth to JFrog |
