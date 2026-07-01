# digi-document-management

> Release bundles promoted to DEV lifecycle stage via `jf rbp`.

Backend document service for the **Digitalization** project. Stores document
binaries on **local machine storage** and metadata in a **lightweight SQLite
database** (users, documents, storage location, upload timestamp, who uploaded,
who updated). Maintains an audit trail. Part of the **DocPortal** AppTrust app.

## Stack
| Concern | Technology |
|---------|------------|
| API | ASP.NET Core 8 Web API (C#, **NuGet**) |
| DB | SQLite via EF Core (lightweight, file-based) |
| Storage | Local filesystem (`/data/documents`, PVC-backed in k8s) |
| Container | **Docker** |
| Deploy | **Helm** (PVC for durable local storage) |

## API
| Method | Path | Purpose |
|--------|------|---------|
| GET | `/health` | liveness/readiness |
| GET | `/api/documents?owner=` | list a client's documents |
| GET | `/api/documents/{id}` | document metadata |
| POST | `/api/documents` | upload (multipart: owner, docType, filename, file) |

## Tests
| Type | How |
|------|-----|
| Unit | `dotnet test tests/DigiDocumentManagement.UnitTests` (xUnit + EF InMemory) |
| Functional | `dotnet test tests/DigiDocumentManagement.FunctionalTests` (WebApplicationFactory) |
| Smoke | `SMOKE_BASE_URL=... tests/smoke/smoke-test.sh` |
| Performance | `k6 run tests/performance/load-test.js` |

## CI/CD & evidence
Restores NuGet from `digi-nuget`, builds & tests, pushes the image to
`digi-docker-dev-local` and the chart to `digi-helm-dev-local`, runs SonarQube,
and produces **signed in-toto/DSSE evidence** (test results with mocked smoke +
performance attached, SonarQube static analysis, SLSA provenance, build
signature), then registers a **DocPortal** AppTrust application version.
