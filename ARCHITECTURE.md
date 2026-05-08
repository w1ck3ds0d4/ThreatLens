# Architecture

ThreatLens is a multi-service .NET 10 / Aspire application for log
ingestion, regex correlation, and querying. The stack is split across
four services that share one Postgres database and a Redis instance,
all wired up by an Aspire AppHost.

## Tech stack

- .NET 10 SDK pinned in `global.json` (`10.0.100`, `latestFeature` roll forward).
- .NET Aspire 9.x for local orchestration, service discovery, and OTel.
- ASP.NET Core minimal APIs for the Ingest and Query services.
- `BackgroundService` worker for the correlator.
- Blazor Server (interactive components) for the Dashboard.
- Entity Framework Core 9 with Npgsql for persistence (code-first).
- Postgres (Aspire-managed container) for storage.
- Redis (Aspire-managed container), wired up but not yet used for
  pub/sub (see `ROADMAP.md`).
- xUnit test project under `tests/ThreatLens.Tests/`.

## Solution layout

The solution `ThreatLens.sln` contains nine projects, grouped as
"src" libraries/services and "tests".

- `src/ThreatLens.AppHost/AppHost.cs`: Aspire orchestrator. Declares
  Postgres (with pgAdmin and a data volume), Redis (with RedisInsight),
  and the four service projects. Adds `WithReference` and `WaitFor`
  edges so dependents start in order.
- `src/ThreatLens.ServiceDefaults/Extensions.cs`: shared
  `AddServiceDefaults` that turns on OpenTelemetry (metrics, traces,
  logs), HTTP resilience, service discovery, and `/health` plus
  `/alive` health endpoints (development only).
- `src/ThreatLens.ServiceDefaults/ApiKeyEndpointFilter.cs`: minimal-API
  endpoint filter that requires a `Bearer` token resolvable to a row
  in the `api_keys` table.
- `src/ThreatLens.Domain/`: POCO entities. `LogEvent.cs` (with the
  `Severity` enum), `ApiKey.cs`, `ServiceCredential.cs`, `User.cs`.
- `src/ThreatLens.Data/ThreatLensDbContext.cs`: EF Core context with
  `DbSet`s for log events, correlation rules, API keys, service
  credentials, and users. Indexes on timestamp, severity, and unique
  name/key fields.
- `src/ThreatLens.Data/Migrations/`: three EF migrations
  (`InitialSchema`, `ServiceCredentials`, `Users`).
- `src/ThreatLens.Data/ApiKeyAuth.cs`: SHA-256 hashing, fixed-time
  comparison, key generation, and `EnsureServiceCredentialAsync` for
  service-to-service credentials.
- `src/ThreatLens.Data/PasswordHasher.cs`: PBKDF2-SHA256 with 200k
  iterations and a self-describing `pbkdf2$iters$salt$hash` format.

## Services

1. **Ingest API** (`src/ThreatLens.Ingest.Api/Program.cs`). Two
   endpoints: `POST /events` and `POST /events/batch`. Both require
   the API key filter. On startup, `IngestAuth.InitializeAsync` runs
   migrations and seeds an initial bootstrap key (logged once).
2. **Correlator Worker**
   (`src/ThreatLens.Correlator.Worker/Worker.cs`). Hosted background
   service. Every 5 seconds it loads enabled rules, pulls up to 200
   uncorrelated events ordered by id, and runs each event's `Message`
   through each rule's regex with a 1-second per-match timeout.
   Matches set `MatchedRule` and elevate severity if the rule's
   `ElevateTo` is higher than the current value. Invalid patterns and
   timeouts are logged and skipped.
3. **Query API** (`src/ThreatLens.Query.Api/Program.cs`). `GET /events`
   (paginated by `afterId` with `limit` clamped to 1..500, optional
   `minSeverity` and `source` filters), `GET /events/{id}`, and
   `GET /stats` (last-24h severity histogram plus total and
   uncorrelated counts). All endpoints require the API key filter.
4. **Dashboard** (`src/ThreatLens.Dashboard/Program.cs`). Blazor
   Server. Cookie auth (`DashboardAuth.cs`) with PBKDF2-hashed
   passwords; bootstraps an `admin@threatlens.local` account on first
   run, password from `THREATLENS_ADMIN_PASSWORD` or generated and
   logged once. Calls the Query API over an internal HTTP client that
   attaches a service-credential bearer key via `BearerKeyHandler`
   (`QueryApiCredential.cs`).

## Data flow

1. A producer POSTs JSON to the Ingest API with a valid bearer key.
2. Ingest writes a `LogEvent` row with `Correlated = false`.
3. The Correlator polls Postgres, evaluates regex rules in-process,
   updates `MatchedRule`, `Severity`, `Correlated`, and
   `CorrelatedAt`, then saves.
4. The Dashboard (or any external caller with a key) queries through
   the Query API over HTTP, which returns paginated events or stats.

Redis is configured by the AppHost and injected into Ingest,
Correlator, and Dashboard, but no service publishes or subscribes
yet; see `ROADMAP.md`.

## Observability

`AddServiceDefaults` enables OpenTelemetry instrumentation for
ASP.NET Core, HttpClient, and runtime metrics. When
`OTEL_EXPORTER_OTLP_ENDPOINT` is set (Aspire injects it), traces,
metrics, and logs are exported to the Aspire dashboard. Health checks
are mapped at `/health` and `/alive` in development only, per the
note in `Extensions.cs`.
